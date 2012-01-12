#light
module Visualizer.Operations

open System
open System.Reflection
open Microsoft.FSharp.Quotations

// ----------------------------------------------------------------------------

/// Reduces an expression into a simplified form. 
///
/// Uses the beta-reduction rule:
///   (fun x -> expr1) expr2  ~~>  expr1[x/expr2]
val ReduceExpression : Expr -> Expr

/// Builds F# source file from given quotation expression and compiles
/// it using FSC compiler. This method expects that given expression
/// contains global declaration of variable 'q'
///
/// Example:
///   CompileExpression "let q=<@@ 1+2 @@>"
val CompileExpression : string -> option<Expr>

/// Opens specified assembly and resolves F# quotations of all top level 
/// definitions (declared using 'ReflectedDefinitions')
///
/// Parameters:
///  - assembly file name
///  - function to be called when a class is loaded
///  - function to be called when a quotation is resolved
val ResolveAssemblyDefinitions : 
  string -> 
  (Type -> 'T) -> 
  ('T -> MemberInfo -> Expr -> unit) -> unit
