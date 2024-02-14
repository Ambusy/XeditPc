<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class InputBoxDialog
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
        Me.Says = New System.Windows.Forms.Label()
        Me.Response = New System.Windows.Forms.TextBox()
        Me.OkB = New System.Windows.Forms.Button()
        Me.CancelB = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'Says
        '
        Me.Says.AutoSize = True
        Me.Says.Location = New System.Drawing.Point(20, 15)
        Me.Says.Margin = New System.Windows.Forms.Padding(5, 0, 5, 0)
        Me.Says.Name = "Says"
        Me.Says.Size = New System.Drawing.Size(64, 22)
        Me.Says.TabIndex = 0
        Me.Says.Text = "Label1"
        '
        'Response
        '
        Me.Response.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Response.Location = New System.Drawing.Point(25, 170)
        Me.Response.Margin = New System.Windows.Forms.Padding(5, 6, 5, 6)
        Me.Response.Name = "Response"
        Me.Response.Size = New System.Drawing.Size(482, 28)
        Me.Response.TabIndex = 1
        '
        'OkB
        '
        Me.OkB.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.OkB.Location = New System.Drawing.Point(339, 229)
        Me.OkB.Margin = New System.Windows.Forms.Padding(5, 6, 5, 6)
        Me.OkB.Name = "OkB"
        Me.OkB.Size = New System.Drawing.Size(170, 34)
        Me.OkB.TabIndex = 2
        Me.OkB.Text = "OK"
        Me.OkB.UseVisualStyleBackColor = True
        '
        'CancelB
        '
        Me.CancelB.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.CancelB.Location = New System.Drawing.Point(25, 214)
        Me.CancelB.Margin = New System.Windows.Forms.Padding(5, 6, 5, 6)
        Me.CancelB.Name = "CancelB"
        Me.CancelB.Size = New System.Drawing.Size(170, 34)
        Me.CancelB.TabIndex = 0
        Me.CancelB.TabStop = False
        Me.CancelB.Text = "Cancel"
        Me.CancelB.UseVisualStyleBackColor = True
        '
        'InputBoxDialog
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(10.0!, 22.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(533, 268)
        Me.ControlBox = False
        Me.Controls.Add(Me.CancelB)
        Me.Controls.Add(Me.OkB)
        Me.Controls.Add(Me.Response)
        Me.Controls.Add(Me.Says)
        Me.Font = New System.Drawing.Font("Microsoft Sans Serif", 10.8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Margin = New System.Windows.Forms.Padding(5, 6, 5, 6)
        Me.Name = "InputBoxDialog"
        Me.Text = "InputBoxDialog"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Says As System.Windows.Forms.Label
    Friend WithEvents Response As System.Windows.Forms.TextBox
    Friend WithEvents OkB As System.Windows.Forms.Button
    Friend WithEvents CancelB As System.Windows.Forms.Button
End Class
