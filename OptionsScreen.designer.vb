<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class OptionsScreen
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
        Me.Label1 = New System.Windows.Forms.Label()
        Me.CBAutosave = New System.Windows.Forms.ComboBox()
        Me.CBCase1 = New System.Windows.Forms.ComboBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.CBCase2 = New System.Windows.Forms.ComboBox()
        Me.CBCmdline = New System.Windows.Forms.ComboBox()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.CBCurline = New System.Windows.Forms.ComboBox()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.CBdisplay2 = New System.Windows.Forms.ComboBox()
        Me.CBdisplay1 = New System.Windows.Forms.ComboBox()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.CBfontsize = New System.Windows.Forms.ComboBox()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.CBHex = New System.Windows.Forms.ComboBox()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.CBLinend = New System.Windows.Forms.ComboBox()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.CBLrecl = New System.Windows.Forms.ComboBox()
        Me.Label9 = New System.Windows.Forms.Label()
        Me.CBMsgline = New System.Windows.Forms.ComboBox()
        Me.Label10 = New System.Windows.Forms.Label()
        Me.CBRecfm = New System.Windows.Forms.ComboBox()
        Me.Label11 = New System.Windows.Forms.Label()
        Me.CBShadow = New System.Windows.Forms.ComboBox()
        Me.Label12 = New System.Windows.Forms.Label()
        Me.CBstay = New System.Windows.Forms.ComboBox()
        Me.Label13 = New System.Windows.Forms.Label()
        Me.CBTrunc = New System.Windows.Forms.ComboBox()
        Me.Label14 = New System.Windows.Forms.Label()
        Me.CBUndo = New System.Windows.Forms.ComboBox()
        Me.Label15 = New System.Windows.Forms.Label()
        Me.CBEncode = New System.Windows.Forms.ComboBox()
        Me.Label16 = New System.Windows.Forms.Label()
        Me.CBProfile = New System.Windows.Forms.ComboBox()
        Me.Label17 = New System.Windows.Forms.Label()
        Me.CBVerify = New System.Windows.Forms.ComboBox()
        Me.Label18 = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(10, 10)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(52, 13)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "Autosave"
        '
        'CBAutosave
        '
        Me.CBAutosave.FormattingEnabled = True
        Me.CBAutosave.Items.AddRange(New Object() {"OFF", "10", "20", "50", "100", "1000"})
        Me.CBAutosave.Location = New System.Drawing.Point(65, 8)
        Me.CBAutosave.Name = "CBAutosave"
        Me.CBAutosave.Size = New System.Drawing.Size(90, 21)
        Me.CBAutosave.TabIndex = 1
        '
        'CBCase1
        '
        Me.CBCase1.FormattingEnabled = True
        Me.CBCase1.Items.AddRange(New Object() {"Mixed", "Uppercase"})
        Me.CBCase1.Location = New System.Drawing.Point(65, 35)
        Me.CBCase1.Name = "CBCase1"
        Me.CBCase1.Size = New System.Drawing.Size(90, 21)
        Me.CBCase1.TabIndex = 2
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(10, 37)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(31, 13)
        Me.Label2.TabIndex = 2
        Me.Label2.Text = "Case"
        '
        'CBCase2
        '
        Me.CBCase2.FormattingEnabled = True
        Me.CBCase2.Items.AddRange(New Object() {"Respect", "Ignore"})
        Me.CBCase2.Location = New System.Drawing.Point(161, 36)
        Me.CBCase2.Name = "CBCase2"
        Me.CBCase2.Size = New System.Drawing.Size(70, 21)
        Me.CBCase2.TabIndex = 3
        '
        'CBCmdline
        '
        Me.CBCmdline.FormattingEnabled = True
        Me.CBCmdline.Items.AddRange(New Object() {"TOP", "BOTTOM"})
        Me.CBCmdline.Location = New System.Drawing.Point(65, 62)
        Me.CBCmdline.Name = "CBCmdline"
        Me.CBCmdline.Size = New System.Drawing.Size(90, 21)
        Me.CBCmdline.TabIndex = 4
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(10, 64)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(44, 13)
        Me.Label3.TabIndex = 5
        Me.Label3.Text = "Cmdline"
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(593, 182)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(84, 20)
        Me.Button1.TabIndex = 0
        Me.Button1.Text = "OK"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'CBCurline
        '
        Me.CBCurline.FormattingEnabled = True
        Me.CBCurline.Items.AddRange(New Object() {"1", "2", "3", "4", "5", "15", "29", "30", "31", "32", "33"})
        Me.CBCurline.Location = New System.Drawing.Point(65, 89)
        Me.CBCurline.Name = "CBCurline"
        Me.CBCurline.Size = New System.Drawing.Size(90, 21)
        Me.CBCurline.TabIndex = 5
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(10, 91)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(39, 13)
        Me.Label4.TabIndex = 7
        Me.Label4.Text = "Curline"
        '
        'CBdisplay2
        '
        Me.CBdisplay2.FormattingEnabled = True
        Me.CBdisplay2.Items.AddRange(New Object() {"0", "1", "2", "3", "4", "5", "*"})
        Me.CBdisplay2.Location = New System.Drawing.Point(161, 116)
        Me.CBdisplay2.Name = "CBdisplay2"
        Me.CBdisplay2.Size = New System.Drawing.Size(70, 21)
        Me.CBdisplay2.TabIndex = 7
        '
        'CBdisplay1
        '
        Me.CBdisplay1.FormattingEnabled = True
        Me.CBdisplay1.Items.AddRange(New Object() {"0", "1", "2", "3", "4", "5"})
        Me.CBdisplay1.Location = New System.Drawing.Point(65, 116)
        Me.CBdisplay1.Name = "CBdisplay1"
        Me.CBdisplay1.Size = New System.Drawing.Size(90, 21)
        Me.CBdisplay1.TabIndex = 6
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(10, 118)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(41, 13)
        Me.Label5.TabIndex = 9
        Me.Label5.Text = "Display"
        '
        'CBfontsize
        '
        Me.CBfontsize.FormattingEnabled = True
        Me.CBfontsize.Items.AddRange(New Object() {"8", "12", "16", "24"})
        Me.CBfontsize.Location = New System.Drawing.Point(65, 146)
        Me.CBfontsize.Name = "CBfontsize"
        Me.CBfontsize.Size = New System.Drawing.Size(90, 21)
        Me.CBfontsize.TabIndex = 8
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(10, 148)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(46, 13)
        Me.Label6.TabIndex = 12
        Me.Label6.Text = "Fontsize"
        '
        'CBHex
        '
        Me.CBHex.FormattingEnabled = True
        Me.CBHex.Items.AddRange(New Object() {"OFF", "ON"})
        Me.CBHex.Location = New System.Drawing.Point(353, 7)
        Me.CBHex.Name = "CBHex"
        Me.CBHex.Size = New System.Drawing.Size(90, 21)
        Me.CBHex.TabIndex = 9
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(298, 9)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(26, 13)
        Me.Label7.TabIndex = 14
        Me.Label7.Text = "Hex"
        '
        'CBLinend
        '
        Me.CBLinend.FormattingEnabled = True
        Me.CBLinend.Items.AddRange(New Object() {"OFF", "ON"})
        Me.CBLinend.Location = New System.Drawing.Point(353, 34)
        Me.CBLinend.Name = "CBLinend"
        Me.CBLinend.Size = New System.Drawing.Size(90, 21)
        Me.CBLinend.TabIndex = 10
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(298, 36)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(40, 13)
        Me.Label8.TabIndex = 16
        Me.Label8.Text = "LinEnd"
        '
        'CBLrecl
        '
        Me.CBLrecl.FormattingEnabled = True
        Me.CBLrecl.Items.AddRange(New Object() {"80", "255", "1024"})
        Me.CBLrecl.Location = New System.Drawing.Point(353, 61)
        Me.CBLrecl.Name = "CBLrecl"
        Me.CBLrecl.Size = New System.Drawing.Size(90, 21)
        Me.CBLrecl.TabIndex = 11
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.Location = New System.Drawing.Point(298, 63)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(30, 13)
        Me.Label9.TabIndex = 18
        Me.Label9.Text = "Lrecl"
        '
        'CBMsgline
        '
        Me.CBMsgline.FormattingEnabled = True
        Me.CBMsgline.Items.AddRange(New Object() {"2", "3", "4", "5"})
        Me.CBMsgline.Location = New System.Drawing.Point(353, 88)
        Me.CBMsgline.Name = "CBMsgline"
        Me.CBMsgline.Size = New System.Drawing.Size(90, 21)
        Me.CBMsgline.TabIndex = 12
        '
        'Label10
        '
        Me.Label10.AutoSize = True
        Me.Label10.Location = New System.Drawing.Point(298, 90)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(43, 13)
        Me.Label10.TabIndex = 20
        Me.Label10.Text = "Msgline"
        '
        'CBRecfm
        '
        Me.CBRecfm.FormattingEnabled = True
        Me.CBRecfm.Items.AddRange(New Object() {"F", "V"})
        Me.CBRecfm.Location = New System.Drawing.Point(353, 145)
        Me.CBRecfm.Name = "CBRecfm"
        Me.CBRecfm.Size = New System.Drawing.Size(90, 21)
        Me.CBRecfm.TabIndex = 13
        '
        'Label11
        '
        Me.Label11.AutoSize = True
        Me.Label11.Location = New System.Drawing.Point(298, 147)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(38, 13)
        Me.Label11.TabIndex = 22
        Me.Label11.Text = "Recfm"
        '
        'CBShadow
        '
        Me.CBShadow.FormattingEnabled = True
        Me.CBShadow.Items.AddRange(New Object() {"ON", "OFF"})
        Me.CBShadow.Location = New System.Drawing.Point(593, 6)
        Me.CBShadow.Name = "CBShadow"
        Me.CBShadow.Size = New System.Drawing.Size(105, 21)
        Me.CBShadow.TabIndex = 14
        '
        'Label12
        '
        Me.Label12.AutoSize = True
        Me.Label12.Location = New System.Drawing.Point(538, 8)
        Me.Label12.Name = "Label12"
        Me.Label12.Size = New System.Drawing.Size(46, 13)
        Me.Label12.TabIndex = 24
        Me.Label12.Text = "Shadow"
        '
        'CBstay
        '
        Me.CBstay.FormattingEnabled = True
        Me.CBstay.Items.AddRange(New Object() {"ON", "OFF"})
        Me.CBstay.Location = New System.Drawing.Point(593, 34)
        Me.CBstay.Name = "CBstay"
        Me.CBstay.Size = New System.Drawing.Size(105, 21)
        Me.CBstay.TabIndex = 15
        '
        'Label13
        '
        Me.Label13.AutoSize = True
        Me.Label13.Location = New System.Drawing.Point(538, 35)
        Me.Label13.Name = "Label13"
        Me.Label13.Size = New System.Drawing.Size(28, 13)
        Me.Label13.TabIndex = 26
        Me.Label13.Text = "Stay"
        '
        'CBTrunc
        '
        Me.CBTrunc.FormattingEnabled = True
        Me.CBTrunc.Items.AddRange(New Object() {"10", "20", "30", "40", "50", "60", "70", "80", "*"})
        Me.CBTrunc.Location = New System.Drawing.Point(593, 60)
        Me.CBTrunc.Name = "CBTrunc"
        Me.CBTrunc.Size = New System.Drawing.Size(105, 21)
        Me.CBTrunc.TabIndex = 16
        '
        'Label14
        '
        Me.Label14.AutoSize = True
        Me.Label14.Location = New System.Drawing.Point(538, 62)
        Me.Label14.Name = "Label14"
        Me.Label14.Size = New System.Drawing.Size(35, 13)
        Me.Label14.TabIndex = 28
        Me.Label14.Text = "Trunc"
        '
        'CBUndo
        '
        Me.CBUndo.FormattingEnabled = True
        Me.CBUndo.Items.AddRange(New Object() {"1", "50", "100"})
        Me.CBUndo.Location = New System.Drawing.Point(593, 87)
        Me.CBUndo.Name = "CBUndo"
        Me.CBUndo.Size = New System.Drawing.Size(105, 21)
        Me.CBUndo.TabIndex = 17
        '
        'Label15
        '
        Me.Label15.AutoSize = True
        Me.Label15.Location = New System.Drawing.Point(538, 89)
        Me.Label15.Name = "Label15"
        Me.Label15.Size = New System.Drawing.Size(33, 13)
        Me.Label15.TabIndex = 30
        Me.Label15.Text = "Undo"
        '
        'CBEncode
        '
        Me.CBEncode.FormattingEnabled = True
        Me.CBEncode.Items.AddRange(New Object() {"UTF8", "UNICODE", "ASCII"})
        Me.CBEncode.Location = New System.Drawing.Point(593, 114)
        Me.CBEncode.Name = "CBEncode"
        Me.CBEncode.Size = New System.Drawing.Size(105, 21)
        Me.CBEncode.TabIndex = 18
        '
        'Label16
        '
        Me.Label16.AutoSize = True
        Me.Label16.Location = New System.Drawing.Point(538, 116)
        Me.Label16.Name = "Label16"
        Me.Label16.Size = New System.Drawing.Size(52, 13)
        Me.Label16.TabIndex = 32
        Me.Label16.Text = "Encoding"
        '
        'CBProfile
        '
        Me.CBProfile.FormattingEnabled = True
        Me.CBProfile.Items.AddRange(New Object() {"NOPROFILE", "PROFILE"})
        Me.CBProfile.Location = New System.Drawing.Point(353, 115)
        Me.CBProfile.Name = "CBProfile"
        Me.CBProfile.Size = New System.Drawing.Size(90, 21)
        Me.CBProfile.TabIndex = 33
        '
        'Label17
        '
        Me.Label17.AutoSize = True
        Me.Label17.Location = New System.Drawing.Point(298, 117)
        Me.Label17.Name = "Label17"
        Me.Label17.Size = New System.Drawing.Size(36, 13)
        Me.Label17.TabIndex = 34
        Me.Label17.Text = "Profile"
        '
        'CBVerify
        '
        Me.CBVerify.FormattingEnabled = True
        Me.CBVerify.Items.AddRange(New Object() {"1 *", "1 80", "1 30 HEX 1 30"})
        Me.CBVerify.Location = New System.Drawing.Point(593, 145)
        Me.CBVerify.Name = "CBVerify"
        Me.CBVerify.Size = New System.Drawing.Size(105, 21)
        Me.CBVerify.TabIndex = 35
        '
        'Label18
        '
        Me.Label18.AutoSize = True
        Me.Label18.Location = New System.Drawing.Point(538, 147)
        Me.Label18.Name = "Label18"
        Me.Label18.Size = New System.Drawing.Size(33, 13)
        Me.Label18.TabIndex = 36
        Me.Label18.Text = "Verify"
        '
        'OptionsScreen
        '
        Me.AcceptButton = Me.Button1
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(710, 202)
        Me.Controls.Add(Me.CBVerify)
        Me.Controls.Add(Me.Label18)
        Me.Controls.Add(Me.CBProfile)
        Me.Controls.Add(Me.Label17)
        Me.Controls.Add(Me.CBEncode)
        Me.Controls.Add(Me.Label16)
        Me.Controls.Add(Me.CBUndo)
        Me.Controls.Add(Me.Label15)
        Me.Controls.Add(Me.CBTrunc)
        Me.Controls.Add(Me.Label14)
        Me.Controls.Add(Me.CBstay)
        Me.Controls.Add(Me.Label13)
        Me.Controls.Add(Me.CBShadow)
        Me.Controls.Add(Me.Label12)
        Me.Controls.Add(Me.CBRecfm)
        Me.Controls.Add(Me.Label11)
        Me.Controls.Add(Me.CBMsgline)
        Me.Controls.Add(Me.Label10)
        Me.Controls.Add(Me.CBLrecl)
        Me.Controls.Add(Me.Label9)
        Me.Controls.Add(Me.CBLinend)
        Me.Controls.Add(Me.Label8)
        Me.Controls.Add(Me.CBHex)
        Me.Controls.Add(Me.Label7)
        Me.Controls.Add(Me.CBfontsize)
        Me.Controls.Add(Me.Label6)
        Me.Controls.Add(Me.CBdisplay2)
        Me.Controls.Add(Me.CBdisplay1)
        Me.Controls.Add(Me.Label5)
        Me.Controls.Add(Me.CBCurline)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.CBCmdline)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.CBCase2)
        Me.Controls.Add(Me.CBCase1)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.CBAutosave)
        Me.Controls.Add(Me.Label1)
        Me.Name = "OptionsScreen"
        Me.Text = "OptionsScreen"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents CBAutosave As System.Windows.Forms.ComboBox
    Friend WithEvents CBCase1 As System.Windows.Forms.ComboBox
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents CBCase2 As System.Windows.Forms.ComboBox
    Friend WithEvents CBCmdline As System.Windows.Forms.ComboBox
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents CBCurline As System.Windows.Forms.ComboBox
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents CBdisplay2 As System.Windows.Forms.ComboBox
    Friend WithEvents CBdisplay1 As System.Windows.Forms.ComboBox
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents CBfontsize As System.Windows.Forms.ComboBox
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents CBHex As System.Windows.Forms.ComboBox
    Friend WithEvents Label7 As System.Windows.Forms.Label
    Friend WithEvents CBLinend As System.Windows.Forms.ComboBox
    Friend WithEvents Label8 As System.Windows.Forms.Label
    Friend WithEvents CBLrecl As System.Windows.Forms.ComboBox
    Friend WithEvents Label9 As System.Windows.Forms.Label
    Friend WithEvents CBMsgline As System.Windows.Forms.ComboBox
    Friend WithEvents Label10 As System.Windows.Forms.Label
    Friend WithEvents CBRecfm As System.Windows.Forms.ComboBox
    Friend WithEvents Label11 As System.Windows.Forms.Label
    Friend WithEvents CBShadow As System.Windows.Forms.ComboBox
    Friend WithEvents Label12 As System.Windows.Forms.Label
    Friend WithEvents CBstay As System.Windows.Forms.ComboBox
    Friend WithEvents Label13 As System.Windows.Forms.Label
    Friend WithEvents CBTrunc As System.Windows.Forms.ComboBox
    Friend WithEvents Label14 As System.Windows.Forms.Label
    Friend WithEvents CBUndo As System.Windows.Forms.ComboBox
    Friend WithEvents Label15 As System.Windows.Forms.Label
    Friend WithEvents CBEncode As System.Windows.Forms.ComboBox
    Friend WithEvents Label16 As System.Windows.Forms.Label
    Friend WithEvents CBProfile As System.Windows.Forms.ComboBox
    Friend WithEvents Label17 As System.Windows.Forms.Label
    Friend WithEvents CBVerify As System.Windows.Forms.ComboBox
    Friend WithEvents Label18 As System.Windows.Forms.Label
End Class
