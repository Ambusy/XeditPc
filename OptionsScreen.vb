Public Class OptionsScreen
    Public Shared CmdString As String
    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        If CBProfile.Text = "NOPROFILE" Then
            CmdString = "NOPROFILE"
        Else
            CmdString = ""
        End If
        'CmdString = addOp(CmdString, CBProfile)
        CmdString = addOp(CmdString, CBAutosave)
        CmdString = addOp2(CmdString, CBCase1, CBCase2)
        CmdString = addOp(CmdString, CBCmdline)
        CmdString = addOp(CmdString, CBCurline)
        CmdString = addOp2(CmdString, CBdisplay1, CBdisplay2)
        CmdString = addOp(CmdString, CBfontsize)
        CmdString = addOp(CmdString, CBHex)
        CmdString = addOp(CmdString, CBLinend)
        CmdString = addOp(CmdString, CBLrecl)
        CmdString = addOp(CmdString, CBMsgline)
        CmdString = addOp(CmdString, CBRecfm)
        CmdString = addOp(CmdString, CBShadow)
        CmdString = addOp(CmdString, CBstay)
        CmdString = addOp(CmdString, CBTrunc)
        CmdString = addOp(CmdString, CBUndo)
        CmdString = addOp(CmdString, CBEncode)
        CmdString = addOp(CmdString, CBVerify)
        Me.Close()
    End Sub
    Function addOp(Cmd As String, tb As ComboBox) As String
        Dim tbn As String = tb.Name.Substring(2).ToUpper
        If tb.Text <> tb.Tag Then Cmd += tbn + " " + tb.Text + " "
        addOp = Cmd
    End Function
    Function addOp2(Cmd As String, tb1 As ComboBox, tb2 As ComboBox) As String
        Dim tbn As String = tb1.Name.Substring(2, tb1.Name.Length() - 3).ToUpper
        If tb1.Text <> tb1.Tag Or tb2.Text <> tb2.Tag Then Cmd += tbn + " " + tb1.Text + " " + tb2.Text + " "
        addOp2 = Cmd
    End Function
    Private Sub OptionsScreen_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        CmdString = ""
        CBProfile.Text = "PROFILE"
        CBProfile.Tag = CBProfile.Text
        CBAutosave.Text = "OFF"
        CBAutosave.Tag = CBAutosave.Text
        CBCase1.Text = "Mixed"
        CBCase1.Tag = CBCase1.Text
        CBCase2.Text = "Ignore"
        CBCase2.Tag = CBCase2.Text
        CBCmdline.Text = "BOTTOM"
        CBCmdline.Tag = CBCmdline.Text
        CBCurline.Text = "3"
        CBCurline.Tag = CBCurline.Text
        CBdisplay1.Text = "0"
        CBdisplay1.Tag = CBdisplay1.Text
        CBdisplay2.Text = "*"
        CBdisplay2.Tag = CBdisplay2.Text
        CBfontsize.Text = "9"
        CBfontsize.Tag = CBfontsize.Text
        CBHex.Text = "ON"
        CBHex.Tag = CBHex.Text
        CBLinend.Text = "OFF"
        CBLinend.Tag = CBLinend.Text
        CBLrecl.Text = "1024"
        CBLrecl.Tag = CBLrecl.Text
        CBMsgline.Text = "2"
        CBMsgline.Tag = CBMsgline.Text
        CBRecfm.Text = "V"
        CBRecfm.Tag = CBRecfm.Text
        CBShadow.Text = "ON"
        CBShadow.Tag = CBShadow.Text
        CBstay.Text = "OFF"
        CBstay.Tag = CBstay.Text
        CBTrunc.Text = "*"
        CBTrunc.Tag = CBTrunc.Text
        CBUndo.Text = "50"
        CBUndo.Tag = CBUndo.Text
        CBEncode.Text = "UTF8"
        CBEncode.Tag = CBEncode.Text
        CBVerify.Text = "1 *"
        CBVerify.Tag = CBVerify.Text
    End Sub
End Class