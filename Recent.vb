Imports System.ComponentModel

Public Class Recent
    Public res As String
    Private Sub Recent_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        res = ""
        ListBox1.Items.Clear()
        ListBox1.Items.Add(GetRegistryKey("XeditPc", "File1", " "))
        ListBox1.Items.Add(GetRegistryKey("XeditPc", "File2", " "))
        ListBox1.Items.Add(GetRegistryKey("XeditPc", "File3", " "))
        ListBox1.Items.Add(GetRegistryKey("XeditPc", "File4", " "))
        ListBox1.Items.Add(GetRegistryKey("XeditPc", "File5", " "))
        ListBox1.Items.Add(GetRegistryKey("XeditPc", "File6", " "))
        ListBox1.Items.Add(GetRegistryKey("XeditPc", "File7", " "))
        ListBox1.Items.Add(GetRegistryKey("XeditPc", "File8", " "))
    End Sub
    Function s(n As String) As String
        If n.Length < 40 Then Return n
        Dim i As Integer = n.LastIndexOf("\"c)
        If i = -1 Then Return n
        Dim t As String = n.Substring(0, i)
        Dim l As Integer = t.Length / 2
        If l > 20 Then l = 20
        t = t.Substring(0, l) & "..." & t.Substring(t.Length - l) & n.Substring(i)
        Return t
    End Function
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