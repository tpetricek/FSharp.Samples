#light

open System
open System.Windows.Forms
open Microsoft.FSharp
open Microsoft.FSharp.Quotations

open Visualizer.Gui

// --------------------------------------------------------------------------------------
// Some quotations that will be displayed when application is started

let var = 2
let quotations = [
  ("Addition", 
    "<@@ 1+(2-var) @@>",
     <@@ 1+(2-var) @@>);
  
  ("Composition",
   "<@@ 
      let compose (f:float -> float) g (x:float) = 
        f(g(x))
      compose sin cos 1.0 @@>",
   <@@ 
      let compose (f:float -> float) g (x:float) = 
        f(g(x))
      compose sin cos 1.0 @@>);
      
  ("Statements", 
     "<@@ for i = 0 to 10 do 
           printf \"%d\" i 
         done
         let n = ref 1 in
         while (!n < 10) do
           n := !n + 1
           printf \"%d\" (!n)
         done @@>",
     <@@ for i = 0 to 10 do 
          printf "%d" i 
         done
         let n = ref 1 in
         while (!n < 10) do
           n := !n + 1
           printf "%d" (!n)
         done @@>);
          
  ("Lambda function", 
    "<@@ let rec fact = fun a -> if (a=0) then 1 else (a*fact a-1) in fact 10; @@>",
     <@@ let rec fact = fun a -> if (a=0) then 1 else (a*fact a-1) in fact 10; @@>);
          
  ("Form creation", 
    "<@@ 
      let tmp = \"Hello world\" in
      let form = new Form() in
      form.Text <- tmp.ToString(); 
      form.Show();
    @@>",
    <@@ 
      let tmp = "Hello world" in
      let form = new Form() in
      form.Text <- tmp.ToString(); 
      form.Show();
    @@>);

  ("Quotations", 
    "<@@ 
      let q = <@@ 1 + (5*2) @@> in q
    @@>",
    <@@ 
      let q = <@@ 1 + (5*2) @@> in q
    @@>);
    
//  ("Lifted value",
//    "(<@ 1 + _ @> (lift 5).Raw",
//    ((<@ 1 + _ @> (lift 5)).Raw)); 
  ]

[<STAThread>] 
do   
  let form = new Main() in
  quotations |> List.iter (fun q -> 
    let (name, code, exp) = q in form.AddQuotation name code exp)
  Application.EnableVisualStyles()
  Application.Run(form)