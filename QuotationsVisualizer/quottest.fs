#light
open System.Windows.Forms;
open Microsoft.FSharp.Quotations;
open Microsoft.FSharp.Quotations.Typed;

[<ReflectedDefinition>]
let form = <@ 
             let tmp = "Hello world"
             let form = new Form()
             form.Text <- tmp.ToString()
             form.Show()
           @>