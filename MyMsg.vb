Imports System.Windows.Forms
Public Class MyMsg
    Dim CloseOk As Boolean = False
    Private Sub Save_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CSave.Click
        ResponseFromMsg = System.Windows.Forms.DialogResult.Yes
        CloseOk = True
        Me.Close()
    End Sub
    Private Sub quit_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CQuit.Click
        ResponseFromMsg = System.Windows.Forms.DialogResult.No
        CloseOk = True
        Me.Close()
    End Sub
    Private Sub Cancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CCancel.Click
        ResponseFromMsg = System.Windows.Forms.DialogResult.Cancel
        CloseOk = True
        Me.Close()
    End Sub

    Private Sub MyMsg_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load
        Me.DesktopLocation = New Point(0, 0)
        CloseOk = False
        Me.BringToFront()
    End Sub
    Private Sub MyMsg_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        e.Cancel = Not CloseOk
    End Sub
End Class
