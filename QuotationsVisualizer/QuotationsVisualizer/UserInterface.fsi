#light
namespace Visualizer.Gui

open System
open System.Windows.Forms
open Microsoft.FSharp
open Microsoft.FSharp.Quotations

// ----------------------------------------------------------------------------

/// Main application window
type Main = 
  inherit Form 
  
  /// Builds the form
  new : unit -> Main
  /// Adds quotation to be displayed at main Form
  member AddQuotation : string -> string -> Expr -> unit
