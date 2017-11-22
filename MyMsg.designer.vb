<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class MyMsg
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        If disposing AndAlso components IsNot Nothing Then
            components.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.CSave = New System.Windows.Forms.Button()
        Me.CQuit = New System.Windows.Forms.Button()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.CCancel = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'CSave
        '
        Me.CSave.Anchor = System.Windows.Forms.AnchorStyles.None
        Me.CSave.DialogResult = System.Windows.Forms.DialogResult.Yes
        Me.CSave.Location = New System.Drawing.Point(15, 33)
        Me.CSave.Name = "CSave"
        Me.CSave.Size = New System.Drawing.Size(115, 23)
        Me.CSave.TabIndex = 0
        Me.CSave.Text = "Save"
        '
        'CQuit
        '
        Me.CQuit.Anchor = System.Windows.Forms.AnchorStyles.None
        Me.CQuit.DialogResult = System.Windows.Forms.DialogResult.No
        Me.CQuit.Location = New System.Drawing.Point(167, 33)
        Me.CQuit.Name = "CQuit"
        Me.CQuit.Size = New System.Drawing.Size(133, 23)
        Me.CQuit.TabIndex = 1
        Me.CQuit.Text = "Quit"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(12, 9)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(0, 13)
        Me.Label1.TabIndex = 1
        '
        'CCancel
        '
        Me.CCancel.Anchor = System.Windows.Forms.AnchorStyles.None
        Me.CCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.CCancel.Location = New System.Drawing.Point(324, 33)
        Me.CCancel.Name = "CCancel"
        Me.CCancel.Size = New System.Drawing.Size(137, 23)
        Me.CCancel.TabIndex = 2
        Me.CCancel.Text = "Return to edit"
        Me.CCancel.UseVisualStyleBackColor = True
        '
        'MyMsg
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(473, 58)
        Me.Controls.Add(Me.CCancel)
        Me.Controls.Add(Me.CQuit)
        Me.Controls.Add(Me.CSave)
        Me.Controls.Add(Me.Label1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "MyMsg"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "MyMsg"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents CSave As System.Windows.Forms.Button
    Friend WithEvents CQuit As System.Windows.Forms.Button
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents CCancel As System.Windows.Forms.Button

End Class
