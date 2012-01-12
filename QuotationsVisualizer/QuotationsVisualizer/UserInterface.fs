#light
#nowarn "47"
namespace Visualizer.Gui

open System
open System.Drawing
open System.Resources
open System.Reflection
open System.Collections.Generic
open System.Windows.Forms
open Microsoft.FSharp
open Microsoft.FSharp.Core
open Microsoft.FSharp.Quotations

open Visualizer.Visualizer
open Visualizer.Operations

// ----------------------------------------------------------------------------
/// Dialog that is opened when user wants to add new F# 
/// (that will be compiled at runtime using FSC)

type EnterCodeDialog(parent:Form) as this =
  inherit Form() 
 
  let mutable expression = <@@ 1 @@>

  // Build GUI...
  do
    base.Owner <- parent
    base.Width <- 400
    base.Height <- 300
    base.StartPosition <- FormStartPosition.CenterParent
    base.Text <- "Enter quotation expression"
    base.ShowInTaskbar <- false
    base.MaximizeBox <- false
    base.MinimizeBox <- false
    base.FormBorderStyle <- FormBorderStyle.FixedDialog
    
  let code = 
    new TextBox(Text = "let q =\r\n <@@\r\n   1\r\n @@>", SelectionLength = 0,
                Multiline = true, WordWrap = false, ScrollBars = ScrollBars.Vertical,
                Font = new Font(FontFamily.GenericMonospace, 9.0f),
                Left = 4, Width = base.ClientSize.Width - 8,
                Top = 28, Height = base.ClientSize.Height - 64,
                AcceptsReturn = true,
                Anchor = (AnchorStyles.Bottom ||| AnchorStyles.Left ||| AnchorStyles.Right ||| AnchorStyles.Top))                
  let title = 
    new TextBox(Text = "Generated", Left = 54, Top = 4,
                Width = base.ClientSize.Width - 58,
                Anchor = (AnchorStyles.Left ||| AnchorStyles.Right ||| AnchorStyles.Top))
  let lbl = 
    new Label(Text = "Title:", Left = 4, Width = 50,
              Top = 4, TextAlign = ContentAlignment.MiddleLeft)
  let ok = 
    new Button(FlatStyle = FlatStyle.System, Text = "OK", Left = base.ClientSize.Width - 64,
               Width = 60, Top = base.ClientSize.Height - 28, Height = 22,
               Anchor = (AnchorStyles.Bottom ||| AnchorStyles.Right))
  do    
    base.Controls.Add(lbl)
    base.Controls.Add(code)
    base.Controls.Add(title)
    base.AcceptButton <- ok
    ok.Click.Add( fun _ -> 
        match CompileExpression(this.EnteredCode) with
          | Some(e) ->
            expression <- e
            this.DialogResult <- DialogResult.OK 
            this.Close() 
          | _ -> ignore() )
    base.Controls.Add(ok)

  /// F# source code entered by user
  member this.EnteredCode = code.Text

  /// Title of entered expression 
  /// (will be used as title for tab page) 
  member this.EnteredTitle = title.Text
  
  /// Compiled expression
  member this.CompiledExpression = expression

// ----------------------------------------------------------------------------
/// Tab page that is used for displaying expression trees
/// It contains original object that represents the expression

type QuotationPanel(s:string, h:string, expression:Expr, imgs:ImageList) as this = 
  inherit UserControl() 

  // Build GUI...
  let pnl = new Panel(Dock = DockStyle.Bottom, Height = 140) 
  let hdr = 
    new TextBox(Text = (h.Replace("\t", "  ")).Replace("\n","\r\n"),
                ReadOnly = true, Multiline = true, WordWrap = false,
                ScrollBars = ScrollBars.Vertical, Left = 0,
                Top = 10, Width = pnl.Width, Height = pnl.Height - 10,
                Anchor = (AnchorStyles.Bottom ||| AnchorStyles.Left ||| AnchorStyles.Right ||| AnchorStyles.Top),
                Font = new Font(FontFamily.GenericMonospace, 9.0f))
  let tree = new TreeView(ImageList = imgs, Dock = DockStyle.Fill)
  do
    this.Padding <- new Padding(5)
    this.Text <- s
    this.Controls.Add(tree)
    this.Controls.Add(pnl)
    pnl.Controls.Add(hdr)

  /// Returns TreeView
  member this.QuotationTree = tree
    
  // Returns expression at this tab 
  member this.Expression = expression    


// ----------------------------------------------------------------------------

