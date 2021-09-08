Imports System.ComponentModel

Public Class Recent
    Public res As String ' return name
    Private Sub Recent_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Res = ""
        ListBox1.Items.Clear()
        For Each fn As String In RecentFiles
            ListBox1.Items.Add(fn.Replace("|", "("))
        Next
    End Sub
    Private Sub Recent_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.KeyCode = Keys.Escape Then
            Me.Close()
        End If
    End Sub
    Private Sub ListBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox1.SelectedIndexChanged
        res = ListBox1.SelectedItem
        Me.Close()
    End Sub
End Class