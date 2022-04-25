Public Class AskFileName
    Public commandLine As String = ""
    Dim IClose As Boolean = False
    Private Sub AskFileName_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.StartPosition = FormStartPosition.Manual
        Me.Location = New Point(0, 0)
        Me.Text = "XeditPC by AMBusy@Duck.com after IBM XEDIT" & Chr(169) & "2007-2022. "
        Empty.Text = SysMsg(26)
        RecentFiles.GetList(RecentList)
        TimerAsk.Enabled = True
    End Sub
    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TimerAsk.Tick
        TimerAsk.Enabled = False ' wait with dialog until form has been displayed
        DoTimer()
    End Sub
    Sub DoTimer()
        Dim listOptions As String = ""
        OpenFileDialog1.Title = Me.Text & SysMsg(12)
        OpenFileDialog1.FileName = ""
        OpenFileDialog1.CheckFileExists = False
        OpenFileDialog1.Multiselect = False
        OpenFileDialog1.ShowHelp = True
        HelpCmd = False
        OpenFileDialog1.ShowDialog()
        If OpenFileDialog1.FileNames.Length > 0 AndAlso OpenFileDialog1.FileName <> "" Then
            If RecentFiles.InRecents(OpenFileDialog1.FileName) Then
                If HelpCmd Then ' Change options in recents list
                    listOptions = RecentFiles.SetOptions(OpenFileDialog1.FileName, OptionsScreen.CmdString, True)
                End If
            End If
            commandLine = OpenFileDialog1.FileNames(0) & " |" & OptionsScreen.CmdString
            IClose = True
            Me.Close()
        End If
    End Sub
    Friend Sub helpPr(ByVal sender As Object, ByVal e As System.EventArgs) Handles OpenFileDialog1.HelpRequest
        HelpCmd = True
        OptionsScreen.ShowDialog()
        Logg("helpPr asked")
    End Sub
    Private Function SysMsg(ByVal i As Integer) As String
        Dim s As String
        s = "SYSMSG" & CStr(i)
        If SysMessages.Contains(s) Then
            SysMsg = CStr(SysMessages.Item(s))
        Else
            SysMsg = s & " not defined in 'system messages.txt'"
        End If
    End Function
    Private Sub RecentList_SelectedIndexChanged(sender As Object, e As EventArgs) Handles RecentList.SelectedIndexChanged
        Dim fn As String = RecentList.SelectedItem
        Dim i = fn.IndexOf(RFsep)
        commandLine = fn.Substring(i + RFsep.Length)
        IClose = True
        Me.Close()
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        QuitPgm = True
        IClose = True
        Me.Close()
    End Sub
    Private Sub AskFileName_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If Not IClose Then QuitPgm = True
    End Sub
    Private Sub Empty_Click(sender As Object, e As EventArgs) Handles Empty.Click
        If MsgBox(SysMsg(26) & "?", vbOKCancel) = vbOK Then
            RecentFiles.EmptyList()
        End If
    End Sub
End Class