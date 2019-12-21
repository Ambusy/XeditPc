Imports VB = Microsoft.VisualBasic
#Const CreLogFile = False

Module MainMod
    Public FormShown As Boolean = False ' Initial form after startup has been shown: paint is now possible
    'Public FormShownOnce As Boolean   ' form has been painted, at least partial, but maybe not completely
    Public ResponseFromMsg As Integer
    Public QuitPgm As Boolean ' User stopped pgm, or catastrofic error encountered
    Public UserCancelledCmd As Boolean ' User stopped command 
    Public FormAlreadyClosed As Boolean ' Form has closed already
    Public CancelCmd As Boolean ' Return to user input
    Public HelpCmd As Boolean ' user pressed help
    Public SaveFileExisted As Boolean ' did save create a new file?
    Public nrCyclesEv As Integer ' controls call to doevents in long loops
    Public rc As Integer  ' Returncode of last command
    Public SysMessages As New Collection
    Public DecimalSepPt As Boolean ' . is decimal separator and not ,
    Public FontSizeOnForm As Integer = 8
    Public RepaintAllScreenLines As Boolean ' repaint all lines?

    Public EdtSessions As New Collection
    Public CurrEdtSession As EdtSession
    Public CurrEditSessionIx As Integer
    Public InvalidatedWin As Boolean ' Form invalidated: needs repainting

    Public mRecentLineCmdFound As Integer
    Public CurLnLineLr As Integer ' source linenr on current line of last user-command, for UNDO

    Public LinesScreenVisible As Short
    Public CharsScreenVisible, pCharsScreenVisible As Short
    Public EditTextHeight, EditTextWidth As Single
    Public RectHeight As Integer ' height of one line of sourcerectangle

    Public RexxCmdActive As Boolean ' true = Form is executing a Rexx commandfile
    Public MacroRecording As Boolean
    Public MacroString As String = ""
    Public MacroFile As String = ""
    Public MacroLength As Integer
    Public MacroState As Integer
    Public MacroClicked As Boolean

    Public ScrList As New Collection
    Public EditRdFile As FileStream ' file to read from (wrkfile or original file)
    Public EditFileWrk As FileStream ' workfile (if opened)
    Public WrkFileName As String ' name
    Public WrkMaxWritePos As Integer ' used up to pos
    Public RexxTrace As Boolean = False
    Public RexxPath As String = ""
    Public RexxFilePath As String = ""
    Public ExecutablePath As String

    Public RecallCmds(10) As String  ' recall buffer
    Public RecallPick As Integer  ' actual recalled command
    Public RecallNrCalled As Integer  ' n° RECALL without Enter
    Public RecallIxAdd As Integer  ' last added command
    Public RecallIxMax As Integer ' nr of cmd's already in buffer
    Public RecalledCmd As Integer ' ix of last cmd that was "recalled"?
    Public tMdX, tMdY As Integer ' for Timer1: where was mouse?
    Public sMdX, sMdY As Integer ' for Select: where was initial mouse?
    Public mMdX, mMdY As Short ' for move: where was mouse?
    Public forTest As Boolean ' to stop in next PAINT (debugging only)
    Public CultInf As New CultureInfo("en-US", False)
    Public logFile As StreamWriter
    Public nInsp As Integer = 0 ' indentation of trace
    Friend Pasting As Boolean ' True if inserting a block of lines.
    Friend CommandLine As String
    ' <Global.System.Diagnostics.DebuggerStepThroughAttribute()> _
    <Conditional("CreLogFile")> _
    Public Sub LoggO()
        logFile = File.CreateText(Path.GetTempPath() & "\XeditDNet.Log.txt")
    End Sub
    <Global.System.Diagnostics.DebuggerStepThroughAttribute()> _
    <Conditional("CreLogFile")> _
    Public Sub LoggC()
        logFile.Close()
    End Sub
    ' <Global.System.Diagnostics.DebuggerStepThroughAttribute()> _
    <Conditional("CreLogFile")> _
    Public Sub Logg(ByVal s As String)
        If logFile Is Nothing Then Return
        Dim i As Integer
        i = s.IndexOf(" "c)
        If i > 0 Then
            If s.Length() > i + 5 Then
                If s.Substring(i + 1, 5) = "start" Then nInsp += 2
            End If
        End If
        logFile.WriteLine(" ".PadRight(nInsp) & s)
        Debug.WriteLine(" ".PadRight(nInsp) & s)
        If i > 0 Then
            If s.Length() > i + 3 Then
                If s.Substring(i + 1, 3) = "end" Then
                    nInsp -= 2
                    If nInsp < 0 Then nInsp = 0
                End If
            End If
        End If
    End Sub
    Public Sub Main(ByVal cmdArgs() As String)
