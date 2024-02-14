Public Class InputBoxDialog
    ' on INPUT:
    Friend InpTit As String ' title
    Friend InpSay As String ' display text
    ' on RETURN:
    Friend OutResp As String ' answer (on entry contains default)
    Friend OKBut As Boolean ' which button
    Private Sub CancelB_Click(sender As Object, e As EventArgs) Handles CancelB.Click
        OKBut = False
        Me.Visible = False
    End Sub
    Private Sub OkB_Click(sender As Object, e As EventArgs) Handles OkB.Click
        OKBut = True
        If Response.Text.Length > 0 Then
            OutResp = Response.Text
        End If
        Me.Visible = False
    End Sub
    Private Sub InputBoxDialog_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        Me.Text = InpTit
        Says.Text = InpSay
        Response.Text = ""
        Response.Focus()
        Me.Refresh()
    End Sub
    Private Sub Response_KeyDown(sender As Object, e As KeyEventArgs) Handles Response.KeyDown
        If e.KeyCode = 13 Then
            OkB_Click(sender, New EventArgs())
        End If
    End Sub
    Private Sub InputBoxDialog_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Top = MeTop  ' overlap xeditscreen
        Me.Left = MeLeft
        Me.Font = New Font(Me.Font.Style, MeFontSize)
    End Sub
End Class