#light
module Visualizer.Operations

open System
open System.IO
open System.Reflection
open System.Diagnostics
open System.Configuration
open System.Windows.Forms
open Microsoft.FSharp
open Microsoft.FSharp.Quotations
    
// ----------------------------------------------------------------------------

// Apply beta-reduction to the given expression
let ReduceExpression(e) = 
  
  // Matches either let bindings or lambda function applications 
  let (|BetaReducible|_|) e = 
    match e with
    | Patterns.Let(v, arg, body) 
    | Patterns.Application(Patterns.Lambda(v, body), arg) ->
        Some(v, arg, body) 
    | _ -> None
  
  // Reduce the expression recursively
  let rec reduce mp e =
    let res = 
      match e with
      | BetaReducible(v, arg, body) -> reduce (Map.add v.Name arg mp) body
      | ExprShape.ShapeLambda(v, e) -> Expr.Lambda(v, reduce mp e)
      | ExprShape.ShapeVar(v) when mp.ContainsKey(v.Name) -> mp.[v.Name]
      | ExprShape.ShapeVar(v) -> Expr.Var(v)
      | ExprShape.ShapeCombination(c, es) ->
          ExprShape.RebuildShapeCombination(c, List.map (reduce mp) es)  
          
    // After we've processed the expression, let's try it again
    // so that we match the case when we replace variable with some expression
    match res with
    | BetaReducible(v, arg, body) -> reduce (Map.add v.Name arg mp) body
    | _ -> res
      
  reduce Map.empty e
  
// ----------------------------------------------------------------------------

let CompileExpression(x:string) =

  // Generate source file
  let fsName = Path.GetTempFileName() in
  let fw = new StreamWriter(fsName + ".fs") in 
  fw.Write
   ("module RuntimeQuot\r\n" +
    "open System\r\n" +
    "open System.Windows.Forms\r\n" +
    "open Microsoft.FSharp\r\n" +
    "open Microsoft.FSharp.Quotations\r\n")
  fw.Write(x)
  fw.Close()
  
  // Invoke the F# compiler...
  let psi = new ProcessStartInfo() in 
  psi.FileName <- ConfigurationSettings.AppSettings.Item("fsc_path")
  psi.Arguments <- " -a -o \"" + fsName + ".dll\" \"" + fsName + ".fs\""
  psi.CreateNoWindow <- true
  psi.UseShellExecute <- false
  psi.RedirectStandardError <- true
  let p = Process.Start(psi) in
  let result = p.StandardError.ReadToEnd() in
  p.WaitForExit()
  File.Delete (fsName)
  File.Delete (fsName + ".fs")
  
  if ((result.Trim()).Length > 0) then 
    // Compilation failed - report the results
    ignore(MessageBox.Show (result,"Compilation error"))
    None
  else 
    // Get value of field with name 'q' using reflection..
    let a = Assembly.LoadFile(fsName + ".dll") in
    let t = a.GetType("RuntimeQuot") in
    let prop = t.GetProperty("q") in
    let o = prop.GetValue(null, [] |> List.toArray) in  
    Some(o :?> Expr)

// ----------------------------------------------------------------------------

/// Read the whole content of a stream
let readToEnd (s : Stream) = 
  let n = int s.Length in 
  let res = Array.zeroCreate n in 
  let i = ref 0 in 
  while (!i < n) do 
    i := !i + s.Read(res,!i,(n - !i)) 
  done
  res

// ----------------------------------------------------------------------------

/// Resolves F# quotations of all top level definitions 
let ResolveAssemblyDefinitions (name:string) (classFun : Type -> 'T) 
                               (defFun : ('T -> MemberInfo -> Expr -> unit)) =

  // Load the specified assembly and register reflected definitions  
  let asm = Assembly.LoadFile(name) in
  for rn in asm.GetManifestResourceNames() do
    if rn.StartsWith("ReflectedDefinitions") then
      Expr.RegisterReflectedDefinitions
        (asm, rn, readToEnd(asm.GetManifestResourceStream(rn)))
  
  // Get all top-level definitions
  let topDefs = 
    [ for t in asm.GetTypes() do
        // Get a list of all methods
        let members : list<MethodBase> = 
          [ yield! t.GetMethods() |> Seq.cast
            yield! [ for p in t.GetProperties() do
                       if p.CanRead then yield p.GetGetMethod(true)
                       if p.CanWrite then yield p.GetSetMethod(true) ] |> Seq.cast
            yield! t.GetConstructors() |> Seq.cast ]
        
        // Find definitions of the methods in the assembly    
        let defns = 
          [ for mi in members do
              match Expr.TryGetReflectedDefinition(mi) with
              | Some(d) -> yield (mi, d)
              | _ -> () ]
        if defns <> [] then 
          yield (t, defns) ]
  
  // Call the provided functions for all definitions
  for (t, dList) in topDefs do
    let r = classFun t in
    for (mi, d) in dList do
      defFun r mi d