#If CreLogFile Then
     LoggO()
#End If
        Logg("XEDIT start")
        ExecutablePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)
        If ExecutablePath(ExecutablePath.Length() - 1) <> "\" Then ExecutablePath += "\"
        Dim i As Integer = ExecutablePath.LastIndexOf(":"c)
        If i > 0 Then ExecutablePath = ExecutablePath.Substring(i - 1)
        Logg(ExecutablePath)
        Dim myForm As New XeditPc()

        Try
            myForm.Top = Math.Min(Math.Max(0, CInt(GetRegistryKey("XeditPc", "Top", Str(myForm.Top)))), Screen.PrimaryScreen.Bounds.Height - 100)
            myForm.Left = Math.Min(Math.Max(0, CInt(GetRegistryKey("XeditPc", "Left", Str(myForm.Left)))), Screen.PrimaryScreen.Bounds.Width - 100)
            myForm.Width = Math.Min(Math.Max(0, CInt(GetRegistryKey("XeditPc", "Width", Str(myForm.Width)))), Screen.PrimaryScreen.Bounds.Width)
            myForm.Height = Math.Min(Math.Max(0, CInt(GetRegistryKey("XeditPc", "Height", Str(myForm.Height)))), Screen.PrimaryScreen.Bounds.Height)
            Dim RectScrn As Rectangle = Screen.GetWorkingArea(New Point(myForm.Width, myForm.Height))
            If myForm.Height > RectScrn.Height Then
                myForm.Height = RectScrn.Height
            End If
        Catch ex As Exception
        End Try
        If cmdArgs.GetUpperBound(0) > -1 Then
            CommandLine = cmdArgs(0).Trim()
        Else
            CommandLine = ""
        End If
        Application.Run(myForm)
        If myForm.WindowState = FormWindowState.Normal Then
            SaveRegistryKey("XeditPc", "Top", Str(myForm.Top))
            SaveRegistryKey("XeditPc", "Left", Str(myForm.Left))
            SaveRegistryKey("XeditPc", "Width", Str(myForm.Width))
            SaveRegistryKey("XeditPc", "Height", Str(myForm.Height))
        End If
        Logg("Main finishes ")
        LoggC()
#If CreLogFile Then
        logFile.Close()
#End If
    End Sub
    Private Function GetRegistryKey(ByVal KeyName As String, ByVal ValueName As String, ByVal Defaultval As String) As String
        If ValidKeyName(KeyName) Then
            Dim readValue As String
            readValue = CStr(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\AMBusy\" & KeyName, ValueName, Nothing))
            If Not (readValue Is Nothing) Then
                GetRegistryKey = readValue
            Else
                GetRegistryKey = Defaultval
            End If
        Else
            GetRegistryKey = Defaultval
        End If
    End Function
    Private Sub SaveRegistryKey(ByRef KeyName As String, ByVal ValueName As String, ByVal ValueData As String)
        If ValidKeyName(KeyName) Then
            My.Computer.Registry.CurrentUser.CreateSubKey("Software\AMBusy\" & KeyName)
            My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\AMBusy\" & KeyName, ValueName, ValueData)
        End If
    End Sub
    Private Function ValidKeyName(ByRef KeyName As String) As Boolean
        'A key name is invalid if it begins or ends with \ or contains \\
        If Not KeyName.StartsWith("\") Then
            If Not KeyName.EndsWith("\") Then
                If KeyName.Contains("\" & "\") = 0 Then
                    Return (True)
                End If
            End If
        End If
        Return False
    End Function
End Module
