Public Class OptionsScreen
    Public Shared CmdString As String
    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        If CBProfile.Text = "NOPROFILE" Then
            CmdString = "NOPROFILE"
        Else
            CmdString = ""
        End If
        CmdString = CmdString _
         & " AUTOSAVE " & CBAutosave.Text _
         & " CASE " & CBCase1.Text & " " & CBCase2.Text _
         & " CMDLINE " & CBCmdline.Text _
         & " CURLINE " & CBCurline.Text _
         & " DISPLAY " & CBdisplay1.Text & " " & CBdisplay2.Text _
         & " FONTSIZE " & CBfontsize.Text _
         & " HEX " & CBHex.Text _
         & " LINEND " & CBHex.Text _
         & " LRECL " & CBLrecl.Text _
         & " MSGLINE " & CBMsgline.Text & " OVERLAY" _
         & " RECFM " & CBRecfm.Text _
         & " SHADOW " & CBShadow.Text _
         & " STAY " & CBstay.Text _
         & " TRUNC " & CBTrunc.Text _
         & " UNDO " & CBUndo.Text _
         & " ENCODE " & CBEncode.Text _
         & " VERIFY " & CBVerify.Text
        Me.Close()
    End Sub

    Private Sub OptionsScreen_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        CmdString = ""
        CBProfile.Text = "PROFILE"
        CBAutosave.Text = "OFF"
        CBCase1.Text = "Mixed"
        CBCase2.Text = "Ignore"
        CBCmdline.Text = "BOTTOM"
        CBCurline.Text = "3"
        CBdisplay1.Text = "0"
        CBdisplay2.Text = "*"
        CBfontsize.Text = "9"
        CBHex.Text = "ON"
        CBLinend.Text = "OFF"
        CBLrecl.Text = "1024"
        CBMsgline.Text = "2"
        CBRecfm.Text = "V"
        CBShadow.Text = "ON"
        CBstay.Text = "OFF"
        CBTrunc.Text = "*"
        CBUndo.Text = "50"
        CBEncode.Text = "UTF8"
        CBVerify.Text = "1 *"
    End Sub

    Private Sub CBEncode_SelectedIndexChanged(sender As Object, e As EventArgs) Handles CBEncode.SelectedIndexChanged

    End Sub
End Class