type Main() as this = 
  inherit Form() 

  /// Build GUI...
  let quotPanels = new List<QuotationPanel>();
  let exprTree = new TreeView()
  let menuPnl = new Panel(Dock = DockStyle.Right, Width = 250 ) 
  let treeIcons = new ImageList()
  do
    let lbl1 = 
      new Label(Font = new Font(base.Font.Name, 10.0f, FontStyle.Bold),
                Text = "Settings", Left = 4, Top = 8)
    let lbl2 = 
      new Label(Font = new Font(base.Font.Name, 10.0f, FontStyle.Bold),
                Text = "Tools", Left = 4, Top = 112)
    let lbl3 = 
      new Label(Font = new Font(base.Font.Name, 10.0f, FontStyle.Bold),
                Text = "Quotations", Left = 4, Top = 264)
    menuPnl.Controls.Add(lbl1)
    menuPnl.Controls.Add(lbl2)
    menuPnl.Controls.Add(lbl3)

  let checkApps = 
    new CheckBox(Text = "Group series together - matching prefers Apps and Lambdas",
                 FlatStyle = FlatStyle.System, Left = 16, Width = 220, Top = 32, Height = 40)
  let checkExpand = 
    new CheckBox(Text = "Call deepMacroExpand on quotation",
                 Left = 16, Width = 220, Top = 72, FlatStyle = FlatStyle.System)
  do    
    checkApps.CheckedChanged.Add(fun _ -> this.RegenerateQuotations() )
    checkExpand.CheckedChanged.Add(fun _ -> this.RegenerateQuotations() )
    
  do
    let lnkExpand = new LinkLabel(Text = "Expand current tree",
                                  Left = 32, Width = 200, Top = 136) 
    let lnkCollapse = new LinkLabel(Text = "Collapse current tree",
                                    Left = 32, Width = 200, Top = 160) 
    lnkExpand.Click.Add(fun _ -> this.CollapseOrExpandTree(true))
    lnkCollapse.Click.Add(fun _ -> this.CollapseOrExpandTree(false))
    menuPnl.Controls.Add(lnkExpand)
    menuPnl.Controls.Add(lnkCollapse)
    let lnkAddQuot = new LinkLabel(Text = "Add new quotation",
                                   Left = 32, Width = 200, Top = 208) 
    let lnkOpenAssembly = new LinkLabel(Text = "Open F# assembly",
                                        Left = 32, Width = 200, Top = 232) 
    let lnkCloseCurrent = new LinkLabel(Text = "Close current page",
                                        Left = 32, Width = 200, Top = 184) 
    lnkCloseCurrent.Click.Add(fun _ -> this.CloseCurrentTab() )    
    lnkAddQuot.Click.Add(fun _ -> this.AddEnteredQuotation() )        
    lnkOpenAssembly.Click.Add(fun _ -> this.OpenFsAssembly() )        
    
    // tabs & form
    this.Text <- "Quotations Visualizer"
    this.Width <- 800
    this.Height <- 600

    exprTree.Left <- 32
    exprTree.Width <- 200
    exprTree.Height <- 268
    exprTree.Top <- 288
    exprTree.HideSelection <- false
    exprTree.AfterSelect.Add(fun e -> 
      if (e.Node.Tag <> null) then 
        this.SelectPanel(e.Node.Tag :?> QuotationPanel))

    menuPnl.Controls.Add(exprTree)    
    menuPnl.Controls.Add(lnkCloseCurrent)
    menuPnl.Controls.Add(lnkAddQuot)
    menuPnl.Controls.Add(lnkOpenAssembly)
    menuPnl.Controls.Add(checkApps)
    menuPnl.Controls.Add(checkExpand)
    this.Controls.Add(menuPnl)
    exprTree.Anchor <- AnchorStyles.Top ||| AnchorStyles.Bottom
        
    // load icons
    treeIcons.TransparentColor <- Color.Fuchsia
    let mgr = QuotationsVisualizer.Resources.Quotations.ResourceManager
    let cult = QuotationsVisualizer.Resources.Quotations.Culture
    //let mgr = new ResourceManager("Resources.Quotations", Assembly.GetExecutingAssembly())    
    ["root"; "app"; "apps"; "action"; "param"; "topdef"; "constant"; "var"; "hole"; 
     "let"; "letin"; "letwhat"; "call"; "ctorcall"; "seq"; "lambda"; "tuple"; "error"; "oper"; "statement";
     "quoted"; "lifted"] |> List.iter (fun n -> 
      treeIcons.Images.Add(mgr.GetObject(n, cult) :?> Bitmap))
    
    let appIcons = new ImageList()
    appIcons.TransparentColor <- Color.Fuchsia
    let mgr = QuotationsVisualizer.Resources.App.ResourceManager
    //let mgr = new ResourceManager("Resources.App", Assembly.GetExecutingAssembly()) 
    ["obj"; "asm"] |> List.iter (fun n -> appIcons.Images.Add(mgr.GetObject(n, cult) :?> Bitmap))
    exprTree.ImageList <- appIcons
    

  member this.AddQuotationNode (t:string) (h:string) (e:Expr) = 
    let pnl = new QuotationPanel(t,h,e,treeIcons) 
    pnl.Dock <- DockStyle.Fill
    this.Controls.Add(pnl)
    menuPnl.SendToBack()
    this.GenerateQuotation pnl
    quotPanels.Add(pnl)
    let nd = new TreeNode(t, 0, 0) 
    nd.Tag <- (pnl :> obj)
    nd
  
  /// Adds new quotation to Tab control
  member this.AddQuotation (t:string) (h:string) (e:Expr) = 
    let nd = this.AddQuotationNode t h e 
    ignore(exprTree.Nodes.Add(nd))
    
  /// Select panel with quotations
  member this.SelectPanel (p:QuotationPanel) =
    p.BringToFront()
    
  /// Close current tab 
  member this.CloseCurrentTab () = 
    if (exprTree.SelectedNode = null) then
      ignore(MessageBox.Show("No quotation is opened!", this.Text))
    else if (exprTree.SelectedNode.Tag <> null) then
      let pnl = (exprTree.SelectedNode.Tag) :?> QuotationPanel 
      ignore(quotPanels.Remove(pnl))
      this.Controls.Remove(pnl)
      exprTree.SelectedNode.Remove()
    else 
      for (nd:TreeNode) in exprTree.SelectedNode.Nodes do
        let pnl = (nd.Tag) :?> QuotationPanel 
        ignore(quotPanels.Remove(pnl))
        this.Controls.Remove(pnl)
      exprTree.SelectedNode.Remove()
    
  /// Generates trees for all expressions
  /// (this is called when settings are changed)
  member this.RegenerateQuotations () =
    for (pnl:QuotationPanel) in quotPanels do 
      this.GenerateQuotation pnl
    
  /// Generates quotation tree on one tab page
  member this.GenerateQuotation (tab:QuotationPanel) =
    let exp = if (checkExpand.Checked) then ReduceExpression(tab.Expression) else tab.Expression 
    let node = GetExpressionTree(exp, checkApps.Checked) 
    tab.QuotationTree.Nodes.Clear()
    ignore(tab.QuotationTree.Nodes.Add(node))
    tab.QuotationTree.ExpandAll()
        
  /// Collapses or expands tree on current tab page
  member this.CollapseOrExpandTree (expand:bool) = 
    if (exprTree.SelectedNode = null) then
      ignore(MessageBox.Show("No quotation is opened!", this.Text))
    else if (exprTree.SelectedNode.Tag <> null) then
      let tr = (exprTree.SelectedNode.Tag :?> QuotationPanel).QuotationTree 
      if (expand) then tr.ExpandAll() else tr.CollapseAll()
    
  /// Add quotation entered by user
  member this.AddEnteredQuotation () =
    let dlg = new EnterCodeDialog(this) 
    if (dlg.ShowDialog() = DialogResult.OK) then
      this.AddQuotation dlg.EnteredTitle dlg.EnteredCode dlg.CompiledExpression           
      
  member this.OpenFsAssembly () =
    let opf = new OpenFileDialog() 
    opf.Filter <- "F# assemblies|*.dll;*.exe";
    match opf.ShowDialog(this) with
      | DialogResult.OK ->
          ResolveAssemblyDefinitions(opf.FileName)
            ( fun t -> 
                let nd = new TreeNode(t.Name, 1, 1) 
                ignore(exprTree.Nodes.Add(nd))
                nd )
            ( fun ndType mi exp -> 
                let fn = new Text.StringBuilder() 
                ignore(fn.AppendFormat("Loaded from assembly: {0}\r\n", mi.DeclaringType.Assembly.FullName));
                ignore(fn.Append("Declared in type: "));
                if (mi.DeclaringType.Namespace <> null) then ignore(fn.AppendFormat("{0}.", mi.DeclaringType.Namespace))
                ignore(fn.Append(mi.DeclaringType.Name));
                let nd = this.AddQuotationNode mi.Name (fn.ToString()) exp 
                ignore(ndType.Nodes.Add(nd)) )          
      | _ -> ()