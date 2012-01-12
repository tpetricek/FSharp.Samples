#light
module Visualizer.Visualizer

open System
open System.Windows.Forms
open Microsoft.FSharp
open Microsoft.FSharp.Quotations

// ----------------------------------------------------------------------------

/// Generates winforms treeview nodes from given expression
/// When second parameter is true, function applications are
/// handled using efApps (series), otherwies efApp is used
val GetExpressionTree : Expr * bool -> TreeNode
