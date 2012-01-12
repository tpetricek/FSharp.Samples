#light
module Visualizer.Visualizer

open System
open System.Windows.Forms
open Microsoft.FSharp
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns

// ----------------------------------------------------------------------------

let buildExprTreeOpt (x:Expr) (tree:TreeNode) (useApps:bool) =
    
  // Function used in pattern matching to force 
  // matching with efApp instead of efApps
  let (|SimpleFailApps|_|) n =
    if (not useApps) then None else 
      match n with | Applications(v,l) when l <> [] -> Some(v,l) | _ -> None       
  let (|SimpleFailLambdas|_|) n =
    if (not useApps) then None else 
      match n with | Lambdas(l,v) when l <> [] -> Some(l,v) | _ -> None 
  
  
  // Recursive function that builds the tree
  let rec buildExprTree x (tree:TreeNode) =
    match x with     
      // ----------------------------------------------------------------------
      // Various basic constructs
      
      // Lifted value - instance of .NET object
      | Value(o, ty) ->                      
          let s = if (o = null) then "" else ((o.GetType()).Name)
          let n = new TreeNode("(Value) " + s, 21, 21) 
          ignore(tree.Nodes.Add(n))
          
      // Inner quotations <@@ ... @@>
      | Quote(quoted) ->                       
          let n = new TreeNode("(Quote)", 20, 20) 
          buildExprTree quoted n;
          ignore(tree.Nodes.Add(n))
          
      // Make tuple          
      | NewTuple(vals) ->                 
          let n = new TreeNode("(NewTuple)", 16, 16) 
          List.iter (fun arg ->
            buildExprTree arg n) vals;
          ignore(tree.Nodes.Add(n))
      
      | Coerce(e, ty) ->
          let n = new TreeNode("(Coerce) " + ty.Name, 5, 5) 
          buildExprTree e n
          ignore(tree.Nodes.Add(n))
            
      // let rec .. in ...
      | LetRecursive(exprs, body) ->                  
          let n = new TreeNode("(LetRec)", 9, 9) 
          exprs |> List.iter (fun (name,b) -> 
            let n1 = new TreeNode("(value) "+name.Name, 11, 11) 
            buildExprTree b n1
            ignore(n.Nodes.Add(n1)) ) 
          let n2 = new TreeNode("(in)", 10, 10) 
          buildExprTree body n2
          ignore(n.Nodes.Add(n2))
          ignore(tree.Nodes.Add(n))
          
      // let .. in ...
      | Let(var, a, b) ->   
          let n = new TreeNode("(Let)", 9, 9) 
          let n1 = new TreeNode("(value) "+var.Name, 11, 11) 
          buildExprTree a n1
          let n2 = new TreeNode("(in)", 10, 10) 
          buildExprTree b n2
          ignore(n.Nodes.Add(n1))
          ignore(n.Nodes.Add(n2))
          ignore(tree.Nodes.Add(n))

      // variable
      | Var(a) ->                              
          let n = new TreeNode("(variable) " + a.Name, 7, 7) 
          ignore(tree.Nodes.Add(n))

      // ----------------------------------------------------------------------
      // Lambda functions & applications
      
      // Lambda expression - match only when user wants "app" instead of "apps"
      | SimpleFailLambdas (a,b) ->                
          let n = new TreeNode("(Lambdas)", 15, 15) 
          let n1 = new TreeNode("(parameters)",4,4) 
          a |> List.iter (fun var -> 
            let parameters = String.concat ", " (seq { for v in var -> v.Name})
            ignore(n1.Nodes.Add(new TreeNode("(parameters) " + parameters, 4, 4))) )
          let n2 = new TreeNode("(operation)",3,3) 
          buildExprTree b n2;
          ignore(n.Nodes.Add(n1))
          ignore(n.Nodes.Add(n2))
          ignore(tree.Nodes.Add(n))
          
      // lambda expression
      | Lambda(a,b) ->                         
          let n = new TreeNode("(Lambda) " + a.Name, 15, 15) 
          buildExprTree b n;
          ignore(tree.Nodes.Add(n))
          
      // Function application - match only when user wants "app" instead of "apps"
      | SimpleFailApps (a,b) ->                   
          let n = new TreeNode("(Applications)",2,2)
          let n1 = new TreeNode("(operation)",3,3) 
          buildExprTree a n1;
          ignore(n.Nodes.Add(n1))
          let n2 = new TreeNode("(parameters)",4,4) 
          for args in b do
            for arg in args do
              buildExprTree arg n2
          n.Nodes.Add(n2) |> ignore
          tree.Nodes.Add(n) |> ignore
          
      // App
      | Application(a,b) -> 
          let n = new TreeNode("(Application)",1,1)
          let n1 = new TreeNode("(operation)",3,3) 
          buildExprTree a n1;
          ignore(n.Nodes.Add(n1))
          let n2 = new TreeNode("(parameter)",4,4) 
          buildExprTree b n2;
          ignore(n.Nodes.Add(n2))
          ignore(tree.Nodes.Add(n))
                  
      // ----------------------------------------------------------------------
      // Constructs that would be statments in C#
      
      // Statement: for i=start to end do body; done;
      | ForIntegerRangeLoop(_, nfrom,nto,body) ->             
          let n = new TreeNode("(ForIntegerRangeLoop)",19,19) 
          let nf = new TreeNode("(from)",4,4) 
          let nt = new TreeNode("(to)",4,4) 
          let nb = new TreeNode("(body)",3,3) 
          buildExprTree nfrom nf;
          buildExprTree nto nt;
          buildExprTree body nb;
          ignore(n.Nodes.Add(nf))
          ignore(n.Nodes.Add(nt))
          ignore(n.Nodes.Add(nb))
          ignore(tree.Nodes.Add(n))
          
      // Statement: while condition do body; done;          
      | WhileLoop(cond,body) ->                
          let n = new TreeNode("(WhileLoop)",19,19) 
          let nc = new TreeNode("(cond)",4,4) 
          let nb = new TreeNode("(body)",3,3) 
          buildExprTree cond nc;
          buildExprTree body nb;
          ignore(n.Nodes.Add(nc))
          ignore(n.Nodes.Add(nb))
          ignore(tree.Nodes.Add(n))
          
      // if .. then .. else .. expression
      | IfThenElse(cond,tr,fl) ->                
          let n = new TreeNode("(IfThenElse)",19,19) 
          let ni = new TreeNode("(if)",4,4) 
          let nt = new TreeNode("(then)",3,3) 
          let nf = new TreeNode("(else)",3,3) 
          buildExprTree cond ni;
          buildExprTree tr nt;
          buildExprTree fl nf;
          ignore(n.Nodes.Add(ni))
          ignore(n.Nodes.Add(nt))
          ignore(n.Nodes.Add(nf))
          ignore(tree.Nodes.Add(n))
          
      // sequence of expressions
      | Sequential(a,b) ->                            
          let n = new TreeNode("(Sequential)", 14, 14) 
          ignore(tree.Nodes.Add(n))
          buildExprTree a n
          buildExprTree b n
                
      // ----------------------------------------------------------------------
      // Operators
      
      // and operator
      | AndAlso(lv,rv) ->
          let n = new TreeNode("(AndAlso)",18,18) 
          let nl = new TreeNode("(left)",4,4) 
          let nr = new TreeNode("(right)",4,4) 
          buildExprTree lv nl
          buildExprTree rv nr
          ignore(n.Nodes.Add(nl))
          ignore(n.Nodes.Add(nr))
          ignore(tree.Nodes.Add(n))
          
      // or operator
      | OrElse(lv,rv) ->                           
          let n = new TreeNode("(OrElse)",18,18) 
          let nl = new TreeNode("(left)",4,4) 
          let nr = new TreeNode("(right)",4,4) 
          buildExprTree lv nl
          buildExprTree rv nr
          ignore(n.Nodes.Add(nl))
          ignore(n.Nodes.Add(nr))
          ignore(tree.Nodes.Add(n))
        
      // ----------------------------------------------------------------------
      // .NET method/property calls
      
      // .NET method call    
      | Call(target,meth,args) ->           
          let n = new TreeNode("(Call) " + meth.Name, 12, 12) 
          target |> Option.iter (fun e -> buildExprTree e n)
          tree.Nodes.Add(n) |> ignore
          for arg in args do
            buildExprTree arg n
            
      // .NET property-get call
      | PropertyGet(target, prop, args) ->           
          let n = new TreeNode("(PropGet) " + prop.Name, 12, 12) 
          target |> Option.iter (fun e -> buildExprTree e n)
          ignore(tree.Nodes.Add(n))
          for arg in args do
            buildExprTree arg n

      | PropertySet(target, prop, args, value) ->
          let n = new TreeNode("(PropSet) " + prop.Name + " <- <value>", 12, 12) 
          if args<>[] then
            let m = new TreeNode("(arguments)", 12, 12) 
            ignore(tree.Nodes.Add(m))
            for arg in args do
              buildExprTree arg m
          target |> Option.iter (fun e -> buildExprTree e n)
          ignore(tree.Nodes.Add(n))
      
      // .NET construtor invocation
      | NewObject(ct,args) ->              
          let n = new TreeNode("(NewObject) " + ct.DeclaringType.Name, 13, 13) 
          ignore(tree.Nodes.Add(n))
          List.iter (fun arg ->
            buildExprTree arg n) args

      // ----------------------------------------------------------------------
      // Primitive data types & values
      
      | Double(x) ->                             // double value
          let n = new TreeNode(sprintf "(Double) %A" x, 6, 6) 
          ignore(tree.Nodes.Add(n))
      | Single(x) ->                             // single value
          let n = new TreeNode(sprintf "(Single) %A" x, 6, 6) 
          ignore(tree.Nodes.Add(n))
      | Bool(x) ->                              // bool value
          let n = new TreeNode(sprintf "(Bool) %A" x, 6, 6) 
          ignore(tree.Nodes.Add(n))
      | Byte(x) ->                             // byte value
          let n = new TreeNode(sprintf "(Byte) %A" x, 6, 6) 
          ignore(tree.Nodes.Add(n))
      | SByte(x) ->                             // signed byte value
          let n = new TreeNode(sprintf "(SByte) %A" x, 6, 6) 
          ignore(tree.Nodes.Add(n))
      | Char(x) ->                             // char value
          let n = new TreeNode(sprintf "(Char) %A" x, 6, 6) 
          ignore(tree.Nodes.Add(n))
      | Int16(x) ->                            // int16 value
          let n = new TreeNode(sprintf "(Int16) %A" x, 6, 6) 
          ignore(tree.Nodes.Add(n))
      | Int32(x) ->                            // int32 value
          let n = new TreeNode(sprintf "(Int32) %A" x, 6, 6) 
          ignore(tree.Nodes.Add(n))
      | UInt16 (x) ->                          // uint16 value
          let n = new TreeNode(sprintf "(UInt16) %A" x, 6, 6) 
          ignore(tree.Nodes.Add(n))
      | UInt32 (x) ->                          // uint32 value
          let n = new TreeNode(sprintf "(UInt32) %A" x, 6, 6) 
          ignore(tree.Nodes.Add(n))
      | Int64(x) ->                            // int64 value
          let n = new TreeNode(sprintf "(Int64) %A" x, 6, 6) 
          ignore(tree.Nodes.Add(n))
      | UInt64 (x) ->                          // uint64 value
          let n = new TreeNode(sprintf "(UInt64) %A" x, 6, 6) 
          ignore(tree.Nodes.Add(n))
      | String(x) ->                           // string value
          let n = new TreeNode(sprintf "(String) %A" x, 6, 6) 
          ignore(tree.Nodes.Add(n))
      | Unit() ->                           // unit value
          let n = new TreeNode("(Unit)", 6, 6) 
          ignore(tree.Nodes.Add(n))
          
      // ----------------------------------------------------------------------
      // Someting unknown?
      
      | _ ->                                     
          let n = new TreeNode("Unknown?", 17, 17) 
          n.ToolTipText <- sprintf "%A" x
          ignore(tree.Nodes.Add(n))
          
  // call buildExprTree
  buildExprTree x tree

// ----------------------------------------------------------------------------

// Builds root node and calls buildExprTreeOpt
let GetExpressionTree(e, useApps) = 
  let rootNode = new TreeNode("(Root)")
  buildExprTreeOpt e rootNode useApps;
  rootNode
  