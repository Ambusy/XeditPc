<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class XeditPc
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(XeditPc))
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog()
        Me.SaveFileDialog1 = New System.Windows.Forms.SaveFileDialog()
        Me.ContextMenuStrip1 = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.menu_Copy = New System.Windows.Forms.ToolStripMenuItem()
        Me.menu_Cut = New System.Windows.Forms.ToolStripMenuItem()
        Me.menu_Paste = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator1 = New System.Windows.Forms.ToolStripSeparator()
        Me.menu_Macro = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator2 = New System.Windows.Forms.ToolStripSeparator()
        Me.menu_Return = New System.Windows.Forms.ToolStripMenuItem()
        Me.FontDialog1 = New System.Windows.Forms.FontDialog()
        Me.PrintDialog1 = New System.Windows.Forms.PrintDialog()
        Me.PageSetupDialog1 = New System.Windows.Forms.PageSetupDialog()
        Me.VSB = New System.Windows.Forms.VScrollBar()
        Me.TimerBar = New System.Windows.Forms.Timer(Me.components)
        Me.ColorDialog1 = New System.Windows.Forms.ColorDialog()
        Me.ContextMenuStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'ContextMenuStrip1
        '
        Me.ContextMenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.menu_Copy, Me.menu_Cut, Me.menu_Paste, Me.ToolStripSeparator1, Me.menu_Macro, Me.ToolStripSeparator2, Me.menu_Return})
        Me.ContextMenuStrip1.Name = "ContextMenuStrip1"
        Me.ContextMenuStrip1.Size = New System.Drawing.Size(158, 126)
        '
        'menu_Copy
        '
        Me.menu_Copy.Name = "menu_Copy"
        Me.menu_Copy.Size = New System.Drawing.Size(157, 22)
        Me.menu_Copy.Text = "copy"
        '
        'menu_Cut
        '
        Me.menu_Cut.Name = "menu_Cut"
        Me.menu_Cut.Size = New System.Drawing.Size(157, 22)
        Me.menu_Cut.Text = "cut"
        '
        'menu_Paste
        '
        Me.menu_Paste.Name = "menu_Paste"
        Me.menu_Paste.Size = New System.Drawing.Size(157, 22)
        Me.menu_Paste.Text = "paste"
        '
        'ToolStripSeparator1
        '
        Me.ToolStripSeparator1.Name = "ToolStripSeparator1"
        Me.ToolStripSeparator1.Size = New System.Drawing.Size(154, 6)
        '
        'menu_Macro
        '
        Me.menu_Macro.Name = "menu_Macro"
        Me.menu_Macro.Size = New System.Drawing.Size(157, 22)
        Me.menu_Macro.Text = "Macro Record"
        '
        'ToolStripSeparator2
        '
        Me.ToolStripSeparator2.Name = "ToolStripSeparator2"
        Me.ToolStripSeparator2.Size = New System.Drawing.Size(154, 6)
        '
        'menu_Return
        '
        Me.menu_Return.Name = "menu_Return"
        Me.menu_Return.Size = New System.Drawing.Size(157, 22)
        Me.menu_Return.Text = "Return to editor"
        '
        'PrintDialog1
        '
        Me.PrintDialog1.UseEXDialog = True
        '
        'VSB
        '
        Me.VSB.Location = New System.Drawing.Point(0, 0)
        Me.VSB.Maximum = 30000
        Me.VSB.Name = "VSB"
        Me.VSB.Size = New System.Drawing.Size(10, 20)
        Me.VSB.TabIndex = 1
        '
        'TimerBar
        '
        '
        'XeditPc
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.White
        Me.ClientSize = New System.Drawing.Size(739, 432)
        Me.Controls.Add(Me.VSB)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.KeyPreview = True
        Me.MinimumSize = New System.Drawing.Size(300, 200)
        Me.Name = "XeditPc"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.Manual
        Me.Text = "XeditPc"
        Me.ContextMenuStrip1.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents OpenFileDialog1 As System.Windows.Forms.OpenFileDialog
    Friend WithEvents SaveFileDialog1 As System.Windows.Forms.SaveFileDialog
    Friend WithEvents ContextMenuStrip1 As System.Windows.Forms.ContextMenuStrip
    Friend WithEvents menu_Copy As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents menu_Cut As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents menu_Paste As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ToolStripSeparator1 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents menu_Return As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents FontDialog1 As System.Windows.Forms.FontDialog
    Friend WithEvents PrintDialog1 As System.Windows.Forms.PrintDialog
    Friend WithEvents PageSetupDialog1 As System.Windows.Forms.PageSetupDialog
    Friend WithEvents menu_Macro As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ToolStripSeparator2 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents VSB As System.Windows.Forms.VScrollBar
    Friend WithEvents TimerBar As System.Windows.Forms.Timer
    Friend WithEvents ColorDialog1 As ColorDialog
End Class
