Imports System.Windows.Forms
Public Class MyMsg
    Private Sub Save_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CSave.Click
        ResponseFromMsg = System.Windows.Forms.DialogResult.Yes
        Me.Close()
    End Sub
    Private Sub quit_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CQuit.Click
        ResponseFromMsg = System.Windows.Forms.DialogResult.No
        Me.Close()
    End Sub
    Private Sub Cancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CCancel.Click
        ResponseFromMsg = System.Windows.Forms.DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub MyMsg_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load
        Me.DesktopLocation = New Point(0, 0)
        Me.BringToFront()
    End Sub
End Class
