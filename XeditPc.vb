Imports VB = Microsoft.VisualBasic
Imports System.Drawing.Printing
Imports System.Xml.Schema
#Const CreLogFile = False
'#Const tracen = True
Public Class XeditPc
    Dim TimerEnabled As Boolean = False
    Dim TimerInterval As Integer = 50
    Dim optionsSpecified As Boolean
    Dim MousePosX, MousePosY As Integer ' if cursor on scrollbar, act on changes of value
    Public WithEvents Rxs As New Rexx
    Private Sub XeditPc_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        RepaintAllScreenLines = True
        DecimalSepPt = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator = "."
        Me.Cursor = System.Windows.Forms.Cursors.WaitCursor
        Logg("Main vers. 15 mar 2015 DirectoryPath " & ExecutablePath)
        If Not QuitPgm Then
            rc = InitAndRunEdit(CommandLine)
        End If
        If Not QuitPgm AndAlso rc = 0 Then
            FormShown = True
            Me.Invalidate()
            Me.Focus()
            Me.Show()
            SendKeys.Send("{DOWN}") ' if not: VSB intercepts first "keydown" !?!?
        Else
            Me.Close()
        End If
    End Sub
    Public Function InitAndRunEdit(ByVal CommandLine As String) As Integer
        Dim i As Integer
        Dim Filename As String
        i = InStr(CommandLine, "|") ' XEDIT  file |options) comment
        optionsSpecified = (i > -1) ' might be empty string
        If i > 0 Then
            Filename = CommandLine.Substring(0, i - 1).Trim()
            CommandLine = CommandLine.Substring(i)
            i = InStr(CommandLine, ")")
            If i > 0 Then
                CommandLine = CommandLine.Substring(0, i - 1)
            End If
        Else
            Filename = CommandLine
            CommandLine = ""
        End If
        InitAndRunEdit = RunEdit(Filename, CommandLine)
    End Function
    Private Function RunEdit(ByVal Filename As String, ByVal CommandLine As String) As Integer
        ' switch to existing edit session, or create a new one and prepare it
        Dim found As Boolean
        Dim i As Integer
        Logg("RunEdit start")
        Me.Cursor = System.Windows.Forms.Cursors.WaitCursor
        If Not RexxCmdActive Then Me.Text = "Xedit " & Application.ProductVersion & " " & Filename
        RunEdit = 0
        found = False
        If Filename.Length() > 0 Then
            Filename = Path.GetFullPath(Filename)
            Logg("RunEdit Filename " & Filename)
            i = 0
            For Each EdtSsn In EdtSessions
                i = i + 1
                If EdtSsn.EditFileName.ToUpper() = Filename.ToUpper() Then
                    found = True
                    CurrEditSessionIx = i ' existing
                    CurrEdtSession = EdtSsn
                    RunEdit = CurrEditSessionIx
                    Logg("RunEdit Filename in ring")
                    Exit For
                End If
            Next EdtSsn
        Else ' next
            Logg("RunEdit Filename new")
            CurrEditSessionIx = CurrEditSessionIx + 1
            If CurrEditSessionIx > EdtSessions.Count() Then CurrEditSessionIx = 1
            CurrEdtSession = DirectCast(EdtSessions.Item(CurrEditSessionIx), EdtSession)
            RunEdit = CurrEditSessionIx
            Me.SetFormCaption()
            found = True
        End If
        If Not found Then '  file not yet in edit-ring
            CurrEdtSession = New EdtSession
            CurrEdtSession.EditFileName = Filename
            EdtSessions.Add(CurrEdtSession)
            CurrEditSessionIx = EdtSessions.Count()
            CommandLine = RecentFiles.SetOptions(Filename, CommandLine, False)
            If Not QuitPgm Then
                ProcessCommandOptions(CommandLine)
                ReadEditFile(Filename)
                SetFormCaption()
                CurrEdtSession.SeqOfFirstSourcelineOnScreen = 1
            End If
        End If
        If Not RexxCmdActive Then Me.Cursor = System.Windows.Forms.Cursors.Arrow
        Logg("RunEdit end")
    End Function
    Private Sub ReadEditFile(ByVal Filename As String)
        '   read each line and store characteristics in the current sourcelist
        Dim ssd As SourceLine
        Dim nxCh As Integer, ch As Integer, vCh As Integer
        Dim iFile, lFile As Long
        Dim nSkip As Integer
reopenFile:
        Logg("InitEditFile start " & Filename)
        Try
            CurrEdtSession.EditFile = New FileStream(Filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            CurrEdtSession.FileUsesEndlineLF = False
            CurrEdtSession.FileUsesEndlineCR = False
            CurrEdtSession.fileHasUtf8BOM = False
            lFile = CurrEdtSession.EditFile.Length()
            ssd = New SourceLine
            ssd.SrcFileIx = "E"c
            ssd.SrcStart = 1
            nxCh = 0
            nSkip = 0 ' nr of bytes to ignore due to encoding characters
            For iFile = 1 To lFile
                ch = nxCh
                If iFile <= lFile Then ' read one char in ahead buffer. ch + nxCh form one unicode char
                    nxCh = CurrEdtSession.EditFile.ReadByte()
                Else
                    nxCh = 0 ' eof
                End If
                If iFile = 1 Then '  Create ahead buffer
                    ch = nxCh
                    If lFile > 1 Then
                        nxCh = CurrEdtSession.EditFile.ReadByte()
                    Else
                        nxCh = 0
                    End If
                End If
                If nSkip > 0 Then
                    nSkip -= 1
                Else
                    If iFile = 1 AndAlso CurrEdtSession.EncodingType = "8"c AndAlso ch = &HEF AndAlso nxCh = &HBB Then
                        CurrEdtSession.fileHasUtf8BOM = True ' skip optional utf8 BOM: 0xEF, 0xBB, 0xBF
                    ElseIf CurrEdtSession.fileHasUtf8BOM AndAlso iFile = 2 AndAlso nxCh = &HBF Then
                        ssd.SrcStart = 4 ' utf8 header found
                        nSkip = 1
                    ElseIf iFile = 1 AndAlso CurrEdtSession.EncodingType <> "A"c AndAlso ch = &HFF AndAlso nxCh = &HFE Then
                        CurrEdtSession.EncodingType = "U"c
                        ssd.SrcStart = 3
                        nSkip = 1
                    ElseIf CurrEdtSession.RecfmV Then
                        If (ch = 10 Or ch = 13) AndAlso (nxCh = 0 Or CurrEdtSession.EncodingType <> "U"c) Then
                            If ch = 13 Then CurrEdtSession.FileUsesEndlineLF = True
                            If ch = 10 Then CurrEdtSession.FileUsesEndlineCR = True
                            If (vCh = 10 And ch <> 10 Or vCh = 13 And ch <> 13) Then
                                ch = 0 ' Lf after CR or Cr after LF has no meaning
                                If CurrEdtSession.EncodingType = "U"c Then
                                    nSkip = 1
                                End If
                                ssd.SrcStart += (nSkip + 1)
                            Else
                                ssd.SrcLength = iFile - ssd.SrcStart
                                CurrEdtSession.SourceList.Add(ssd)
                                Logg("InitEditFile read " & CStr(CurrEdtSession.SourceList.Count) & " " & CStr(ssd.SrcStart) & " " & CStr(ssd.SrcLength))
                                If CurrEdtSession.EncodingType = "U"c Then nSkip = 1
                                ssd = New SourceLine
                                ssd.SrcFileIx = "E"c
                                ssd.SrcStart = iFile + 1 + nSkip
                            End If
                        End If
                    Else ' Recfm F
                        If iFile - ssd.SrcStart = CurrEdtSession.Lrecl Then
                            ssd.SrcLength = CurrEdtSession.Lrecl
                            CurrEdtSession.SourceList.Add(ssd)
                            Logg("InitEditFile read " & CStr(CurrEdtSession.SourceList.Count) & " " & CStr(ssd.SrcStart) & " " & CStr(ssd.SrcLength))
                            ssd = New SourceLine
                            ssd.SrcFileIx = "E"c
                            ssd.SrcStart = iFile
                        End If
                    End If
                    vCh = ch
                End If
            Next
            ' EditFile . Close() is done, when EdtSession is disposed off
            ssd.SrcLength = iFile - ssd.SrcStart
            CurrEdtSession.SourceList.Add(ssd)
            Logg("InitEditFile read " & CStr(CurrEdtSession.SourceList.Count) & " " & CStr(ssd.SrcStart) & " " & CStr(ssd.SrcLength))
        Catch E As Exception
            Logg("InitEditFile catch " & Filename)
            If File.Exists(Filename) Then
                Dim Msg As String = SysMsg(16) & " " & Filename
                Dim Response As Integer = MsgBox(Msg, MsgBoxStyle.RetryCancel, Msg)
                If Response = MsgBoxResult.Retry Then ' user requests RETRY.
                    GoTo reopenFile
                End If
            End If
            CurrEdtSession.EditFile = Nothing
        End Try
        ' prepare the ScrList to represent the editscreen
        CurrEdtSession.CursorDisplayLine = 2 ' linenr of cursor on screen on Top Of File
        CurrEdtSession.CursorDisplayColumn = 1 ' pos cursor on screen
        Logg("InitEditFile end ")
    End Sub
    Private Sub ProcessCommandOptions(ByVal CommandLine As String)
        Dim Words() As String
        Dim i, inop As Integer
        Dim profile, s As String
        Dim prevRexxCmdActive As Boolean
        Logg("ProcessCommandOptions start")
        prevRexxCmdActive = RexxCmdActive
        RexxCmdActive = True
        CurrEdtSession.CursorDisplayLine = 0
        If RunRexx("SYSTEM PROFILE", "") = 256 Then QuitPgm = True
        If Not QuitPgm Then
            Logg("ProcessCommandOptions  " & CommandLine)
            profile = "PROFILE"
            Words = Split(CommandLine.Trim.ToUpper(CultInf))
            For i = 1 To UBound(Words, 1) + 1 Step 2
                If Words(i - 1) = "PROFILE" Then
                    profile = Words(i)
                    inop = i
                End If
                If Words(i - 1) = "NOPROFILE" Then
                    profile = ""
                    inop = i
                End If
            Next
            If profile <> "" Then
                If inop > 0 Then
                    For i = inop + 1 To UBound(Words, 1)
                        Words(i - 2) = Words(i)
                    Next
                    Words(i - 2) = ""
                    Words(i - 1) = ""
                End If
                If RunRexx(profile, "") = 256 Then QuitPgm = True
            Else
                For i = inop To UBound(Words, 1)
                    Words(i - 1) = Words(i)
                Next
                Words(i - 1) = ""
            End If
            i = 0
            While i < UBound(Words, 1)
                If Words(i) = "VERIFY" Then ' rest of option string
                    s = "SET "
                    For i = i To UBound(Words, 1)
                        s = s & Words(i) & " "
                    Next
                    DoCmd1(s, False)
                ElseIf Words(i) = "CASE" Or Words(i) = "DISPLAY" Or Words(i) = "MSGLINE" Then
                    DoCmd1("SET " & Words(i) & " " & Words(i + 1) & " " & Words(i + 2), False)
                    i += 1
                Else
                    DoCmd1("SET " & Words(i) & " " & Words(i + 1), False)
                    If Words(i) = "ENCODE" Then
                        i = i
                    End If
                End If
                i += 2
            End While
        End If
        RexxCmdActive = prevRexxCmdActive
        Logg("ProcessCommandOptions end")
    End Sub

    Public Function SysMsg(ByVal i As Integer) As String
        Dim s As String
        s = "SYSMSG" & CStr(i)
        If SysMessages.Contains(s) Then
            SysMsg = CStr(SysMessages.Item(s))
        Else
            SysMsg = s & " not defined in 'system messages.txt'"
        End If
    End Function

    Private Function GetPointLine(PointName As String) As Integer
        Dim ret As Integer = 0
        Dim iSrPt As Integer = 0
        Dim found As Boolean = False
        For Each sl As SourceLine In CurrEdtSession.SourceList
            iSrPt += 1
            If sl.SrcPoint = PointName Then
                ret = iSrPt
                found = True
                Exit For
            End If
        Next
        If Not found Then rc = 2
        Return ret
    End Function
    Private Function ChaDelTarget(ByRef s As String, dsScr As ScreenLine) As Integer
        Dim OrgParmS As String = s
        If s.Length() > 1 AndAlso s(0) = "*"c AndAlso s(1) <> " " Then
            s = "* " & s.Substring(1)
        End If
        Dim w As String = NxtWordFromStr(s, "", " ", False)
        Dim ret As Integer = -dsScr.CurSrcNr
        If w = "*" Then
            ret += CurrEdtSession.SourceList.Count() + 1
        Else
            Dim Wrap As Boolean = CurrEdtSession.Wrap
            CurrEdtSession.Wrap = False
            Dim RetString() As String = Target(OrgParmS, dsScr, False, True).Split(" "c)
            If RetString(0).Length() > 0 Then
                ret += CInt(RetString(0))
            End If
            CurrEdtSession.Wrap = Wrap
        End If
        Return ret
    End Function
    Private Function Target(ByRef s As String, dsScr As ScreenLine, Optional ByVal TypeColon As Boolean = True, Optional ByVal TypeNumber As Boolean = False, Optional ByVal Backwards As Boolean = False) As String
        Dim OrgParmS As String = s
        Dim w As String = NxtWordFromStr(s, "", " ", False)
        If w.Length() = 1 AndAlso (w(0) = ":"c Or w(0) = "-"c Or w(0) = "+"c) Then  ' take 2nd part: LOCATE might have command specified
            w = w & NxtWordFromStr(s, "", " ", False)
        End If
        Dim pattern As String
        Dim reg As Regex = New Regex("")
        If TypeNumber Or TypeColon Then
            pattern = "^[0-9]+$"
            reg = New Regex(pattern)
        End If
        Dim res As String = " "
        Dim LocLine, LocPos As Integer
        If w.Length() > 0 Then
            If TypeColon AndAlso w(0) = ":"c Then
                If reg.IsMatch(w.Substring(1)) Then
                    res = w.Substring(1) & " " & CStr(CurrEdtSession.EditZoneLeft)
                Else
                    LocateString(w, CurrEdtSession.EditZoneLeft, CurrEdtSession.EditZoneRight, LocLine, LocPos)
                    If rc = 0 Then
                        res = CStr(LocLine) & " " & CStr(LocPos)
                    End If
                End If
            ElseIf TypeNumber AndAlso reg.IsMatch(w) Then
                res = CStr(CInt(w) + dsScr.CurSrcNr)
            ElseIf w(0) = "+"c Then
                If w.Length > 1 Then
                    If w(1) = "*" Then
                        res = CStr(CurrEdtSession.SourceList.Count() + 1) & " " & CStr(CurrEdtSession.EditZoneLeft) 'BOT of File
                    Else
                        res = CStr(dsScr.CurSrcNr + CInt(w.Substring(1))) & " " & CStr(CurrEdtSession.EditZoneLeft)
                    End If
                Else
                    res = CStr(dsScr.CurSrcNr + 1) & " " & CStr(CurrEdtSession.EditZoneLeft)
                End If
            ElseIf w(0) = "-"c Then
                If w.Length > 1 Then
                    If w(1) = "*" Then
                        res = "0 " & CStr(CurrEdtSession.EditZoneLeft) ' TOP of file
                    ElseIf w(1) < "0" Or w(1) > "9" Then ' not a -number
                        s = LocateString(OrgParmS.Substring(1), CurrEdtSession.EditZoneLeft, CurrEdtSession.EditZoneRight, LocLine, LocPos, True) ' backward locate command
                        If rc = 0 Then
                            res = CStr(LocLine) & " " & CStr(LocPos)
                        End If
                    Else
                        Try
                            res = CStr(dsScr.CurSrcNr - CInt(w.Substring(1))) & " " & CStr(CurrEdtSession.EditZoneLeft)
                        Catch ex As Exception
                            s = LocateString(OrgParmS, CurrEdtSession.EditZoneLeft, CurrEdtSession.EditZoneRight, LocLine, LocPos) ' remains command
                            If rc = 0 Then
                                res = CStr(LocLine) & " " & CStr(LocPos)
                            End If
                        End Try
                    End If
                Else
                    res = CStr(dsScr.CurSrcNr - 1) & " " & CStr(CurrEdtSession.EditZoneLeft)
                End If
            ElseIf w(0) = "."c Then
                res = GetPointLine(w.ToUpper(CultInf)) & " " & CStr(CurrEdtSession.EditZoneLeft)
            Else
                s = LocateString(OrgParmS, CurrEdtSession.EditZoneLeft, CurrEdtSession.EditZoneRight, LocLine, LocPos) ' remains command
                If rc = 0 Then
                    res = CStr(LocLine) & " " & CStr(LocPos)
                End If
            End If
        End If
        Return res
    End Function

    Private Function NxtNumFromStr(ByRef s As String, Optional ByVal Def As String = "") As Integer
        Dim u As String
        If Not IsNothing(Def) Then
            u = NxtWordFromStr(s, Def)
        Else
            u = NxtWordFromStr(s)
        End If
        If u.Length() > 0 Then
            NxtNumFromStr = CIntUserCor(u)
        Else
            NxtNumFromStr = 0
        End If
    End Function
    Public Function CIntUserCor(ByVal number As String) As Integer
        Dim j As Integer
opn:
        If Not DecimalSepPt Then
            j = InStr(1, number, ".")
            If j > 0 Then Mid(number, j, 1) = ","
        End If
        Try
            CIntUserCor = CInt(number)
        Catch e As Exception
            CIntUserCor = 0
            number = InputBox(SysMsg(5) + number, SysMsg(5), "0")
            If number.Length() = 0 Then
                rc = 16
            Else
                GoTo opn
            End If
        End Try
    End Function
    Public Function Numeric(ByVal number As String) As Boolean
        Dim j As Integer
        If Not DecimalSepPt Then
            j = InStr(1, number, ".")
            If j > 0 Then Mid(number, j, 1) = ","
        End If
        Try
            Dim n As Integer = CInt(number)
            Numeric = True
        Catch e As Exception
            Numeric = False
        End Try
    End Function
    Private Function Abbrev(ByVal inptStr As String, ByVal RefStr As String, Optional ByVal MinL As Integer = 1) As Boolean
        If inptStr.Length() >= MinL AndAlso inptStr.Length() <= RefStr.Length() AndAlso RefStr.Substring(0, inptStr.Length()) = inptStr Then
            Return True
        End If
        Return False
    End Function
    Private Function NumOrAst(ByRef CommandLine As String, ByVal DefSt As String, ByVal DefNum As Integer) As Integer
        Dim strng As String
        strng = CommandLine.TrimStart()
        If strng.Length() > 0 AndAlso strng.Substring(0, 1) = "*" Then
            If CommandLine.Length() > 1 Then
                CommandLine = CommandLine.Substring(1)
            Else
                CommandLine = ""
            End If
            NumOrAst = DefNum
        ElseIf strng.Length() > 0 AndAlso strng.Substring(0, 1).ToUpper = "B" Then
            NumOrAst = -1
            CommandLine = CommandLine.Substring(1)
        ElseIf strng.Length() > 0 AndAlso strng.Substring(0, 1).ToUpper = "E" Then
            NumOrAst = -2
            CommandLine = CommandLine.Substring(1)
        Else
            strng = NxtWordFromStr(CommandLine, DefSt)
            NumOrAst = CIntUserCor(strng)
        End If
    End Function
    Private Sub SetFormCaption()
        If Not RexxCmdActive Then
            Dim i As Integer, s As String
            Try
                i = InStrRev(CurrEdtSession.EditFileName, "\")
                If i = 0 Then
                    s = CurrEdtSession.EditFileName
                Else
                    s = Mid(CurrEdtSession.EditFileName, i + 1)
                End If
                Me.Text = s
            Catch ex As Exception
            End Try
        End If
    End Sub
    Friend Sub DoCmd(ByVal CommandLine As String, ByVal FromKb As Boolean)
        Dim orgCmd As String = ""
        'Debug.WriteLine("DoCmd start " & CStr(FromKb) & " " & CommandLine)
        Logg("DoCmd start " & CStr(FromKb) & " " & CommandLine)
        If FromKb Then
            SetFormCaption()
            Me.Cursor = System.Windows.Forms.Cursors.WaitCursor
        End If
#If Not DEBUG Then
        Try
#End If
        orgCmd = CommandLine
        Dim i As Integer
        If FormShown Then
            If Not CurrEdtSession.LinEndOff Then
                i = CommandLine.IndexOf(CurrEdtSession.LinEndChar)
                While i >= 0
                    DoCmd1(CommandLine.Substring(0, i), FromKb)
                    CommandLine = CommandLine.Substring(i + 1)
                    i = CommandLine.IndexOf(CurrEdtSession.LinEndChar)
                End While
            End If
        End If
        DoCmd1(CommandLine, FromKb)
#If Not DEBUG Then
        Catch E As Exception ' might be an error
            Logg("DoCmd catch " & E.GetBaseException.ToString)
            rc = 256
            If MsgBox("Internal program error while processing command " & orgCmd & ": " & Err.Description, MsgBoxStyle.OkCancel) = vbCancel Then
                QuitPgm = True
            End If
        End Try
#End If
        If FromKb And Not QuitPgm Then
            Me.Cursor = System.Windows.Forms.Cursors.Arrow
        End If
        Logg("DoCmd end ")
    End Sub
    Private Sub DoCmd1(ByVal CommandLine As String, ByVal FromKb As Boolean)
        Dim FirstWord, NextWord, strng, tmp As String, dsScr As ScreenLine = Nothing
        Dim i, j, l As Integer
        Logg("DoCmd1 start " & CommandLine)
        Debug.WriteLine("DoCmd start " & CStr(FromKb) & " " & CommandLine)
        If FormShown AndAlso Not CurrEdtSession.CaseMU Then
            CommandLine = CommandLine.ToUpper()
        End If
        strng = CommandLine.TrimStart()
        If strng.Length() > 8 Then
            If strng.Substring(0, 8) = "COMMAND " Then ' ignore COMMAND
                strng = strng.Substring(8).TrimStart()
                CommandLine = strng
            End If
        End If
        rc = 0
        If strng.Length() = 0 Then Exit Sub
        If strng.Length() >= 1 AndAlso strng.Substring(0, 1) = "/" Or strng.Substring(0, 1) = "," Or strng.Substring(0, 1) = "." Or strng.Substring(0, 1) = ":" Or strng.Substring(0, 1) = "+" Or strng.Substring(0, 1) = "-" Or strng.Substring(0, 1) = "<" Then
            CommandLine = "LOCATE " & strng
        End If
        If strng.Length() >= 2 AndAlso strng.Substring(0, 2) = "-/" Then ' Backward locate
            CommandLine = "LOCATE " & strng
        End If
        FirstWord = NxtWordFromStr(CommandLine)
        tmp = CommandLine
        NextWord = NxtWordFromStr(tmp)
        If Abbrev(FirstWord, "LEFT", 2) Then
            Dim vp As VerifyPair
            i = NxtNumFromStr(CommandLine, "1")
            For Each vp In CurrEdtSession.Verif
                If vp.VerFrom <= i Then i = vp.VerFrom - 1
            Next
            For Each vp In CurrEdtSession.Verif
                vp.VerFrom -= i
                vp.VerTo -= i
            Next
            RepaintAllScreenLines = True
        ElseIf Abbrev(FirstWord, "RIGHT", 2) Then
            Dim vp As VerifyPair
            i = NxtNumFromStr(CommandLine, "1")
            For Each vp In CurrEdtSession.Verif
                If (vp.VerFrom + i) > CurrEdtSession.Lrecl Then
                    i = CurrEdtSession.Lrecl - vp.VerFrom
                End If
            Next
            For Each vp In CurrEdtSession.Verif
                vp.VerFrom += i
                vp.VerTo += i
            Next
            RepaintAllScreenLines = True
        ElseIf Abbrev(FirstWord, "UP", 1) OrElse Abbrev(FirstWord, "DOWN", 2) Then
            NextWord = NxtWordFromStr(CommandLine)
            If NextWord = "*" Then
                dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                If FirstWord.Substring(0, 1) = "U" Then
                    i = dsScr.CurSrcNr + 1
                Else
                    i = CurrEdtSession.SourceList.Count - dsScr.CurSrcNr + 1
                End If
            Else
                i = NxtNumFromStr(NextWord, "1")
            End If
            If FirstWord.Substring(0, 1) = "U" Then
                While i > 0
                    ShiftScreenDown(CurrEdtSession.SrcOnScrn(1), 1, True)
                    i -= 1
                End While
            ElseIf FirstWord.Substring(0, 1) = "D" Then
                While i > 0
                    ShiftScreenUp(CurrEdtSession.SrcOnScrn(1), 1)
                    i -= 1
                End While
            End If
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
            If dsScr.CurLinType = "B" Or dsScr.CurLinType = "T" Then rc = 4
            Logg("rc U/D=" & CStr(rc))
            If rc > 0 Then
                rc = 4
            End If
        ElseIf Abbrev(FirstWord, "LOCATE", 1) Then
            strng = CommandLine.TrimStart()
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
            Dim retS = Target(CommandLine, dsScr, True)
            Dim retArr() As String = retS.Split(" "c)
            If rc = 0 Then
                strng = retArr(0)
                Dim ixPosFound As Integer = CInt(retArr(1))
                If strng.Length() > 0 Then
                    Dim LocLine As Integer = CInt(strng)
                    If LocLine = 0 Or LocLine = CurrEdtSession.SourceList.Count() + 1 Then rc = 1
                    If LocLine < 0 Then
                        LocLine = 0
                        rc = 5
                    End If
                    If LocLine > CurrEdtSession.SourceList.Count() + 1 Then
                        LocLine = CurrEdtSession.SourceList.Count() + 1
                        rc = 5
                    End If
                    MoveToSourceLine(LocLine)
                    If FromKb Then
                        CurrEdtSession.CursorDisplayLine = CurrEdtSession.CurLineNr 'cursor on current line
                    End If
                    If ixPosFound > 0 Then ' visible? (copy from MoveToSourcePos proc)
                        Dim p, ln, fac As Integer
                        Dim vp As VerifyPair, ok As Boolean
                        ok = False
                        For Each vp In CurrEdtSession.Verif
                            If vp.VerHex Then
                                fac = 2
                            Else
                                fac = 1
                            End If
                            If ixPosFound >= vp.VerFrom AndAlso ixPosFound <= vp.VerTo Then
                                If ixPosFound + p < vp.VerFrom + CharsOnScreen Then
                                    ok = True
                                    Exit For
                                End If
                            End If
                            ln = vp.VerTo - vp.VerFrom + 1
                            p = p + ln * fac
                        Next
                        If ok Then
                            If FromKb Then
                                MoveToSourcePos(ixPosFound)
                            End If
                        Else
                            DoCmd1("MSG " & SysMsg(21) & ", pos " & CStr(ixPosFound), False)
                        End If
                    ElseIf ixPosFound = 0 Then
                        rc = 4
                    End If
                    If Not CurrEdtSession.Stay And rc > 1 Then ' not found? to bottom line
                        Dim rrc As Integer = rc
                        MoveToSourceLine(CurrEdtSession.SourceList.Count() + 1)
                        rc = rrc
                    End If
                End If
            End If
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
            If dsScr.CurLinType = "B" Or dsScr.CurLinType = "T" Then rc = 4
            If FromKb AndAlso rc > 1 AndAlso retArr(0) <> "0" Then
                CurrEdtSession.Msgs.Add(SysMsg(17))
            Else
                If CommandLine.Length() > 0 Then
                    DoCmd1(CommandLine, FromKb)
                End If
            End If
            If CancelCmd Then Exit Sub
        ElseIf Abbrev(FirstWord, "CHANGE", 1) Then
            ChangeString(CommandLine)
            If FromKb And rc = 4 Then
                CurrEdtSession.Msgs.Add(SysMsg(22))
            End If
            If CancelCmd Then Exit Sub
        ElseIf Abbrev(FirstWord, "XEDIT", 1) Then ' Edit next file
            SaveAllModifiedLines()
            SaveSsd()
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
            CurrEdtSession.SeqOfFirstSourcelineOnScreen = dsScr.CurSrcNr ' save linenr of old file
            If CommandLine.Length() = 0 Then
                RunEdit("", "")
            Else
                If CommandLine = "?" Then
                    Dim sForm As New AskFileName()
                    sForm.ShowDialog() ' sets commandline
                    CommandLine = sForm.commandLine
                    InitAndRunEdit(CommandLine)
                Else
                    InitAndRunEdit(CommandLine)
                End If
            End If
            If CancelCmd Then Exit Sub
            CurrEdtSession.PrevEditLineScr = CurrEdtSession.CursorDisplayLine
            CurrEdtSession.PrevEditPosScr = CurrEdtSession.CursorDisplayColumn
            RestoreSsd()
            If ScrList Is Nothing Then
                ScrList = New Collection
                FillScreenBuffer(CurrEdtSession.SeqOfFirstSourcelineOnScreen, True) ' linenr of "new" file
            Else
                FillScreenBuffer(CurrEdtSession.SeqOfFirstSourcelineOnScreen, False) ' linenr of "old" file
            End If
        ElseIf Abbrev(FirstWord, "SAVE", 4) Then
            If CommandLine.Length() = 0 OrElse CommandLine = CurrEdtSession.EditFileName Then
                SaveFile(CurrEdtSession.EditFileName, False)
            Else
                SaveFile(CommandLine, True)
            End If
            If CancelCmd Then rc = 32
            If rc = 0 Then
                Dim AuSv As Boolean = False
                If CurrEdtSession.AutoSavNames.Count > 0 Then
                    AuSv = (CommandLine = CStr(CurrEdtSession.AutoSavNames.Item(CurrEdtSession.AutoSavNames.Count)))
                End If
                If Not AuSv Then ' not performing autosave?
                    For Each fs As String In CurrEdtSession.AutoSavNames
                        File.Delete(fs)
                    Next
                    CurrEdtSession.AutoSavNames.Clear()
                    CurrEdtSession.FileChanged = False
                    CurrEdtSession.chgCount = 0
                End If
            End If
            'If EdtSessions.Count() = 1 And FromKb Then
            '    WrkMaxWritePos = 0
            'End If
            If FromKb And rc = 0 Then
                CurrEdtSession.Msgs.Add(SysMsg(18))
            End If
        ElseIf Abbrev(FirstWord, "QUIT", 4) Then
            SaveAllModifiedLines()
            Dim Response As String = "QUIT"
            If FormShown AndAlso CurrEdtSession.FileChanged Then
                Response = "SAVE"
                Dim mt As Integer = Me.Top
                Dim pst As Integer = Me.WindowState
                If Me.WindowState <> FormWindowState.Normal And Me.Top < 90 Then
                    Me.WindowState = FormWindowState.Normal
                    Me.Top = 90
                End If
                MyMsg.Text = CurrEdtSession.EditFileName
                MyMsg.Label1.Text = SysMsg(3)
                MyMsg.CSave.Text = SysMsg(9)
                MyMsg.CQuit.Text = SysMsg(10)
                MyMsg.CCancel.Text = SysMsg(11)
                MyMsg.ShowDialog()
                If Me.Top <> mt Then
                    Me.Top = mt
                    Me.WindowState = pst
                End If
                If ResponseFromMsg = System.Windows.Forms.DialogResult.Yes Then
                    Response = "SAVE"
                ElseIf ResponseFromMsg = System.Windows.Forms.DialogResult.No Then
                    Response = "QUIT"
                ElseIf ResponseFromMsg = System.Windows.Forms.DialogResult.Cancel Then
                    Response = "CANCEL"
                End If
            End If
            If Response = "SAVE" Then
                DoCmd1("SAVE", False)
                If rc = 0 Then Response = "QUIT"
            End If
            If Response = "QUIT" Then
                If EdtSessions.Count() = 1 Then
                    If Not EditFileWrk Is Nothing Then
                        EditFileWrk.Dispose()
                        EditFileWrk.Close()
                        Try
                            Kill(WrkFileName)
                        Catch e As Exception
                        End Try
                    End If
                    QuitPgm = True
                    Rexx.CancRexx = True
                Else
                    If FormShown Then
                        If Not CurrEdtSession.EditFile Is Nothing Then
                            CurrEdtSession.EditFile.Dispose()
                            CurrEdtSession.EditFile.Close()
                        End If
                        CurrEdtSession = Nothing
                        EdtSessions.Remove(CurrEditSessionIx)
                        CurrEditSessionIx = CurrEditSessionIx - 1
                        If CurrEditSessionIx = 0 Then CurrEditSessionIx = EdtSessions.Count()
                        CurrEdtSession = DirectCast(EdtSessions.Item(CurrEditSessionIx), EdtSession)
                        SetFormCaption()
                        RepaintAllScreenLines = True
                        FillScreenBuffer(CurrEdtSession.SeqOfFirstSourcelineOnScreen, False) ' linenr of "new" file
                    End If
                End If
            Else
                rc = 32
            End If
        ElseIf Abbrev(FirstWord, "QQUIT", 5) Then
            If Not CurrEdtSession.EditFile Is Nothing Then
                CurrEdtSession.EditFile.Dispose()
                CurrEdtSession.EditFile.Close()
            End If
            CurrEdtSession = Nothing
            EdtSessions.Remove(CurrEditSessionIx)
            If EdtSessions.Count() = 0 Then
                If Not EditFileWrk Is Nothing Then
                    EditFileWrk.Dispose()
                    EditFileWrk.Close()
                    Try
                        File.Delete(WrkFileName)
                    Catch ex As Exception
                    End Try
                End If
                QuitPgm = True
                Rexx.CancRexx = True
                Exit Sub
            Else
                CurrEditSessionIx = CurrEditSessionIx - 1
                If CurrEditSessionIx = 0 Then CurrEditSessionIx = EdtSessions.Count()
                CurrEdtSession = DirectCast(EdtSessions.Item(CurrEditSessionIx), EdtSession)
                SetFormCaption()
                RepaintAllScreenLines = True
                FillScreenBuffer(CurrEdtSession.SeqOfFirstSourcelineOnScreen, False) ' linenr of "new" file
            End If
        ElseIf Abbrev(FirstWord, "RECALL", 3) Then ' reshows last used command
            If RecallPick > 0 Then
                Dim lcmd As Integer = CurrEdtSession.CmdLineNr
                If lcmd = -1 Then lcmd = LinesScreenVisible
                RecalledCmd = RecallPick - RecallNrCalled
                While RecalledCmd < 1
                    RecalledCmd += RecallIxMax
                End While
                RecallNrCalled += 1
                dsScr = DirectCast(ScrList.Item(lcmd), ScreenLine)
                dsScr.CurLinSrc = RecallCmds(RecalledCmd)
                dsScr.CurRepaint = True
            End If
        ElseIf FirstWord = "=" Then ' re-executes last used command
            If RecallPick > 0 Then DoCmd1(RecallCmds(RecallPick), False)
        ElseIf Abbrev(FirstWord, "INPUT") Then ' INSERT sourcetext ' inserts after current line
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
            If dsScr.CurLinType = "L"c Or dsScr.CurLinType = "X"c Or dsScr.CurLinType = "T"c Then
                Dim pCursor As Integer = 1
                If CommandLine.Length() = 0 Then
                    If FromKb Then
                        For i = 1 To dsScr.CurLinSrc.Length()
                            If dsScr.CurLinSrc.Substring(i - 1, 1) = " " Then
                                pCursor = i + 1
                            Else
                                Exit For
                            End If
                        Next
                    End If
                    CommandLine = Space(pCursor - 1)
                End If
                InsertOneLine(CommandLine)
                CurrEdtSession.CursorDisplayColumn = CInt(pCursor)
                DoCmd1("DOWN 1", False)
                RepaintFromLine((CurrEdtSession.CurLineNr))
            Else
                rc = 16
            End If
        ElseIf Abbrev(FirstWord, "DELETE", 3) Then ' DELETE n°lines/*
            Dim Xed As Boolean = False
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
            While rc = 0 AndAlso (dsScr.CurLinType = "T"c Or dsScr.CurLinType = "X"c)
                DoCmd1("DOWN 1", False)
                dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
            End While
            Dim xLne As Integer = dsScr.CurSrcNr
            If dsScr.CurLinType = "L"c Then
                j = CurrEdtSession.SourceList.Count
                Dim nLines As Integer
                If CommandLine.Length > 0 Then
                    nLines = ChaDelTarget(CommandLine, dsScr)
                    If nLines < 1 Then rc = 5
                Else
                    nLines = 1
                End If
                For i = 1 To nLines
                    nrCyclesEv += 1 : If nrCyclesEv > 5000 Then CallDoEvent()
                    If CancelCmd Then Exit Sub
                    dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                    If dsScr.CurLinType = "L"c Then
                        DeleteLine()
                    ElseIf dsScr.CurLinType = "X"c Then
                        Xed = True
                        DoCmd1("DOWN 1", False)
                    End If
                    If rc > 0 Then Exit For ' reached BOT
                Next
                If Xed Then FillScreenBuffer(xLne, False)
                RepaintFromLine(CurrEdtSession.CurLineNr)
            Else
                rc = 16
            End If
        ElseIf Abbrev(FirstWord, "REPLACE") Then
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
            If dsScr.CurLinType = "L"c Then
                If Not dsScr.CurSrcRead Then ReadSourceInScrBuf(dsScr)
                AddUndo(3, dsScr)
                dsScr.CurLinModified = True
                dsScr.CurRepaint = True
                dsScr.CurLinSrc = CommandLine
            Else
                rc = 16
            End If
        ElseIf Abbrev(FirstWord, "MCOPY", 3) Then
            Dim temp As String = "", ii As Integer
            If CurrEdtSession.mseSelTop = -1 Then ' on cmdline
                If CurrEdtSession.CmdLineNr = -1 Then
                    ii = LinesScreenVisible
                Else
                    ii = CurrEdtSession.CmdLineNr
                End If
                dsScr = DirectCast(ScrList.Item(ii), ScreenLine)
                temp = dsScr.CurLinSrc
            End If
            CopySelectedToClipboard()
            If CurrEdtSession.mseSelTop = -1 Then ' on cmdline
                dsScr = DirectCast(ScrList.Item(ii), ScreenLine)
                dsScr.CurLinSrc = temp
            End If
        ElseIf Abbrev(FirstWord, "MCUT", 3) Then
            If CurrEdtSession.mSelect Then
                CopySelectedToClipboard()
                SetmSelect(True)
                DeleteSelectedArea()
            End If
        ElseIf Abbrev(FirstWord, "MPASTE", 2) Then
            CopyFromClipboard()
        ElseIf Abbrev(FirstWord, "SELECTSET", 5) Then
            NextWord = NxtWordFromStr(CommandLine)
            If NextWord = "OFF" Then
                SetmSelect(False)
                RepaintSelectedLines()
            Else
                If NextWord = "ON" Then NextWord = NxtWordFromStr(CommandLine)
                CurrEdtSession.mseSelTop = CIntUserCor(NextWord)
                CurrEdtSession.mseSelLeft = CInt(NxtNumFromStr(CommandLine, "1"))
                Dim vp As VerifyPair = DirectCast(CurrEdtSession.Verif.Item(1), VerifyPair)
                CurrEdtSession.mseSelLeftVer = CurrEdtSession.mseSelLeft - CInt(vp.VerFrom + 1)
                CurrEdtSession.mseSelBot = NxtNumFromStr(CommandLine, "1")
                CurrEdtSession.mseSelRight = CInt(NxtNumFromStr(CommandLine, "1"))
                CurrEdtSession.mseSelRightVer = CurrEdtSession.mseSelRight - CInt(vp.VerFrom + 1)
                CurrEdtSession.mSelRctg = (NxtWordFromStr(CommandLine, "") = "RECT")
                SetmSelect(True)
                RepaintSelectedLines()
            End If
        ElseIf Abbrev(FirstWord, "TOP", 3) Then
            MoveToSourceLine(0)
            rc = 0
        ElseIf Abbrev(FirstWord, "BOTTOM", 1) Then
            MoveToSourceLine(CurrEdtSession.SourceList.Count() + 1)
            rc = 0
        ElseIf Abbrev(FirstWord, "SHOWEOL", 5) Then
            FirstWord = NxtWordFromStr(CommandLine, "ON")
            RepaintAllScreenLines = True
            CurrEdtSession.ShowEol = (FirstWord <> "OFF")
        ElseIf Abbrev(FirstWord, "UNDO", 4) Then
            UnDo()
        ElseIf Abbrev(FirstWord, "REDO", 4) Then
            ReDo()
        ElseIf Abbrev(FirstWord, "HELP") Then
            FirstWord = NxtWordFromStr(CommandLine)
            If FirstWord = "" Then
                DoCmd1("xedit " & ExecutablePath & "\help.txt", False)
            Else
                If File.Exists(ExecutablePath & "\help " & FirstWord & ".txt") Then
                    DoCmd1("xedit " & ExecutablePath & "\help " & FirstWord & ".txt", False)
                Else
                    rc = 4
                End If
            End If
        ElseIf Abbrev(FirstWord, "CURSOR", 3) Then ' CURSOR [SCREEN/FILE] COMMAND/linen° coln°
            NextWord = NxtWordFromStr(CommandLine, "SCREEN")
            If Abbrev(NextWord, "COMMAND") Then
                NextWord = "SCREEN"
                i = CurrEdtSession.CmdLineNr
                If i = -1 Then i = LinesScreenVisible
            Else
                i = NxtNumFromStr(CommandLine, "1")
            End If
            j = NxtNumFromStr(CommandLine, "1")
            If Abbrev(NextWord, "SCREEN") Then
                If i > 0 And i <= LinesScreenVisible Then
                    CurrEdtSession.CursorDisplayLine = CInt(i)
                    CurrEdtSession.CursorDisplayColumn = CInt(j)
                Else
                    rc = 16
                End If
            ElseIf Abbrev(NextWord, "FILE") Then
                l = LineIxOnScreen(i)
                If l > 0 Then
                    CurrEdtSession.CursorDisplayLine = CInt(l)
                    MoveToSourcePos(j)
                Else
                    If i > 0 And i <= CurrEdtSession.SourceList.Count Then
                        MoveToSourceLine(i)
                        CurrEdtSession.CursorDisplayLine = CurrEdtSession.CurLineNr
                        MoveToSourcePos(j)
                    Else
                        rc = 16
                    End If
                End If
            End If
        ElseIf Abbrev(FirstWord, "FIND", 1) Then
            FindString(CommandLine, True)
            If FromKb And rc = 4 Then
                CurrEdtSession.Msgs.Add(SysMsg(17))
            End If
            If CancelCmd Then Exit Sub
        ElseIf Abbrev(FirstWord, "FINDUP", 5) Then
            FindString(CommandLine, False)
            If FromKb And rc = 4 Then
                CurrEdtSession.Msgs.Add(SysMsg(17))
            End If
            If CancelCmd Then Exit Sub
        ElseIf Abbrev(FirstWord, "PLAY", 4) Then
            Dim MacroFile As String = NxtWordFromStr(CommandLine, "")
            If MacroFile.Length() < 5 OrElse Not MacroFile.ToUpper.EndsWith(".TXT") Then
                MacroFile += ".txt"
            End If
            If Not MacroFile.Contains("\") Then
                MacroFile = My.Application.Info.DirectoryPath + "\" + MacroFile
            End If
            SendKey(MacroFile, False)
            If CancelCmd Then Exit Sub
        ElseIf Abbrev(FirstWord, "READ", 4) Then ' retrieve contents of commandline
            Dim s As String = NxtWordFromStr(CommandLine, "")
            If s <> "CMDLINE" Then
                rc = 8
            Else
                Dim cmdln As Integer = CurrEdtSession.CmdLineNr
                If cmdln = -1 Then cmdln = ScrList.Count
                dsScr = DirectCast(ScrList.Item(cmdln), ScreenLine)
                Rexx.QStack.Add(dsScr.CurLinSrc)
            End If
        ElseIf Abbrev(FirstWord, "SET", 3) Then
            FirstWord = NxtWordFromStr(CommandLine)
            If Abbrev(FirstWord, "ZONE") Then ' ZONE leftcol° rightcol°/*
                CurrEdtSession.EditZoneLeft = CInt(NxtNumFromStr(CommandLine, "1"))
                CurrEdtSession.EditZoneRight = CInt(NumOrAst(CommandLine, "*", 32767))
            ElseIf Abbrev(FirstWord, "PENDING", 4) Then ' set pending BLOCK commandnaam / OFF"
                dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                FirstWord = NxtWordFromStr(CommandLine, "BLOCK")
                If FirstWord = "BLOCK" Or FirstWord = "ON" Then
                    CurrEdtSession.AddPendingCmd(dsScr.CurSrcNr, CommandLine)
                ElseIf FirstWord = "OFF" Then
                    j = CurrEdtSession.SearchPendingLnr(dsScr.CurSrcNr)
                    If j > 0 Then
                        CurrEdtSession.PendingCommands.Remove(j)
                    Else
                        j = CurrEdtSession.SearchLineLnr(dsScr.CurSrcNr)
                        If j > 0 Then
                            CurrEdtSession.LineCommands.Remove(j)
                        End If
                    End If
                End If
            ElseIf Abbrev(FirstWord, "TRUNC", 5) Then ' set TRUNC rightcol/*
                CurrEdtSession.Trunc = CInt(NumOrAst(CommandLine, "*", 32767))
            ElseIf Abbrev(FirstWord, "SCOPE", 3) Then ' set SCOPE ALL/DISPLAY
                Dim prvScope As Boolean = CurrEdtSession.ScopeAllDisplay
                CurrEdtSession.ScopeAllDisplay = Not Abbrev(NxtWordFromStr(CommandLine, "DISPLAY"), "DISPLAY")
                If prvScope <> CurrEdtSession.ScopeAllDisplay AndAlso CurrEdtSession.SessionInited Then
                    RefrScrBuf()
                End If
            ElseIf Abbrev(FirstWord, "RECFM", 4) Then ' set RECFM F/V/FB/VB
                CurrEdtSession.RecfmV = (NxtWordFromStr(CommandLine, "V").Substring(0, 1) = "V")
                If CurrEdtSession.RecfmV Then ' V type file must have at least one line separatot character
                    If Not CurrEdtSession.FileUsesEndlineCR AndAlso Not CurrEdtSession.FileUsesEndlineLF Then
                        CurrEdtSession.FileUsesEndlineLF = True
                        CurrEdtSession.FileUsesEndlineCR = True
                    End If
                End If
                If Not CurrEdtSession.RecfmV Then
                    CurrEdtSession.InsOvertype = False
                    If CurrEdtSession.EncodingType = "8"c Then CurrEdtSession.EncodingType = "A"c
                End If
            ElseIf Abbrev(FirstWord, "NULLS", 3) Then ' set NULLSA ON/OFF
                CurrEdtSession.Nulls = (NxtWordFromStr(CommandLine, "OFF") = "ON")
            ElseIf Abbrev(FirstWord, "LRECL", 2) Then ' set LRECL nnn
                CurrEdtSession.Lrecl = CInt(Math.Max(1, CIntUserCor(NxtWordFromStr(CommandLine, "32767"))))
            ElseIf Abbrev(FirstWord, "CURLINE", 4) Then  ' set CURLINE ON line
                FirstWord = NxtWordFromStr(CommandLine, "2")
                If FirstWord = "ON" Then FirstWord = NxtWordFromStr(CommandLine, "2")
                Dim nCl As Integer = CInt(CIntUserCor(FirstWord))
                If nCl < 2 Then nCl = 2
                If CurrEdtSession.SessionInited Then
                    If nCl > LinesScreenVisible - 2 Then nCl = LinesScreenVisible - 2
                    dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                End If
                CurrEdtSession.CurLineNr = nCl
                If CurrEdtSession.SessionInited Then
                    DoCmd1(":" & CStr(dsScr.CurSrcNr), False)
                End If
            ElseIf Abbrev(FirstWord, "CMDLINE", 3) Then 'AndAlso Not CurrEdtSession.SessionInited Then ' set CMDLINE ON/OFF/TOP/BOTTOM
                FirstWord = NxtWordFromStr(CommandLine, "TOP")
                If FirstWord = "OFF" Then MsgBox("CMDLINE OFF not implemented", MsgBoxStyle.OkOnly)
                CurrEdtSession.CmdLineNr = 2
                If Abbrev(FirstWord, "BOTTOM") Then CurrEdtSession.CmdLineNr = -1
                If CurrEdtSession.SessionInited Then
                    dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                    FillScreenBuffer(1, False)
                    DoCmd1(":" & CStr(dsScr.CurSrcNr), False)
                End If
            ElseIf Abbrev(FirstWord, "MSGLINE", 4) AndAlso Not CurrEdtSession.SessionInited Then ' set MSGLINE ON n [m] [Overlay]
                FirstWord = NxtWordFromStr(CommandLine, "ON")
                If FirstWord = "ON" Then FirstWord = NxtWordFromStr(CommandLine, "1")
                If FirstWord = "OFF" Then
                    MsgBox("MSGLINE OFF not implemented", MsgBoxStyle.OkOnly)
                Else
                    CurrEdtSession.MsgLineNrF = CInt(CIntUserCor(FirstWord))
                    NextWord = NxtWordFromStr(CommandLine, "1")
                    If Abbrev(NextWord, "OVERLAY", 2) Then
                        CurrEdtSession.MsgLineNrT = CurrEdtSession.MsgLineNrF
                    Else
                        CurrEdtSession.MsgLineNrT = CInt(CIntUserCor(NextWord))
                        NextWord = NxtWordFromStr(CommandLine, "")
                    End If
                    CurrEdtSession.MsgOverlay = Abbrev(NextWord, "OVERLAY", 2)
                End If
            ElseIf Abbrev(FirstWord, "WRAP", 2) Then ' set WRAP ON/OFF
                CurrEdtSession.Wrap = (NxtWordFromStr(CommandLine, "OFF") = "ON")
            ElseIf Abbrev(FirstWord, "HEX", 3) Then ' set HEX ON/OFF
                CurrEdtSession.HexM = (NxtWordFromStr(CommandLine, "OFF") = "ON")
            ElseIf Abbrev(FirstWord, "STAY", 4) Then ' set STAY ON/OFF
                CurrEdtSession.Stay = (NxtWordFromStr(CommandLine, "OFF") = "ON")
            ElseIf Abbrev(FirstWord, "TABS", 4) Then ' set TABS OFF / [ON] [ n n n n ]
                Dim ii As Integer
                NextWord = NxtWordFromStr(CommandLine, "OFF")
                If NextWord = "OFF" Then
                    CurrEdtSession.ExpTabs = False
                ElseIf NextWord = "ON" Then
                    CurrEdtSession.ExpTabs = True
                Else
                    CommandLine = NextWord + " " + CommandLine ' add first tab to list 
                    CurrEdtSession.ExpTabs = True
                End If
                If CommandLine.Trim <> "" Then ' else tabs remain the same
                    ii = 0
                    While CommandLine <> ""
                        ii += 1
                        CurrEdtSession.Tabs(ii) = CIntUserCor(NxtWordFromStr(CommandLine))
                    End While
                    While ii < CurrEdtSession.Tabs.Length - 1
                        ii += 1
                        CurrEdtSession.Tabs(ii) = 0
                    End While
                End If
                If FormShown Then RepaintAllScreenLines = True
            ElseIf Abbrev(FirstWord, "MSGMODE", 4) Then ' set MSGMODE ON/OFF
                CurrEdtSession.MsgMode = (NxtWordFromStr(CommandLine, "OFF") = "ON")
            ElseIf Abbrev(FirstWord, "VERIFY") Then ' set VERIFY [HEX] ON/OFF  [ranges]
                Dim vp As VerifyPair
                FirstWord = NxtWordFromStr(CommandLine, "")
                If FirstWord = "ON" Or FirstWord = "OFF" Then
                    CurrEdtSession.VerifyOn = (FirstWord = "ON")
                    FirstWord = NxtWordFromStr(CommandLine, "")
                End If
                CurrEdtSession.Verif.Clear()
                If FirstWord.Length() > 0 Then
                    While FirstWord.Length() > 0
                        vp = New VerifyPair
                        vp.VerHex = Abbrev(FirstWord, "HEX")
                        If vp.VerHex Then FirstWord = NxtWordFromStr(CommandLine, "")
                        vp.VerFrom = CIntUserCor(FirstWord)
                        vp.VerTo = NumOrAst(CommandLine, "*", CurrEdtSession.Trunc)
                        If vp.VerTo > CurrEdtSession.Lrecl Then
                            vp.VerTo = CurrEdtSession.Lrecl
                        End If
                        CurrEdtSession.Verif.Add(vp)
                        FirstWord = NxtWordFromStr(CommandLine, "")
                    End While
                Else
                    vp = New VerifyPair
                    vp.VerFrom = 1
                    vp.VerTo = CurrEdtSession.Trunc
                    vp.VerHex = False
                    CurrEdtSession.Verif.Add(vp)
                End If
                If CurrEdtSession.SessionInited Then RefrScrBuf()
                If CurrEdtSession.Verif.Count() > 1 Then SetmSelect(False) ' no mouse select
            ElseIf Abbrev(FirstWord, "CASE", 4) Then ' set CASE MIXED/UPPERCASE IGNORE/RESPECT
                CurrEdtSession.CaseMU = Abbrev(NxtWordFromStr(CommandLine, "MIXED"), "MIXED")
                CurrEdtSession.CaseIR = Abbrev(NxtWordFromStr(CommandLine, "IGNORE"), "IGNORE")
            ElseIf Abbrev(FirstWord, "LINEND", 5) Then ' set LINEND OFF / ON char
                CurrEdtSession.LinEndOff = (NxtWordFromStr(CommandLine, "ON") <> "ON")
                CurrEdtSession.LinEndChar = CChar(NxtWordFromStr(CommandLine, "#"))
            ElseIf Abbrev(FirstWord, "AUTOSAVE", 2) Then ' set AUTOSAVE OFF / n [fm]
                FirstWord = NxtWordFromStr(CommandLine, "OFF")
                If FirstWord = "OFF" Then
                    CurrEdtSession.EditAutoSave = 0
                Else
                    CurrEdtSession.EditAutoSave = CIntUserCor(FirstWord)
                End If
            ElseIf Abbrev(FirstWord, "SHADOW", 4) Then ' set SHADOW ON/OFF
                Dim prvShadow As Boolean = CurrEdtSession.Shadow
                CurrEdtSession.Shadow = (NxtWordFromStr(CommandLine, "OFF") = "ON")
                If prvShadow <> CurrEdtSession.Shadow Then
                    If CurrEdtSession.SessionInited Then RefrScrBuf()
                End If
            ElseIf Abbrev(FirstWord, "STREAM", 4) Then ' set STREAM ON/OFF
                CurrEdtSession.StreamsOn = (NxtWordFromStr(CommandLine, "OFF") = "ON")
            ElseIf Abbrev(FirstWord, "SELECT", 3) Then ' SET SELECT level n°lines/* 
                dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                Dim k As Integer = NxtNumFromStr(CommandLine, "0")
                j = NumOrAst(CommandLine, "*", CurrEdtSession.SourceList.Count() - dsScr.CurSrcNr + 1) + dsScr.CurSrcNr - 1
                If j > CurrEdtSession.SourceList.Count() Then
                    j = CurrEdtSession.SourceList.Count()
                    rc = 16
                End If
                For i = dsScr.CurSrcNr To j
                    If i > 0 Then
                        nrCyclesEv += 1 : If nrCyclesEv > 5000 Then CallDoEvent()
                        If CancelCmd Then Exit Sub
                        Dim ssd As SourceLine = DirectCast(CurrEdtSession.SourceList.Item(i), SourceLine)
                        ssd.SrcSelect = k
                    End If
                Next
                If CurrEdtSession.SessionInited Then
                    RefrScrBuf()
                End If
            ElseIf Abbrev(FirstWord, "DISPLAY", 4) Then ' set DISPLAY low high/*
                CurrEdtSession.EditDisplayMin = CInt(CIntUserCor(NxtWordFromStr(CommandLine, "0")))
                CurrEdtSession.EditDisplayMax = CInt(NumOrAst(CommandLine, "*", 32767))
                If CurrEdtSession.SessionInited Then RefrScrBuf()
            ElseIf Abbrev(FirstWord, "UNDO", 4) Then ' set UNDO n
                CurrEdtSession.UndoSet = CIntUserCor(NxtWordFromStr(CommandLine, "0"))
                CurrEdtSession.DoUnDo = (CurrEdtSession.UndoSet > 0)
            ElseIf Abbrev(FirstWord, "SYNONYM", 3) Then
                DefSynonym(CommandLine)
            ElseIf Abbrev(FirstWord, "TABCHAR", 5) Then
                Dim p As String = NxtWordFromStr(CommandLine, "OFF")
                If p = "OFF" Then
                    CurrEdtSession.TabChar = Nothing
                Else
                    CurrEdtSession.TabChar = p(0)
                End If
            ElseIf Abbrev(FirstWord, "FONTSIZE", 4) Then ' set FONTSIZE size
                FontSizeOnForm = Math.Max(5, CIntUserCor(NxtWordFromStr(CommandLine, "8")))
                MeFontSize = FontSizeOnForm
                RepaintAllScreenLines = True
                If FormShown AndAlso Not RexxCmdActive Then
                    Invalidate() ' to cancel bottom part after last full new line
                End If
            ElseIf Abbrev(FirstWord, "POINT", 3) Then ' set POINT .labelF
                Dim p As String = NxtWordFromStr(CommandLine, "")
                If p.Length() > 0 AndAlso p(0) = "." Then
                    Dim pt As Integer = GetPointLine(p.ToUpper(CultInf))
                    If pt > 0 Then
                        Dim ssdp As SourceLine = DirectCast(CurrEdtSession.SourceList.Item(pt), SourceLine)
                        ssdp.SrcPoint = ""
                    End If
                    dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                    Dim ssd As SourceLine = DirectCast(dsScr.CurLinSsd, SourceLine)
                    ssd.SrcPoint = p.ToUpper(CultInf)
                End If
            ElseIf Abbrev(FirstWord, "RESERVED", 5) Then ' set RESERVED n OFF / WHITE/BLUE/RED/GREEN/YELLOW NONE [no]high text
                i = CIntUserCor(NxtWordFromStr(CommandLine, "0"))
                Dim s As String = CommandLine
                FirstWord = NxtWordFromStr(CommandLine, "")
                If FirstWord = "OFF" Then
                    If CurrEdtSession.ReservedLines.Contains(CStr(i)) Then
                        CurrEdtSession.ReservedLines.Remove(CStr(i))
                    End If
                Else
                    While InStr(" WHITE BLUE RED GREEN YELLOW NONE HIGH NOHIGH ", " " & FirstWord & " ") > 0
                        s = CommandLine
                        FirstWord = NxtWordFromStr(CommandLine, "")
                    End While
                    CommandLine = s
                    CurrEdtSession.ReservedLines.Add(CommandLine, CStr(i))
                End If
            ElseIf Abbrev(FirstWord, "ENCODE", 3) Then ' set ENCODING UTF8 / ASCII / UNICODE
                Dim s As String = NxtWordFromStr(CommandLine, "UTF8")
                If s = "ASCII" Then
                    CurrEdtSession.EncodingType = "A"c
                ElseIf s = "UNICODE" Then
                    CurrEdtSession.EncodingType = "U"c
                Else
                    CurrEdtSession.EncodingType = "8"c
                End If
            ElseIf Abbrev(FirstWord, "REXXTRACE", 3) Then ' set REXXTRACE ON/OFF
                RexxTrace = Abbrev(NxtWordFromStr(CommandLine, "ON"), "ON")
            ElseIf Abbrev(FirstWord, "REXXPATH", 5) Then ' set REXXPATH path; path; path ...
                RexxPath = ""
                Dim RexxPathElements() As String = Split(CommandLine, ";"c)
                For i = 0 To RexxPathElements.Length() - 1
                    RexxPathElements(i) = RexxPathElements(i).Trim()
                    If RexxPathElements(i) <> "" Then
                        If RexxPathElements(i)(RexxPathElements(i).Length - 1) <> "\" Then
                            RexxPathElements(i) += "\"
                        End If
                        If RexxPathElements(i) = ".\" Then
                            RexxPathElements(i) = Path.GetDirectoryName(CurrEdtSession.EditFileName)
                        End If
                        RexxPath += RexxPathElements(i) + ";"
                    End If
                Next
                RexxPath += ExecutablePath + ";"
            ElseIf FirstWord = "COLOR" Then
                Dim s As String = NxtWordFromStr(CommandLine, "")
                Dim c As String = NxtWordFromStr(CommandLine, "")
                If c = "" Then
                    If (ColorDialog1.ShowDialog() = Windows.Forms.DialogResult.OK) Then
                        c = ConvertFromRbg(ColorDialog1.Color)
                        Call DoCmd1("MSG Selected color = " & c, False)
                    End If
                End If
                If Abbrev(s, "SELECT") Then
                    CurrEdtSession.color_select = ConvertToRbg(c)
                ElseIf Abbrev(s, "SELECTBG") Then
                    CurrEdtSession.color_selectbg = ConvertToRbg(c)
                ElseIf Abbrev(s, "COMMAND") Then
                    CurrEdtSession.color_command = ConvertToRbg(c)
                ElseIf Abbrev(s, "COMMANDBG") Then
                    CurrEdtSession.color_commandbg = ConvertToRbg(c)
                ElseIf Abbrev(s, "LINENUMBER") Then
                    CurrEdtSession.color_linenr = ConvertToRbg(c)
                ElseIf Abbrev(s, "CURLINENUMBER") Then
                    CurrEdtSession.color_curline = ConvertToRbg(c)
                ElseIf Abbrev(s, "LINENUMBERBG") Then
                    CurrEdtSession.color_linenrbg = ConvertToRbg(c)
                ElseIf Abbrev(s, "TEXT") Then
                    CurrEdtSession.color_text = ConvertToRbg(c)
                ElseIf Abbrev(s, "TEXTBG") Then
                    CurrEdtSession.color_textbg = ConvertToRbg(c)
                ElseIf Abbrev(s, "CURLINETEXT") Then
                    CurrEdtSession.color_curline = ConvertToRbg(c)
                ElseIf Abbrev(s, "CURSOR") Then
                    CurrEdtSession.color_cursor = ConvertToRbg(c)
                ElseIf Abbrev(s, "SCREEN") Then
                    CurrEdtSession.color_screen = ConvertToRbg(c)
                Else
                    rc = 16
                End If
            Else
                If CurrEdtSession.Settings.Contains(FirstWord) Then
                    CurrEdtSession.Settings.Remove(FirstWord)
                End If
                CurrEdtSession.Settings.Add(CommandLine, FirstWord)
            End If
        ElseIf Abbrev(FirstWord, "EXTRACT", 4) Then
            If RexxCmdActive Then
                Dim sep As Char
                CommandLine = CommandLine.Trim()
                sep = CChar(CommandLine.Substring(0, 1))
                CommandLine = Mid(CommandLine, 2)
                strng = NxtWordFromStr(CommandLine, "", sep)
                While strng <> ""
                    FirstWord = NxtWordFromStr(strng)
                    Select Case FirstWord
                        Case "CURSOR"
                            Dim vp As VerifyPair = Nothing, ps As Integer
                            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CursorDisplayLine), ScreenLine)
                            Dim vpNibble As Boolean
                            ps = EditPosSrc(vp, vpNibble, dsScr)
                            StoreExtract4("CURSOR", CStr(CurrEdtSession.CursorDisplayLine), CStr(CurrEdtSession.CursorDisplayColumn), CStr(dsScr.CurSrcNr), CStr(ps))
                        Case "SIZE"
                            StoreExtract1("SIZE", CStr(CurrEdtSession.SourceList.Count()))
                        Case "ZONE"
                            StoreExtract2("ZONE", CStr(CurrEdtSession.EditZoneLeft), CStr(CurrEdtSession.EditZoneRight))
                        Case "CURLINE"
                            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                            If dsScr.CurLinType = "L"c Then
                                If Not dsScr.CurSrcRead Then ReadSourceInScrBuf(dsScr)
                                StoreExtract3("CURLINE", "M", CStr(dsScr.CurSrcNr), dsScr.CurLinSrc)
                            ElseIf dsScr.CurLinType = "X"c Then
                                StoreExtract3("CURLINE", "M", "======", dsScr.CurLinSrc)
                            Else
                                StoreExtract3("CURLINE", "M", CStr(dsScr.CurSrcNr), "")
                            End If
                        Case "LINE"
                            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                            StoreExtract1("LINE", CStr(dsScr.CurSrcNr))
                        Case "LENGTH"
                            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                            StoreExtract1("LENGTH", CStr(dsScr.CurLinSrc.Trim().Length()))
                        Case "SCOPE"
                            StoreExtract1("SCOPE", CStr(IIf(CurrEdtSession.ScopeAllDisplay, "ALL", "DISPLAY")))
                        Case "SELECT"
                            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                            Dim k As Integer = 0
                            i = dsScr.CurSrcNr
                            If i > 0 Then
                                Dim ssd As SourceLine = DirectCast(CurrEdtSession.SourceList.Item(i), SourceLine)
                                k = ssd.SrcSelect
                            End If
                            Dim m As Integer = 0
                            For i = 1 To CurrEdtSession.SourceList.Count
                                Dim ssd As SourceLine = DirectCast(CurrEdtSession.SourceList.Item(i), SourceLine)
                                If ssd.SrcSelect > m Then
                                    m = ssd.SrcSelect
                                End If
                            Next
                            StoreExtract2("SELECT", k, m)
                        Case "WRAP"
                            StoreExtract1("WRAP", CStr(IIf(CurrEdtSession.Wrap, "ON", "OFF")))
                        Case "MSGMODE"
                            StoreExtract1("MSGMODE", CStr(IIf(CurrEdtSession.MsgMode, "ON", "OFF")))
                        Case "SHADOW"
                            StoreExtract1("SHADOW", CStr(IIf(CurrEdtSession.Shadow, "ON", "OFF")))
                        Case "STREAM"
                            StoreExtract1("STREAM", CStr(IIf(CurrEdtSession.StreamsOn, "ON", "OFF")))
                        Case "FULLFILENAME"
                            strng = CurrEdtSession.EditFileName
                            StoreExtract1("FULLFILENAME", strng)
                        Case "FTYPE"
                            strng = CurrEdtSession.EditFileName
                            i = strng.LastIndexOf("\")
                            If i > -1 Then
                                strng = strng.Substring(i + 1)
                            End If
                            i = InStrRev(strng, ".")
                            If i > 0 Then
                                strng = Mid(strng, i + 1)
                            Else
                                strng = ""
                            End If
                            StoreExtract1("FTYPE", strng.ToUpper())
                        Case "FNAME"
                            strng = CurrEdtSession.EditFileName
                            i = InStrRev(strng, ".")
                            If i > 0 Then
                                strng = Microsoft.VisualBasic.Left(strng, i - 1)
                                i = InStrRev(strng, "\")
                                If i > 0 Then strng = Mid(strng, i + 1)
                            End If
                            StoreExtract1("FNAME", strng)
                        Case "PENDING"
                            Dim n As String = strng
                            Dim mLnCmd As LineCommand = Nothing
                            FirstWord = NxtWordFromStr(n) ' skip BLOCK and OLDNAME
                            If FirstWord = "BLOCK" Then strng = n Else n = strng
                            FirstWord = NxtWordFromStr(n)
                            If FirstWord = "OLDNAME" Then strng = n Else n = strng
                            Dim pname As String = NxtWordFromStr(strng)
                            n = NxtWordFromStr(strng, ":1")
                            Dim fromE As Integer = NxtNumFromStr(Mid(n, 2), "1")
                            n = NxtWordFromStr(strng, "*")
                            If n = "*" Then n = ":9999999"
                            Dim toE As Integer = NxtNumFromStr(Mid(n, 2), "999999999")
                            Dim nLines As Integer = 0
                            j = CurrEdtSession.SearchPendingCm(pname, fromE, toE)
                            If j > 0 Then
                                mLnCmd = DirectCast(CurrEdtSession.PendingCommands.Item(j), LineCommand)
                            Else
                                j = CurrEdtSession.SearchLineCm(pname, fromE, toE)
                                If j > 0 Then
                                    mLnCmd = DirectCast(CurrEdtSession.LineCommands.Item(j), LineCommand)
                                End If
                            End If
                            If j > 0 Then
                                StoreExtract5("PENDING", CStr(mLnCmd.Linenr), pname, pname, "", CStr(IIf(mLnCmd.RepeatFactorPresent, CStr(mLnCmd.RepeatFactor), "")))
                            Else
                                StoreExtract0("PENDING")
                            End If
                        Case "FONTSIZE"
                            StoreExtract1("FONTSIZE", CStr(FontSizeOnForm))
                        Case "VERIFY"
                            Dim s As String, vp As VerifyPair
                            s = ""
                            For Each vp In CurrEdtSession.Verif
                                If vp.VerHex Then s = s & "H"
                                s = s & CStr(vp.VerFrom) & " " & CStr(vp.VerTo) & " "
                            Next
                            StoreExtract1("VERIFY", s.Trim())
                        Case "TABS"
                            Dim s As String
                            If Not CurrEdtSession.ExpTabs Then
                                s = "OFF"
                            Else
                                s = "ON"
                                For Each tab As Integer In CurrEdtSession.Tabs
                                    If tab > 0 Then s = s & " " & CStr(tab)
                                Next
                            End If
                            StoreExtract1("TABS", s.Trim())
                        Case "TERMINAL"
                            StoreExtract1("TERMINAL", "DISPLAY")
                        Case "SELECTSET"
                            If CurrEdtSession.mSelect Then
                                StoreExtract5("SELECTSET", CStr(CurrEdtSession.mseSelTop), CStr(CurrEdtSession.mseSelLeft), CStr(CurrEdtSession.mseSelBot), CStr(CurrEdtSession.mseSelRight), CStr(IIf(CurrEdtSession.mSelRctg, "1", "0")))
                            Else
                                StoreExtract5("SELECTSET", "-1", "-1", "-1", "-1", "0")
                            End If
                        Case "LRECL"
                            StoreExtract1("LRECL", CStr(CurrEdtSession.Lrecl))
                        Case "RING"
                            storeVarT("RING.0", CStr(EdtSessions.Count))
                            For i = 1 To EdtSessions.Count
                                storeVarT("RING." + CStr(i), DirectCast(EdtSessions.Item(i), EdtSession).EditFileName)
                            Next i
                        Case "CASE"
                            StoreExtract2("CASE", CStr(IIf(CurrEdtSession.CaseMU, "MIXED", "UPPPER")), CStr(IIf(CurrEdtSession.CaseIR, "IGNORE", "RESPECT")))
                        Case "LINEND"
                            StoreExtract2("LINEND", CStr(IIf(CurrEdtSession.LinEndOff, "OFF", "ON")), CStr(CurrEdtSession.LinEndChar))
                        Case "LSCREEN"
                            dsScr = DirectCast(ScrList.Item(CurrEdtSession.SrcOnScrn(1)), ScreenLine)
                            Dim vp As VerifyPair
                            vp = DirectCast(CurrEdtSession.Verif.Item(1), VerifyPair)
                            StoreExtract6("LSCREEN", CStr(LinesScreenVisible), CStr(CharsOnScreen), CStr(dsScr.CurSrcNr), CStr(vp.VerFrom), CStr(LinesScreenVisible), CStr(CharsOnScreen))
                        Case "TEMP"
                            StoreExtract1("TEMP", Path.GetTempPath())
                        Case "MSGLINE"
                            StoreExtract4("MSGLINE", "ON", CStr(CurrEdtSession.MsgLineNrF), CStr(CurrEdtSession.MsgLineNrT), CStr(IIf(CurrEdtSession.MsgOverlay, "OVERLAY", "")))
                        Case "TRUNC"
                            StoreExtract1("TRUNC", CStr(CurrEdtSession.Trunc))
                        Case "RECFM"
                            StoreExtract1("RECFM", CStr(IIf(CurrEdtSession.RecfmV, "V", "F")))
                        Case "HEX"
                            StoreExtract1("HEX", CStr(IIf(CurrEdtSession.HexM, "ON", "OFF")))
                        Case "AUTOSAVE"
                            StoreExtract1("AUTOSAVE", CStr(IIf(CurrEdtSession.EditAutoSave, "ON", "OFF")))
                        Case "POINT"
                            If strng.Length = 0 Then
                                dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                                Dim ssd As SourceLine = DirectCast(dsScr.CurLinSsd, SourceLine)
                                If ssd.SrcPoint.Length() = 0 Then
                                    StoreExtract0("POINT")
                                Else
                                    StoreExtract1("POINT", ssd.SrcPoint)
                                End If
                            Else ' POINT *
                                Dim iSrPt As Integer = 0
                                Dim nSrPt As Integer = 0
                                For Each sl As SourceLine In CurrEdtSession.SourceList
                                    iSrPt += 1
                                    If sl.SrcPoint.Length() > 0 Then
                                        nSrPt += 1
                                        storeVarT("POINT." & CStr(nSrPt), CStr(iSrPt) & " " & sl.SrcPoint)
                                    End If
                                Next
                                storeVarT("POINT.0", CStr(nSrPt))
                            End If
                        Case "APPLICATIONPATH"
                            StoreExtract1("APPLICATIONPATH", My.Application.Info.DirectoryPath)
                        Case "EXEPATH"
                            StoreExtract1("EXEPATH", ExecutablePath)
                        Case "REXXPATH"
                            StoreExtract1("REXXPATH", RexxPath)
                        Case Else
                            If CurrEdtSession.Settings.Contains(FirstWord) Then
                                StoreExtract1(FirstWord, CStr(CurrEdtSession.Settings.Item(FirstWord)))
                            Else
                                StoreExtract0(FirstWord)
                                rc = 16
                            End If
                    End Select
                    strng = NxtWordFromStr(CommandLine, "", sep)
                End While
            Else
                rc = 16
            End If
        ElseIf Abbrev(FirstWord, "NULLKEY", 7) Then
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
            dsScr.CurLinSrc = dsScr.CurLinSrc.TrimEnd
            dsScr.CurRepaint = True
        ElseIf Abbrev(FirstWord, "CMSG", 4) Then
            Dim cmdl As Integer = CurrEdtSession.CmdLineNr
            If cmdl = -1 Then cmdl = ScrList.Count
            dsScr = DirectCast(ScrList.Item(cmdl), ScreenLine)
            dsScr.CurLinSrc = CommandLine
            dsScr.CurRepaint = True
        ElseIf Abbrev(FirstWord, "MSG", 3) Or Abbrev(FirstWord, "EMSG", 4) Or Abbrev(FirstWord, "SMSG", 4) Then
            If CurrEdtSession.MsgMode Then
                If Not CurrEdtSession.Msgs.Contains(CommandLine) Then
                    CurrEdtSession.Msgs.Add(CommandLine, CommandLine)
                End If
            End If
        ElseIf FirstWord = "GLOBALV" Then
            Globalv(CommandLine)
        ElseIf FirstWord = "EXECIO" Then
            ExecIo(CommandLine)
        ElseIf FirstWord = "STATE" Then
            rc = cmsState(CommandLine)
        ElseIf Abbrev(FirstWord, "RENAME", 3) Then
            rc = cmsRename(CommandLine)
        ElseIf FirstWord = "ERASE" Then
            rc = cmsDelete(CommandLine)
        ElseIf FirstWord = "MAKEBUF" Then
            Makebufs.Add(Rexx.QStack.Count)
            rc = Makebufs.Count
        ElseIf FirstWord = "DROPBUF" Then
            DropBuf(CommandLine)
        ElseIf FirstWord = "PRINT" Then
            PrintFile(CommandLine)
        ElseIf Abbrev(FirstWord, "RESET", 3) Then
            For Each lc As LineCommand In CurrEdtSession.LineCommands
                l = LineIxOnScreen(lc.Linenr)
                If l > 0 Then
                    DirectCast(ScrList.Item(l), ScreenLine).CurRepaint = True
                End If
            Next
            CurrEdtSession.LineCommands.Clear()
        ElseIf Abbrev(FirstWord, "VMFCLEAR", 8) Then
            'VSCREENarea.Initialize()
            Array.Clear(VSCREENarea, 0, VSCREENarea.Length())
        ElseIf Abbrev(FirstWord, "VSCREEN", 4) Then
            VSCREENProcs(CommandLine)
        ElseIf Abbrev(FirstWord, "WINDOW", 6) Then
        Else
            Dim cmdOk As Boolean = False
            Dim Syn As Synonym, prevRexxCmdActive As Boolean, rrc As Integer
            For Each Syn In CurrEdtSession.Synonyms
                If Abbrev(FirstWord, Syn.SynAbbrev, Syn.SynLength) AndAlso FirstWord <> Syn.SynAbbrev Then
                    cmdOk = True
                    DoCmd1(Syn.SynCommand & " " & CommandLine, False)
                    If QuitPgm Then Exit Sub
                    Exit For
                End If
            Next Syn
            If Not cmdOk Then
                If FirstWord = "MACRO" Then
                    FirstWord = NxtWordFromStr(CommandLine)
                End If
                prevRexxCmdActive = RexxCmdActive
                RexxCmdActive = True
                rrc = RunRexx(FirstWord, CommandLine)
                RexxCmdActive = prevRexxCmdActive
                If QuitPgm Then
                    Exit Sub
                End If
                rc = rrc
            End If
        End If
        Debug.Print("cmd rc: " & CStr(rc))
        Logg("DoCmd1 end " & CStr(rc))
    End Sub
    Private Function RunRexx(ByVal cmd As String, ByVal parms As String) As Integer
        Logg("RunRexx starts " & cmd & " " & parms)
        MeTop = Me.Top ' Rexx InputBoxDialog is overlayed on this window
        MeLeft = Me.Left
        Dim rc As Integer
        If Rxs.CompileRexxScript(cmd) = 0 Then
            rc = Rxs.ExecuteRexxScript(parms)
        End If
        Logg("RunRexx ends " & CStr(rc))
        Return rc
    End Function
    Private Sub DoLineCmd()
        Dim mPendingCmd As LineCommand, lRep, i, j As Integer, mLnCmd, nLnCmd As LineCommand
        Dim dsScr As ScreenLine, RetLineNr As Integer, LineCommandsExist As Boolean
        LineCommandsExist = (CurrEdtSession.LineCommands.Count() > 0)
        If LineCommandsExist Then
            Logg("DoLineCmd start")
            Me.Cursor = System.Windows.Forms.Cursors.WaitCursor
        End If
        While CurrEdtSession.LineCommands.Count() > 0
            mLnCmd = DirectCast(CurrEdtSession.LineCommands.Item(1), LineCommand)
            CurrEdtSession.LineCommands.Remove(1)
            If mLnCmd.RepeatFactorPresent Then
                lRep = mLnCmd.RepeatFactor
            Else
                lRep = 1
            End If
            Logg("DoLineCmd " & mLnCmd.LinecmdText & " " & CStr(lRep))
            Select Case mLnCmd.LinecmdText.ToUpper(CultInf)
                Case "I" ' insert empty lines
                    If mLnCmd.Linenr >= 0 AndAlso mLnCmd.Linenr <= CurrEdtSession.SourceList.Count() Then
                        dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                        RetLineNr = dsScr.CurSrcNr
                        For i = 1 To lRep
                            MoveToSourceLine(mLnCmd.Linenr + i - 1) ' make line to insert after the current line
                            DoCmd1("INPUT", False)
                            dsScr.CurLinModified = False
                        Next
                        MoveToSourceLine(RetLineNr) ' move to actual current line
                        dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                        i = mLnCmd.Linenr - dsScr.CurSrcNr + LineIxOnScreen(dsScr.CurSrcNr) + 1
                        If i <= LinesScreenVisible Then
                            CurrEdtSession.CursorDisplayLine = CInt(i)
                        End If
                    Else
                        rc = 16
                    End If
                Case "D" ' delete n lines
                    If mLnCmd.Linenr > 0 And mLnCmd.Linenr <= CurrEdtSession.SourceList.Count() Then
                        dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                        RetLineNr = dsScr.CurSrcNr
                        MoveToSourceLine(mLnCmd.Linenr) ' make line to delete the current line
                        DoCmd1("DELETE " & CStr(lRep), False)
                        MoveToSourceLine(RetLineNr) ' move to actual current line
                    Else
                        rc = 16
                    End If
                Case "X" ' exclude n lines from view 0
                    If mLnCmd.Linenr > 0 And mLnCmd.Linenr <= CurrEdtSession.SourceList.Count Then
                        HideLines(mLnCmd.Linenr, lRep)
                    Else
                        rc = 16
                    End If
                Case "S" ' select excluded lines for view 0
                    If mLnCmd.Linenr > 0 And mLnCmd.Linenr <= CurrEdtSession.SourceList.Count Then
                        UnHideLines(mLnCmd.Linenr)
                    Else
                        rc = 16
                    End If
                Case ">" ' shift right over lRep pos
                    dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                    RetLineNr = dsScr.CurSrcNr
                    MoveToSourceLine(mLnCmd.Linenr) ' make line the current line
                    dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                    If dsScr.CurLinType = "L"c Then
                        If Not dsScr.CurSrcRead Then ReadSourceInScrBuf(dsScr)
                        Dim nc As Integer = lRep
                        If nc > dsScr.CurLinSrc.Length Then nc = dsScr.CurLinSrc.Length
                        DoCmd1("R " + StrDup(lRep, " "c) + dsScr.CurLinSrc, False)
                        MoveToSourceLine(RetLineNr) ' move to actual current line
                    Else
                        rc = 16
                    End If
                Case ">>" ' shift right over lRep pos
                    dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                    RetLineNr = dsScr.CurSrcNr
                    j = 0
                    MoveToSourceLine(mLnCmd.Linenr) ' make line the current line
                    For Each nLnCmd In CurrEdtSession.LineCommands
                        j = j + 1
                        If Not mLnCmd.RepeatFactorPresent And nLnCmd.RepeatFactorPresent Then
                            lRep = nLnCmd.RepeatFactor
                        End If
                        If nLnCmd.LinecmdText = ">>" Then
                            For i = mLnCmd.Linenr To nLnCmd.Linenr
                                MoveToSourceLine(mLnCmd.Linenr + i - mLnCmd.Linenr)
                                dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                                If dsScr.CurLinType = "L"c Then
                                    If Not dsScr.CurSrcRead Then ReadSourceInScrBuf(dsScr)
                                    Dim nc As Integer = lRep
                                    If nc > dsScr.CurLinSrc.Length Then nc = dsScr.CurLinSrc.Length
                                    DoCmd1("R " + StrDup(lRep, " "c) + dsScr.CurLinSrc, False)
                                End If
                            Next
                            CurrEdtSession.LineCommands.Remove(j)
                            Exit For
                        End If
                    Next
                    MoveToSourceLine(RetLineNr) ' move to actual current line
                Case "<" ' shift left over lRep pos
                    dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                    RetLineNr = dsScr.CurSrcNr
                    MoveToSourceLine(mLnCmd.Linenr) ' make line the current line
                    dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                    If dsScr.CurLinType = "L"c Then
                        If Not dsScr.CurSrcRead Then ReadSourceInScrBuf(dsScr)
                        Dim nc As Integer = lRep
                        If nc > dsScr.CurLinSrc.Length Then nc = dsScr.CurLinSrc.Length
                        DoCmd1("R " + dsScr.CurLinSrc.Substring(nc), False)
                    Else
                        rc = 16
                    End If
                Case "<<" ' shift left over lRep pos
                    dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                    RetLineNr = dsScr.CurSrcNr
                    j = 0
                    MoveToSourceLine(mLnCmd.Linenr) ' make line the current line
                    Dim found As Boolean = False
                    For Each nLnCmd In CurrEdtSession.LineCommands
                        j = j + 1
                        If Not mLnCmd.RepeatFactorPresent And nLnCmd.RepeatFactorPresent Then
                            lRep = nLnCmd.RepeatFactor
                        End If
                        If nLnCmd.LinecmdText = "<<" Then
                            found = True
                            Dim retc As Integer = 0
                            For i = mLnCmd.Linenr To nLnCmd.Linenr
                                MoveToSourceLine(mLnCmd.Linenr + i - mLnCmd.Linenr)
                                dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                                If dsScr.CurLinType = "L"c Then
                                    If Not dsScr.CurSrcRead Then ReadSourceInScrBuf(dsScr)
                                    Dim nc As Integer = lRep
                                    If nc > dsScr.CurLinSrc.Length Then nc = dsScr.CurLinSrc.Length
                                    If dsScr.CurLinSrc.Substring(0, nc).Trim <> "" Then
                                        retc = 4
                                    End If
                                    DoCmd1("R " + dsScr.CurLinSrc.Substring(nc), False)
                                End If
                            Next
                            CurrEdtSession.LineCommands.Remove(j)
                            If retc <> 0 Then DoCmd1("MSG " + SysMsg(23), False)
                            Exit For
                        End If
                    Next
                    If Not found Then
                        CurrEdtSession.PendingCommands.Add(mLnCmd)
                    End If
                    MoveToSourceLine(RetLineNr) ' move to actual current line
                Case """" ' repeat lRep times
                    dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                    RetLineNr = dsScr.CurSrcNr
                    MoveToSourceLine(mLnCmd.Linenr) ' make line the current line
                    dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                    If dsScr.CurLinType = "L"c Then
                        If Not dsScr.CurSrcRead Then ReadSourceInScrBuf(dsScr)
                        Dim pLinend As Boolean = CurrEdtSession.LinEndOff
                        Dim s As String = dsScr.CurLinSrc
                        CurrEdtSession.LinEndOff = True
                        For i = 1 To lRep
                            DoCmd1("INPUT " & s, False)
                        Next
                        CurrEdtSession.LinEndOff = pLinend
                        MoveToSourceLine(RetLineNr) ' move to actual current line
                    Else
                        rc = 16
                    End If
                Case """""" ' repeat lRep times
                    dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                    RetLineNr = dsScr.CurSrcNr
                    j = 0
                    MoveToSourceLine(mLnCmd.Linenr) ' make line the current line
                    For Each nLnCmd In CurrEdtSession.LineCommands
                        j = j + 1
                        If Not mLnCmd.RepeatFactorPresent And nLnCmd.RepeatFactorPresent Then
                            lRep = nLnCmd.RepeatFactor
                        End If
                        If nLnCmd.LinecmdText = """""" Then
                            Dim LinesRep As New Collection
                            For i = mLnCmd.Linenr To nLnCmd.Linenr
                                MoveToSourceLine(mLnCmd.Linenr + i - mLnCmd.Linenr)
                                dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                                If dsScr.CurLinType = "L"c Then
                                    If Not dsScr.CurSrcRead Then ReadSourceInScrBuf(dsScr)
                                    LinesRep.Add(dsScr.CurLinSrc)
                                End If
                            Next
                            Dim pLinend As Boolean = CurrEdtSession.LinEndOff
                            CurrEdtSession.LinEndOff = True
                            For k As Integer = 1 To lRep
                                For i = mLnCmd.Linenr To nLnCmd.Linenr
                                    DoCmd1("INPUT " & CStr(LinesRep.Item(i - mLnCmd.Linenr + 1)), False)
                                Next
                            Next
                            CurrEdtSession.LinEndOff = pLinend
                            CurrEdtSession.LineCommands.Remove(j)
                            Exit For
                        End If
                    Next
                    MoveToSourceLine(RetLineNr) ' move to actual current line
                Case "/" ' make current line
                    MoveToSourceLine(mLnCmd.Linenr) ' make line the current line
                Case Else
                    Dim PointName As String = mLnCmd.LinecmdText.Trim()
                    If PointName.Length() > 0 AndAlso PointName(0) = "." Then
                        dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                        RetLineNr = dsScr.CurSrcNr
                        MoveToSourceLine(mLnCmd.Linenr) ' make line the current line
                        dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                        dsScr.CurRepaint = True
                        Dim ssd As SourceLine
                        Dim pt As Integer = GetPointLine(PointName.ToUpper(CultInf))
                        If pt > 0 Then
                            ssd = DirectCast(CurrEdtSession.SourceList.Item(pt), SourceLine)
                            ssd.SrcPoint = ""
                        End If
                        ssd = DirectCast(CurrEdtSession.SourceList.Item(mLnCmd.Linenr), SourceLine)
                        ssd.SrcPoint = PointName.ToUpper(CultInf)
                        MoveToSourceLine(RetLineNr) ' move to actual current line
                    Else
                        Dim lCmd As String = "PREFIX LINE " & CStr(mLnCmd.Linenr)
                        If mLnCmd.RepeatFactorPresent Then lCmd = lCmd & " " & CStr(mLnCmd.RepeatFactor)
                        Dim prevRexxCmdActive As Boolean = RexxCmdActive
                        RexxCmdActive = True
                        Dim rrc As Integer = RunRexx(mLnCmd.LinecmdText, lCmd)
                        RexxCmdActive = prevRexxCmdActive
                        If QuitPgm Then Exit Sub
                        rc = rrc
                    End If
            End Select
        End While
        'now move pending commands to line commands for next screen
        While CurrEdtSession.PendingCommands.Count() > 0
            mPendingCmd = DirectCast(CurrEdtSession.PendingCommands.Item(1), LineCommand)
            CurrEdtSession.PendingCommands.Remove(1)
            If mPendingCmd.RepeatFactorPresent Then
                CurrEdtSession.AddLineCmd(mPendingCmd.Linenr, mPendingCmd.LinecmdText & CStr(mPendingCmd.RepeatFactor))
            Else
                CurrEdtSession.AddLineCmd(mPendingCmd.Linenr, mPendingCmd.LinecmdText)
            End If
        End While
        If LineCommandsExist Then
            Logg("DoLineCmd end")
            Me.Cursor = System.Windows.Forms.Cursors.Default
        End If
    End Sub
    Private Function cmsDelete(ByVal cmd As String) As Integer
        Dim Fo As String
        Logg("cmsDelete start")
        cmd = cmd.Trim()
        If VB.Left(cmd, 1) = "'" Or VB.Left(cmd, 1) = """" Then
            Fo = NxtWordFromStr(cmd, "", VB.Left(cmd, 1))
        Else
            Fo = NxtWordFromStr(cmd)
        End If
        If File.Exists(Fo) Then
            Try
                File.Delete(Fo)
                cmsDelete = 0
            Catch ex As Exception
                Logg("cmsDelete catch " & CStr(ex.Message))
                cmsDelete = 4
            End Try
        Else
            cmsDelete = 4
        End If
        Logg("cmsDelete end " & CStr(cmsDelete))
    End Function
    Private Function cmsRename(ByVal cmd As String) As Integer
        Dim Fo, fn As String
        Logg("cmsRename start")
        cmd = cmd.Trim()
        If VB.Left(cmd, 1) = "'" Or VB.Left(cmd, 1) = """" Then
            Fo = NxtWordFromStr(cmd, "", VB.Left(cmd, 1))
        Else
            Fo = NxtWordFromStr(cmd)
        End If
        cmd = cmd.Trim()
        If VB.Left(cmd, 1) = "'" Or VB.Left(cmd, 1) = """" Then
            fn = NxtWordFromStr(cmd, "", VB.Left(cmd, 1))
        Else
            fn = NxtWordFromStr(cmd)
        End If
        RenameFile(Fo, fn)
        cmsRename = rc
        Logg("cmsRename end " & CStr(rc))
    End Function
    Private Sub DropBuf(ByVal cmd As String)
        If cmd = "" Then
            cmd = "1"
        End If
        Dim nb As Integer = Convert.ToInt32(cmd)
        For i As Integer = nb To Makebufs.Count
            Dim pq As Integer = Convert.ToInt32(Makebufs.Item(Makebufs.Count))
            Makebufs.Remove(Makebufs.Count)
            Do While Rexx.QStack.Count > pq
                Rexx.QStack.Remove(1)
            Loop
        Next
    End Sub
    Private Sub Globalv(ByVal cmd As String)
        Dim fnd, w, selectName As String
        Static fiTime As Boolean
        If Not fiTime Then
            fiTime = True
            Try
                Kill(Path.GetTempPath() & "\TEMP.GLOBALV")
            Catch ex As Exception
                Logg("Globalv catch " & CStr(ex.Message))
            End Try
        End If
        w = NxtWordFromStr(cmd)
        If w = "SELECT" Then
            selectName = NxtWordFromStr(cmd)
            w = NxtWordFromStr(cmd)
        Else
            selectName = " "
        End If
        selectName = VB.Left(selectName & "        ", 8)
        fnd = VB.Left(cmd.ToUpper(CultInf) & "                ", 16)
        If w = "GET" Then
            GlobalvGet(selectName, fnd)
        Else
            GlobalvPut(w, selectName, fnd)
        End If
    End Sub
    Private Sub GlobalvGet(ByVal selectName As String, ByVal fnd As String)
        Dim found, exi As Boolean
        Dim s, dbx, got As String
        Dim cvr As New DefVariable
        Dim i, k As Integer
        Dim n As String = "", en As String = ""
        got = ""
        found = False
        i = 1
        ' MsgBox("GET: " + fnd + ":" + selectName)
        While i <= 3 And Not found
            If i = 1 Then
                dbx = Path.GetTempPath() & "TEMP.GLOBALV"
            ElseIf i = 2 Then
                dbx = Path.GetTempPath() & "SESSION.GLOBALV"
            Else
                dbx = Path.GetTempPath() & "LASTING.GLOBALV"
            End If
            exi = File.Exists(dbx)
            If exi Then
                FileOpen(1, dbx, OpenMode.Input)
                While Not EOF(1) 'And Not found until last
                    s = LineInput(1)
                    If Mid(s, 10, 16) = fnd And Mid(s, 1, 8) = selectName Then
                        got = s
                        found = True
                    End If
                End While
                FileClose(1)
            End If
            i = i + 1
        End While
        i = Rxs.SourceNameIndexPosition(fnd.Trim(), Rexx.tpSymbol.tpUnknown, cvr)
        If found Then
            s = Mid(got, 27)
        Else
            s = ""
        End If
        Rxs.StoreVar(i, s, k, en, n) ' new value
        If Not found Then rc = 4
    End Sub
    Private Sub GlobalvPut(ByVal w As String, ByVal selectName As String, ByVal fnd As String)
        Dim exi As Boolean
        Dim s, Variab, Val, dbx As String
        Dim cvr As DefVariable = Nothing
        Dim i As Integer
        Dim n As String = "", en As String = "", m As String
        Variab = NxtWordFromStr(fnd).PadRight(16)
        Val = fnd
        dbx = "TEMP"
        If w = "PUTS" Then
            dbx = "SESSION"
        ElseIf w = "PUTP" Then
            dbx = "LASTING"
        End If
        dbx = Path.GetTempPath() & dbx & ".GLOBALV"
        exi = File.Exists(dbx)
        If exi Then
            If Not WritableFile(dbx, "GLOBALV") Then Exit Sub
            FileOpen(1, dbx, OpenMode.Append)
        Else
            FileOpen(1, dbx, OpenMode.Output)
        End If
        i = Rxs.SourceNameIndexPosition(Variab.Trim(), Rexx.tpSymbol.tpUnknown, cvr)
        m = Rxs.GetVar(i, en, n)
        s = selectName & " " & Variab & " " & m
        PrintLine(1, s)
        FileClose(1)
        'RenameFile(dbx & ".$$$", dbx)
    End Sub
    Private Sub GlobalvPutold(ByVal w As String, ByVal selectName As String, ByVal fnd As String)
        Dim exi As Boolean
        Dim s, Variab, Val, dbx, got As String
        Dim cvr As DefVariable = Nothing
        Dim i As Integer
        Dim n As String = "", en As String = "", m As String
        Variab = NxtWordFromStr(fnd).PadRight(16)
        Val = fnd
        dbx = "TEMP"
        If w = "PUTS" Then
            dbx = "SESSION"
        ElseIf w = "PUTP" Then
            dbx = "LASTING"
        End If
        dbx = Path.GetTempPath() & dbx & ".GLOBALV"
        exi = File.Exists(dbx)
        If exi Then
            If Not WritableFile(dbx, "GLOBALV") Then Exit Sub
            FileOpen(1, dbx, OpenMode.Input)
        End If
        FileOpen(2, dbx & ".$$$", OpenMode.Output)
        If exi Then
            While Not EOF(1)
                s = LineInput(1)
                If Mid(s, 10, 16) = Variab And Mid(s, 1, 8) = selectName Then
                    got = s
                Else
                    PrintLine(2, s)
                End If
            End While
            FileClose(1)
        End If
        i = Rxs.SourceNameIndexPosition(Variab.Trim(), Rexx.tpSymbol.tpUnknown, cvr)
        m = Rxs.GetVar(i, en, n)
        s = selectName & " " & Variab & " " & m
        PrintLine(2, s)
        FileClose(2)
        RenameFile(dbx & ".$$$", dbx)
    End Sub
    Private Function FromQueueStack() As String
        Dim s As Object
        s = Rexx.QStack.Item(Rexx.QStack.Count())
        Rexx.QStack.Remove(Rexx.QStack.Count())
        FromQueueStack = CStr(s)
    End Function
    Private Sub ExecIo(ByVal cmd As String)
        Dim fn, LinesToDoS, Ecmd, s As String
        Dim StartNr, CurRecNr, LinesToReadWrite As Integer
        Dim exi As Boolean
        Dim q, RecFm As String
        Dim Lrecl, i As Integer
        Dim en As String = "", src As String = "", vr As String = "", n As String = "", Opt As String = ""
        Dim cvr As New DefVariable
        Dim k, Copy1st, CopyLast, nr As Integer
        RecFm = "V"
        Lrecl = 32767
        i = InStr(cmd, "(")
        If i > 0 Then
            Opt = Mid(cmd, i + 1)
            cmd = VB.Left(cmd, i - 1)
        End If
        src = "Q" ' data from queue
        LinesToDoS = NxtWordFromStr(cmd)
        If LinesToDoS = "*" Then
            LinesToReadWrite = 9999999
        Else
            LinesToReadWrite = CIntUserCor(LinesToDoS)
            If LinesToReadWrite = 0 Then LinesToReadWrite = 9999999 ' after last
        End If
        Ecmd = NxtWordFromStr(cmd)
        cmd = cmd.TrimStart()
        If VB.Left(cmd, 1) = "'" Or VB.Left(cmd, 1) = """" Then
            fn = NxtWordFromStr(cmd, "", VB.Left(cmd, 1))
        Else
            fn = NxtWordFromStr(cmd)
        End If
        StartNr = NxtNumFromStr(cmd, CStr(0))
        RecFm = VB.Left(NxtWordFromStr(cmd, "V"), 1).ToUpper(CultInf)
        Lrecl = NxtNumFromStr(cmd, CStr(32767))
        Opt = Opt.ToUpper(CultInf).Trim()
        If VB.Left(Opt, 4) = "VAR " Then
            src = "V"
            vr = Mid(Opt, 5).Trim()
        End If
        If VB.Left(Opt, 5) = "STEM " Then
            src = "S"
            vr = Mid(Opt, 6).Trim()
        End If
        Dim counter As Integer
        If Ecmd = "DISKW" Then ' EXECIO n° FN startn°
            If Not WritableFile(fn, "EXECIO") Then Exit Sub
            If StartNr = 0 Then
                Copy1st = 0
                CopyLast = 0
            Else
                Copy1st = 1
                CopyLast = StartNr - 1
            End If
            exi = File.Exists(fn)
            If StartNr = 0 Then
                FileOpen(2, fn, OpenMode.Append)
            Else
                FileOpen(2, fn & ".$$$", OpenMode.Output)
            End If
            CurRecNr = 1
            If exi And StartNr > 0 Then
                FileOpen(1, fn, OpenMode.Input)
                While Not EOF(1) And (CurRecNr >= Copy1st And CurRecNr <= CopyLast)
                    s = LineInput(1)
                    PrintLine(2, s)
                    CurRecNr = CurRecNr + 1
                End While
            End If
            While CurRecNr < StartNr
                PrintLine(2, "")
                CurRecNr = CurRecNr + 1
            End While
            If src = "V" Then
                If exi And StartNr > 0 Then If Not EOF(1) Then s = LineInput(1)
                Rxs.CurrRexxRun.IdExpose = DirectCast(Rxs.CurrRexxRun.IdExposeStk.Item(Rxs.CurrRexxRun.ProcNum + 1), Collection)
                If LinesToReadWrite > 1 Then
                    LinesToReadWrite = 1
                End If
                i = Rxs.SourceNameIndexPosition(vr, Rexx.tpSymbol.tpUnknown, cvr)
                s = Rxs.GetVar(i, en, n)
                PrintLine(2, s)
                CurRecNr = CurRecNr + 1
            ElseIf src = "S" Then
                Rxs.CurrRexxRun.IdExpose = DirectCast(Rxs.CurrRexxRun.IdExposeStk.Item(Rxs.CurrRexxRun.ProcNum + 1), Collection)
                i = Rxs.SourceNameIndexPosition(vr & "0", Rexx.tpSymbol.tpUnknown, cvr)
                Dim NumRecs As Integer = CIntUserCor(Rxs.GetVar(i, en, n))
                If LinesToReadWrite > NumRecs Then
                    LinesToReadWrite = NumRecs
                End If
                counter = LinesToReadWrite
                For j As Integer = 1 To counter
                    If exi And StartNr > 0 Then If Not EOF(1) Then s = LineInput(1)
                    i = Rxs.SourceNameIndexPosition(vr & CStr(j), Rexx.tpSymbol.tpUnknown, cvr)
                    s = Rxs.GetVar(i, en, n)
                    PrintLine(2, s)
                    CurRecNr = CurRecNr + 1
                Next
            Else
                If LinesToReadWrite > Rexx.QStack.Count() Then
                    LinesToReadWrite = Rexx.QStack.Count()
                End If
                For i = 1 To LinesToReadWrite
                    If exi And StartNr > 0 Then If Not EOF(1) Then s = LineInput(1)
                    q = CStr(FromQueueStack())
                    If RecFm = "F" Then
                        s = Space(Lrecl)
                        Mid(s, 1, q.Length()) = q
                        q = s
                    End If
                    If q.Length() > Lrecl Then q = VB.Left(q, Lrecl)
                    PrintLine(2, q)
                    CurRecNr = CurRecNr + 1
                Next
            End If
            If exi And StartNr > 0 Then
                If RecFm = "F" Then
                    Copy1st = Copy1st + LinesToReadWrite - 1
                    CopyLast = 9999999
                End If
                While Not EOF(1) And (CurRecNr >= Copy1st And CurRecNr <= CopyLast)
                    s = LineInput(1)
                    PrintLine(2, s)
                    CurRecNr = CurRecNr + 1
                End While
            End If
            FileClose(2)
            If StartNr > 0 Then
                If exi Then
                    FileClose(1)
                    File.Move(fn, fn & ".Ł.$$$")
                End If
                File.Move(fn & ".$$$", fn)
                If exi Then File.Delete(fn & ".Ł.$$$")
            End If
        Else ' DISKR
            CurRecNr = 0
            nr = 0
            If StartNr > 1 Then LinesToReadWrite = LinesToReadWrite + StartNr - 1
            exi = File.Exists(fn)
            If Not exi Then
                rc = 16
            Else
                Try
                    FileOpen(1, fn, OpenMode.Input)
                Catch e As Exception
                    LinesToReadWrite = -1
                End Try
                While CurRecNr < LinesToReadWrite AndAlso Not EOF(1)
                    CurRecNr = CurRecNr + 1
                    s = LineInput(1)
                    If CurRecNr >= StartNr Or StartNr = 0 Then
                        nr = nr + 1
                        If src = "V" Then
                            If LinesToReadWrite > 1 Then
                                LinesToReadWrite = 1
                            End If
                            Rxs.CurrRexxRun.IdExpose = DirectCast(Rxs.CurrRexxRun.IdExposeStk.Item(Rxs.CurrRexxRun.ProcNum + 1), Collection)
                            i = Rxs.SourceNameIndexPosition(vr, Rexx.tpSymbol.tpUnknown, cvr)
                            Rxs.StoreVar(i, s, k, en, n) ' new value
                        ElseIf src = "S" Then
                            i = Rxs.SourceNameIndexPosition(vr & CStr(nr), Rexx.tpSymbol.tpUnknown, cvr)
                            Rxs.StoreVar(i, s, k, en, n) ' new value
                        Else
                            If Rexx.QStack.Count() = 0 Then
                                Rexx.QStack.Add(s)
                            Else
                                Rexx.QStack.Add(s, , 1)
                            End If
                        End If
                    End If
                End While
            End If
            If src = "S" Then
                i = Rxs.SourceNameIndexPosition(vr & "0", Rexx.tpSymbol.tpUnknown, cvr)
                Rxs.StoreVar(i, CStr(nr), k, en, n) ' new value
            End If
            FileClose(1)
        End If
    End Sub
    Private Function cmsState(ByRef fn As String) As Integer
        If File.Exists(fn) Then
            cmsState = 0
        Else
            cmsState = 16
        End If
    End Function
    Dim WithEvents printDoc As PrintDocument
    Dim LinesPerPage As Integer, PrintCharSize As Single
    Dim fiPage, laPage, curPage, curPrLine As Integer
    Dim DialShown As Boolean = False
    Dim PrintLineNrs As Boolean = False
    Dim PrintHeading As Boolean = False
    Private Sub PrintFile(ByVal cmd As String)
        Dim wrd As String, ssd As SourceLine, nsLin As Integer
        cmd = cmd.Trim
        While cmd.Length > 0
            wrd = NxtWordFromStr(cmd)
            If Abbrev(wrd, "LINENUMBERS") Then PrintLineNrs = True
            If Abbrev(wrd, "HEADING") Then PrintHeading = True
        End While
        If Not DialShown Then
            With FontDialog1
                If .ShowDialog <> Windows.Forms.DialogResult.OK Then Exit Sub
            End With
            With PageSetupDialog1
                .PageSettings = New PageSettings()
                .AllowPrinter = False
                .MinMargins = New Margins(1, 1, 1, 1)
                If .ShowDialog <> Windows.Forms.DialogResult.OK Then Exit Sub
            End With
            PrintCharSize = FontDialog1.Font.Size * 1.4F
            DialShown = True
        End If
        If PageSetupDialog1.PageSettings.Landscape Then
            LinesPerPage = CInt(CSng(PageSetupDialog1.PageSettings.PaperSize.Width - PageSetupDialog1.PageSettings.Margins.Top - PageSetupDialog1.PageSettings.Margins.Bottom - PageSetupDialog1.PageSettings.HardMarginX - PageSetupDialog1.PageSettings.HardMarginY) / PrintCharSize) - 2
        Else
            LinesPerPage = CInt(CSng(PageSetupDialog1.PageSettings.PaperSize.Height - PageSetupDialog1.PageSettings.Margins.Top - PageSetupDialog1.PageSettings.Margins.Bottom - PageSetupDialog1.PageSettings.HardMarginX - PageSetupDialog1.PageSettings.HardMarginY) / PrintCharSize) - 2
        End If
        If PrintHeading Then
            LinesPerPage -= 2
        End If
        SaveAllModifiedLines()
        With PrintDialog1
            .AllowPrintToFile = True
            .AllowSelection = True
            .AllowSomePages = True
            .PrinterSettings = New PrinterSettings()
            With .PrinterSettings
                curPrLine = 0
                nsLin = 0 ' count lines to be printed
                While curPrLine < CurrEdtSession.SourceList.Count
                    curPrLine += 1
                    ssd = DirectCast(CurrEdtSession.SourceList.Item(curPrLine), SourceLine)
                    If (ssd.SrcSelect <= CurrEdtSession.EditDisplayMax And ssd.SrcSelect >= CurrEdtSession.EditDisplayMin) Or CurrEdtSession.ScopeAllDisplay Then
                        nsLin += 1
                    End If
                End While
                .PrintRange = PrintRange.SomePages
                fiPage = 1
                laPage = CInt(Math.Floor((nsLin - 1) / LinesPerPage)) + 1
                .FromPage = fiPage
                .ToPage = laPage
                .MinimumPage = fiPage
                .MaximumPage = laPage
            End With
            If .ShowDialog <> Windows.Forms.DialogResult.OK Then Exit Sub
            If .PrinterSettings.PrintRange = PrintRange.SomePages Then
                fiPage = .PrinterSettings.FromPage
                laPage = .PrinterSettings.ToPage
            End If
            curPage = fiPage
            curPrLine = 0 ' skip lines on unprinted pages at start
            Dim i As Integer = (fiPage - 1) * LinesPerPage
            Dim n As Integer = 0
            While n < i
                curPrLine += 1
                ssd = DirectCast(CurrEdtSession.SourceList.Item(curPrLine), SourceLine)
                If (ssd.SrcSelect <= CurrEdtSession.EditDisplayMax And ssd.SrcSelect >= CurrEdtSession.EditDisplayMin) Or CurrEdtSession.ScopeAllDisplay Then
                    n += 1
                End If
            End While
            PrintDocumentPages(fiPage, laPage)
        End With
    End Sub
    Private Sub PrintDocumentPages(ByVal fromPage As Integer, ByVal toPage As Integer)
        Me.printDoc = New PrintDocument()
        Me.printDoc.DefaultPageSettings.Landscape = PageSetupDialog1.PageSettings.Landscape
        Try
            printDoc.DocumentName = CurrEdtSession.EditFileName
            printDoc.Print()
        Catch ex As Exception
            MessageBox.Show(ex.Message, SysMsg(20))
        End Try
    End Sub
    Private Sub printDoc_PrintPage(ByVal sender As Object, ByVal e As System.Drawing.Printing.PrintPageEventArgs) Handles printDoc.PrintPage
        Dim pX As Single = PageSetupDialog1.PageSettings.Margins.Left
        Dim pY As Single = PageSetupDialog1.PageSettings.Margins.Top
        Dim i, l As Integer, ts As Size
        Dim src, linS As String, ssd As SourceLine
        If PrintHeading Then
            e.Graphics.DrawString(CurrEdtSession.EditFileName, FontDialog1.Font, Brushes.Black, pX, pY)
            pY += PrintCharSize
            linS = "- " & CStr(curPage) & " -"
            ts = e.Graphics.MeasureString(linS, FontDialog1.Font).ToSize()
            If PageSetupDialog1.PageSettings.Landscape Then
                pX = printDoc.DefaultPageSettings.PrintableArea.Height
            Else
                pX = printDoc.DefaultPageSettings.PrintableArea.Width
            End If
            pX = pX - ts.Width
            e.Graphics.DrawString(linS, FontDialog1.Font, Brushes.Black, pX, pY)
            pY += PrintCharSize
        End If
        l = CInt(Math.Floor(Math.Log10(CurrEdtSession.SourceList.Count))) + 1
        i = 1
        While i <= LinesPerPage AndAlso curPrLine < CurrEdtSession.SourceList.Count
            curPrLine += 1
            ssd = DirectCast(CurrEdtSession.SourceList.Item(curPrLine), SourceLine)
            If (ssd.SrcSelect <= CurrEdtSession.EditDisplayMax And ssd.SrcSelect >= CurrEdtSession.EditDisplayMin) Or CurrEdtSession.ScopeAllDisplay Then
                src = ReadOneSourceLine(ssd)
                pX = PageSetupDialog1.PageSettings.Margins.Left
                i += 1
                If PrintLineNrs Then
                    linS = curPrLine.ToString("0000000: ")
                    linS = linS.Substring(7 - l, l + 2)
                    e.Graphics.DrawString(linS, FontDialog1.Font, Brushes.Black, pX, pY)
                    ts = e.Graphics.MeasureString(linS, FontDialog1.Font).ToSize()
                    pX += ts.Width
                End If
                e.Graphics.DrawString(src, FontDialog1.Font, Brushes.Black, pX, pY)
                pY += PrintCharSize
            End If
        End While
        curPage += 1
        e.HasMorePages = (curPage <= laPage)
    End Sub
    Sub SaveSsd()
        CurrEdtSession.ScrListSaved = ScrList
    End Sub
    Sub RestoreSsd()
        ScrList = CurrEdtSession.ScrListSaved
    End Sub
    Private Sub ChangeString(ByVal CmdString As String)
        Dim iOcc, nLines, NrOfOcc, FirstOcc, iLine, cFiLin, cLaLin As Integer
        Dim dsScr As ScreenLine
        Dim sep, TrStr As String
        Dim fromP, i, FiLocLine, LaLocLine As Integer
        Dim OldString, NewString As String
        Dim RetLine, OnLinenr As Integer
        Dim firstCh As Boolean
        Dim Chgd As Boolean, nOccChg As Integer = 0
        Chgd = False
        firstCh = True
        dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
        RetLine = dsScr.CurSrcNr ' linenr of current line on screen
        SaveAllModifiedLines()
        CmdString = CmdString.TrimStart()
        If CmdString.Length() = 0 Then Exit Sub
        If CmdString(0) >= "0"c And CmdString(0) <= "9"c Then '  CHANGE from to -aaaa-bbbb-
            cFiLin = NxtNumFromStr(CmdString)
            If CmdString = "" Then Exit Sub ' user cancelled invalid number in change from to opeation
            If CmdString(0) >= "0"c And CmdString(0) <= "9"c Then
                cLaLin = NxtNumFromStr(CmdString)
            Else
                cLaLin = cFiLin
            End If
            CmdString = CmdString.TrimStart()
        End If
        sep = CmdString.Substring(0, 1)
        CmdString = CmdString.Substring(1)
        i = InStr(1, CmdString, sep)
        If i > 0 Then
            OldString = CmdString.Substring(0, i - 1)
            If CurrEdtSession.CaseIR Then
                OldString = OldString.ToUpper()
            End If
            If CurrEdtSession.HexM AndAlso OldString.Length() >= 5 Then
                TrStr = OldString.ToUpper()
                If TrStr.Substring(0, 2) = "X'" And TrStr.Substring(TrStr.Length() - 1, 1) = "'" Then
                    OldString = X2C(TrStr.Substring(2, TrStr.Length() - 3))
                ElseIf TrStr.Substring(TrStr.Length() - 2, 2) = "'X" And TrStr.Substring(0, 1) = "'" Then
                    OldString = X2C(TrStr.Substring(1, TrStr.Length() - 3))
                ElseIf TrStr.Substring(0, 2) = "X""" And TrStr.Substring(TrStr.Length() - 1, 1) = """" Then
                    OldString = X2C(TrStr.Substring(2, TrStr.Length() - 3))
                ElseIf TrStr.Substring(TrStr.Length() - 2, 2) = """X" And TrStr.Substring(0, 1) = """" Then
                    OldString = X2C(TrStr.Substring(1, TrStr.Length() - 3))
                End If
            End If
            CmdString = CmdString.Substring(i)
            i = InStr(1, CmdString, sep)
            If i > 0 Then
                NewString = CmdString.Substring(0, i - 1)
                If CurrEdtSession.HexM AndAlso NewString.Length() >= 5 Then
                    TrStr = NewString.ToUpper()
                    If TrStr.Substring(0, 2) = "X'" And TrStr.Substring(TrStr.Length() - 1, 1) = "'" Then
                        NewString = X2C(TrStr.Substring(2, TrStr.Length() - 3))
                    ElseIf TrStr.Substring(TrStr.Length() - 2, 2) = "'X" And TrStr.Substring(0, 1) = "'" Then
                        NewString = X2C(TrStr.Substring(1, TrStr.Length() - 3))
                    ElseIf TrStr.Substring(0, 2) = "X""" And TrStr.Substring(TrStr.Length() - 1, 1) = """" Then
                        NewString = X2C(TrStr.Substring(2, TrStr.Length() - 3))
                    ElseIf TrStr.Substring(TrStr.Length() - 2, 2) = """X" And TrStr.Substring(0, 1) = """" Then
                        NewString = X2C(TrStr.Substring(1, TrStr.Length() - 3))
                    End If
                End If
                CmdString = CmdString.Substring(i).TrimStart
                If CmdString.Length > 0 Then
                    nLines = ChaDelTarget(CmdString, dsScr)
                Else
                    nLines = 1
                End If
                If nLines < 1 Then
                    rc = 5
                End If
                If rc = 0 Then
                    NrOfOcc = NumOrAst(CmdString, "1", 99999)
                End If
                If rc = 0 Then
                    FirstOcc = NumOrAst(CmdString, "1", 1)
                    If FirstOcc < 0 Then
                        NrOfOcc = FirstOcc
                        FirstOcc = 1
                    End If
                End If
                If rc <> 0 Then Exit Sub
                If cFiLin = 0 AndAlso cLaLin = 0 Then
                    FiLocLine = RetLine
                    LaLocLine = FiLocLine + nLines - 1
                Else
                    FiLocLine = cFiLin
                    LaLocLine = cLaLin
                End If
                iLine = 0
                fromP = CurrEdtSession.EditZoneLeft
                i = Locate1String(OldString, OnLinenr, FiLocLine, LaLocLine, fromP, CurrEdtSession.EditZoneRight, NrOfOcc)
                If CancelCmd Then Exit Sub
                While i > 0
                    iOcc = 1
                    While i > 0 And iOcc < FirstOcc
                        iOcc = iOcc + 1
                        fromP = i + OldString.Length()
                        i = Locate1String(OldString, OnLinenr, OnLinenr, OnLinenr, fromP, CurrEdtSession.EditZoneRight, NrOfOcc)
                        If CancelCmd Then Exit Sub
                    End While
                    iOcc = 0
                    While i > 0 And (iOcc < NrOfOcc Or NrOfOcc < 0)
                        Chgd = True
                        If firstCh Then
                            firstCh = False
                            RetLine = OnLinenr
                        End If
                        Dim stPos As Integer = i
                        If NrOfOcc = -2 Then
                            stPos = dsScr.CurLinSrc.Length() - OldString.Length() + 1
                        End If
                        ChangeStringAct(OnLinenr, OldString, NewString, i)
                        nOccChg += 1
                        iOcc = iOcc + 1
                        If iOcc = NrOfOcc Or NrOfOcc < 0 Then
                            i = 0
                        Else
                            fromP = i + NewString.Length()
                            i = Locate1String(OldString, OnLinenr, OnLinenr, OnLinenr, fromP, CurrEdtSession.EditZoneRight, NrOfOcc)
                            If CancelCmd Then Exit Sub
                        End If
                    End While
                    fromP = CurrEdtSession.EditZoneLeft
                    i = Locate1String(OldString, OnLinenr, OnLinenr + 1, LaLocLine, fromP, CurrEdtSession.EditZoneRight, NrOfOcc)
                    If CancelCmd Then Exit Sub
                End While
            Else
                rc = 16
            End If
        Else
            rc = 16
        End If
        If Not Chgd Then rc = 4
        If nOccChg > 0 Then
            DoCmd1("MSG " & CStr(nOccChg) & " occurrences changed.", False)
        End If
        If Not CurrEdtSession.Stay Then
            MoveToSourceLine(RetLine)
        End If
    End Sub
    Private Sub ChangeStringAct(ByVal OnLinenr As Integer, ByVal OldString As String, ByVal NewString As String, ByVal StPos As Integer)
        Dim Strng, TrStr As String, i As Integer
        Dim ssd As SourceLine, dsScr As New ScreenLine
        ssd = DirectCast(CurrEdtSession.SourceList.Item(OnLinenr), SourceLine)
        Strng = ReadOneSourceLine(ssd)
        dsScr.CurSrcNr = OnLinenr
        dsScr.CurLinSrc = Strng
        dsScr.CurSrcRead = True
        dsScr.CurLinModified = True
        dsScr.CurLinType = "L"c
        dsScr.CurLinSsd = ssd
        AddUndo(3, dsScr)
        If Strng.Length() > CurrEdtSession.Trunc Then
            TrStr = Strng.Substring(CurrEdtSession.Trunc)
            Strng = Strng.Substring(0, CurrEdtSession.Trunc)
        Else
            TrStr = ""
        End If
        If StPos <= Strng.Length() Then
            Strng = Strng.Substring(0, StPos - 1) & NewString & Strng.Substring(StPos + OldString.Length() - 1)
        Else
            If StPos > 1 Then Strng = Strng.PadRight(StPos - 1, " ") & NewString
        End If
        If Strng.Length() > CurrEdtSession.Trunc Then
            Strng = Strng.Substring(0, CurrEdtSession.Trunc)
        End If
        dsScr.CurLinSrc = Strng & TrStr
        SaveModifiedLine(dsScr)
        i = LineIxOnScreen(OnLinenr)
        If i > 0 Then
            dsScr = DirectCast(ScrList.Item(i), ScreenLine)
            dsScr.CurRepaint = True
            dsScr.CurSrcRead = False
        End If
    End Sub
    Private Sub SendKey(ByVal Path As String, ByVal down As Boolean)
        Try
            Using sr As StreamReader = New StreamReader(Path)
                Dim s As String = sr.ReadToEnd
                SendKeys.Send(s)
            End Using
        Catch ex As Exception
            DoCmd1("MSG Macro not found: " + ex.Message, False)
        End Try
    End Sub
    Private Sub FindString(ByVal LocString As String, ByVal down As Boolean)
        Dim dsScr As ScreenLine, ssd As SourceLine
        Dim i, RetLine, fromLine, toLine, curLine, iPass As Integer, Found As Boolean
        Dim src As String
        dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
        RetLine = dsScr.CurSrcNr ' linenr of current line on screen
        SaveAllModifiedLines()
        If LocString.Length() = 0 Then
            rc = 4
            Exit Sub
        End If
        If down Then
            fromLine = RetLine + 1
            toLine = CurrEdtSession.SourceList.Count()
        Else
            fromLine = RetLine - 1
            toLine = 1
        End If
        For iPass = 1 To 2 ' curline to bot, top to curline-1 if wrap on
            Found = False
            curLine = fromLine
            While Not Found AndAlso ((down AndAlso curLine <= toLine AndAlso curLine >= 1) OrElse (Not down AndAlso curLine >= toLine AndAlso curLine <= CurrEdtSession.SourceList.Count()))
                nrCyclesEv += 1
                If nrCyclesEv > 1000 Then
                    CallDoEvent()
                    If CancelCmd Then Exit Sub
                End If
                ssd = DirectCast(CurrEdtSession.SourceList.Item(curLine), SourceLine)
                If (ssd.SrcSelect <= CurrEdtSession.EditDisplayMax And ssd.SrcSelect >= CurrEdtSession.EditDisplayMin) Or CurrEdtSession.ScopeAllDisplay Then
                    src = ReadOneSourceLine(ssd).TrimStart
                    Found = LocString.Length() <= src.Length()
                    If Found Then
                        For i = 0 To LocString.Length() - 1
                            If src(i) <> LocString(i) And LocString(i) <> " "c Then
                                Found = False
                                Exit For
                            End If
                        Next
                    End If
                    If Found Then RetLine = curLine
                End If
                If Not Found Then
                    If down Then
                        curLine += 1
                    Else
                        curLine -= 1
                    End If
                End If
            End While
            If Found Then Exit For
            If iPass = 1 Then ' wrap if not found
                If Not CurrEdtSession.Wrap Then Exit For
                If down Then
                    fromLine = 1
                Else
                    fromLine = CurrEdtSession.SourceList.Count()
                End If
                toLine = RetLine
            End If
        Next
        MoveToSourceLine(RetLine)
        rc = CInt(IIf(Found, 0, 4))
    End Sub
    Private Function LocateString(ByVal CmdString As String, ByVal ZoneL As Integer, ByVal ZoneR As Integer, ByRef FinalLine As Integer, ByRef FinalPos As Integer, Optional ByVal backWards As Boolean = False) As String
        Dim ixLineFound, toLine, fromLine, ixPosFound, savLine As Integer
        Dim RetString As String
        Dim sep As String
        Dim LocStrings As New Collection
        Dim LocOper As New Collection
        Dim i, l, PartFndLine As Integer
        Dim found, PartFnd As Boolean
        Dim fl, tL, iPass, OnLinenr As Integer
        Dim LocStr, TmpStr As String
        Dim bestLFound, bestPosFound As Integer
        Dim CurLine As Integer
        Dim dsScr As ScreenLine
        rc = 0
        dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
        CurLine = dsScr.CurSrcNr ' linenr of current line on screen
        SaveAllModifiedLines()
        CmdString = CmdString.TrimStart()
        If CmdString.Length() = 0 Then Return ""
        Dim FirstTime As Boolean = True
        While CmdString.Length() > 0 AndAlso (FirstTime Or CmdString(0) = "|" Or CmdString(0) = "&")
            If Not FirstTime Then
                LocOper.Add(CmdString.Substring(0, 1))
                CmdString = CmdString.Substring(1).TrimStart()
            Else
                LocOper.Add("|")
                FirstTime = False
            End If
            sep = CmdString.Substring(0, 1)
            i = InStr(2, CmdString, sep)
            If i > 0 Then
                LocStr = CmdString.Substring(1, i - 2)
                CmdString = CmdString.Substring(i).Trim
            Else
                LocStr = CmdString.Substring(1)
                CmdString = ""
            End If
            If CurrEdtSession.HexM AndAlso LocStr.Length() >= 5 Then
                TmpStr = LocStr.ToUpper()
                If TmpStr.Substring(0, 2) = "X'" And TmpStr.Substring(TmpStr.Length() - 1, 1) = "'" Then
                    LocStr = X2C(LocStr.Substring(2, LocStr.Length() - 3))
                End If
            End If
            If CurrEdtSession.CaseIR Then
                LocStr = LocStr.ToUpper()
            End If
            LocStrings.Add(LocStr)
        End While
        RetString = CmdString
        Dim moveDirection As Integer = 1
        If backWards Then moveDirection = -1
        savLine = CurLine ' last line in case we restart locate from TOP
        fromLine = savLine + moveDirection
        If backWards Then
            toLine = 1
        Else
            toLine = CurrEdtSession.SourceList.Count()
        End If
        LocOper.Add("|")
        iPass = 1
        While iPass <= 2 ' curline to bot, top to curline-1 if wrap on
            found = False
            FinalLine = 9999999
            PartFnd = True
            While PartFnd AndAlso FinalLine = 9999999 ' found partially, but not each string
                PartFndLine = fromLine
                PartFnd = False
                For l = 1 To LocStrings.Count()
                    'now each string:
                    Dim locString As String = DirectCast(LocStrings.Item(l), String)
                    If DirectCast(LocOper.Item(l), String) = "|" Then
                        fl = fromLine
                        tL = toLine
                    Else
                        fl = ixLineFound ' on same line where text was found 
                        tL = ixLineFound
                    End If
                    bestLFound = 9999999
                    ixPosFound = Locate1String(locString, OnLinenr, fl, tL, ZoneL, ZoneR, 0, backWards)
                    If CancelCmd Then Exit For
                    If ixPosFound > 0 Then
                        ixLineFound = OnLinenr
                        PartFnd = True
                        If PartFndLine = fromLine Or (OnLinenr < PartFndLine And PartFndLine > fromLine) Then
                            PartFndLine = OnLinenr ' best partial line nr, where to restart after
                        End If
                    Else
                        ixLineFound = 0
                    End If
                    If ixPosFound > 0 Then
                        If OnLinenr < bestLFound Then
                            bestLFound = ixLineFound
                            If bestPosFound = 0 OrElse DirectCast(LocOper.Item(l), String) = "|" Then
                                bestPosFound = ixPosFound
                            End If
                        End If
                    Else
                        bestLFound = 9999999
                        bestPosFound = 0
                    End If
                    If DirectCast(LocOper.Item(l + 1), String) = "|" Then
                        If bestLFound < FinalLine Then
                            FinalLine = bestLFound
                            FinalPos = bestPosFound
                        End If
                    End If
                Next
                If CancelCmd Then Exit While
                fromLine = PartFndLine + moveDirection
                If iPass = 1 AndAlso savLine > 0 AndAlso CurrEdtSession.Wrap AndAlso FinalLine = 9999999 Then ' wrap if not found
                    DoCmd1("MSG Wrap", False)
                    If backWards Then
                        fromLine = CurrEdtSession.SourceList.Count()
                    Else
                        fromLine = 1
                    End If
                    toLine = savLine
                Else
                    iPass = 2
                End If
            End While
            iPass += 1
        End While
        LocStrings.Clear()
        LocOper.Clear()
        If bestLFound = 9999999 Then rc = 3
        Return RetString
    End Function
    Private Function Locate1String(ByVal LocString As String, ByRef OnLineNr As Integer, ByVal fromLine As Integer, ByVal toLine As Integer, ByVal ZoneL As Integer, ByVal ZoneR As Integer, NrOcc As Integer, Optional ByVal Backwards As Boolean = False) As Integer
        Dim found, curLine, Leng As Integer
        Dim fStr, src As String
        Dim ssd As SourceLine
        Logg("Locate1String start")
        Logg("Locate1String loc = " & CStr(curLine))
        Dim moveDirection As Integer = 1
        If Backwards Then moveDirection = -1
        found = 0
        If fromLine < 1 Then
            fromLine = 1
            If Backwards Then Return 0
        ElseIf fromLine > CurrEdtSession.SourceList.Count Then
            fromLine = CurrEdtSession.SourceList.Count
            If Not Backwards Then Return 0
        End If
        If toLine < 1 Then toLine = 1
        If toLine > CurrEdtSession.SourceList.Count Then toLine = CurrEdtSession.SourceList.Count
        curLine = fromLine
        While rc = 0 And found = 0 And (curLine <= toLine And Not Backwards Or curLine >= toLine And Backwards)
            nrCyclesEv += 1
            If nrCyclesEv > 1000 Then
                CallDoEvent()
                If CancelCmd Then
                    Locate1String = found
                    Exit Function
                End If
            End If
            Logg("Locate1String curline = " & CStr(curLine))
            ssd = DirectCast(CurrEdtSession.SourceList.Item(curLine), SourceLine)
            If (ssd.SrcSelect <= CurrEdtSession.EditDisplayMax And ssd.SrcSelect >= CurrEdtSession.EditDisplayMin) Or CurrEdtSession.ScopeAllDisplay Then
                src = ReadOneSourceLine(ssd)
                Logg("Locate1String src = " & src)
                'If src.Length < ZoneR Then
                '    src = src.PadRight(ZoneR, vbTab)
                'End If
                If LocString.Length() = 0 Then ' point before first char
                    If NrOcc = -2 Then
                        found = src.Length()
                    Else
                        found = 1
                    End If
                ElseIf src.Length >= ZoneL Then
                    Leng = src.Length() - ZoneL + 1
                    If Leng < 0 Then Leng = 0
                    If Leng > (ZoneR - ZoneL + 1) Then
                        Leng = (ZoneR - ZoneL + 1)
                    End If
                    fStr = src.Substring(ZoneL - 1, Leng)
                    Dim lString As String
                    If Not CurrEdtSession.CaseIR Then
                        lString = LocString
                    Else
                        lString = LocString.ToUpper()
                        fStr = fStr.ToUpper
                    End If
                    If NrOcc = -1 Then ' at begin of line
                        If fStr.Length < LocString.Length() Then
                            found = 0
                        Else
                            If fStr.Substring(0, lString.Length) = lString Then
                                found = 1
                            Else
                                found = 0
                            End If
                        End If
                    ElseIf NrOcc = -2 Then ' at end of line
                        fStr += " "
                        If fStr.Length < LocString.Length() Then
                            found = 0
                        Else
                            If fStr.Substring(fStr.Length - lString.Length, lString.Length) = lString Then
                                found = fStr.Length - lString.Length + 1
                            Else
                                found = 0
                            End If
                        End If
                    Else
                        'found = InStr(1, fStr, lString)
                        found = fStr.IndexOf(lString) + 1
                    End If
                Else
                    found = 0 ' before left zone
                End If
            End If
            Logg("Locate1String found = " & CStr(found))
            If found = 0 Then
                curLine += moveDirection
            End If
        End While
        If found > 0 Then
            found = found + ZoneL - 1
            OnLineNr = curLine
        End If
        Locate1String = found
        Logg("Locate1String end")
    End Function
    Private Sub InsertOneLine(ByVal Text As String)
        ' insert new line of text in screenbuffer after current line, shift out last line
        Dim addSpace As Boolean = False
        If Text.Length() = 0 And Pasting Then
            Text = " "
            addSpace = True
        End If
        Dim ssd As SourceLine, dsScr, dsScrP As ScreenLine
        dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
        CurrEdtSession.ChangeRememberedLinenrs(1, dsScr.CurSrcNr)
        ShiftScreenDown(CurrEdtSession.CurLineNr + 1, 1, False)
        dsScrP = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine) ' current line
        dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr + 1), ScreenLine) ' inserted line
        dsScr.CurLinType = "L"c
        dsScr.CurLinModified = True
        If addSpace Then
            dsScr.CurLinSrc = ""
        Else
            dsScr.CurLinSrc = Text
        End If
        dsScr.CurSrcRead = True
        dsScr.CurSrcNr = dsScrP.CurSrcNr + dsScrP.CurNrLines
        dsScr.CurLinNr = Format(dsScr.CurSrcNr, "000000")
        dsScr.CurNrLines = 1
        If Text.Length() = 0 Then
            Dim ie As New IedLine
            ie.Linenr = dsScr.CurSrcNr
            CurrEdtSession.IedLines.Add(ie)
        End If
        ssd = New SourceLine
        ssd.SrcFileIx = "W"c
        ssd.SrcLength = -1 ' No true file entry exists
        ssd.SrcStart = -1
        CurrEdtSession.FileUsesEndlineCR = True
        CurrEdtSession.FileUsesEndlineLF = True
        dsScr.CurLinSsd = ssd
        If CurrEdtSession.SourceList.Count() = 0 Then
            CurrEdtSession.SourceList.Add(ssd)
        Else
            CurrEdtSession.SourceList.Add(ssd, , , dsScr.CurSrcNr - 1)
        End If
        CurrEdtSession.FileChanged = True
        AddUndo(2, dsScr)
        RenumScrList(CurrEdtSession.CurLineNr + 2, 1)
    End Sub
    Private Sub RenumScrList(ByVal from As Integer, ByVal Delta As Integer)
        Dim i As Integer, dsScr As ScreenLine
        Logg("Renum: " & CStr(from) & " to " & CStr(ScrList.Count) & " by " & CStr(Delta))
        For i = from To ScrList.Count
            dsScr = DirectCast(ScrList.Item(i), ScreenLine) ' line to  renumber
            If Not dsScr.CurLinFixTp Then
                dsScr.CurSrcNr = dsScr.CurSrcNr + Delta
                If dsScr.CurLinType = "L"c Then
                    dsScr.CurLinNr = Format(dsScr.CurSrcNr, "000000")
                End If
            End If
        Next
    End Sub
    Private Sub DeleteLine()
        'delete current line
        Dim sourceI As Integer
        Dim dsScr As ScreenLine
        dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine) ' inserted line
        sourceI = dsScr.CurSrcNr
        Logg("Delete line " & CStr(sourceI) & " " & dsScr.CurLinSrc)
        AddUndo(1, dsScr)
        CurrEdtSession.ChangeRememberedLinenrs(-1, dsScr.CurSrcNr) ' change linenrs in linecommands waiting
        ShiftScreenUp(CurrEdtSession.CurLineNr, 1)
        RenumScrList(CurrEdtSession.CurLineNr, -1)
        CurrEdtSession.SourceList.Remove(sourceI)
        Logg("Remv source" & CStr(sourceI))
        CurrEdtSession.FileChanged = True
    End Sub
    Private Sub SaveFile(ByVal Filename As String, ByRef SaveAs As Boolean)
        '   save each line and it's characteristics in the current sourcelist
#If Not DEBUG Then
        Try
#End If
        Dim tFileName As String
        Dim WrPos, l As Integer
        Dim newSt As New Collection
        Dim EditSaveFile As FileStream
        Dim ssd As SourceLine
        Logg("SaveFile start" & Filename)
        SaveAllModifiedLines()
        RestoreIedLines()
        If Filename = "?" Then
            SaveFileDialog1.FileName = CurrEdtSession.EditFileName
            SaveFileDialog1.CheckFileExists = False
            SaveFileDialog1.Title = "SAVE"
            SaveFileDialog1.ShowDialog()
            Filename = SaveFileDialog1.FileName
            If Filename = "" Then
                rc = 8
                Exit Sub
            End If
        End If
        If Not WritableFile(Filename, "SAVE") Then
            Logg("SaveFile not writable " & Filename)
            Exit Sub
        End If
        If SaveAs Then
            Logg("SaveFile saveas" & Filename)
            tFileName = Filename
FileDeleteErrorRes:
            Try
                If File.Exists(Filename) Then File.Delete(Filename)
            Catch ex As Exception
                Dim Msg As String = SysMsg(16) & " " & Filename
                Dim Response As Integer = MsgBox(Msg, MsgBoxStyle.RetryCancel, Msg)
                If Response = MsgBoxResult.Retry Then ' user requests RETRY.
                    GoTo FileDeleteErrorRes
                Else
                    rc = 48
                    Exit Sub
                End If
            End Try
        Else
            tFileName = Filename & ".$$$"
        End If
        EditSaveFile = New FileStream(tFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite)
        WrPos = 0
        Dim lne As Integer = 0
        If CurrEdtSession.EncodingType = "U"c Then
            Dim buf(2) As Byte
            buf(0) = &HFF
            buf(1) = &HFE
            EditSaveFile.Write(buf, 0, 2)
        End If
        If CurrEdtSession.EncodingType = "8"c Then
            If CurrEdtSession.fileHasUtf8BOM Then
                Dim buf(3) As Byte
                buf(0) = &HEF
                buf(1) = &HBB
                buf(2) = &HBF
                EditSaveFile.Write(buf, 0, 3)
            End If
        End If
        'rc = 0
        For Each ssd In CurrEdtSession.SourceList
            lne += 1
            nrCyclesEv += 1 : If nrCyclesEv > 5000 Then CallDoEvent()
            If CancelCmd Then
                EditSaveFile.Dispose()
                EditSaveFile.Close()
                Exit Sub
            End If
            If ssd.SrcLength = -1 Then ' Ied line
                ssd.SrcLength = 0
            End If
            If ssd.SrcStart < 1 Then
                ssd.SrcStart = 1
            End If
            If CurrEdtSession.RecfmV Then
                l = ssd.SrcLength
                If CurrEdtSession.EncodingType = "U"c Then l /= 2
                If lne < CurrEdtSession.SourceList.Count Then
                    If CurrEdtSession.FileUsesEndlineLF Then l += 1
                    If CurrEdtSession.FileUsesEndlineCR Then l += 1
                End If
            Else
                l = CurrEdtSession.Lrecl
            End If
            If CurrEdtSession.EncodingType = "U"c Then l *= 2
            Dim buf(l - 1) As Byte
            If Not CurrEdtSession.RecfmV Then
                If CurrEdtSession.EncodingType = "U"c Then
                    For i = 0 To l Step 2
                        buf(i) = &H20
                        buf(i + 1) = &H0
                    Next
                Else
                    For i = 0 To l - 1
                        buf(i) = &H20
                    Next
                End If
            End If
            EditRdFile = FileWithData(ssd.SrcFileIx)
            EditRdFile.Seek(ssd.SrcStart - 1, SeekOrigin.Begin)
            EditRdFile.Read(buf, 0, Math.Min(l, ssd.SrcLength))
            If lne < CurrEdtSession.SourceList.Count Then ' add crlf
                If CurrEdtSession.EncodingType = "U"c Then
                    If CurrEdtSession.RecfmV Then
                        If CurrEdtSession.FileUsesEndlineLF Then
                            If CurrEdtSession.FileUsesEndlineCR Then
                                buf(l - 4) = 13
                            Else
                                buf(l - 2) = 13
                            End If
                        End If
                        If CurrEdtSession.FileUsesEndlineCR Then buf(l - 2) = 10
                    End If
                Else
                    If CurrEdtSession.RecfmV Then
                        If CurrEdtSession.FileUsesEndlineLF Then
                            If CurrEdtSession.FileUsesEndlineCR Then
                                buf(l - 2) = 13
                            Else
                                buf(l - 1) = 13
                            End If
                        End If
                        If CurrEdtSession.FileUsesEndlineCR Then buf(l - 1) = 10
                    End If
                End If
            End If
            If Not SaveAs Then newSt.Add(WrPos) ' will be the new list of start positions in saved file
            EditSaveFile.Write(buf, 0, l)
            WrPos = WrPos + l
        Next ssd
        EditSaveFile.Dispose()
        EditSaveFile.Close()
        Logg("SaveFile written")
        If Not SaveFileExisted Then
            If Not File.Exists(tFileName) Then
                MsgBox(SysMsg(13) & ": " & tFileName)
                rc = 16
                Exit Sub
            End If
        End If
        If Not SaveAs Then
            If Not CurrEdtSession.EditFile Is Nothing Then
                CurrEdtSession.EditFile.Dispose()
                CurrEdtSession.EditFile.Close()
                If Not CurrEdtSession.BakfileAlreadyCreated Then
                    RenameFile(Filename, Filename & ".bak")
                    If rc <> 48 Then CurrEdtSession.BakfileAlreadyCreated = True
                End If
            End If
            If rc <> 48 Then RenameFile(tFileName, Filename)
            Logg("SaveFile renamed")
            CurrEdtSession.EditFile = New FileStream(Filename, FileMode.Open, FileAccess.Read)
            If rc <> 48 Then
                Dim Linr As Integer = 0
                For Each ssd In CurrEdtSession.SourceList
                    nrCyclesEv += 1 : If nrCyclesEv > 5000 Then CallDoEvent()
                    If CancelCmd Then Exit Sub
                    Linr = Linr + 1
                    ssd.SrcFileIx = "E"c
                    ssd.SrcStart = CInt(newSt.Item(Linr)) + 1
                Next ssd
            End If
        End If
        Logg("SaveFile end")
#If Not DEBUG Then
        Catch ex As Exception
        End Try
#End If
    End Sub
    Private Function FileWithData(ByVal t As Char) As FileStream
        If t = "E" Then
            Return CurrEdtSession.EditFile
        Else
            Return EditFileWrk
        End If
    End Function
    Private Sub RenameFile(ByRef Fo As String, ByRef fn As String)
        Dim Msg As String, Response As Integer
FileDeleteErrorRes:
        'Debug.WriteLine("RenameFile from " + Fo + " to " + fn)
        Try
            If File.Exists(Fo) And File.Exists(fn) Then
                File.Delete(fn)
            End If
        Catch ex As Exception
            Msg = SysMsg(8) & fn
            Response = MsgBox(Msg, MsgBoxStyle.RetryCancel, Msg)
            If Response = MsgBoxResult.Retry Then ' user requests RETRY.
                GoTo FileDeleteErrorRes
            Else
                'Debug.WriteLine("rc = 48")
                rc = 48
                Exit Sub
            End If
        End Try
        Try
            'Debug.WriteLine("RenameFile move " + Fo + " to " + fn)
            File.Move(Fo, fn)
        Catch ex As Exception
            Msg = SysMsg(1) & Fo
            Response = MsgBox(Msg, MsgBoxStyle.RetryCancel, Msg)
            If Response = MsgBoxResult.Retry Then ' user requests RETRY.
                GoTo FileDeleteErrorRes
            Else
                'Debug.WriteLine("rc = 48")
                rc = 48
                Exit Sub
            End If
        End Try
        Exit Sub
    End Sub
    Private Function WritableFile(ByVal fn As String, ByRef orig As String) As Boolean
        Dim f As String
        WritableFile = False
        f = Path.GetFullPath(fn)
        Dim dr As New DirectoryInfo(f.Substring(0, 2))
        If dr.Attributes = -1 Then '  drive NReady
            MsgBox(SysMsg(15) & ": " & f.Substring(0, 2), , orig)
            'Debug.WriteLine("rc = 16")
            rc = 16
            Exit Function
        End If
        If File.Exists(f) Then
            SaveFileExisted = True
            Dim fi As New FileInfo(f)
            If CBool(fi.IsReadOnly) Then ' R/O file
                MsgBox(SysMsg(14) & ": " & f, , orig)
                'Debug.WriteLine("rc = 20")
                rc = 20
                Exit Function
            End If
        Else
            SaveFileExisted = False
        End If
        WritableFile = True
    End Function
    Private Sub AddUndo(ByVal TypeOfChange As Integer, ByVal dsScr As ScreenLine)
        Dim LowKeep, i As Integer
        If Not CurrEdtSession.UnDoing Then  ' new change: no more redo!
            For Each lRedos In CurrEdtSession.RedoS ' release memory
                lRedos = Nothing
            Next
            CurrEdtSession.RedoS.Clear()
        End If
        Dim lUndo As sUndo
        If CurrEdtSession.DoUnDo Then
            If TypeOfChange = 3 AndAlso CurrEdtSession.UndoLineP = CurrEdtSession.CursorDisplayLine AndAlso (CurrEdtSession.UndoPosP + 1) = CurrEdtSession.CursorDisplayColumn Then
                CurrEdtSession.IncrUnDoCnt = False ' continous changes in one line counts as one
            End If
            If CurrEdtSession.IncrUnDoCnt Then
                CurrEdtSession.chgCount += 1
                LowKeep = CurrEdtSession.UnDoCnt - CurrEdtSession.UndoSet + 1
                For i = CurrEdtSession.UndoS.Count() To 1 Step -1
                    lUndo = DirectCast(CurrEdtSession.UndoS.Item(i), sUndo)
                    If lUndo.UndoGrp <= LowKeep Then
                        lUndo = Nothing
                        CurrEdtSession.UndoS.Remove(i)
                    End If
                Next
                CurrEdtSession.UnDoCnt = CurrEdtSession.UnDoCnt + 1
            End If
            If Not (TypeOfChange = 3 AndAlso CurrEdtSession.UndoLineP = CurrEdtSession.CursorDisplayLine AndAlso (CurrEdtSession.UndoPosP + 1) = CurrEdtSession.CursorDisplayColumn) Then
                If CurrEdtSession.IncrUnDoCnt Then
                    CurrEdtSession.AutosaveModifications += 1
                End If
                lUndo = New sUndo ' change is not continous
                lUndo.UndoGrp = CurrEdtSession.UnDoCnt
                lUndo.UndoCursorLine = CurrEdtSession.CursorDisplayLine
                lUndo.UndoCursorPos = CurrEdtSession.CursorDisplayColumn
                lUndo.UndoCurLine = CurLnLineLr
                lUndo.UndoLineNr = dsScr.CurSrcNr
                lUndo.UndoT = TypeOfChange
                If TypeOfChange = 2 Then ' insert
                    lUndo.UndoSrc = ""
                Else
                    If Not dsScr.CurSrcRead Then
                        ReadSourceInScrBuf(dsScr)
                    End If
                    lUndo.UndoSrc = dsScr.CurLinSrc
                End If
                CurrEdtSession.UndoS.Add(lUndo)
            End If
        End If
        If CurrEdtSession.IncrUnDoCnt Then
            CurrEdtSession.IncrUnDoCnt = False
            If CurrEdtSession.EditAutoSave > 0 And CurrEdtSession.EditAutoSave <= CurrEdtSession.AutosaveModifications Then
                Dim fs As String
                CurrEdtSession.AutoSavedTimes += 1
                fs = CurrEdtSession.EditFileName & " AUTOSAVE " & CStr(CurrEdtSession.AutoSavedTimes)
                CurrEdtSession.AutoSavNames.Add(fs)
                DoCmd1("SAVE " & fs, False)
                CurrEdtSession.AutosaveModifications = 0
            End If
        End If
        CurrEdtSession.UndoLineP = CurrEdtSession.CursorDisplayLine
        CurrEdtSession.UndoPosP = CurrEdtSession.CursorDisplayColumn
    End Sub
    Private Sub UnDo()
        Dim i As Integer, sUndo As Boolean
        Dim lUndo As sUndo
        Dim found, pScope, pLinend As Boolean
        Dim dsScr As ScreenLine, dsssrc As String, retLine As Integer
        pScope = CurrEdtSession.ScopeAllDisplay
        pLinend = CurrEdtSession.LinEndOff
        CurrEdtSession.LinEndOff = True
        CurrEdtSession.ScopeAllDisplay = True
        dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine) ' inserted line
        retLine = dsScr.CurSrcNr
        sUndo = CurrEdtSession.DoUnDo
        CurrEdtSession.DoUnDo = False
        found = False
        For i = CurrEdtSession.UndoS.Count() To 1 Step -1
            lUndo = DirectCast(CurrEdtSession.UndoS.Item(i), sUndo)
            If lUndo.UndoGrp = CurrEdtSession.UnDoCnt Then
                found = True
                CurrEdtSession.UnDoing = True ' don't disturb Redo queue
                If lUndo.UndoT = 1 Then 'delete
                    MoveToSourceLine(lUndo.UndoLineNr - 1)
                    DoCmd1("INPUT " & lUndo.UndoSrc, False)
                ElseIf lUndo.UndoT = 2 Then  'insert
                    MoveToSourceLine(lUndo.UndoLineNr)
                    dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                    If Not dsScr.CurSrcRead Then
                        ReadSourceInScrBuf(dsScr)
                    End If
                    dsssrc = dsScr.CurLinSrc
                    DoCmd1("DELETE 1", False)
                    lUndo.UndoSrc = dsssrc ' source to re-insert in case of Redo
                ElseIf lUndo.UndoT = 3 Then
                    MoveToSourceLine(lUndo.UndoLineNr)
                    dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                    If Not dsScr.CurSrcRead Then
                        ReadSourceInScrBuf(dsScr)
                    End If
                    dsssrc = dsScr.CurLinSrc
                    DoCmd1("REPLACE " & lUndo.UndoSrc, False)
                    lUndo.UndoSrc = dsssrc ' source to re-replace in case of Redo
                End If
                CurrEdtSession.RedoS.Add(lUndo)
                CurrEdtSession.UnDoing = False
                CurrEdtSession.CursorDisplayColumn = lUndo.UndoCursorPos
                CurrEdtSession.CursorDisplayLine = lUndo.UndoCursorLine
                retLine = lUndo.UndoCurLine
                CurrEdtSession.UndoS.Remove(i)
            Else
                Exit For
            End If
        Next
        If Not found Then rc = 4
        If CurrEdtSession.UndoS.Count() > 0 Then
            lUndo = DirectCast(CurrEdtSession.UndoS.Item(CurrEdtSession.UndoS.Count()), sUndo)
            CurrEdtSession.UnDoCnt = lUndo.UndoGrp
        Else
            CurrEdtSession.UnDoCnt = 0
        End If
        MoveToSourceLine(retLine)
        CurrEdtSession.ScopeAllDisplay = pScope
        CurrEdtSession.LinEndOff = pLinend
        CurrEdtSession.DoUnDo = sUndo
        CurrEdtSession.UndoLineP = 0
        CurrEdtSession.UndoPosP = 0
    End Sub
    Private Sub ReDo()
        If CurrEdtSession.RedoS.Count() > 0 Then
            Dim pScope, pLinend As Boolean
            Dim dsScr As ScreenLine, retline As Integer
            pScope = CurrEdtSession.ScopeAllDisplay
            pLinend = CurrEdtSession.LinEndOff
            CurrEdtSession.LinEndOff = True
            CurrEdtSession.ScopeAllDisplay = True
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine) ' current line
            retline = dsScr.CurSrcNr
            Dim lRedo As sUndo = DirectCast(CurrEdtSession.RedoS.Item(CurrEdtSession.RedoS.Count()), sUndo)
            Dim RedoGrp As Integer = lRedo.UndoGrp
            While CurrEdtSession.RedoS.Count() > 0 AndAlso lRedo.UndoGrp = RedoGrp
                CurrEdtSession.UnDoing = True ' don't disturb Redo queue
                If lRedo.UndoT = 2 Then 'insert deleted
                    MoveToSourceLine(lRedo.UndoLineNr - 1)
                    DoCmd1("INPUT " & lRedo.UndoSrc, False)
                ElseIf lRedo.UndoT = 1 Then  'delete inserted
                    MoveToSourceLine(lRedo.UndoLineNr)
                    DoCmd1("DELETE 1", False)
                ElseIf lRedo.UndoT = 3 Then
                    MoveToSourceLine(lRedo.UndoLineNr)
                    DoCmd1("REPLACE " & lRedo.UndoSrc, False)
                End If
                CurrEdtSession.UnDoing = False
                CurrEdtSession.CursorDisplayColumn = lRedo.UndoCursorPos
                CurrEdtSession.CursorDisplayLine = lRedo.UndoCursorLine
                retline = lRedo.UndoCurLine
                lRedo = Nothing
                CurrEdtSession.RedoS.Remove(CurrEdtSession.RedoS.Count())
                If CurrEdtSession.RedoS.Count() > 0 Then lRedo = DirectCast(CurrEdtSession.RedoS.Item(CurrEdtSession.RedoS.Count()), sUndo)
            End While
            CurrEdtSession.LinEndOff = pLinend
            CurrEdtSession.ScopeAllDisplay = pScope
            MoveToSourceLine(retline)
        End If
    End Sub
    Sub VSCREENProcs(Commandline As String)
        Dim cmd As String = NxtWordFromStr(Commandline, "")
        Select Case cmd
            Case "DEFINE"
                VSCREENname = NxtWordFromStr(Commandline, "screen")
                VSCREENlines = NxtNumFromStr(Commandline, "24")
                VSCREENcols = NxtNumFromStr(Commandline, "80")
            Case "WRITE"
                VSCREENname = NxtWordFromStr(Commandline, "screen")
                Dim ln As Integer = NxtNumFromStr(Commandline, "1") - 1
                Dim cl As Integer = NxtNumFromStr(Commandline, "1") - 1
                Dim nc As Integer = NxtNumFromStr(Commandline, "0")
                VSCREENarea(ln, cl, 1) = "U" ' attrbyte: unprot
                VSCREENarea(ln, cl, 2) = "7" ' colorbyte: black
                Dim options As String = Commandline.Substring(Commandline.IndexOf("(") + 1)
                While options.Length > 0
                    Dim op As String = NxtWordFromStr(options, " ").ToUpper()
                    If Abbrev(op, "UNPROT", 4) Then
                        VSCREENarea(ln, cl, 1) = "U" ' attr unprot
                    ElseIf Abbrev(op, "PROT", 2) Then
                        VSCREENarea(ln, cl, 1) = "P" ' attr prot
                    ElseIf op = "FIELD" Then
                        For i = 1 To options.Length
                            If cl + i <= VSCREENarea.GetUpperBound(1) Then
                                VSCREENarea(ln, cl + i, 0) = options.Substring(i - 1, 1) 'character on screen in this position
                                VSCREENarea(ln, cl + i, 1) = "T"c ' attrbyte: processing a field 
                                VSCREENarea(ln, cl + i, 2) = " "c ' colorByte: none
                            End If
                        Next
                        If options.Length < VSCREENarea.GetUpperBound(1) AndAlso (VSCREENarea(ln, cl + options.Length + 1, 1) = vbNullChar OrElse VSCREENarea(ln, cl + options.Length + 1, 1) = " "c) Then
                            VSCREENarea(ln, cl + options.Length + 1, 1) = "E" ' end of field attribute byte, if next position is not occupied
                        End If
                        options = ""
                    ElseIf Abbrev(op, "BLUE", 1) Then
                        VSCREENarea(ln, cl, 2) = "1" ' color
                    ElseIf Abbrev(op, "RED", 1) Then
                        VSCREENarea(ln, cl, 2) = "2" ' color
                    ElseIf Abbrev(op, "PINK", 1) Then
                        VSCREENarea(ln, cl, 2) = "3" ' color
                    ElseIf Abbrev(op, "GREEN", 1) Then
                        VSCREENarea(ln, cl, 2) = "4" ' color
                    ElseIf Abbrev(op, "TURQUOISE", 1) Then
                        VSCREENarea(ln, cl, 2) = "5" ' color
                    ElseIf Abbrev(op, "YELLOW", 1) Then
                        VSCREENarea(ln, cl, 2) = "6" ' color
                    ElseIf Abbrev(op, "WHITE", 1) Then
                        VSCREENarea(ln, cl, 2) = "7" ' color BLACK
                    ElseIf Abbrev(op, "BLACK", 2) Then
                        VSCREENarea(ln, cl, 2) = "7" ' color BLACK
                    ElseIf Abbrev(op, "DEFAULT", 1) Then
                        VSCREENarea(ln, cl, 2) = "7" ' color
                    End If
                End While
            Case "CURSOR"
                VSCREENname = NxtWordFromStr(Commandline, "screen")
                VSCREENcursorline = NxtNumFromStr(Commandline, "1") - 1
                VSCREENcursorcol = NxtNumFromStr(Commandline, "1")
            Case "WAITREAD"
                VSCREENname = NxtWordFromStr(Commandline, "screen")
                Dim vsc As New Vscreen
                VSCREENRexx = Rxs
                While Commandline <> ""
                    Dim nWrd As String = NxtWordFromStr(Commandline, " ").ToUpper()
                    vsc.TopMost = (nWrd = "TOPMOST") Or vsc.TopMost
                    vsc.ClickToEnter = (nWrd = "CLICK") Or vsc.ClickToEnter
                End While
                If vsc.TopMost Then Me.Visible = False
                vsc.ShowDialog()
                vsc.Close()
                vsc.Dispose()
                If vsc.TopMost Then Me.Visible = True
                vsc = Nothing
        End Select
    End Sub
#If DEBUG Or tracen Then
    Public Sub DumpScr() ' for testing only : display  the actual screenbuffer
        Dim i As Integer, dsScr As ScreenLine, s As String
        i = 0
        For Each dsScr In ScrList
            i += 1
            s = dsScr.CurLinSrc
            If Not dsScr.CurSrcRead Then s = "not read ........"
            Debug.WriteLine(CStr(i) & ": " & CStr(dsScr.CurLinType) & " " & CStr(dsScr.CurSrcNr) & " " & CStr(dsScr.CurRepaint) & " " & dsScr.CurLinNr & " " & s)
            Logg(CStr(i) & ": " & CStr(dsScr.CurLinType) & " " & CStr(dsScr.CurSrcNr) & " " & CStr(dsScr.CurRepaint) & " " & dsScr.CurLinNr & " " & s)
        Next
    End Sub
#End If
    Private Sub XeditPc_Activated(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Activated
        Logg("XeditPc_Activated start")
        If Not RexxCmdActive Then
            RepaintAllScreenLines = True
            Invalidate()
        End If
        ForcePaint()
        Logg("XeditPc_Activated end")
    End Sub
    Private Sub XeditPc_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.GotFocus
        Logg("XeditPc_GotFocus start")
        If FormShown Then
            RepaintAllScreenLines = True
            InvalidatedWin = True
            ForcePaint()
        End If
        Logg("XeditPc_GotFocus end")
    End Sub
    Private Sub XeditPc_Paint(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Me.Paint
        If Not FormShown Or RexxCmdActive Then Return
#If Not DEBUG Then
        Try
#End If
        Logg("XeditPc_Paint start")
        Dim crLine, j, n, SourceI As Integer
        Dim CharsOnScreenToShow As Integer
        Dim StrToBuild As System.Text.StringBuilder, StrToShow, StrToShowTabs, StrToShowExp, TxtCurLinCr, VerCurStr, sNr As String
        Dim Bsl, Sel, Asl, VerF, VerLn As Integer
        Dim g As Graphics = e.Graphics
        Me.BackColor = CurrEdtSession.color_screen
        Dim selectBrush As New SolidBrush(CurrEdtSession.color_select)
        Dim selectBgBrush As New SolidBrush(CurrEdtSession.color_selectbg)
        Dim commandBrush As New SolidBrush(CurrEdtSession.color_command)
        Dim commandbgBrush As New SolidBrush(CurrEdtSession.color_commandbg)
        Dim linenrBrush As New SolidBrush(CurrEdtSession.color_linenr)
        Dim curlineBrush As New SolidBrush(CurrEdtSession.color_curline)
        Dim linenrBgBrush As New SolidBrush(CurrEdtSession.color_linenrbg)
        Dim lineWithCursorBrush As New SolidBrush(CurrEdtSession.color_textcursor)
        Dim textBgBrush As New SolidBrush(CurrEdtSession.color_textbg)
        Dim textBrush As New SolidBrush(CurrEdtSession.color_text)
        Dim whiteBrush As New SolidBrush(Color.White)
        Dim redPen As New Pen(CurrEdtSession.color_cursor, 2)
        Dim aRectangle As Rectangle
        Dim EditSize As SizeF
        Dim EditFont As New Font("Courier New", FontSizeOnForm)
        Dim dsScr, dsScrn As ScreenLine
        Dim dsSource As SourceLine
        Dim dsSrcDm As SourceLine = New SourceLine ' dummy source, for C, M, 0, etc.-type of lines
        If QuitPgm Then Exit Sub
        EditTextHeight = EditFont.Height
        RectHeight = CInt(EditTextHeight) + 1
        EditSize = g.MeasureString("0", EditFont)
        Dim ExtraWidth1stChar As Single = EditSize.Width ' width of 1st char on a line
        EditSize = g.MeasureString("00", EditFont)
        EditTextWidth = EditSize.Width - ExtraWidth1stChar ' width of 2nd to last char on a line
        CurrEdtSession.EditTextWidth = EditTextWidth
        CurrEdtSession.RectHeight = RectHeight
        CurrEdtSession.EditTextHeight = EditTextHeight
        ExtraWidth1stChar = ExtraWidth1stChar - EditTextWidth 'extra width of 1st char on line
        LinesScreenVisible = CInt(Math.Floor(ClientRectangle.Size.Height / RectHeight))
        pCharsScreenVisible = CharsOnScreen
        CharsOnScreen = CInt(CSng(ClientSize.Width - VSB.Width) / EditTextWidth) - 7S
        CharsOnScreenToShow = CharsOnScreen
        If CurrEdtSession.ShowEol Then CharsOnScreenToShow -= 1
        If InvalidatedWin Then ' we enter PAINT from a previous PAINT that modified parts of the window data
            Logg("XeditPc_Paint paint again")
            InvalidatedWin = False ' now we just have to paint those parts
        Else ' First, see if any part of the screen is modified: if so, invalidate those parts and invoke paint again
            If ScrList.Count = 0 Then ' first time around, create and fill SCREEN list
                Logg("XeditPc_Paint build new screenbuf")
                Dim NrLine As Integer = 1 ' first line with filename etc.
                If CurrEdtSession.CmdLineNr <= CurrEdtSession.CurLineNr And CurrEdtSession.CmdLineNr > -1 Then NrLine += 1S
                If CurrEdtSession.MsgLineNrF <= CurrEdtSession.CurLineNr And Not CurrEdtSession.MsgOverlay Then NrLine += CurrEdtSession.MsgLineNrT
                'CurrEdtSession.NrBeforeCurL = CurrEdtSession.CurLineNr - NrLine - 1S
                FillScreenBuffer(1, True)
            End If
            While ScrList.Count < LinesScreenVisible And LinesScreenVisible > 0 ' visible screen was resized: lines added, add also to screen list
                Logg("XeditPc_Paint added")
                crLine = CurrEdtSession.SrcOnScrn(CurrEdtSession.nSrcOnScrn) ' find next sourcelinenr
                If crLine = 0 Then crLine = 1 ' close from taskbar
                dsScr = DirectCast(ScrList.Item(crLine), ScreenLine)
                SourceI = dsScr.CurSrcNr + 1
                crLine = ScrList.Count           ' find line where to insert
                If CurrEdtSession.CmdLineNr = -1 Then crLine -= 1
                j = crLine
                crLine = crLine + 1
                dsScr = New ScreenLine()
                GetTextOfScrline(dsScr, crLine, SourceI, True)
                dsScr.CurRepaint = True        '
                ScrList.Add(dsScr, , , j)
                CurrEdtSession.SrcOnScrn(CurrEdtSession.nSrcOnScrn) += 1S
            End While
            If ScrList.Count > LinesScreenVisible Then ' screen was resized: lines deleted, delete from screenlist
                Logg("XeditPc_Paint deleted")
                Dim dsCmd As New ScreenLine
                If CurrEdtSession.CmdLineNr = -1 Then
                    dsCmd = DirectCast(ScrList.Item(ScrList.Count), ScreenLine)
                    Logg("Scrlist remove last line: " + CStr(ScrList.Count))
                    ScrList.Remove(ScrList.Count)
                    LinesScreenVisible -= 1S
                End If
                While ScrList.Count > LinesScreenVisible And LinesScreenVisible > 0 ' lines deleted, delete from screenlist
                    crLine = ScrList.Count ' find line where to insert
                    dsScr = DirectCast(ScrList.Item(crLine), ScreenLine)
                    If Not dsScr.CurLinFixTp Then
                        SaveModifiedLine(dsScr)
                    End If
                    dsScr = Nothing
                    Logg("Scrlist remove line: " + CStr(crLine))
                    ScrList.Remove(crLine)
                End While
                If CurrEdtSession.CmdLineNr = -1 Then
                    LinesScreenVisible += 1S
                    ScrList.Add(dsCmd)
                End If
                CalcIxInSLines()
            End If
            If CurrEdtSession.PrevEditLineScr < 1 Or CurrEdtSession.PrevEditLineScr > LinesScreenVisible Then
                CurrEdtSession.PrevEditLineScr = 1
            End If
            If CurrEdtSession.CursorDisplayLine < 1 Or CurrEdtSession.CursorDisplayLine > LinesScreenVisible Then
                CurrEdtSession.CursorDisplayLine = 1
            End If
            If CurrEdtSession.CursorDisplayLine <> CurrEdtSession.PrevEditLineScr Or CurrEdtSession.CursorDisplayColumn <> CurrEdtSession.PrevEditPosScr Then
                Logg("XeditPc_Paint cursor moved")
                dsScr = DirectCast(ScrList.Item(CurrEdtSession.CursorDisplayLine), ScreenLine) 'cursor was moved: repaint old and new cursor line
                dsScr.CurRepaint = True
                If CurrEdtSession.CursorDisplayLine <> CurrEdtSession.PrevEditLineScr Then
                    dsScr = DirectCast(ScrList.Item(CurrEdtSession.PrevEditLineScr), ScreenLine) 'repaint cursor line(VerCurStr)
                    dsScr.CurRepaint = True
                    CurrEdtSession.PrevEditLineScr = CurrEdtSession.CursorDisplayLine
                End If
                CurrEdtSession.PrevEditPosScr = CurrEdtSession.CursorDisplayColumn
            End If
            dsScr = DirectCast(ScrList.Item(1), ScreenLine) 'always repaint 1st line
            GetTextOfScrline(dsScr, 1, 0, True)
            dsScr.CurRepaint = True
            If CurrEdtSession.MsgOverlay Then ' insert overlay messages, save sourcelines 
                If CurrEdtSession.ScrOverlayed.Count = 0 Then ' show messages
                    n = CurrEdtSession.MsgLineNrT
                    If n > CurrEdtSession.Msgs.Count Then n = CurrEdtSession.Msgs.Count
                    If CurrEdtSession.Msgs.Count > 0 Then
                    End If
                    Logg("XeditPc_Paint ovl msg" & CStr(n))
                    For crLine = 1 To n
                        dsScr = DirectCast(ScrList.Item(CurrEdtSession.MsgLineNrF - 1 + crLine), ScreenLine) 'repaint msg line
                        dsScrn = New ScreenLine
                        dsScrn.CopyFrom(dsScr) ' save in Scrn
                        CurrEdtSession.ScrOverlayed.Add(dsScrn)
                        dsScr.CurLinType = "M"c ' Message
                        dsScr.CurLinFixTp = True
                        dsScr.CurSrcRead = True
                        dsScr.CurLinSrc = DirectCast(CurrEdtSession.Msgs.Item(1), String)
                        dsScr.CurLinNr = ""
                        CurrEdtSession.Msgs.Remove(1)
                        dsScr.CurRepaint = True
                    Next
                End If
            Else ' insert message on fixed lines
                n = CurrEdtSession.MsgLineNrT
                If n > CurrEdtSession.Msgs.Count Then n = CurrEdtSession.Msgs.Count
                Logg("XeditPc_Paint fixed msg" & CStr(n))
                For crLine = 1 To n
                    dsScr = DirectCast(ScrList.Item(CurrEdtSession.MsgLineNrF - 1 + crLine), ScreenLine) 'repaint msg line
                    dsScr.CurLinSrc = DirectCast(CurrEdtSession.Msgs.Item(1), String)
                    CurrEdtSession.Msgs.Remove(1)
                    dsScr.CurRepaint = True
                Next
            End If
            If CharsOnScreen > pCharsScreenVisible Then ' need show more characters on line
                For Each dsScr In ScrList
                    If dsScr.CurLinSrc.Length() >= CharsOnScreen Then
                        dsScr.CurRepaint = True
                    End If
                Next
                pCharsScreenVisible = CharsOnScreen
            End If
            crLine = 0
            For Each dsScr In ScrList
                crLine += 1
                If dsScr.CurRepaint Or RepaintAllScreenLines Then
                    InvalidatedWin = True
                    RepaintLine(dsScr, crLine)
                End If
            Next
            If InvalidatedWin Then ' data to be painted has been hanged, start paint all over
                ForcePaint()
                InvalidatedWin = True ' Forcepaint switched it off
                Logg("XeditPc_Paint end invalidated" & CStr(n))
                Exit Sub
            End If
        End If
        VSB.Visible = (LinesScreenVisible < CurrEdtSession.SourceList.Count)
        VSB.Maximum = CurrEdtSession.SourceList.Count / 10
        If VSB.Maximum < 100 Then VSB.Maximum = 100
        VSB.Height = ClientSize.Height
        VSB.Width = ClientSize.Width / 90
        VSB.Left = ClientSize.Width - VSB.Width - 3
        dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
        Dim vv As Integer
        If LinesScreenVisible <> 0 And CurrEdtSession.SourceList.Count <> 0 Then
            vv = Math.Round(CDbl(dsScr.CurSrcNr) / CDbl(CurrEdtSession.SourceList.Count) * VSB.Maximum)
        Else
            vv = 1
        End If
        If vv > VSB.Maximum Then vv = VSB.Maximum
        If vv < VSB.Minimum Then vv = VSB.Minimum
        VSB.Value = vv
        Debug.WriteLine("XeditPc_Paint start")
        Dim vp As VerifyPair = DirectCast(CurrEdtSession.Verif.Item(1), VerifyPair)
        CurrEdtSession.mseSelLeftVer = CurrEdtSession.mseSelLeft - CInt(vp.VerFrom - 1)
        CurrEdtSession.mseSelRightVer = CurrEdtSession.mseSelRight - CInt(vp.VerFrom - 1)
        Dim TxtCurLin As String
        TxtCurLinCr = " "
        Dim chRs(6) As CharacterRange
        Dim ForegrBrush(6) As SolidBrush
        Dim BgBrush(6) As SolidBrush
        For crLine = 1 To LinesScreenVisible  ' repaint the screen
#If Not DEBUG Then
                Try
#End If
            TxtCurLin = ""
            dsScr = DirectCast(ScrList.Item(crLine), ScreenLine)
            If dsScr.CurRepaint Or RepaintAllScreenLines Then
                dsScr.CurRepaint = False
                If dsScr.CurLinType = "L"c Then
                    dsSource = DirectCast(dsScr.CurLinSsd, SourceLine)
                Else
                    dsSource = dsSrcDm
                End If
                If Not dsScr.CurSrcRead Then
                    ReadSourceInScrBuf(dsScr)
                End If
                sNr = dsScr.CurLinNr
                If dsScr.CurLinType = "L"c Or dsScr.CurLinType = "T"c Or dsScr.CurLinType = "B"c Or dsScr.CurLinType = "X"c Then
                    If CurrEdtSession.FindLineCmd(dsScr.CurSrcNr, j) Then
                        Dim mLnCmd As LineCommand
                        mLnCmd = DirectCast(CurrEdtSession.LineCommands.Item(j), LineCommand)
                        sNr = mLnCmd.LinecmdText
                        If mLnCmd.RepeatFactorPresent Then sNr = sNr & CStr(mLnCmd.RepeatFactor)
                        sNr = sNr.PadRight(6).Substring(0, 6)
                    End If
                Else
                    If dsScr.CurLinType = "M"c AndAlso dsScr.CurLinSrc.Length() > 0 Then sNr = "MSG.. "
                End If
                sNr = sNr.PadRight(6)
                TxtCurLin += sNr + " "
                chRs(0) = New CharacterRange(0, sNr.Length)
                BgBrush(0) = linenrBgBrush
                If dsScr.CurLinType = "C"c Then
                    ForegrBrush(0) = commandBrush
                    BgBrush(0) = commandbgBrush
                ElseIf dsScr.CurLinType <> "L"c Then
                    ForegrBrush(0) = linenrBrush
                    BgBrush(0) = whiteBrush
                ElseIf crLine = CurrEdtSession.CurLineNr Then
                    ForegrBrush(0) = curlineBrush
                Else
                    ForegrBrush(0) = linenrBrush
                End If
                StrToShow = dsScr.CurLinSrc
                If dsScr.CurLinType = "L"c Then
                    If CurrEdtSession.ShowEol Then
                        StrToShow = StrToShow & Chr(182)
                    End If
                    StrToShowExp = StrToShow ' with tabs expanded, if tabs on 
                    StrToShowTabs = StrToShow ' with tabs original for display hex in U8 and Unicode files
                    Dim nTabs As Integer = -1
                    dsScr.tabExpandPos(0) = 0
                    Dim i As Integer = StrToShowExp.IndexOf(vbTab)
                    If i > -1 Then
                        If CurrEdtSession.ExpTabs Then
                            While i > -1
                                Dim PosOfNextTab As Integer = -1
                                For k As Integer = 1 To CurrEdtSession.Tabs.Length - 1
                                    If CurrEdtSession.Tabs(k) = 0 Then Exit For
                                    If i + 2 <= CurrEdtSession.Tabs(k) Then
                                        PosOfNextTab = CurrEdtSession.Tabs(k)
                                        Exit For
                                    End If
                                Next
                                If PosOfNextTab = -1 Then
                                    PosOfNextTab = i + 2 ' replace tab by 1 space and continue there
                                End If
                                Dim st As String = StrToShowExp.Substring(0, i)
                                If PosOfNextTab - i - 1 > 0 Then st = st + "".PadRight(PosOfNextTab - i - 1, " "c)
                                StrToShowExp = st + StrToShowExp.Substring(i + 1)
                                If nTabs < dsScr.tabExpandPos.Length - 1 Then
                                    nTabs += 1
                                End If
                                dsScr.tabExpandPos(nTabs) = PosOfNextTab
                                i = StrToShowExp.IndexOf(vbTab)
                            End While
                            dsScr.CurLinSrcExp = StrToShowExp ' if expanded text is changed, tabs dissapear from source, if not they remain: edit with tabs off
                        Else
                            If CurrEdtSession.EncodingType <> "A"c Then
                                StrToShow = StrToShow.Replace(vbTab, "•"c)
                                If Not TabSymbMsg Then
                                    DoCmd("MSG • " + SysMsg(25), False)
                                    TabSymbMsg = True
                                End If
                            End If
                        End If
                    End If
                    StrToBuild = New System.Text.StringBuilder(CharsOnScreenToShow)
                    Dim LengthPrevVer As Integer = 0
                    Dim CurVerIx As Integer = -1 ' each VER set
                    Dim VerTextPos As Integer = 0 ' pos of char in text
                    For Each vp In CurrEdtSession.Verif
                        CurVerIx += 1
                        dsScr.VerifPartFrom(CurVerIx) = StrToBuild.ToString.Length ' this is pos on screen, with (ix) -> curr.Verif.from -> source pos
                        dsScr.VerifPartHex(CurVerIx) = vp.VerHex
                        VerF = vp.VerFrom - 1
                        VerLn = vp.VerTo - VerF
                        If VerF < 0 Then VerF = 0
                        If VerLn < 0 Then VerLn = 0
                        If vp.VerHex Then
                            VerCurStr = C2X(StrToShowTabs) ' always including tabs
                            VerLn *= 2 ' 2 chars per symbol
                            VerF = VerF * 2
                        Else
                            If CurrEdtSession.ExpTabs Then ' tabs shown if no SET TABS active
                                VerCurStr = StrToShowExp
                            Else
                                VerCurStr = StrToShow
                            End If
                        End If
                        If VerLn > CharsOnScreenToShow Then VerLn = CharsOnScreenToShow
                        If VerF >= VerCurStr.Length Then ' outside visible part
                            VerCurStr = ""
                        Else
                            If VerF <> 0 Then VerCurStr = VerCurStr.Substring(VerF)
                        End If
                        If (VerCurStr.Length > VerLn And Not CurrEdtSession.ShowEol) OrElse (VerCurStr.Length > VerLn + 1 And CurrEdtSession.ShowEol) Then
                            VerCurStr = VerCurStr.Substring(0, VerLn)
                        Else
                            VerCurStr = VerCurStr.PadRight(VerLn, " "c)
                        End If
                        StrToBuild.Insert(LengthPrevVer, VerCurStr)
                        LengthPrevVer = LengthPrevVer + VerCurStr.Length()
                        dsScr.VerifPartLen(CurVerIx) = StrToBuild.ToString.Length - dsScr.VerifPartFrom(CurVerIx)
                    Next
                    If LengthPrevVer > CharsOnScreenToShow Then
                        StrToShow = StrToBuild.ToString.Substring(0, CharsOnScreenToShow)
                    Else
                        StrToShow = StrToBuild.ToString
                    End If
                End If
                StrToShow = StrToShow.TrimEnd()
                If CurrEdtSession.EncodingType = "A"c Then
                    Dim buf() As Byte = System.Text.Encoding.Default.GetBytes(StrToShow)
                    UnPrint(buf)
                    StrToShow = System.Text.Encoding.Default.GetString(buf)
                End If
                dsScr.CharsOnScr = CInt(StrToShow.Length())
                CalcSelectedLineParts(dsScr, StrToShow, Bsl, Sel, Asl)
                Dim PntPiece As Integer = 1
                Logg("XeditPc_Paint line " & CStr(crLine) & " sel: " & CStr(Bsl) & " " & CStr(Sel) & " " & CStr(Asl))
                TxtCurLin += StrToShow
                If Bsl > 0 Then
                    chRs(PntPiece) = New CharacterRange(7, Bsl)
                    BgBrush(PntPiece) = textBgBrush
                    If dsScr.CurLinType = "C"c Then
                        ForegrBrush(PntPiece) = commandBrush
                        BgBrush(PntPiece) = commandbgBrush
                    ElseIf crLine = CurrEdtSession.CursorDisplayLine Then
                        ForegrBrush(PntPiece) = lineWithCursorBrush
                    Else
                        ForegrBrush(PntPiece) = textBrush
                    End If
                    PntPiece += 1
                End If
                If Sel > 0 Then
                    chRs(PntPiece) = New CharacterRange(7 + Bsl, Sel)
                    ForegrBrush(PntPiece) = selectBrush
                    BgBrush(PntPiece) = selectBgBrush
                    PntPiece += 1
                End If
                If Asl > 0 Then
                    chRs(PntPiece) = New CharacterRange(7 + Bsl + Sel, Asl)
                    BgBrush(PntPiece) = textBgBrush
                    If dsScr.CurLinType = "C"c Then
                        ForegrBrush(PntPiece) = commandBrush
                        BgBrush(PntPiece) = commandbgBrush
                    ElseIf crLine = CurrEdtSession.CursorDisplayLine Then
                        ForegrBrush(PntPiece) = lineWithCursorBrush
                    Else
                        ForegrBrush(PntPiece) = textBrush
                    End If
                    PntPiece += 1
                End If
                Dim chRsa As Array = Array.CreateInstance(GetType(CharacterRange), PntPiece)
                Array.Copy(chRs, 0, chRsa, 0, PntPiece)
                aRectangle = New Rectangle(0, CInt((crLine - 1) * RectHeight) - 2, ClientSize.Width - VSB.Width, RectHeight) ' x, y, w, h 
                ' Set string format.
                Dim stringFormat As New StringFormat
                stringFormat.SetMeasurableCharacterRanges(chRsa)
                ' Measure ranges in string.
                Dim stringRegions As Array = Array.CreateInstance(GetType([Region]), PntPiece)
                stringRegions = e.Graphics.MeasureCharacterRanges(TxtCurLin, EditFont, aRectangle, stringFormat)
                For ir As Integer = stringRegions.GetLowerBound(0) To stringRegions.GetUpperBound(0)
                    Dim measureRect1 As RectangleF = stringRegions(ir).GetBounds(e.Graphics)
                    Logg("  |" & TxtCurLin.Substring(chRs(ir).First, chRs(ir).Length) & "|")
                    g.FillRectangle(BgBrush(ir), measureRect1)
                    g.DrawString(TxtCurLin.Substring(chRs(ir).First, chRs(ir).Length), EditFont, ForegrBrush(ir), CSng(measureRect1.X), CSng(measureRect1.Y))
                Next
                If crLine = CurrEdtSession.CursorDisplayLine Then
                    TxtCurLinCr = TxtCurLin
                End If
            End If
#If Not DEBUG Then
                Catch ex As Exception
                    If MsgBox("ERROR displaying a line: " & ex.Message, MsgBoxStyle.OkCancel, "Internal Programming Error") = MsgBoxResult.Cancel Then
                        Me.Close()
                        End
                        Exit Sub
                    End If
                End Try
#End If
        Next
        ' draw the cursor in red, measure the text before the cursor pos
        Dim crRs(0) As CharacterRange
        If CurrEdtSession.CursorDisplayColumn < -6 Then CurrEdtSession.CursorDisplayColumn = -6
        crRs(0) = New CharacterRange(6 + CurrEdtSession.CursorDisplayColumn, 1)
        TxtCurLinCr = TxtCurLinCr.PadRight(9 + CurrEdtSession.CursorDisplayColumn)
        aRectangle = New Rectangle(0, CInt((CurrEdtSession.CursorDisplayLine - 1) * RectHeight + 1), ClientSize.Width - VSB.Width, RectHeight) ' x, y, w, h 
        Dim stringFormatC As New StringFormat
        stringFormatC.SetMeasurableCharacterRanges(crRs)
        Dim stringRegionsC(0) As [Region]
        stringRegionsC = e.Graphics.MeasureCharacterRanges(TxtCurLinCr, EditFont, aRectangle, stringFormatC)
        Dim measureRect2 As RectangleF = stringRegionsC(0).GetBounds(e.Graphics)
        Dim CuX As Integer = measureRect2.X + 2
        Dim CuXt As Integer = CuX
        Dim CuY As Integer = CInt((CurrEdtSession.CursorDisplayLine) * RectHeight) + 1
        Dim CuYt As Integer = CuY
        If Not CurrEdtSession.InsOvertype Then
            CuXt = CuX + CInt(EditTextWidth)
            CuYt = CuY - 1
            CuY = CuY - 1
        Else
            CuYt = CuY - CInt(EditTextHeight)
        End If
        Dim pF As New Point(CuX, CuY)
        Dim pT As New Point(CuXt, CuYt)
        g.DrawLine(redPen, pF, pT)
        g.Dispose()
        RepaintAllScreenLines = False
        CalcIxInSLines()
        CurrEdtSession.SessionInited = True
        Logg("XeditPc_Paint end")
#If Not DEBUG Then
        Catch ex As Exception
            MsgBox("ERROR displaying the screen. " & ex.Message, MsgBoxStyle.Critical, "Internal Programming Error")
        End Try
#End If
    End Sub
    Sub UnPrint(buf() As Byte)
        For i As Integer = 0 To buf.Length - 1 ' remove unprintables"
            If buf(i) < 32 Or buf(i) = 127 Or buf(i) = 129 Or buf(i) = 131 Or buf(i) = 136 Or buf(i) = 141 Or buf(i) = 144 Or buf(i) = 150 Or buf(i) = 151 Or buf(i) = 152 Or buf(i) = 157 Then
                buf(i) = 149
            End If
        Next
    End Sub
    Function cb(s As String) As String
        Return (s + " ")
    End Function
    Private Sub VScrollBar1_ValueChanged(sender As Object, e As EventArgs) Handles VSB.ValueChanged
        Logg("VBS start value ")
        'Debug.WriteLine("V -" + CStr(VSB.Minimum) + " v " + CStr(VSB.Value) + " + " + CStr(VSB.Maximum))
        If Me.WindowState <> FormWindowState.Minimized Then
            If MousePosX > (Me.ClientSize.Width - VSB.Width - 3) Then
                Logg("VBS strt timer")
                TimerBar.Enabled = False
                TimerBar.Interval = 100
                TimerBar.Enabled = True
            End If
        End If
        Logg("VBS end value ")
    End Sub
    Private Sub TimerBar_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TimerBar.Tick
        Logg("VBS timer start")
        TimerBar.Enabled = False
        Try
            Dim l = Math.Round(CDbl(VSB.Value) / CDbl(VSB.Maximum) * CurrEdtSession.SourceList.Count)
            DoCmd(":" & CStr(l), True)
        Catch ex As Exception
        End Try
        Logg("VBS timer end")
    End Sub
    Private Sub CalcIxInSLines()
        Dim dsScr As ScreenLine ', s As String = ""
        CurrEdtSession.nSrcOnScrn = 0 ' index of sourcelines within ScrList
        Dim i As Integer = 0
        For Each dsScr In ScrList
            i += 1
            If Not dsScr.CurLinFixTp Then
                CurrEdtSession.nSrcOnScrn += 1S
                CurrEdtSession.SrcOnScrn(CurrEdtSession.nSrcOnScrn) = CInt(i)
                ' s = s & CStr(i) & " "
            End If
        Next
        'Logg("SLines=" & s)
    End Sub
    Private Sub CalcSelectedLineParts(ByVal dsscr As ScreenLine, ByVal s As String, ByRef Bsl As Integer, ByRef Sel As Integer, ByRef Asl As Integer)
        Bsl = 0
        Sel = 0
        Asl = 0
        If dsscr.CurLinType <> "L"c Or Not CurrEdtSession.mSelect Or dsscr.CurSrcNr < CurrEdtSession.mseSelTop Or dsscr.CurSrcNr > CurrEdtSession.mseSelBot Or CurrEdtSession.mseSelTop = -1 Then
            ' CMDLINE has part selected?
            If dsscr.CurLinType = "C"c And CurrEdtSession.mseSelTop = -1 And CurrEdtSession.mSelect Then
                Dim ltxt, sl, sr As Integer
                ltxt = s.Length
                If ltxt > 0 Then
                    sr = CurrEdtSession.mseSelRight
                    sl = CurrEdtSession.mseSelLeft
                    If sl > ltxt Then sl = ltxt
                    If sr > ltxt Then sr = ltxt
                    Bsl = sl - 1
                    Sel = sr - Bsl
                    Asl = ltxt - Sel - Bsl
                End If
            Else
                Bsl = s.Length()
            End If
        Else ' source selected
            If (Not CurrEdtSession.mSelRctg And dsscr.CurSrcNr = CurrEdtSession.mseSelTop And dsscr.CurSrcNr = CurrEdtSession.mseSelBot) _
                       OrElse (CurrEdtSession.mSelRctg And dsscr.CurSrcNr >= CurrEdtSession.mseSelTop And dsscr.CurSrcNr <= CurrEdtSession.mseSelBot) _
                       Then ' only 1 line selected / each line in block selected as 1st
                Bsl = CurrEdtSession.mseSelLeftVer - 1
                If Bsl < 0 Then Bsl = 0
                Sel = CurrEdtSession.mseSelRightVer - Bsl
                If Sel < 0 Then Sel = 0
                Asl = s.Length() - Sel - Bsl
            Else
                If dsscr.CurSrcNr = CurrEdtSession.mseSelTop Then ' 1st line selected
                    Bsl = CurrEdtSession.mseSelLeftVer - 1
                    If Bsl < 0 Then Bsl = 0
                    Sel = s.Length() - Bsl
                Else
                    If dsscr.CurSrcNr = CurrEdtSession.mseSelBot And CurrEdtSession.mseSelTop <> -1 Then ' last line selected
                        Sel = CurrEdtSession.mseSelRightVer
                        If Sel < 0 Then Sel = 0
                        Asl = s.Length() - Sel
                    Else ' intermediate line selected
                        Sel = s.Length()
                    End If
                End If
            End If
            If Bsl > s.Length Then
                Bsl = s.Length
                Sel = 0
                Asl = 0
            Else
                If (Bsl + Sel) > s.Length Then
                    Sel = s.Length() - Bsl
                    Asl = 0
                Else
                    If (Bsl + Sel + Asl) > s.Length Then
                        Asl = s.Length() - Bsl - Sel
                    End If
                End If
            End If
        End If
    End Sub
    Private Sub InitDsScrStruct(ByVal dsScr As ScreenLine, ByVal SourceI As Integer)
        dsScr.CurNrLines = 1
        dsScr.CurSrcRead = True
        dsScr.CurLinModified = False
        dsScr.CurLinFixTp = False
        dsScr.CurSrcNr = SourceI
    End Sub
    Private Sub GetTextOfScrline(ByRef dsScr As ScreenLine, ByRef ScrI As Integer, ByRef SourceI As Integer, ByVal Down As Boolean)
        '   define screen line "ScrI" according sourceline(s) "SourceI"
        Dim SrcExcluded As Boolean, SourceDi As Integer
        Dim ssd As SourceLine
        If Down Then
            SourceDi = 1
        Else
            SourceDi = -1
        End If
        InitDsScrStruct(dsScr, SourceI)
        If ScrI = 1 Then
            dsScr.CurLinType = "F"c ' FileIdent
            dsScr.CurLinFixTp = True
            dsScr.CurLinNr = "XEDIT"
            Dim s As String = ""
            If EdtSessions.Count > 1 Then
                s = "(" & CStr(CurrEditSessionIx) & " / " & CStr(EdtSessions.Count) & ") "
            End If
            s = s & CurrEdtSession.EditFileName
            If s.Length() > 30 Then
                Dim nmAr() As String = Split(s, "\")
                If nmAr.Length < 5 Then
                    s = s
                ElseIf nmAr.Length = 5 Then
                    s = nmAr(0) & "\" & nmAr(1) & "\...\" & nmAr(3) & "\" & nmAr(4)
                Else
                    s = nmAr(0) & "\" & nmAr(1) & "\...\" & nmAr(nmAr.Length - 2) & "\" & nmAr(nmAr.Length - 1)
                End If
            End If
            If CurrEdtSession.RecfmV Then
                s = s & " V"
            Else
                s = s & " F"
            End If
            Dim vp As VerifyPair
            vp = DirectCast(CurrEdtSession.Verif.Item(1), VerifyPair)
            s = s & " " & CStr(CurrEdtSession.Lrecl)
            If CurrEdtSession.EncodingType = "U"c Then
                s = s & " Unicode"
            End If
            If CurrEdtSession.EncodingType = "8"c Then
                s = s & " UTF8"
            End If
            s = s & " TRUNC=" & CStr(CurrEdtSession.Trunc) & " SIZE=" & CStr(CurrEdtSession.SourceList.Count) & " LINE=" & CStr(CurrEdtSession.CursorDisplayLine) & " COLUMN=" & CStr(CurrEdtSession.CursorDisplayColumn - 1 + vp.VerFrom) & " ALT=" & CStr(CurrEdtSession.chgCount)
            dsScr.CurLinSrc = s
        ElseIf Not CurrEdtSession.MsgOverlay And ScrI >= CurrEdtSession.MsgLineNrF And ScrI <= (CurrEdtSession.MsgLineNrF + CurrEdtSession.MsgLineNrT - 1) Then
            dsScr.CurLinType = "M"c ' Message
            dsScr.CurLinFixTp = True
            dsScr.CurLinNr = ""
            dsScr.CurLinSrc = ""
        ElseIf ScrI = CurrEdtSession.CmdLineNr Or CurrEdtSession.CmdLineNr = -1 And ScrI = LinesScreenVisible Then
            dsScr.CurLinType = "C"c ' cmd
            dsScr.CurLinFixTp = True
            dsScr.CurLinNr = "=====>"
            If Not IsNothing(dsScr.CurLinSrc) AndAlso dsScr.CurLinSrc.Trim.Length > 0 AndAlso dsScr.CurLinSrc.Trim()(0) = "&"c Then
                ' nop
            Else
                dsScr.CurLinSrc = ""
            End If
        ElseIf CurrEdtSession.ReservedLines.Contains(CStr(ScrI)) Then
            dsScr.CurLinType = "R"c ' Reserved line
            dsScr.CurLinNr = ""
            dsScr.CurLinSrc = CStr(CurrEdtSession.ReservedLines.Item(CStr(ScrI)))
            dsScr.CurLinFixTp = True
        ElseIf CurrEdtSession.ReservedLines.Contains(CStr(ScrI - LinesScreenVisible)) And CurrEdtSession.CmdLineNr = -1 Then
            dsScr.CurLinType = "R"c ' Reserved line
            dsScr.CurLinNr = ""
            dsScr.CurLinSrc = CStr(CurrEdtSession.ReservedLines.Item(CStr(ScrI - LinesScreenVisible)))
            dsScr.CurLinFixTp = True
        ElseIf CurrEdtSession.ReservedLines.Contains(CStr(ScrI - LinesScreenVisible - 1)) And CurrEdtSession.CmdLineNr <> -1 Then
            dsScr.CurLinType = "R"c ' Reserved line
            dsScr.CurLinNr = ""
            dsScr.CurLinSrc = CStr(CurrEdtSession.ReservedLines.Item(CStr(ScrI - LinesScreenVisible - 1)))
            dsScr.CurLinFixTp = True
        ElseIf SourceI = 0 Then
            dsScr.CurLinType = "T"c ' TOP of file
            dsScr.CurLinNr = ""
            dsScr.CurLinSrc = "Top of file"
            SourceI += SourceDi
        ElseIf SourceI = CurrEdtSession.SourceList.Count() + 1 Then
            dsScr.CurLinType = "B"c '  BOT of file
            dsScr.CurLinNr = ""
            dsScr.CurLinSrc = "Bot of file"
            SourceI += SourceDi
        ElseIf SourceI > CurrEdtSession.SourceList.Count() Or SourceI < 0 Then
            dsScr.CurLinType = "0"c '  empty
            dsScr.CurLinNr = "......"
            dsScr.CurLinSrc = ""
            SourceI += SourceDi
        Else
            ssd = DirectCast(CurrEdtSession.SourceList.Item(SourceI), SourceLine)
            SrcExcluded = False
            If (ssd.SrcSelect <= CurrEdtSession.EditDisplayMax And ssd.SrcSelect >= CurrEdtSession.EditDisplayMin) Or CurrEdtSession.ScopeAllDisplay Then
                dsScr.CurLinType = "L"c ' Line of source
                dsScr.CurLinSsd = ssd
                dsScr.CurLinNr = SourceI.ToString("000000")
                dsScr.CurSrcRead = False
                dsScr.CurLinSrc = ""
            Else
                SrcExcluded = True
                dsScr.CurLinType = "X"c ' Line of source excluded
                dsScr.CurLinNr = "======"
                If Down Then ' count # lines excluded
                    SourceI += 1
                    While SourceI <= CurrEdtSession.SourceList.Count() And SrcExcluded
                        nrCyclesEv += 1 : If nrCyclesEv > 5000 Then CallDoEvent()
                        If CancelCmd Then Exit Sub
                        ssd = DirectCast(CurrEdtSession.SourceList.Item(SourceI), SourceLine)
                        SrcExcluded = False
                        If ssd.SrcSelect < CurrEdtSession.EditDisplayMin Or ssd.SrcSelect > CurrEdtSession.EditDisplayMax Then
                            SrcExcluded = True
                            dsScr.CurNrLines = dsScr.CurNrLines + 1
                            SourceI = SourceI + 1
                        End If
                    End While
                    SourceI -= 1
                Else
                    SourceI -= 1
                    While SourceI >= 1 And SrcExcluded
                        nrCyclesEv += 1 : If nrCyclesEv > 5000 Then CallDoEvent()
                        If CancelCmd Then Exit Sub
                        ssd = DirectCast(CurrEdtSession.SourceList.Item(SourceI), SourceLine)
                        SrcExcluded = False
                        If ssd.SrcSelect < CurrEdtSession.EditDisplayMin Or ssd.SrcSelect > CurrEdtSession.EditDisplayMax Then
                            SrcExcluded = True
                            dsScr.CurNrLines = dsScr.CurNrLines + 1
                            dsScr.CurSrcNr = SourceI
                            SourceI = SourceI - 1
                        End If
                    End While
                    SourceI += 1
                End If
                If CurrEdtSession.Shadow Then ' show X line
                    dsScr.CurLinSrc = CStr(dsScr.CurNrLines) & " lines excluded."
                Else ' Show next line after last excluded
                    SourceI += SourceDi
                    GetTextOfScrline(dsScr, ScrI, SourceI, Down)
                    SourceI -= SourceDi ' don't increase recursive
                End If
            End If
            SourceI += SourceDi
        End If
        Logg("GetTextOfScrline " & CStr(ScrI) & " " & dsScr.CurLinSrc)
    End Sub
    Private Sub ReadSourceInScrBuf(ByRef fCl As ScreenLine)
        Dim ssd As SourceLine
        fCl.CurSrcRead = True
        ssd = DirectCast(CurrEdtSession.SourceList.Item(fCl.CurSrcNr), SourceLine)
        fCl.CurLinSrc = ReadOneSourceLine(ssd)
        ' Debug.WriteLine("read source " & CStr(fCl.CurSrcNr) & " " & CStr(ssd.SrcFileIx) & " " & CStr(ssd.SrcStart) & " " & CStr(ssd.SrcLength) & " " & fCl.CurLinSrc)
    End Sub
    Dim WarnUtf8 As Boolean = False
    Private Function ReadOneSourceLine(ByVal ssd As SourceLine) As String
        Dim value As String = ""
        If ssd.SrcLength > -1 Then
            Dim buf(ssd.SrcLength - 1) As Byte
            EditRdFile = FileWithData(ssd.SrcFileIx)
            EditRdFile.Seek(ssd.SrcStart - 1, SeekOrigin.Begin)
            EditRdFile.Read(buf, 0, ssd.SrcLength)
            ' unclear what error is solved here.
            'Dim chag As Boolean = False
            'For lb As Integer = buf.Length - 1 To 1 Step -2
            '    If buf(lb) = 0 AndAlso buf(lb - 1) = 0 Then
            '        chag = True
            '        ssd.SrcLength -= 2
            '    End If
            'Next
            'If chag Then
            '    Array.Resize(buf, ssd.SrcLength)
            'End If
            If CurrEdtSession.EncodingType = "U"c Then
                Try
                    Dim enc As System.Text.Encoding = New System.Text.UnicodeEncoding(False, True, True)
                    value = enc.GetString(buf)
                Catch ex As Exception
                    value = "text is NOT unicode on positions " & CStr(ssd.SrcStart) & " to " & CStr(ssd.SrcStart + ssd.SrcLength - 1) & " in file. " & ex.Message
                End Try
            ElseIf CurrEdtSession.EncodingType = "8"c Then
                Dim inUtf As Boolean = False
                Dim isUtf As Boolean = False
                Dim tryAscii As Boolean = False
                For i As Integer = 0 To buf.Length - 1
                    If Not inUtf AndAlso buf(i) >= 192 AndAlso buf(i) <= 254 Then ' start of utf1 set was 223
                        inUtf = True
                        isUtf = True
                    ElseIf inUtf AndAlso buf(i) >= 128 AndAlso buf(i) <= 191 Then ' 2nd, 3rd, ... byte of utf8
                        inUtf = True
                    ElseIf buf(i) > 127 Then
                        tryAscii = True ' > 127, not utf: extended ascii char
                    End If
                Next
                If Not isUtf And tryAscii Then ' try with ascii
                    value = System.Text.Encoding.Default.GetString(buf)
                Else ' utf seems valid
                    Dim enc As System.Text.Encoding = New System.Text.UTF8Encoding()
                    value = enc.GetString(buf)
                End If
            Else
                value = System.Text.Encoding.Default.GetString(buf)
            End If
        End If
        'Debug.WriteLine("read source " & CStr(ssd.SrcFileIx) & " " & CStr(ssd.SrcStart) & " " & CStr(ssd.SrcLength) & " " & value)
        Return value
    End Function
    Private Sub StoreExtract6(ByVal ky As String, ByVal S1 As String, ByVal S2 As String, ByVal S3 As String, ByVal S4 As String, ByVal s5 As String, ByVal s6 As String)
        storeVarT(ky & ".0", "5")
        storeVarT(ky & ".1", S1)
        storeVarT(ky & ".2", S2)
        storeVarT(ky & ".3", S3)
        storeVarT(ky & ".4", S4)
        storeVarT(ky & ".5", s5)
        storeVarT(ky & ".6", s6)
    End Sub
    Private Sub StoreExtract5(ByVal ky As String, ByVal S1 As String, ByVal S2 As String, ByVal S3 As String, ByVal S4 As String, ByVal s5 As String)
        storeVarT(ky & ".0", "5")
        storeVarT(ky & ".1", S1)
        storeVarT(ky & ".2", S2)
        storeVarT(ky & ".3", S3)
        storeVarT(ky & ".4", S4)
        storeVarT(ky & ".5", s5)
    End Sub
    Private Sub StoreExtract4(ByVal ky As String, ByVal S1 As String, ByVal S2 As String, ByVal S3 As String, ByVal S4 As String)
        storeVarT(ky & ".0", "4")
        storeVarT(ky & ".1", S1)
        storeVarT(ky & ".2", S2)
        storeVarT(ky & ".3", S3)
        storeVarT(ky & ".4", S4)
    End Sub
    Private Sub StoreExtract3(ByVal ky As String, ByVal S1 As String, ByVal S2 As String, ByVal S3 As String)
        storeVarT(ky & ".0", "3")
        storeVarT(ky & ".1", S1)
        storeVarT(ky & ".2", S2)
        storeVarT(ky & ".3", S3)
    End Sub
    Private Sub StoreExtract2(ByVal ky As String, ByVal S1 As String, ByVal S2 As String)
        storeVarT(ky & ".0", "2")
        storeVarT(ky & ".1", S1)
        storeVarT(ky & ".2", S2)
    End Sub
    Private Sub StoreExtract1(ByVal ky As String, ByVal S1 As String)
        storeVarT(ky & ".0", "1")
        storeVarT(ky & ".1", S1)
    End Sub
    Private Sub StoreExtract0(ByVal ky As String)
        storeVarT(ky & ".0", "0")
    End Sub
    Private Sub storeVarT(ByVal ky As String, ByVal S1 As String)
        Dim cvr As New DefVariable
        Dim execName As String = "", n As String = ""
        Dim k As Integer
        Rxs.StoreVar(Rxs.SourceNameIndexPosition(ky, Rexx.tpSymbol.tpVariable, cvr), S1, k, execName, n)
        Logg("Setvar " + ky + "=" + S1)
    End Sub
    Private Sub XeditPc_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Resize
        Logg("XeditPc_Resize")
        SaveAllModifiedLines()
        If ScrList.Count > 0 Then
            Dim dsScr As ScreenLine = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine) ' fill screen  
            FillScreenBuffer(dsScr.CurSrcNr, False)
        End If
        ForcePaint()
    End Sub
    Private Sub ForcePaint()
        If FormShown AndAlso Not RexxCmdActive Then
            Dim aRectangle As Rectangle
            aRectangle = New Rectangle(1, 1, 1, 1) ' just one pixel
            Invalidate(aRectangle) ' needed only to force execution of PAINT event
        End If
        InvalidatedWin = False ' And look for screen updates
    End Sub
    Private Function C2X(ByVal x As String) As String
        Dim c1, i, l, c2 As Integer
        Dim ccc As Char
        Dim ic As Integer
        Dim s, r As String
        r = ""
        s = "0123456789ABCDEF"
        l = x.Length()
        For i = 1 To l
            ccc = Mid(x, i, 1)
            'Dim byteArray As Byte() = BitConverter.GetBytes(ccc)
            'If byteArray.Length > 1 AndAlso byteArray(1) <> 0 Then
            'r = r & "EF"
            'Else
            ic = Asc(ccc)
            c1 = ic \ 16
            c2 = ic And &HFS
            r = r & Mid(s, c1 + 1, 1) & Mid(s, c2 + 1, 1)
            'End If
        Next
        Return r
    End Function
    Private Function X2C(ByVal x As String) As String
        Dim h, i, l, j As Integer
        Dim ccc, X2Cr As String
        Dim ic As Integer
        x = x.ToUpper(CultInf)
        l = x.Length()
        X2Cr = ""
        j = 1
        For i = 1 To l
            If (j = 1) Then h = 0
            ccc = Mid(x, i, 1)
            If ccc <> " " Then
                If (ccc >= "0" And ccc <= "9") Then
                    ic = Asc(ccc) - 48
                ElseIf (ccc >= "A" And ccc <= "F") Then
                    ic = Asc(ccc) - 55
                Else
                    DoCmd1("MSG " & SysMsg(121), False)
                    Return ""
                End If
                If (j = 1) Then
                    h = h + ic * 16
                Else
                    h = h + ic
                    X2Cr = X2Cr & Chr(h)
                End If
                j = 3 - j
            End If
        Next
        Return X2Cr
    End Function
    Private Sub SaveAllModifiedLines()
        Logg("SaveAllModifiedLines")
        Dim dsScr As ScreenLine
        For Each dsScr In ScrList
            SaveModifiedLine(dsScr)
        Next dsScr
    End Sub
    Private Sub SaveModifiedLine(ByVal dsScr As ScreenLine)
        Dim ssd As SourceLine
        If dsScr.CurLinModified And dsScr.CurLinType = "L"c Then
            If EditFileWrk Is Nothing Then
                EditFileWrk = OpenWrkFile()
            End If
            ssd = dsScr.CurLinSsd
            'Debug.WriteLine("SaveModifiedLine " & CStr(dsScr.CurLinNr) & " " & CStr(ssd.SrcFileIx) & " " & CStr(ssd.SrcStart) & " " & CStr(ssd.SrcLength) & " " & CStr(dsScr.CurLinSrc.Length()) & " ")
            If ssd.SrcLength > -1 Or dsScr.CurLinSrc.Length() > 0 Then ' skip Ied line with no contents
                If Not CurrEdtSession.RecfmV Then
                    If dsScr.CurLinSrc.Length() < CurrEdtSession.Lrecl Then
                        dsScr.CurLinSrc = dsScr.CurLinSrc & Space(CurrEdtSession.Lrecl - dsScr.CurLinSrc.Length())
                    End If
                    If dsScr.CurLinSrc.Length() > CurrEdtSession.Lrecl Then dsScr.CurLinSrc = dsScr.CurLinSrc.Substring(0, CurrEdtSession.Lrecl)
                End If
                Dim nBytes As Integer = 1
                Dim buf() As Byte
                If CurrEdtSession.EncodingType = "U"c Then
                    Dim uniLE As System.Text.Encoding = System.Text.Encoding.Unicode
                    nBytes = uniLE.GetByteCount(dsScr.CurLinSrc)
                    buf = uniLE.GetBytes(dsScr.CurLinSrc)
                ElseIf CurrEdtSession.EncodingType = "8"c Then
                    Dim u8LE As System.Text.Encoding = System.Text.Encoding.UTF8
                    nBytes = u8LE.GetByteCount(dsScr.CurLinSrc)
                    buf = u8LE.GetBytes(dsScr.CurLinSrc)
                Else
                    nBytes = dsScr.CurLinSrc.Length()
                    buf = System.Text.Encoding.Default.GetBytes(dsScr.CurLinSrc)
                End If
                If ssd.SrcLength = -1 Or nBytes > ssd.SrcLength Or ssd.SrcFileIx = "E"c Then
                    ssd.SrcFileIx = "W"c
                    ssd.SrcStart = WrkMaxWritePos + 1 ' if sourceline modified for first time, or becomes longer
                End If
                EditFileWrk.Seek(ssd.SrcStart - 1, SeekOrigin.Begin)
                ssd.SrcLength = nBytes
                EditFileWrk.Write(buf, 0, nBytes)
                'Debug.WriteLine("WRITE " & ssd.SrcFileIx & " " & CStr(ssd.SrcStart) & " " & CStr(ssd.SrcLength) & " " & CStr(dsScr.CurLinNr) & " " & CStr(dsScr.CurLinSrc.Length()) & " " & CStr(dsScr.CurLinSrc))
                WrkMaxWritePos = WrkMaxWritePos + nBytes
            End If
            dsScr.CurLinModified = False
            CurrEdtSession.FileChanged = True
        End If
    End Sub
    Private Function OpenWrkFile() As FileStream
        Dim i As Integer
        Logg("OpenWrkFile start")
        For i = 1 To 99
            WrkFileName = Path.GetTempPath()
            If WrkFileName = "" Then WrkFileName = Environ("TEMP")
            If WrkFileName = "" Then WrkFileName = Environ("TMP")
            If WrkFileName = "" Then WrkFileName = "C"
            If Not WrkFileName.EndsWith("\") Then WrkFileName &= "\"
            WrkFileName = WrkFileName & "Xedit" & CStr(i) & ".tmp"
            Logg("OpenWrkFile " & WrkFileName)
            If File.Exists(WrkFileName) Then
                Try
                    Logg("OpenWrkFile kill " & WrkFileName)
                    Kill(WrkFileName)
                Catch E As Exception ' might be allocated
                    i = i
                End Try
            End If
            If Not File.Exists(WrkFileName) Then
                Logg("OpenWrkFile end")
                Return File.Open(WrkFileName, FileMode.CreateNew, FileAccess.ReadWrite)
            End If
        Next
        MsgBox("Unable to allocate a temporary workfile. Check TEMP / TMP environment variables of your system properties, please.")
        Logg("OpenWrkFile end error")
        Return Nothing
    End Function
    Private Sub RepaintLine(ByRef dsScr As ScreenLine, ByVal lineNr As Integer)
        If FormShown AndAlso Not RexxCmdActive Then
            Logg("RepaintLine " & CStr(lineNr))
            Dim aRectangle As Rectangle
            aRectangle = New Rectangle(CInt(1), CInt((lineNr - 1) * RectHeight + 1), CInt(EditTextWidth * (CharsOnScreen + 7)), CInt(EditTextHeight) + 1) ' x, y, w, h 
            Invalidate(aRectangle)
        End If
        dsScr.CurRepaint = True ' paint this line 
    End Sub
    Private Function LineIxOnScreen(ByVal lnr As Integer) As Integer
        Dim dsScr As ScreenLine, i As Integer
        dsScr = DirectCast(ScrList.Item(CurrEdtSession.SrcOnScrn(1)), ScreenLine)
        If lnr >= dsScr.CurSrcNr Then
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.SrcOnScrn(CurrEdtSession.nSrcOnScrn)), ScreenLine)
            If lnr <= dsScr.CurSrcNr Then
                For i = 1 To CurrEdtSession.nSrcOnScrn
                    dsScr = DirectCast(ScrList.Item(CurrEdtSession.SrcOnScrn(i)), ScreenLine)
                    If lnr >= dsScr.CurSrcNr And lnr < dsScr.CurSrcNr + dsScr.CurNrLines Then
                        Return CurrEdtSession.SrcOnScrn(i)
                    End If
                Next
            End If
        End If
        Return 0
    End Function
    Private Function IxInvsLines(ByVal lnr As Integer) As Integer
        Dim i As Integer
        For i = 1 To CurrEdtSession.nSrcOnScrn
            If CurrEdtSession.SrcOnScrn(i) = lnr Then
                Return i
            End If
        Next
        Return 0
    End Function
    Private Sub RepaintLineIfVisible(ByVal lnr As Integer)
        Dim dsScr As ScreenLine
        Dim i As Integer = LineIxOnScreen(lnr)
        If i > 0 Then
            dsScr = DirectCast(ScrList.Item(i), ScreenLine)
            dsScr.CurRepaint = True
        End If
    End Sub
    Private Sub DefSynonym(ByVal CommandLine As String)
        Dim abbrev As String
        Dim nSym As Synonym
        abbrev = NxtWordFromStr(CommandLine, "")
        If Not CurrEdtSession.Synonyms.Contains(abbrev) Then
            nSym = New Synonym
            nSym.SynAbbrev = abbrev
            CurrEdtSession.Synonyms.Add(nSym, abbrev)
        Else
            nSym = DirectCast(CurrEdtSession.Synonyms.Item(abbrev), Synonym)
        End If
        If CommandLine(0) = """"c Then
            CommandLine = CommandLine.Substring(1)
            Dim i As Integer = CommandLine.IndexOf(""""c)
            If i > 0 Then
                nSym.SynCommand = CommandLine.Substring(0, i)
                CommandLine = CommandLine.Substring(i + 1).TrimStart
            End If
        Else
            nSym.SynCommand = NxtWordFromStr(CommandLine, "")
        End If
        nSym.SynLength = NxtNumFromStr(CommandLine, CStr(abbrev.Length()))
    End Sub
    Private Sub KeyArrowDown(ByRef dsScr As ScreenLine)
        If CurrEdtSession.CursorDisplayLine < LinesScreenVisible Then
            CurrEdtSession.CursorDisplayLine += 1S
        Else
            CurrEdtSession.CursorDisplayLine = 1
        End If
    End Sub
    Private Sub KeyArrowUp(ByRef dsScr As ScreenLine)
        If CurrEdtSession.CursorDisplayLine > 1 Then
            CurrEdtSession.CursorDisplayLine -= 1S
        Else
            CurrEdtSession.CursorDisplayLine = CInt(LinesScreenVisible)
        End If
    End Sub
    Private Sub KeyArrowRight(ByRef dsScr As ScreenLine)
        If CurrEdtSession.CursorDisplayColumn < CharsOnScreen Then
            CurrEdtSession.CursorDisplayColumn += 1S
        Else
            If CurrEdtSession.CursorDisplayLine < LinesScreenVisible Then
                CurrEdtSession.CursorDisplayColumn = 0
                CurrEdtSession.CursorDisplayLine += 1S
            Else
                CurrEdtSession.CursorDisplayColumn = 0
                CurrEdtSession.CursorDisplayLine = 1
            End If
            KeyArrowRight(DirectCast(ScrList.Item(CurrEdtSession.CursorDisplayLine), ScreenLine))
        End If
    End Sub
    Private Sub KeyArrowLeft(ByRef dsScr As ScreenLine)
        If CurrEdtSession.CursorDisplayColumn > 1 Then
            CurrEdtSession.CursorDisplayColumn -= 1S
        Else
            If CurrEdtSession.CursorDisplayLine > 1 Then
                CurrEdtSession.CursorDisplayColumn = CharsOnScreen + 1S
                CurrEdtSession.CursorDisplayLine -= 1S
            Else
                CurrEdtSession.CursorDisplayColumn = CharsOnScreen + 1S
                CurrEdtSession.CursorDisplayLine = LinesScreenVisible
            End If
            KeyArrowLeft(DirectCast(ScrList.Item(CurrEdtSession.CursorDisplayLine), ScreenLine))
        End If
    End Sub
    Private Sub KeyTab(ByRef dsScr As ScreenLine)
        If CurrEdtSession.CursorDisplayColumn < 0 Then
            CurrEdtSession.CursorDisplayColumn = 1
        Else
            CurrEdtSession.CursorDisplayColumn = -6
            If CurrEdtSession.CursorDisplayLine < LinesScreenVisible Then
                CurrEdtSession.CursorDisplayLine += 1S
            Else
                CurrEdtSession.CursorDisplayLine = 1
            End If
            While CurrEdtSession.CursorDisplayLine > 0 AndAlso CurrEdtSession.CursorDisplayLine <= ScrList.Count AndAlso CurrEdtSession.CursorDisplayLine < LinesScreenVisible AndAlso "0F".Contains(DirectCast(ScrList.Item(CurrEdtSession.CursorDisplayLine), ScreenLine).CurLinType)
                If CurrEdtSession.CursorDisplayLine < LinesScreenVisible Then
                    CurrEdtSession.CursorDisplayLine += 1S
                Else
                    CurrEdtSession.CursorDisplayLine = 1
                End If
            End While
        End If
    End Sub
    Private Sub KeyBackTab(ByRef dsScr As ScreenLine)
        If CurrEdtSession.CursorDisplayColumn > 0 Then
            CurrEdtSession.CursorDisplayColumn = -6
        Else
            CurrEdtSession.CursorDisplayColumn = 1
            If CurrEdtSession.CursorDisplayLine > 1 Then
                CurrEdtSession.CursorDisplayLine -= 1S
            Else
                CurrEdtSession.CursorDisplayLine = LinesScreenVisible
            End If
            While CurrEdtSession.CursorDisplayLine > 0 AndAlso CurrEdtSession.CursorDisplayLine <= ScrList.Count AndAlso CurrEdtSession.CursorDisplayLine < LinesScreenVisible AndAlso "0F".Contains(DirectCast(ScrList.Item(CurrEdtSession.CursorDisplayLine), ScreenLine).CurLinType)
                If CurrEdtSession.CursorDisplayLine > 1 Then
                    CurrEdtSession.CursorDisplayLine -= 1S
                Else
                    CurrEdtSession.CursorDisplayLine = LinesScreenVisible
                End If
            End While
        End If
    End Sub
    Private Sub KeyEnd(ByRef dsScr As ScreenLine)
        If dsScr.CurLinType = "L"c OrElse dsScr.CurLinType = "C"c Then
            If CurrEdtSession.CursorDisplayColumn = dsScr.CharsOnScr + 1S Then
                CurrEdtSession.CursorDisplayColumn = 1
            Else
                CurrEdtSession.CursorDisplayColumn = dsScr.CharsOnScr + 1S
            End If
        End If
    End Sub
    Private Sub KeyPageUp()
        ReshowMsgSrc(False)
        Dim dsScrf As ScreenLine = DirectCast(ScrList.Item(CurrEdtSession.SrcOnScrn(1)), ScreenLine)
        If dsScrf.CurLinType = "T"c Or dsScrf.CurLinType = "0"c Then
            rc = 4
        Else
            DoCmd1("UP " & CStr(CurrEdtSession.nSrcOnScrn), False)
        End If
    End Sub
    Private Sub KeyPageDown()
        ReshowMsgSrc(False)
        Dim dsScrl As ScreenLine = DirectCast(ScrList.Item(CurrEdtSession.SrcOnScrn(CurrEdtSession.nSrcOnScrn)), ScreenLine)
        If dsScrl.CurLinType = "B"c Or dsScrl.CurLinType = "0"c Then
            rc = 4
        Else
            DoCmd1("DOWN " & CStr(CurrEdtSession.nSrcOnScrn), False)
        End If
    End Sub
    Private Sub KeyPfKey(ByVal KeyCode As Integer, ByVal KeyShift As Integer)
        Dim s As String
        ReshowMsgSrc(False)
        s = KeyPfTxt(KeyCode, KeyShift)
        If CurrEdtSession.Settings.Contains(s) Then
            s = CStr(CurrEdtSession.Settings.Item(s))
            DoCmd(s, True)
        Else
            MsgBox(SysMsg(4) + " " + s, MsgBoxStyle.Exclamation)
        End If
    End Sub
    Private Function KeyPfTxt(ByVal KeyCode As Integer, ByVal KeyShift As Integer) As String
        KeyPfTxt = "PF"
        If KeyCode < 121 Then KeyPfTxt = KeyPfTxt & "0"
        KeyPfTxt = KeyPfTxt & CStr(KeyCode - 111)
        If KeyShift = 1 Then
            KeyPfTxt = "SHIFT-" & KeyPfTxt
        ElseIf KeyShift = 2 Then
            KeyPfTxt = "CTRL-" & KeyPfTxt
        ElseIf KeyShift = 3 Then
            KeyPfTxt = "CTRL-SHIFT-" & KeyPfTxt
        ElseIf KeyShift <> 0 Then '4 or 5
            KeyPfTxt = "ALT-" & KeyPfTxt
        End If
    End Function
    Private Function KeyCtrlAlt(ByVal KeyCode As Integer, ByVal Shift As Integer) As Boolean
        Dim s As String
        If Shift = 2 Then
            s = "CTRL-"
        ElseIf Shift = 3 Then
            s = "CTRL-SHIFT-"
        ElseIf Shift <= 3 > 0 Then ' 4 alt 5 sh/alt
            s = "ALT-"
        End If
        If KeyCode >= 112 And KeyCode <= 123 Then
            If MacroRecording Then MacroString += "{F" + CStr(KeyCode - 111) + "}"
            s = KeyPfTxt(KeyCode, Shift)
        Else
            Dim KeyT As String = ""
            Select Case KeyCode
                Case System.Windows.Forms.Keys.Insert
                    KeyT = "INS"
                Case System.Windows.Forms.Keys.Tab
                    KeyT = "TAB"
                Case System.Windows.Forms.Keys.Left
                    KeyT = "LEFT"
                Case System.Windows.Forms.Keys.Right
                    KeyT = "RIGHT"
                Case System.Windows.Forms.Keys.Up
                    KeyT = "Up"
                Case System.Windows.Forms.Keys.Down
                    KeyT = "DOWN"
                Case System.Windows.Forms.Keys.End
                    KeyT = "END"
                Case System.Windows.Forms.Keys.Delete
                    KeyT = "DEL"
                Case System.Windows.Forms.Keys.PageUp
                    KeyT = "PGUP"
                Case System.Windows.Forms.Keys.PageDown
                    KeyT = "PGDN"
                Case System.Windows.Forms.Keys.Home
                    KeyT = "HOME"
                Case System.Windows.Forms.Keys.Return ' Enter
                    KeyT = "ENTER"
                Case System.Windows.Forms.Keys.F1 To System.Windows.Forms.Keys.F12
                    KeyT = "F" + (KeyCode - 111)
            End Select
            If MacroRecording Then
                Dim c As String = Chr(KeyCode)
                MacroString += c.ToLower
            End If
            If KeyT <> "" Then
                s = s & KeyT
            Else
                s = s & Chr(KeyCode)
            End If
        End If
        If CurrEdtSession.Settings.Contains(s) Then
            s = CStr(CurrEdtSession.Settings.Item(s))
            DoCmd(s, True)
            KeyCtrlAlt = True
        Else
            'If KeyCode < 128 Then
            MsgBox(SysMsg(7) + " " + s, MsgBoxStyle.Exclamation)
            'End If
            KeyCtrlAlt = False
        End If
    End Function
    Private Sub ReshowMsgSrc(ByVal ClearAll As Boolean, Optional ByVal EscPressed As Boolean = False)
        Dim dsScrn, dsScr As ScreenLine, i As Integer
        If Not ClearAll AndAlso CurrEdtSession.MsgOverlay Then ' restore overlayed source lines
            If CurrEdtSession.ScrOverlayed.Count > 0 Then
                Logg("ReshowMsgSrc")
                For i = 1 To CurrEdtSession.ScrOverlayed.Count
                    dsScrn = DirectCast(ScrList.Item(CurrEdtSession.MsgLineNrF - 1 + i), ScreenLine)
                    dsScr = DirectCast(CurrEdtSession.ScrOverlayed.Item(1), ScreenLine)
                    CurrEdtSession.ScrOverlayed.Remove(1)
                    dsScrn.CopyFrom(dsScr)
                    dsScrn.CurRepaint = True
                Next
                CalcIxInSLines()
            Else
                If EscPressed Then DoCmd1("$ESC$", True)
            End If
        Else ' clear messages
            Logg("ReshowMsgSrc")
            For i = 1 To CurrEdtSession.MsgLineNrT
                dsScrn = DirectCast(ScrList.Item(CurrEdtSession.MsgLineNrF - 1 + i), ScreenLine)
                If dsScrn.CurLinType = "M"c AndAlso dsScrn.CurLinSrc.Length > 0 Then
                    dsScrn.CurLinSrc = ""
                    dsScrn.CurRepaint = True
                End If
            Next
        End If
    End Sub
    Private Sub RestoreIedLines()
        If CurrEdtSession.IedLines.Count > 0 Then
            Logg("RestoreIedLines start")
            Dim retLineNr, l As Integer
            Dim ssd As SourceLine, dsScr As ScreenLine, ie As IedLine
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
            retLineNr = dsScr.CurSrcNr
            For Each ie In CurrEdtSession.IedLines ' delete Ied lines without text inserted
                l = LineIxOnScreen(ie.Linenr)
                If l > 0 Then
                    dsScr = DirectCast(ScrList.Item(l), ScreenLine)
                    SaveModifiedLine(dsScr)
                End If
                If ie.Linenr <= CurrEdtSession.SourceList.Count Then
                    ssd = DirectCast(CurrEdtSession.SourceList(ie.Linenr), SourceLine)
                    If ssd.SrcLength < 0 Then ' not yet touched by user
                        Logg("RestoreIedLines " & CStr(ie.Linenr))
                        MoveToSourceLine(ie.Linenr)
                        l = CurrEdtSession.LineCommands.Count ' remove linecommands on empty I-lines
                        While l > 0
                            Dim mLnCmd As LineCommand = DirectCast(CurrEdtSession.LineCommands.Item(l), LineCommand)
                            If mLnCmd.Linenr = ie.Linenr Then
                                CurrEdtSession.LineCommands.Remove(l)
                            End If
                            l -= 1
                        End While
                        dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                        DeleteLine()
                    End If
                End If
            Next
            CurrEdtSession.IedLines.Clear()
            MoveToSourceLine(retLineNr)
            Logg("RestoreIedLines end")
        End If
    End Sub
    Private Sub KeyEnter(ByRef dsScr As ScreenLine)
        Dim lCmd, lCmdU As String
        Dim cmdl As Integer = CurrEdtSession.CmdLineNr
        If cmdl = -1 Then cmdl = ScrList.Count
        dsScr = DirectCast(ScrList.Item(cmdl), ScreenLine)
        dsScr.CurRepaint = True
        Dim t As Char = dsScr.CurLinType ' might be overlayed with M
        lCmd = dsScr.CurLinSrc.Trim
        If Not CurrEdtSession.TabChar = vbNullChar Then
            lCmd = lCmd.Replace(CurrEdtSession.TabChar, vbTab)
        End If
        lCmdU = lCmd.ToUpper(CultInf)
        ReshowMsgSrc(False)
        RestoreIedLines()
        If t = "C"c Then            'process command
            If lCmd.Length() > 0 Then
                If lCmdU <> "RECALL" Then RecallPick = RecalledCmd
                If lCmd = "?" Then
                    lCmd = "RECALL"
                    lCmdU = lCmd.ToUpper(CultInf)
                End If
                If lCmdU <> "RECALL" And lCmd(0) <> "=" And RecallCmds(RecallPick) <> lCmd Then
                    Dim i As Integer = 0
                    For j As Integer = 1 To RecallIxMax
                        If RecallCmds(j).ToUpper(CultInf) = lCmdU Then
                            i = j
                            Exit For
                        End If
                    Next
                    If i = 0 Then
                        RecallIxAdd = RecallIxAdd + 1
                        If RecallIxAdd > RecallCmds.GetUpperBound(0) Then RecallIxAdd = 1
                        If RecallIxAdd > RecallIxMax Then RecallIxMax = RecallIxAdd
                        RecallCmds(RecallIxAdd) = lCmd
                        RecallPick = RecallIxAdd
                    Else
                        RecallPick = i
                    End If
                End If
                RecalledCmd = 0
                If lCmdU <> "RECALL" Then
                    dsScr.CurLinSrc = ""
                    If CurrEdtSession.CursorDisplayLine = CurrEdtSession.CmdLineNr Or (CurrEdtSession.CursorDisplayLine = LinesScreenVisible And CurrEdtSession.CmdLineNr = -1) Then
                        CurrEdtSession.CursorDisplayColumn = 1
                    End If
                End If
                rc = 0
                If lCmd.Substring(0, 1) = "&" Then
                    DoCmd(lCmd.Substring(1), True)
                    Dim pRc As Integer = rc
                    DoCmd1("CMSG " & lCmd, False)
                    rc = pRc
                Else
                    DoCmd(lCmd, True)
                End If
            End If
            RecallNrCalled = 0
            If Not QuitPgm Then DoLineCmd()
        End If
    End Sub
    Private Sub KeyDelete(ByRef dsScr As ScreenLine)
        Dim strng As String, ps As Integer, vp As New VerifyPair
        If CurrEdtSession.CursorDisplayColumn > 0 Then
            If CurrEdtSession.mSelect Then
                DeleteSelectedArea()
            ElseIf dsScr.CurLinType = "L"c Or dsScr.CurLinType = "C"c Then
                If dsScr.CurLinType = "L"c Then
                    AddUndo(3, dsScr)
                End If
                Dim vpNibble As Boolean
                ps = EditPosSrc(vp, vpNibble, dsScr)
                If vp.VerHex Then
                    ps = (ps - 1) * 2 + 1
                    If vpNibble Then ps += 1
                    strng = C2X(dsScr.CurLinSrc.PadRight(ps))
                    If ps > 1 Then
                        strng = strng.Remove(ps - 1, 1) & "0"
                    Else
                        strng = strng.Substring(ps) & "0"
                    End If
                    dsScr.CurLinSrc = X2C(strng)
                Else
                    strng = dsScr.CurLinSrc.PadRight(ps)
                    If ps > 1 Then
                        dsScr.CurLinSrc = strng.Remove(ps - 1, 1)
                    Else
                        dsScr.CurLinSrc = strng.Substring(ps)
                    End If
                End If
                dsScr.CurLinModified = True
                dsScr.CurRepaint = True
            End If
        End If
    End Sub
    Private Sub FillScreenBuffer(ByVal nfiLineScr As Integer, ByVal allocList As Boolean)
        Dim dsScr, dsScrS As ScreenLine, i, sI As Integer
        If FormShown AndAlso Not RexxCmdActive Then
            Invalidate() ' Paint all
        End If
        RepaintAllScreenLines = True
        sI = nfiLineScr - CurrEdtSession.CurLineNr + 2 ' load from given sourceline skip top
        If CurrEdtSession.CmdLineNr > 0 Then sI += 1
        Dim nMsgOverl = 0
        For i = 1 To LinesScreenVisible
            If allocList Then
                dsScr = New ScreenLine()
            Else
                dsScr = DirectCast(ScrList.Item(i), ScreenLine)
            End If
            If dsScr.CurLinType = "M" Then
                nMsgOverl += 1
                dsScr.CurRepaint = True
                dsScr = DirectCast(CurrEdtSession.ScrOverlayed.Item(nMsgOverl), ScreenLine) ' build in saved sourceline
            End If
            GetTextOfScrline(dsScr, i, sI, True)
            If allocList Then ScrList.Add(dsScr)
            dsScr.CurRepaint = True
        Next
        CalcIxInSLines() ' make xref line on screen <==> line in source
        rc = 0 ' If curline halfway in EXCLUDED? start at first one further UP
        dsScrS = DirectCast(ScrList.Item(CurrEdtSession.SrcOnScrn(1)), ScreenLine)
        Dim fiType As Char = dsScrS.CurLinType
        If fiType = "X"c Then
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
            If dsScr.CurSrcNr > nfiLineScr Then
                ShiftScreenDown(CurrEdtSession.SrcOnScrn(1), 1, True)
                dsScr = DirectCast(ScrList.Item(CurrEdtSession.SrcOnScrn(1)), ScreenLine)
                If dsScr.CurLinType = "X"c Then
                    FillScreenBuffer(dsScrS.CurSrcNr - dsScr.CurNrLines, False)
                    Exit Sub
                End If
            End If
        End If
        dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
        While dsScr.CurSrcNr > nfiLineScr AndAlso rc = 0
            ShiftScreenDown(CurrEdtSession.SrcOnScrn(1), 1, True)
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
        End While
        CurrEdtSession.PrevEditLineScr = CurrEdtSession.CursorDisplayLine
        CurrEdtSession.PrevEditPosScr = CurrEdtSession.CursorDisplayColumn
        CalcIxInSLines()
    End Sub
    Private Sub ShiftScreenDown(ByVal from As Integer, ByVal nL As Integer, ByVal ReadSrc As Boolean)
        Dim i, SourceI, ScrI As Integer, dsScr As ScreenLine
        Dim ResLines, ResIx As New Collection, deleted As Boolean
        i = LinesScreenVisible
        While i >= 1
            If i < ScrList.Count AndAlso DirectCast(ScrList.Item(i), ScreenLine).CurLinType = "R"c Then
                ResIx.Add(i)
                ResLines.Add(DirectCast(ScrList.Item(i), ScreenLine))
                Logg("Scrlist down remove line " + CStr(i))
                ScrList.Remove(i)
                deleted = True
            Else
                i -= 1
            End If
        End While
        If deleted Then
            CalcIxInSLines()
        End If
        If ReadSrc Then
            dsScr = DirectCast(ScrList.Item(from), ScreenLine)
            SourceI = dsScr.CurSrcNr - 1
        End If
        For i = 1 To nL
            If from <= CurrEdtSession.CurLineNr Then ' if current line is not moved, it's not possible to point before the first line!
                dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                If dsScr.CurLinType = "T"c Then
                    Exit For
                End If
            End If
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.SrcOnScrn(CurrEdtSession.nSrcOnScrn)), ScreenLine)
            SaveModifiedLine(dsScr)
            Logg("Scrlist down remove line " + CStr(CurrEdtSession.SrcOnScrn(CurrEdtSession.nSrcOnScrn)))
            ScrList.Remove(CurrEdtSession.SrcOnScrn(CurrEdtSession.nSrcOnScrn))
            ScrI = from
            ScrList.Add(dsScr, , ScrI)
            If ReadSrc Then
                GetTextOfScrline(dsScr, ScrI, SourceI, False)
            Else
                InitDsScrStruct(dsScr, SourceI + 1)
            End If
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
        Next
        If deleted Then
            For i = ResIx.Count To 1 Step -1
                ScrList.Add(DirectCast(ResLines.Item(i), ScreenLine), , ResIx.Item(i))
            Next
            ResIx.Clear()
            ResLines.Clear()
        End If
        If ReadSrc Then CalcIxInSLines()
        RepaintFromLine(CurrEdtSession.SrcOnScrn(1))
    End Sub
    Private Sub ShiftScreenUp(ByVal from As Integer, ByVal nL As Integer)
        Dim i, SourceI, ScrI As Integer, dsScr As ScreenLine
        Dim ResLines, ResIx As New Collection, deleted As Boolean
        Logg("ShScrUp " & CStr(from) & " " & CStr(nL))
        i = LinesScreenVisible
        While i >= 1
            If i < ScrList.Count AndAlso DirectCast(ScrList.Item(i), ScreenLine).CurLinType = "R"c Then
                ResIx.Add(i)
                ResLines.Add(DirectCast(ScrList.Item(i), ScreenLine))
                Logg("ScrList.Remove up screenline " & CStr(i))
                ScrList.Remove(i)
                deleted = True
            Else
                i -= 1
            End If
        End While
        If deleted Then
            CalcIxInSLines()
        End If
        dsScr = DirectCast(ScrList.Item(CurrEdtSession.SrcOnScrn(CurrEdtSession.nSrcOnScrn)), ScreenLine)
        SourceI = dsScr.CurSrcNr + dsScr.CurNrLines
        For i = 1 To nL
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
            If dsScr.CurLinType = "B"c Then
                Exit For
            End If
            dsScr = DirectCast(ScrList.Item(from), ScreenLine)
            SaveModifiedLine(dsScr)
            Logg("ScrList.Remove up 2e screenline " & CStr(from))
            ScrList.Remove(from)
            ScrI = CurrEdtSession.SrcOnScrn(CurrEdtSession.nSrcOnScrn) - 1
            Logg("MovLine " & CStr(from) & " " & CStr(ScrI) & " " & dsScr.CurLinSrc)
            ScrList.Add(dsScr, , , ScrI) ' temp, will be replaced by first invisible line
            GetTextOfScrline(dsScr, ScrI, SourceI, True)
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
        Next
        If deleted Then
            For i = ResIx.Count To 1 Step -1
                Logg("ScrList.add(i) " & CStr(ResIx.Item(i)))
                ScrList.Add(DirectCast(ResLines.Item(i), ScreenLine), , ResIx.Item(i))
            Next
            ResIx.Clear()
            ResLines.Clear()
        End If
        CalcIxInSLines()
        RepaintFromLine(CurrEdtSession.SrcOnScrn(1))
    End Sub
    Private Function EditPosSrc(ByRef vp As VerifyPair, ByRef LowNibble As Boolean, ByVal dsScr As ScreenLine) As Integer ' where is the cursor in the actual source?
        Dim CharPosInSource, OrigPos As Integer
        OrigPos = CurrEdtSession.CursorDisplayColumn
        If CurrEdtSession.ExpTabs Then ' get real position in text with tabs not expanded

        End If
        If CurrEdtSession.CursorDisplayColumn > -1 Then ' not in prefix area
            If dsScr.CurLinType = "L"c Then ' on a sourceline
                For i As Integer = 0 To dsScr.VerifPartFrom.Length - 1
                    If OrigPos <= dsScr.VerifPartFrom(i) + dsScr.VerifPartLen(i) Then ' which verify part are we in?
                        vp = CurrEdtSession.Verif(i + 1)
                        OrigPos = OrigPos - dsScr.VerifPartFrom(i) ' from which position in sourceline is it?
                        If vp.VerHex Then
                            If OrigPos Mod 2 = 0 Then ' modify low nibble
                                CharPosInSource = vp.VerFrom + Math.Floor(OrigPos / 2) - 1
                                LowNibble = True ' pass this info to source change routine
                            Else
                                CharPosInSource = vp.VerFrom + Math.Floor((OrigPos + 1) / 2) - 1
                            End If
                        Else
                            If CurrEdtSession.ExpTabs Then
                                Dim expSrcPos As Integer = 1
                                Dim realSrcPos As Integer = 0
                                Dim tabsSeen As Integer = 0
                                For j = 0 To dsScr.CurLinSrc.Length - 1
                                    If expSrcPos >= CurrEdtSession.CursorDisplayColumn Then
                                        Exit For ' found the real pos
                                    End If
                                    If dsScr.CurLinSrc(j) <> vbTab Then
                                        expSrcPos += 1
                                    Else
                                        tabsSeen += 1
                                        ' find expanded pos in dsScr.tabExpandPos 
                                        If dsScr.tabExpandPos(tabsSeen - 1) > 0 Then
                                            expSrcPos = dsScr.tabExpandPos(tabsSeen - 1)
                                        Else
                                            expSrcPos += 1 ' tab becomes a space
                                        End If
                                    End If
                                    realSrcPos = j + 1
                                Next
                                CharPosInSource = vp.VerFrom + realSrcPos
                            Else
                                CharPosInSource = vp.VerFrom + OrigPos - 1
                            End If
                        End If
                        Return CharPosInSource
                    End If
                Next
            End If
        End If
        vp = New VerifyPair ' if not on a "L" type line or on prefix area
        vp.VerFrom = 1
        vp.VerTo = CurrEdtSession.Trunc
        vp.VerHex = False
        Return OrigPos
    End Function
    Private Sub MoveToSourcePos(ByVal PosNr As Integer)
        Dim p, l, fac As Integer
        Dim vp As VerifyPair
        For Each vp In CurrEdtSession.Verif
            If vp.VerHex Then
                fac = 2
            Else
                fac = 1
            End If
            If PosNr >= vp.VerFrom AndAlso PosNr <= vp.VerTo Then
                CurrEdtSession.CursorDisplayColumn = CInt(p + (PosNr - vp.VerFrom + 1) * fac)
                Exit For
            End If
            l = vp.VerTo - vp.VerFrom + 1
            p = p + l * fac
        Next
    End Sub
    Private Sub MoveToSourceLine(ByVal LineNr As Integer)
        Dim moved As Boolean, nL As Integer
        Logg("moveto " & CStr(LineNr))
#If tracen Then
        Logg("before")
        DumpScr()
#End If
        Dim dsScr As ScreenLine = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
        If LineNr <> dsScr.CurSrcNr Then
            nL = LineIxOnScreen(LineNr)
            If nL > 0 Then
                nL = nL - CurrEdtSession.CurLineNr
                If nL > 0 Then
                    DoCmd1("DOWN " & CStr(nL), False)
                    'Dim dsScrxx As ScreenLine = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                    moved = True
                Else
                    DoCmd1("UP " & CStr(-nL), False)
                    'Dim dsScrxx As ScreenLine = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                    moved = True
                End If
            End If
            If Not moved Then ' completely change screen
                SaveAllModifiedLines()
                FillScreenBuffer(LineNr, False)
            End If
        End If
#If tracen Then
        Logg("after")
        DumpScr()
#End If
    End Sub
    Private Sub HideLines(ByVal SourceI As Integer, ByVal nrL As Integer)
        Dim i, n As Integer
        Dim ssd As New SourceLine, dsScr As ScreenLine
        n = SourceI + CInt(nrL) - 1
        If n > CurrEdtSession.SourceList.Count() Then n = CurrEdtSession.SourceList.Count()
        For i = SourceI To n
            nrCyclesEv += 1 : If nrCyclesEv > 5000 Then CallDoEvent()
            If CancelCmd Then Exit Sub
            ssd = DirectCast(CurrEdtSession.SourceList.Item(i), SourceLine)
            ssd.SrcSelect = 0 ' hidden
        Next
        If LineIxOnScreen(SourceI) > 0 Then
            SaveAllModifiedLines()
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine) ' fill screen from 1st line actually on screen
            FillScreenBuffer(dsScr.CurSrcNr, False)
        End If
    End Sub
    Private Sub UnHideLines(ByVal SourceI As Integer)
        Dim i As Integer
        Dim ssd As New SourceLine, dsscr As ScreenLine
        For i = SourceI To CurrEdtSession.SourceList.Count()
            nrCyclesEv += 1 : If nrCyclesEv > 5000 Then CallDoEvent()
            If CancelCmd Then Exit Sub
            ssd = DirectCast(CurrEdtSession.SourceList.Item(i), SourceLine)
            If ssd.SrcSelect = 1 Then
                Exit For
            Else
                ssd.SrcSelect = 1 ' visible
            End If
        Next
        If LineIxOnScreen(SourceI) > 0 Then
            SaveAllModifiedLines()
            dsscr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
            FillScreenBuffer(dsscr.CurSrcNr, False)
        End If
    End Sub
    Dim KeyDownCTRL As Boolean = False
    Dim KeyDownALT As Boolean = False
    Dim KeyDownSHIFT As Boolean = False
    Private Sub Form1_KeyDown(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.KeyEventArgs) Handles MyBase.KeyDown, VSB.KeyDown
        Dim Shift As Integer = eventArgs.KeyData \ &H10000
        Dim i As Integer
        If eventArgs.Control Then KeyDownCTRL = True
        If eventArgs.Alt Then KeyDownALT = True
        If eventArgs.Shift Then KeyDownCTRL = True
        Dim dsScr As ScreenLine, KeyAlreadyProcessed As Boolean
        If ScrList.Count = 0 Then Exit Sub ' form not yet visible
        Logg("KeyDown start")
        If Shift = 4 Then ' Composing an ALT-nnn character
            If Not My.Computer.Keyboard.NumLock Then
                If eventArgs.KeyCode = 12 Or (eventArgs.KeyCode >= 33 And eventArgs.KeyCode <= 40) Or eventArgs.KeyCode = 45 Then
                    Logg("KeyDown end compose ALT-char")
                    Exit Sub
                End If
            Else
                If eventArgs.KeyCode >= 96 And eventArgs.KeyCode <= 105 Then
                    Logg("KeyDown end compose ALT-char")
                    Exit Sub
                End If
            End If
        End If
        If eventArgs.KeyCode = 19 Then
            If Me.Cursor = System.Windows.Forms.Cursors.WaitCursor Then
                If MsgBox(SysMsg(19), MsgBoxStyle.OkCancel, "B R E A K") = MsgBoxResult.Ok Then
                    CancelCmd = True
                    If RexxCmdActive Then
                        Rexx.CancRexx = True
                    End If
                End If
            End If
            Logg("KeyDown end BREAK key")
            Exit Sub
        Else
            CancelCmd = False
            Rexx.CancRexx = False
        End If
        KeyAlreadyProcessed = False
        CurrEdtSession.IncrUnDoCnt = True
        CurLnLineLr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine).CurSrcNr
        dsScr = DirectCast(ScrList.Item(CurrEdtSession.CursorDisplayLine), ScreenLine)
        Logg("KeyDown " & CStr(eventArgs.KeyCode) & " " & CStr(Shift))
        If MacroRecording And Shift > 0 Then
            Dim lc As Char = " "c
            If MacroString.Length > 0 Then lc = MacroString(MacroString.Length() - 1)
            If Shift = 1 Then If lc <> "+"c Then MacroString += "+"
            If Shift = 2 Then If lc <> "^" Then MacroString += "^"
            If Shift = 4 Then If lc <> "%" Then MacroString += "%"
        End If
        If Shift <= 1 Then ' not for ALT and CTRL
            KeyAlreadyProcessed = True
            Select Case eventArgs.KeyCode
                Case System.Windows.Forms.Keys.Insert
                    If MacroRecording Then MacroString += "{INS}"
                    CurrEdtSession.InsOvertype = Not CurrEdtSession.InsOvertype
                    dsScr.CurRepaint = True
                Case System.Windows.Forms.Keys.Tab
                    SetmSelect(False)
                    If Shift = 1 Then
                        If MacroRecording Then MacroString += "+{TAB}"
                        KeyBackTab(dsScr)
                    Else
                        If MacroRecording Then MacroString += "{TAB}"
                        KeyTab(dsScr)
                    End If
                Case System.Windows.Forms.Keys.Left
                    If MacroRecording Then MacroString += "{LEFT}"
                    SetmSelect(False)
                    KeyArrowLeft(dsScr)
                Case System.Windows.Forms.Keys.Right
                    If MacroRecording Then MacroString += "{RIGHT}"
                    SetmSelect(False)
                    KeyArrowRight(dsScr)
                Case System.Windows.Forms.Keys.Up
                    If MacroRecording Then MacroString += "{Up}"
                    CurrEdtSession.mSelect = False
                    KeyArrowUp(dsScr)
                Case System.Windows.Forms.Keys.Down
                    If MacroRecording Then MacroString += "{DOWN}"
                    SetmSelect(False)
                    KeyArrowDown(dsScr)
                Case System.Windows.Forms.Keys.End
                    If MacroRecording Then MacroString += "{END}"
                    SetmSelect(False)
                    KeyEnd(dsScr)
                Case System.Windows.Forms.Keys.Delete
                    If MacroRecording Then MacroString += "{DEL}"
                    KeyDelete(dsScr)
                Case System.Windows.Forms.Keys.PageUp
                    If MacroRecording Then MacroString += "{PGUP}"
                    KeyPageUp()
                Case System.Windows.Forms.Keys.PageDown
                    If MacroRecording Then MacroString += "{PGDN}"
                    KeyPageDown()
                Case System.Windows.Forms.Keys.Home
                    If MacroRecording Then MacroString += "{HOME}"
                    i = CurrEdtSession.CmdLineNr
                    If i = -1 Then i = LinesScreenVisible
                    DoCmd1("CURSOR SCREEN " & CStr(i) & " 1", False)
                Case System.Windows.Forms.Keys.Return ' Enter
                    If MacroRecording Then MacroString += "{ENTER}"
                    KeyEnter(dsScr)
                Case System.Windows.Forms.Keys.F1 To System.Windows.Forms.Keys.F12
                    If MacroRecording Then MacroString += "{F" + CStr(eventArgs.KeyCode - 111) + "}"
                    KeyPfKey(eventArgs.KeyCode, Shift)
                Case Else
                    KeyAlreadyProcessed = False
            End Select
        Else
            If Shift > 1 And Shift <> 6 And Shift <> 7 And (eventArgs.KeyCode < 16 Or eventArgs.KeyCode > 18) Then ' AltGr or CTRL or ALT keys
                If KeyCtrlAlt(eventArgs.KeyCode, Shift) Then KeyAlreadyProcessed = True
            End If
        End If
        If QuitPgm Then
            Logg("KeyDown quitpgm")
            CurrEdtSession = Nothing
            EdtSessions.Remove(1)
            FormAlreadyClosed = True
            Me.Close()
        End If
        If KeyAlreadyProcessed Then
            eventArgs.Handled = True
#If Not DEBUG Then
            If rc > 0 Then
                DoCmd1("MSG Rc=" & CStr(rc), False)
            End If
#End If
            ForcePaint()
        End If
        Logg("KeyDown end")
    End Sub
    Private Sub Form1_KeyPress(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.KeyPressEventArgs) Handles MyBase.KeyPress, VSB.KeyPress
        Dim strng, NewCh As String, vp As New VerifyPair, InsOver As Boolean
        Dim ps, nChrAft, nChrBef, j As Integer, KeyAlreadyProcessed As Boolean
        Dim chBef, chAft As String
        Dim WasSel As Boolean, f As Boolean, lCmd As String
        Dim KeyAscii As Integer = Asc(eventArgs.KeyChar)
        Logg("KeyPress start " & CStr(KeyAscii))
        If ScrList.Count = 0 Then
            eventArgs.Handled = True
            Logg("KeyPress ignored")
            Exit Sub
        End If
        If KeyAscii = 9 Or KeyAscii = 13 Then
            eventArgs.Handled = True
            Logg("KeyPress end 9/13")
            Exit Sub ' Tab passes to here? Enter is generated, if the window was overlayed
        End If
        rc = 0
        KeyAlreadyProcessed = False
        WasSel = CurrEdtSession.mSelect ' delete selected area
        If CurrEdtSession.mSelect Then
            If CurrEdtSession.CursorDisplayColumn > 0 And (KeyAscii = System.Windows.Forms.Keys.Back Or KeyAscii = -1 Or (KeyAscii >= 32 And KeyAscii <= 255)) Then
                DeleteSelectedArea()
            End If
        End If
        Dim dsScr As ScreenLine = DirectCast(ScrList.Item(CurrEdtSession.CursorDisplayLine), ScreenLine)
        dsScr.CurRepaint = True
        If Not dsScr.CurSrcRead Then ReadSourceInScrBuf(dsScr)
        Logg("Curr. line " & dsScr.CurLinSrc)
        Dim vpNibble As Boolean
        ps = EditPosSrc(vp, vpNibble, dsScr)
        If ps > -1 Or CurrEdtSession.CursorDisplayColumn < 0 Then
            Select Case KeyAscii
                Case 32 To 255
                    Dim c As Char = Chr(KeyAscii)
                    If MacroRecording Then
                        Dim s As String = c
                        If "()+^%~{}[]".IndexOf(c) > 0 Then
                            s = "{" & c & "}"
                        End If
                        MacroString += s
                    End If
                    KeyAlreadyProcessed = True
                    If Not CurrEdtSession.CaseMU Or vp.VerHex Then
                        If KeyAscii >= 97 And KeyAscii <= 122 Then
                            KeyAscii = KeyAscii - 32
                        End If
                    End If
                    NewCh = Chr(KeyAscii)
                    If ps > 0 Then
                        If dsScr.CurLinType = "L"c Or dsScr.CurLinType = "C"c Then
                            strng = dsScr.CurLinSrc.PadRight(ps - 1)
                            InsOver = CurrEdtSession.InsOvertype
                            If vp.VerHex Then
                                Dim i As Integer = "0123456789ABCEDF".IndexOf(NewCh.ToUpper)
                                If i = -1 Then
                                    NewCh = "0"
                                    DoCmd1("MSG " & SysMsg(121), False)
                                End If
                                strng = C2X(strng)
                                ps = (ps - 1) * 2 + 1
                                If vpNibble Then ps += 1
                            End If
                            If InsOver Then
                                nChrBef = ps - 1
                                nChrAft = nChrBef ' insert
                            Else
                                nChrBef = ps - 1
                                nChrAft = nChrBef + 1 ' replace
                            End If
                            If CurrEdtSession.Nulls And dsScr.CurLinType = "L"c AndAlso Not CurrEdtSession.RecfmV AndAlso (nChrBef > CurrEdtSession.Lrecl Or (CurrEdtSession.InsOvertype And strng.Length() = CurrEdtSession.Lrecl)) Then
                                If strng.EndsWith(" ") Then
                                    strng = strng.Substring(0, strng.Length - 1)
                                End If
                            End If
                            If dsScr.CurLinType = "L"c AndAlso Not CurrEdtSession.RecfmV AndAlso (nChrBef > CurrEdtSession.Lrecl Or (CurrEdtSession.InsOvertype And strng.Length() = CurrEdtSession.Lrecl)) Then
                                rc = 16 ' kan niet invoegen als de regel al vol is
                                Beep()
                            Else
                                If dsScr.CurLinType = "L"c Then AddUndo(3, dsScr)
                                If nChrBef > 0 Then
                                    chBef = strng.Substring(0, nChrBef)
                                Else
                                    chBef = ""
                                End If
                                If nChrAft < strng.Length Then
                                    chAft = strng.Substring(nChrAft)
                                Else
                                    chAft = ""
                                End If
                                strng = chBef & NewCh & chAft
                                If vp.VerHex Then
                                    If strng.Length Mod 2 > 0 Then strng += "0"
                                    strng = X2C(strng)
                                End If
                                dsScr.CurLinSrc = strng
                                dsScr.CurLinModified = True
                                KeyArrowRight(dsScr)
                            End If
                        End If
                    Else
                        If dsScr.CurLinType = "L"c Or dsScr.CurLinType = "T"c Or dsScr.CurLinType = "B"c Or dsScr.CurLinType = "X"c Then
                            f = CurrEdtSession.FindLineCmd(dsScr.CurSrcNr, j)
                            If f Then
                                Dim mLnCmd As LineCommand
                                mLnCmd = DirectCast(CurrEdtSession.LineCommands.Item(j), LineCommand)
                                lCmd = mLnCmd.LinecmdText
                                If mLnCmd.RepeatFactorPresent Then lCmd = lCmd & CStr(mLnCmd.RepeatFactor)
                            Else
                                lCmd = ""
                            End If
                            lCmd = lCmd & Chr(KeyAscii)
                            If Not f Then
                                CurrEdtSession.AddLineCmd(dsScr.CurSrcNr, lCmd)
                            Else
                                CurrEdtSession.ModLineCmd(dsScr.CurSrcNr, lCmd)
                            End If
                            CurrEdtSession.CursorDisplayColumn = CInt(-6 + lCmd.Length())
                        Else
                            rc = 16
                        End If
                    End If
                Case System.Windows.Forms.Keys.Back
                    If MacroRecording Then MacroString += "{BKSP}"
                    KeyAlreadyProcessed = True
                    If CurrEdtSession.CursorDisplayColumn > 0 Then
                        If dsScr.CurLinType = "L"c Or dsScr.CurLinType = "C"c Then
                            If Not WasSel Then
                                strng = dsScr.CurLinSrc.PadRight(ps - 1)
                                nChrBef = ps - 2
                                nChrAft = nChrBef + 1 ' insert
                                If nChrBef > -1 Then
                                    If dsScr.CurLinType = "L"c Then AddUndo(3, dsScr)
                                    If nChrBef > 0 Then
                                        chBef = strng.Substring(0, nChrBef)
                                    Else
                                        chBef = ""
                                    End If
                                    If nChrAft < strng.Length Then
                                        chAft = strng.Substring(nChrAft)
                                    Else
                                        chAft = ""
                                    End If
                                    dsScr.CurLinSrc = chBef & chAft
                                    dsScr.CurLinModified = True
                                    If vp.VerHex Then
                                        CurrEdtSession.CursorDisplayColumn = CurrEdtSession.CursorDisplayColumn - 2S
                                    Else
                                        CurrEdtSession.CursorDisplayColumn = CurrEdtSession.CursorDisplayColumn - 1S
                                    End If
                                End If
                            End If
                        End If
                    Else
                        f = CurrEdtSession.FindLineCmd(dsScr.CurSrcNr, j)
                        If f Then
                            Dim mLnCmd As LineCommand
                            mLnCmd = DirectCast(CurrEdtSession.LineCommands.Item(j), LineCommand)
                            lCmd = mLnCmd.LinecmdText
                            If mLnCmd.RepeatFactorPresent Then lCmd = lCmd & CStr(mLnCmd.RepeatFactor)
                            lCmd = lCmd.Substring(0, lCmd.Length() - 1)
                            If lCmd.Length() = 0 Then
                                CurrEdtSession.LineCommands.Remove(mRecentLineCmdFound)
                            Else
                                CurrEdtSession.ModLineCmd(dsScr.CurSrcNr, lCmd)
                            End If
                            CurrEdtSession.CursorDisplayColumn = CInt(-6 + lCmd.Length())
                        End If
                    End If
                Case 27 ' escape
                    If MacroRecording Then MacroString += "{ESC}"
                    KeyAlreadyProcessed = True
                    ReshowMsgSrc(False, True)
                    Dim cmdl As Integer = CurrEdtSession.CmdLineNr
                    If cmdl = -1 Then cmdl = ScrList.Count
                    dsScr = DirectCast(ScrList.Item(cmdl), ScreenLine)
                    dsScr.CurLinSrc = ""
                    dsScr.CurRepaint = True
                    SetmSelect(False)
                    RepaintSelectedLines()
            End Select
        Else
            KeyAlreadyProcessed = True ' ps = -1: outside VERIFY limits
        End If
        If KeyAlreadyProcessed Then
            eventArgs.Handled = True
#If Not DEBUG Then
            If rc > 0 Then
                DoCmd1("MSG Rc=" & CStr(rc), False)
            End If
#End If
            ForcePaint()
        End If
        Logg("KeyPress end")
    End Sub
    Private Sub XeditPc_Move(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Move
        Logg("XeditPc_Move")
        RepaintAllScreenLines = True
        ForcePaint()
    End Sub
    Private Sub Form1_DoubleClick(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles MyBase.DoubleClick, VSB.DoubleClick
        Logg("Form1_DoubleClick")
        CurrEdtSession.mMouseDown = False ' Mouse up follows mouse double
        DoCmd("MACRO DoubleClick", True)
        ForcePaint()
    End Sub
    Private Sub Form1_MouseDown(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.MouseEventArgs) Handles MyBase.MouseDown
        Dim dsScr As ScreenLine
        Dim Button As Integer = eventArgs.Button \ &H100000
        Dim Shift As Integer = System.Windows.Forms.Control.ModifierKeys \ &H10000
        Dim MouseScrLine, MouseScrPos As Integer
        RepaintSelectedLines()
        CurLnLineLr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine).CurSrcNr
        CurrEdtSession.IncrUnDoCnt = True
        MouseScrPos = CInt(eventArgs.X / CurrEdtSession.EditTextWidth) - 7S
        If MouseScrPos = 0 Then MouseScrPos = 1
        MouseScrLine = CInt(Math.Floor(CDbl(eventArgs.Y) / CurrEdtSession.RectHeight)) + 1S
        Logg("MouseDown " & CStr(MouseScrLine) & " " & CStr(MouseScrPos))
        If MouseScrLine < 1 Then
            MouseScrLine = 1
        Else
            If MouseScrLine > LinesScreenVisible Then
                MouseScrLine = LinesScreenVisible
            End If
        End If
        CurrEdtSession.CursorDisplayLine = MouseScrLine
        dsScr = DirectCast(ScrList.Item(MouseScrLine), ScreenLine)
        If dsScr.CurLinType = "C"c AndAlso MouseScrPos > dsScr.CharsOnScr Then
            MouseScrPos = dsScr.CharsOnScr + 1 ' place cursor after last char on cmdline
        End If
        CurrEdtSession.CursorDisplayColumn = MouseScrPos
        If Button = 1 And CurrEdtSession.Verif.Count() = 1 Then
            Dim vp As VerifyPair = DirectCast(CurrEdtSession.Verif.Item(1), VerifyPair)
            MacroClicked = True
            SetmSelect(False) ' wait for mousemove
            If MouseScrLine >= 1 And MouseScrLine <= LinesScreenVisible Then
                Logg("mouse d on " & CStr(dsScr.CurLinNr) & " " & dsScr.CurLinSrc)
                If MouseScrPos >= 0 AndAlso MouseScrLine >= CurrEdtSession.SrcOnScrn(1) AndAlso MouseScrLine <= CurrEdtSession.SrcOnScrn(CurrEdtSession.nSrcOnScrn) Then
                    If dsScr.CurLinType = "L"c Then
                        If vp.VerHex Then
                            sMdX = CInt(Math.Floor((MouseScrPos - 1) / 2S)) + 1S + CInt(vp.VerFrom - 1) ' initial selection mouse position in the file
                        Else
                            sMdX = MouseScrPos + CInt(vp.VerFrom - 1) ' initial selection mouse position in the file
                        End If
                        sMdY = dsScr.CurSrcNr
                        CurrEdtSession.mseSelLeft = CInt(sMdX)
                        CurrEdtSession.mseSelRight = CInt(sMdX)
                        CurrEdtSession.mseSelTop = sMdY
                        CurrEdtSession.mseSelBot = sMdY
                        CurrEdtSession.mSelRctg = (Shift = 2) ' Ctrl
                        CurrEdtSession.mMouseDown = True
                    End If
                End If
            End If
        End If
        If Button = 1 AndAlso Not CurrEdtSession.mMouseDown AndAlso MouseScrPos >= 0 Then
            If MouseScrLine = CurrEdtSession.CmdLineNr Or CurrEdtSession.CmdLineNr = -1 And MouseScrLine = LinesScreenVisible Then
                sMdX = MouseScrPos ' initial position
                CurrEdtSession.mseSelTop = -1 ' on cmdline
                CurrEdtSession.mseSelLeft = CInt(sMdX)
                CurrEdtSession.mseSelRight = CInt(sMdX)
                CurrEdtSession.mMouseDownCmd = True
            End If
        End If
        If Button = 2 And MouseScrPos >= 0 And Shift = 0 Then
            Me.ContextMenuStrip = ContextMenuStrip1
        Else
            Me.ContextMenuStrip = Nothing
        End If
        ForcePaint()
    End Sub
    Private Sub Formx_MouseMove(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.MouseEventArgs) Handles VSB.MouseMove
        Dim v As VScrollBar = DirectCast(eventSender, VScrollBar)
        MousePosX = eventArgs.X + v.Left
        MousePosY = eventArgs.Y + v.Left
    End Sub
    Private Sub Form1_MouseMove(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.MouseEventArgs) Handles MyBase.MouseMove
        If Not FormShown Then Return
        Dim dsScr As ScreenLine
        Dim Button As Integer = eventArgs.Button \ &H100000
        Dim Shift As Integer = System.Windows.Forms.Control.ModifierKeys \ &H10000
        Dim MouseScrPos, MouseScrLine, PrvMouseScrPos As Integer
        MousePosX = eventArgs.X
        MousePosY = eventArgs.Y
        Dim Pchanged As Boolean, i As Integer
        If CurrEdtSession.mMouseDownCmd Then
            MouseScrPos = CInt(eventArgs.X / CurrEdtSession.EditTextWidth) - 6S
            If MouseScrPos < 1 Then MouseScrPos = 1
            Pchanged = (CurrEdtSession.CursorDisplayColumn <> MouseScrPos)
            If Pchanged Then
                Logg("MouseMove cmd")
                If MouseScrPos >= sMdX Then
                    If MouseScrPos > 0 Then
                        CurrEdtSession.mseSelRight = MouseScrPos - 1S
                    Else
                        CurrEdtSession.mseSelRight = 0
                    End If
                Else
                    CurrEdtSession.mseSelLeft = MouseScrPos
                    If sMdX > 0 Then
                        CurrEdtSession.mseSelRight = CInt(sMdX - 1)
                    Else
                        CurrEdtSession.mseSelRight = 0
                    End If
                End If
                SetmSelect(True)
                If CurrEdtSession.CmdLineNr = -1 Then
                    i = LinesScreenVisible
                Else
                    i = CurrEdtSession.CmdLineNr
                End If
                dsScr = DirectCast(ScrList.Item(i), ScreenLine)
                dsScr.CurRepaint = True
                CurrEdtSession.CursorDisplayColumn = MouseScrPos
                ForcePaint()
            End If
        End If
        If CurrEdtSession.mMouseDown Then
            If Not FormShown Then Exit Sub
            Dim vp As VerifyPair = DirectCast(CurrEdtSession.Verif.Item(1), VerifyPair)
            MouseScrPos = CInt(eventArgs.X / CurrEdtSession.EditTextWidth) - 6S
            If MouseScrPos < 0 Then MouseScrPos = 1
            MouseScrLine = CInt(Math.Floor(CDbl(eventArgs.Y) / CurrEdtSession.RectHeight)) + 1S
            Logg("MouseMove " & CStr(MouseScrLine) & " " & CStr(MouseScrPos))
            If vp.VerHex Then
                mMdX = CInt(Math.Floor((MouseScrPos - 1) / 2S)) + 1S + CInt(vp.VerFrom - 1) ' initial selection mouse position in the file
            Else
                mMdX = MouseScrPos + CInt(vp.VerFrom - 1) ' initial selection mouse position in the file
            End If
            If MouseScrLine < CurrEdtSession.SrcOnScrn(1) Then
                TimerTick()
                DoCmd1("UP 1", False)
                If rc = 0 Then
                    ForcePaint()
                    dsScr = DirectCast(ScrList.Item(CurrEdtSession.SrcOnScrn(1)), ScreenLine)
                    CurrEdtSession.mseSelTop = dsScr.CurSrcNr
                    If CurrEdtSession.mseSelTop < 1 Then CurrEdtSession.mseSelTop = 1
                    CurrEdtSession.mseSelLeft = CInt(mMdX) - 1S
                    MouseScrLine = MouseScrLine + 1S
                    tMdX = eventArgs.X ' used in Timer1 to select text
                    tMdY = eventArgs.Y
                    TimerEnabled = True
                Else
                    rc = 0
                End If
            ElseIf MouseScrLine > CurrEdtSession.SrcOnScrn(CurrEdtSession.nSrcOnScrn) Then
                TimerTick()
                DoCmd1("DOWN 1", False)
                If rc = 0 Then
                    ForcePaint()
                    dsScr = DirectCast(ScrList.Item(CurrEdtSession.SrcOnScrn(CurrEdtSession.nSrcOnScrn)), ScreenLine)
                    CurrEdtSession.mseSelBot = dsScr.CurSrcNr
                    If MouseScrPos > 0 Then
                        CurrEdtSession.mseSelRight = CInt(mMdX) - 1S
                    Else
                        CurrEdtSession.mseSelRight = 0
                    End If
                    MouseScrLine = MouseScrLine - 1S
                    tMdX = eventArgs.X ' used in Timer1 to select text
                    tMdY = eventArgs.Y
                    TimerEnabled = True
                Else
                    rc = 0
                End If
            Else
                TimerInterval = 50 ' reset scrolling to "slow"
            End If
            Pchanged = (CurrEdtSession.CursorDisplayLine <> MouseScrLine Or CurrEdtSession.CursorDisplayColumn <> MouseScrPos)
            If MouseScrLine > 0 And MouseScrLine <= LinesScreenVisible And MouseScrPos > 0 Then
                CurrEdtSession.CursorDisplayLine = MouseScrLine
                CurrEdtSession.CursorDisplayColumn = MouseScrPos
            End If
            If Pchanged AndAlso MouseScrLine >= CurrEdtSession.SrcOnScrn(1) AndAlso MouseScrLine <= CurrEdtSession.SrcOnScrn(CurrEdtSession.nSrcOnScrn) Then
                dsScr = DirectCast(ScrList.Item(MouseScrLine), ScreenLine)
                If dsScr.CurLinType = "L"c Or dsScr.CurLinType = "X"c Then
                    mMdY = CInt(dsScr.CurSrcNr)
                    dsScr.CurRepaint = True
                    PrvMouseScrPos = CInt(CurrEdtSession.mseSelRight)
                    If (mMdX <> CurrEdtSession.mseSelRight Or mMdY <> CurrEdtSession.mseSelTop) Then
                        If Not CurrEdtSession.mSelRctg Then
                            If (mMdY * 100000) + mMdX >= (sMdY * 100000) + sMdX Then
                                CurrEdtSession.mseSelLeft = CInt(sMdX)
                                CurrEdtSession.mseSelTop = CInt(sMdY)
                                If mMdX > 0 Then
                                    CurrEdtSession.mseSelRight = mMdX - 1S
                                Else
                                    CurrEdtSession.mseSelRight = 0
                                End If
                                CurrEdtSession.mseSelBot = mMdY
                            Else
                                CurrEdtSession.mseSelLeft = mMdX
                                CurrEdtSession.mseSelTop = mMdY
                                If sMdX > 0 Then
                                    CurrEdtSession.mseSelRight = CInt(sMdX - 1)
                                Else
                                    CurrEdtSession.mseSelRight = 0
                                End If
                                CurrEdtSession.mseSelBot = sMdY
                            End If
                        Else ' select rectangle
                            If mMdY >= sMdY Then
                                CurrEdtSession.mseSelBot = mMdY
                            End If
                            If mMdY <= sMdY Then
                                CurrEdtSession.mseSelTop = dsScr.CurSrcNr
                            End If
                            If mMdX >= sMdX Then
                                If CurrEdtSession.mseSelRight <> mMdX - 1S Then
                                    RepaintSelectedLines()
                                    CurrEdtSession.mseSelRight = mMdX - 1S
                                End If
                            End If
                            If mMdX <= sMdX Then
                                If CurrEdtSession.mseSelLeft <> mMdX Then
                                    RepaintSelectedLines()
                                    CurrEdtSession.mseSelLeft = mMdX
                                End If
                            End If
                        End If
                    End If
                    If CurrEdtSession.mseSelRight < (vp.VerFrom - 1) Then CurrEdtSession.mseSelRight = CInt(vp.VerFrom - 1)
                    SetmSelect(True)
                End If
                ForcePaint()
            End If
        End If
    End Sub
    Private Sub Form1_MouseUp(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.MouseEventArgs) Handles MyBase.MouseUp
        CurrEdtSession.mMouseDown = False
        CurrEdtSession.mMouseDownCmd = False
        TimerInterval = 50
        ForcePaint()
        Logg("MouseUp Rect l " & CStr(CurrEdtSession.mseSelLeft) + " r: " + CStr(CurrEdtSession.mseSelRight) + " t: " + CStr(CurrEdtSession.mseSelTop) + " b: " + CStr(CurrEdtSession.mseSelBot))
    End Sub
    Private Sub TimerTick()
        If TimerEnabled Then
            TimerEnabled = False
            Thread.Sleep(TimerInterval)
            Form1_MouseMove(Me, New System.Windows.Forms.MouseEventArgs(Windows.Forms.MouseButtons.Left, 0, tMdX, tMdY, 0)) ' simulate mousemove, to continue selecting
            If TimerInterval > 3 Then TimerInterval = TimerInterval - 1 ' scroll a bit faster! but not to fast
        End If
    End Sub
    Private Sub RepaintFromLine(ByVal from As Integer)
        Dim i As Integer
        For i = from To ScrList.Count
            DirectCast(ScrList.Item(i), ScreenLine).CurRepaint = True
        Next
    End Sub
    Private Sub RepaintSelectedLines() ' repaint all selected lines
        Dim i, j, fj As Integer
        Dim dsScr, dsScrf, dsScrl As ScreenLine
        dsScrf = DirectCast(ScrList.Item(CurrEdtSession.SrcOnScrn(1)), ScreenLine)
        dsScrl = DirectCast(ScrList.Item(CurrEdtSession.SrcOnScrn(CurrEdtSession.nSrcOnScrn)), ScreenLine)
        fj = 1
        If CurrEdtSession.mseSelTop > 0 Then
            For i = CurrEdtSession.mseSelTop To CurrEdtSession.mseSelBot
                If i >= dsScrf.CurSrcNr Then
                    If i <= dsScrl.CurSrcNr Then
                        For j = fj To CurrEdtSession.nSrcOnScrn
                            dsScr = DirectCast(ScrList.Item(CurrEdtSession.SrcOnScrn(j)), ScreenLine)
                            If dsScr.CurSrcNr = i Then
                                fj = j + 1
                                dsScr.CurRepaint = True
                                Exit For
                            End If
                        Next
                    End If
                End If
            Next
        End If
    End Sub
    Private Sub RefrScrBuf()
        SaveAllModifiedLines()
        Dim dsScr As ScreenLine = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
        FillScreenBuffer(dsScr.CurSrcNr, False)
    End Sub
    Private Sub SetmSelect(ByRef indic As Boolean)
        CurrEdtSession.mSelect = indic
        If Not indic Then
            indic = False
        End If
        menu_Copy.Enabled = indic
        menu_Cut.Enabled = indic
    End Sub
    Private Sub DeleteSelectedArea()
        Logg("DeleteSelectedArea")
        Dim srcI, retLine, i As Integer
        Dim s As String, dsScr As ScreenLine
        If CurrEdtSession.mseSelTop = -1 Then ' on cmdline
            If CurrEdtSession.CmdLineNr = -1 Then
                i = LinesScreenVisible
            Else
                i = CurrEdtSession.CmdLineNr
            End If
            dsScr = DirectCast(ScrList.Item(i), ScreenLine)
            If CurrEdtSession.mseSelLeft > 1 And CurrEdtSession.mseSelLeft <= dsScr.CurLinSrc.Length Then
                s = dsScr.CurLinSrc.Substring(0, CurrEdtSession.mseSelLeft - 1)
            Else
                s = ""
            End If
            If CurrEdtSession.mseSelRight <= dsScr.CurLinSrc.Length Then
                s = s & dsScr.CurLinSrc.Substring(CurrEdtSession.mseSelRight)
            End If
            CurrEdtSession.CursorDisplayColumn -= CInt(CurrEdtSession.mseSelRight - CurrEdtSession.mseSelLeft + 1)
            If CurrEdtSession.CursorDisplayColumn < 1 Then CurrEdtSession.CursorDisplayColumn = 1
            SetmSelect(False)
            dsScr.CurLinSrc = s
            dsScr.CurRepaint = True
            ForcePaint()
        Else
            Dim nLinesDeleted As Integer, tScr, bScr As Integer
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.SrcOnScrn(1)), ScreenLine)
            tScr = dsScr.CurSrcNr ' actual top line on screen
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.SrcOnScrn(CurrEdtSession.nSrcOnScrn)), ScreenLine)
            bScr = dsScr.CurSrcNr ' actual bot line on screen
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
            retLine = dsScr.CurSrcNr ' actual current line on screen
            MoveToSourceLine(CurrEdtSession.mseSelTop)
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
            RepaintFromLine(CurrEdtSession.CurLineNr) '??????
            DelSelectedPartOfLine()
            nLinesDeleted = 0
            If CurrEdtSession.mseSelBot > CurrEdtSession.mseSelTop Then
                DoCmd1("DOWN", False)
                If CurrEdtSession.mseSelBot - CurrEdtSession.mseSelTop > 1 Then
                    If Not CurrEdtSession.mSelRctg Then
                        nLinesDeleted += CInt(CurrEdtSession.mseSelBot - CurrEdtSession.mseSelTop - 1)
                        DoCmd1("DELETE " & CStr(CurrEdtSession.mseSelBot - CurrEdtSession.mseSelTop - 1), False)
                        CurrEdtSession.mseSelBot = CurrEdtSession.mseSelTop + 1
                    Else
                        For srcI = CurrEdtSession.mseSelTop + 1 To CurrEdtSession.mseSelBot - 1
                            DelSelectedPartOfLine()
                            DoCmd1("DOWN", False)
                        Next
                    End If
                End If
                DelSelectedPartOfLine()
                If Not CurrEdtSession.mSelRctg And CurrEdtSession.EditZoneLeft = 1 Then ' zone > 1: left part of line remains intact, don't concatenate 
                    dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                    If dsScr.CurLinType = "L"c Then
                        If Not dsScr.CurSrcRead Then ReadSourceInScrBuf(dsScr)
                        s = dsScr.CurLinSrc
                        nLinesDeleted += 1S
                        DoCmd1("DELETE 1", False)
                        MoveToSourceLine(CurrEdtSession.mseSelTop)
                        dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                        If Not dsScr.CurSrcRead Then ReadSourceInScrBuf(dsScr)
                        dsScr.CurLinSrc = dsScr.CurLinSrc & s
                        dsScr.CurLinModified = True
                    End If
                End If
            End If
            If CurrEdtSession.mseSelTop >= tScr AndAlso CurrEdtSession.mseSelBot <= bScr AndAlso retLine <= CurrEdtSession.SourceList.Count Then
                ' delete was completely on screen, stay where i was on the screen
                MoveToSourceLine(retLine)
                CurrEdtSession.CursorDisplayLine -= nLinesDeleted ' to start of selected area
                If CurrEdtSession.CursorDisplayLine < 1 Then CurrEdtSession.CursorDisplayLine = 1 ' No area left
            Else ' goto 1st deleted pos
                MoveToSourceLine(CurrEdtSession.mseSelTop)
                CurrEdtSession.CursorDisplayLine = CurrEdtSession.CurLineNr
            End If
            MoveToSourcePos(CurrEdtSession.mseSelLeft)
            SetmSelect(False)
        End If
        rc = 0
    End Sub
    Private Sub DelSelectedPartOfLine()
        Dim S2 As String = "", S1 As String = "", S3 As String = "", dsScr As ScreenLine
        Dim vp As VerifyPair = DirectCast(CurrEdtSession.Verif.Item(1), VerifyPair)
        CurrEdtSession.mseSelLeftVer = CurrEdtSession.mseSelLeft
        CurrEdtSession.mseSelRightVer = CurrEdtSession.mseSelRight
        dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
        SelSelectedString(CurrEdtSession.CurLineNr, S1, S2, S3)
        AddUndo(3, dsScr)
        dsScr.CurLinSrc = S1 & S3
        dsScr.CurLinModified = True
    End Sub
    Private Sub SelSelectedString(ByVal ScrI As Integer, ByRef S1 As String, ByRef S2 As String, ByRef S3 As String)
        Dim dsscr As ScreenLine = DirectCast(ScrList.Item(ScrI), ScreenLine)
        Dim Asl, Bsl, Sel As Integer
        If Not dsscr.CurSrcRead Then ReadSourceInScrBuf(dsscr)
        'Dim s As String = dsscr.CurLinSrc
        CalcSelectedLineParts(dsscr, dsscr.CurLinSrc, Bsl, Sel, Asl)
        If Bsl > 0 Then
            S1 = dsscr.CurLinSrc.Substring(0, Bsl)
        Else
            S1 = ""
        End If
        If Sel > 0 Then
            S2 = dsscr.CurLinSrc.Substring(Bsl, Sel)
        Else
            S2 = ""
        End If
        If Asl > 0 Then
            S3 = dsscr.CurLinSrc.Substring(Bsl + Sel, Asl)
        Else
            S3 = ""
        End If
    End Sub
    Private Sub CopySelectedToClipboard()
        Logg("CopySelectedToClipboard")
        Dim S3 As String = "", S1 As String = "", S2 As String = "", SCl As String
        Dim SrcI, ScrI, RetLine, i, j As Integer, dsScr As ScreenLine
        Dim mClipbVk As String
        If CurrEdtSession.mseSelTop = -1 Then ' on cmdline
            If CurrEdtSession.CmdLineNr = -1 Then
                i = LinesScreenVisible
            Else
                i = CurrEdtSession.CmdLineNr
            End If
            dsScr = DirectCast(ScrList.Item(i), ScreenLine)
            j = CurrEdtSession.mseSelRight
            If j > dsScr.CurLinSrc.Length Then
                j = dsScr.CurLinSrc.Length
            End If
            SCl = dsScr.CurLinSrc.Substring(CurrEdtSession.mseSelLeft - 1, j - CurrEdtSession.mseSelLeft + 1)
        Else
            If CurrEdtSession.mSelRctg Then
                mClipbVk = "XEDIT COPY RECT"
            Else
                mClipbVk = ""
            End If
            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
            RetLine = dsScr.CurSrcNr
            CurrEdtSession.mseSelLeftVer = CurrEdtSession.mseSelLeft
            CurrEdtSession.mseSelRightVer = CurrEdtSession.mseSelRight
            MoveToSourceLine(CurrEdtSession.mseSelTop)
            ScrI = CurrEdtSession.CurLineNr
            SelSelectedString(ScrI, S1, S2, S3)
            SCl = mClipbVk & S2
            If CurrEdtSession.mseSelBot > CurrEdtSession.mseSelTop Then
                DoCmd1("DOWN", False)
                For SrcI = CurrEdtSession.mseSelTop + 1 To CurrEdtSession.mseSelBot - 1
                    SelSelectedString(ScrI, S1, S2, S3)
                    dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                    If dsScr.CurLinType = "L" Then
                        SCl = SCl & vbCrLf & S2
                    End If
                    DoCmd1("DOWN", False)
                Next
                SelSelectedString(ScrI, S1, S2, S3)
                dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                If dsScr.CurLinType = "L" Then SCl = SCl & vbCrLf & S2
            End If
        End If
        My.Computer.Clipboard.Clear()
        If SCl.Length() > 0 Then My.Computer.Clipboard.SetText(SCl)
        MoveToSourceLine(RetLine)
        SetmSelect(False)
        rc = 0 ' in case we reach BOT
    End Sub
    Private Sub CopyFromClipboard()
        Logg("CopyFromClipboard")
        Dim sN, s, temp, S2, sNl As String, dsScr As ScreenLine
        Dim sRest As String = ""
        Dim sbRest, mClipbVk As Boolean
        Dim AfterFirstLine As Boolean = False ' line 1 respects first visible column, 2, 3, 4 etc not
        Dim vScrPos, cScrPos As Integer
        Dim RetLine, i As Integer
        Dim LineIns As Integer, RetCP, RetCL As Integer
        Dim vp As New VerifyPair
        Pasting = True
        ReshowMsgSrc(True) ' away with all messages
        dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
        RetLine = dsScr.CurSrcNr
        RetCP = CurrEdtSession.CursorDisplayColumn
        RetCL = CurrEdtSession.CursorDisplayLine
        Dim multiLine As Boolean = False
        temp = My.Computer.Clipboard.GetText()
        dsScr = DirectCast(ScrList.Item(CurrEdtSession.CursorDisplayLine), ScreenLine)
        If dsScr.CurLinType <> "C"c Then
            If CurrEdtSession.mSelect Then
                DeleteSelectedArea()
                MoveToSourceLine(CurrEdtSession.mseSelTop)
                dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
            End If
            If dsScr.CurLinType = "B"c Then
                MoveToSourceLine(dsScr.CurSrcNr - 1)
                DoCmd1("INPUT", False)
                dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
            End If
            MoveToSourceLine(dsScr.CurSrcNr)
            If temp.Length() >= 15 AndAlso temp.Substring(0, 15) = "XEDIT COPY RECT" Then
                mClipbVk = True
                temp = temp.Substring(15)
            Else
                mClipbVk = False
            End If
            If Not mClipbVk Then
                Dim Sep As String = vbCrLf
                If temp.IndexOf(vbCrLf) = -1 AndAlso temp.IndexOf(vbLf) > -1 Then
                    Sep = vbLf
                ElseIf temp.IndexOf(vbCrLf) = -1 AndAlso temp.IndexOf(vbCr) > -1 Then
                    Sep = vbCr
                End If
                sbRest = False
                While temp.Length() > 0
                    If temp.Length() >= Sep.Length Then
                        S2 = temp.Substring(0, Sep.Length)
                    Else
                        S2 = ""
                    End If
                    If S2 = Sep Then ' when multiple lines are present, I loop twice: first I create an empty line for each separatot, next I add the contents
                        'SaveModifiedLine(dsScr)
                        DoCmd1("INPUT", False)
                        dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                        dsScr.CurSrcRead = True
                        temp = temp.Substring(Sep.Length)
                        LineIns = LineIns + 1
                        If RetCL < LinesScreenVisible Then
                            RetCL += 1
                        End If
                        multiLine = True
                    Else
                        'i = InStr(1, temp, Sep)
                        i = temp.IndexOf(Sep)
                        If i = -1 Then
                            s = temp
                            temp = ""
                        Else
                            s = temp.Substring(0, i)
                            temp = temp.Substring(i)
                            multiLine = True
                        End If
                        If dsScr.CurLinType = "T"c Then
                            DoCmd1("INPUT", False)
                            dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                        End If
                        If dsScr.CurLinType = "L" Then
                            If Not dsScr.CurSrcRead Then ReadSourceInScrBuf(dsScr)
                            AddUndo(3, dsScr)
                            If Not AfterFirstLine Then
                                vp = DirectCast(CurrEdtSession.Verif.Item(1), VerifyPair)
                                sN = dsScr.CurLinSrc.PadRight(CurrEdtSession.CursorDisplayColumn - 1 + vp.VerFrom - 1)
                            Else
                                sN = dsScr.CurLinSrc.PadRight(CurrEdtSession.CursorDisplayColumn - 1)
                            End If
                            If Not sbRest Then
                                sbRest = True
                                LineIns = 1
                                If Not AfterFirstLine Then
                                    cScrPos = CInt(CurrEdtSession.CursorDisplayColumn + vp.VerFrom - 1)
                                Else
                                    cScrPos = CInt(CurrEdtSession.CursorDisplayColumn)
                                End If
                                sRest = sN.Substring(cScrPos - 1)
                            Else
                                If LineIns = 1 Then
                                    cScrPos = cScrPos + 1S
                                Else
                                    cScrPos = CInt(CurrEdtSession.CursorDisplayColumn)
                                End If
                            End If
                            sNl = sN.Substring(0, cScrPos - 1) & s
                            If dsScr.CurLinSsd.SrcLength = -1 Then
                                dsScr.CurLinSsd.SrcLength = 0 ' paste on Ied line
                                dsScr.CurLinSsd.SrcStart = 1
                            End If
                            If CurrEdtSession.CursorDisplayColumn + s.Length > CurrEdtSession.Lrecl Then
                                sNl = sNl.Substring(0, CurrEdtSession.Lrecl - 1 - CurrEdtSession.CursorDisplayColumn)
                                DoCmd1("MSG Truncated", False)
                            End If
                            dsScr.CurLinSrc = sNl
                            dsScr.CurLinModified = True
                            dsScr.CurRepaint = True
                            CurrEdtSession.CursorDisplayColumn = CInt(CurrEdtSession.CursorDisplayColumn + s.Length())
                        End If
                        AfterFirstLine = True
                    End If
                End While
                If sbRest Then
                    AddUndo(3, dsScr)
                    If dsScr.CurLinSsd.SrcLength = -1 Then
                        dsScr.CurLinSsd.SrcLength = 0 ' paste on Ied line
                        dsScr.CurLinSsd.SrcStart = 1
                    End If
                    RetCP = dsScr.CurLinSrc.Length + 1
                    dsScr.CurLinSrc = dsScr.CurLinSrc & sRest
                End If
            Else
                vScrPos = CurrEdtSession.CursorDisplayColumn
                While temp.Length() > 0
                    If temp.Length() >= 2 Then
                        S2 = temp.Substring(0, 2)
                    Else
                        S2 = ""
                    End If
                    If S2 = vbCrLf Then
                        DoCmd1("DOWN", False)
                        temp = temp.Substring(2)
                        dsScr = DirectCast(ScrList.Item(CurrEdtSession.CurLineNr), ScreenLine)
                        CurrEdtSession.CursorDisplayColumn = vScrPos
                    Else
                        i = InStr(1, temp, vbCrLf)
                        If i = 0 Then
                            s = temp
                            temp = ""
                        Else
                            s = temp.Substring(0, i - 1)
                            temp = temp.Substring(i - 1)
                        End If
                        If dsScr.CurLinType = "L" Then
                            If Not dsScr.CurSrcRead Then ReadSourceInScrBuf(dsScr)
                            If dsScr.CurLinSsd.SrcLength = -1 Then
                                dsScr.CurLinSsd.SrcLength = 0 ' paste on Ied line
                                dsScr.CurLinSsd.SrcStart = 1
                            End If
                            AddUndo(3, dsScr)
                            Dim vpNibble As Boolean
                            Dim ps As Integer = EditPosSrc(vp, vpNibble, dsScr)
                            cScrPos = CurrEdtSession.CursorDisplayColumn - 1 + vp.VerFrom
                            sN = dsScr.CurLinSrc.PadRight(cScrPos - 1)
                            sRest = Mid(sN, cScrPos)
                            If CurrEdtSession.InsOvertype Then
                                sNl = sN.Substring(0, cScrPos - 1) & s & sRest
                            Else
                                If sRest.Length() > 0 Then sRest = Mid(sRest, s.Length() + 1)
                                sNl = sN.Substring(0, cScrPos - 1) & s & sRest
                            End If
                            dsScr.CurLinSrc = sNl
                            dsScr.CurLinModified = True
                            CurrEdtSession.CursorDisplayColumn = CInt(CurrEdtSession.CursorDisplayColumn + s.Length())
                            RetCP = CurrEdtSession.CursorDisplayColumn
                        End If
                    End If
                End While
            End If
            MoveToSourceLine(RetLine)
            CurrEdtSession.CursorDisplayColumn = RetCP
            CurrEdtSession.CursorDisplayLine = RetCL
        Else ' paste on commandline
            s = dsScr.CurLinSrc.PadRight(CurrEdtSession.CursorDisplayColumn)
            s = s.Substring(0, CurrEdtSession.CursorDisplayColumn - 1) & temp & s.Substring(CurrEdtSession.CursorDisplayColumn - 1)
            If s.Length + CurrEdtSession.CursorDisplayColumn > Integer.MaxValue Then s = s.Substring(0, Integer.MaxValue - 1 - CurrEdtSession.CursorDisplayColumn)
            dsScr.CurLinSrc = s
            dsScr.CurRepaint = True
            CurrEdtSession.CursorDisplayColumn += temp.Length()
        End If
        rc = 0
        Pasting = False
    End Sub
    Private Function Translate(ByVal s As String, ByRef froms As String, ByRef tos As String) As String
        Dim d, i, l, p As Integer
        Dim c As String
        l = s.Length()
        p = 1
        For i = 1 To l
            c = Mid(s, i, 1)
            d = 0
            p = InStr(froms, c)
            If p > 0 Then
                s = VB.Left(s, i - 1) & Mid(tos, p, 1) & Mid(s, i + 1)
            End If
        Next
        Translate = s
    End Function
    Private Sub XeditPc_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        Logg("FormClosing")
        If Not FormAlreadyClosed Then
            FormAlreadyClosed = True
            While EdtSessions.Count > 1
                DoCmd("QUIT", True)
                If rc = 32 Then ' user chose RETURN TO EDIT
                    e.Cancel = True
                    FormAlreadyClosed = False
                    Exit Sub
                End If
            End While
            DoCmd("QUIT", True)
        End If
        If rc = 32 Then
            e.Cancel = True
            FormAlreadyClosed = False
        End If
        CancelCmd = True
    End Sub
    Private Sub menu_Copy_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles menu_Copy.Click
        CopySelectedToClipboard()
        RepaintAllScreenLines = True
        ForcePaint()
    End Sub
    Private Sub menu_Cut_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles menu_Cut.Click
        DeleteSelectedArea()
        RepaintAllScreenLines = True
        ForcePaint()
    End Sub
    Private Sub menu_Paste_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles menu_Paste.Click
        CopyFromClipboard()
        RepaintAllScreenLines = True
        ForcePaint()
    End Sub
    Private Sub menu_Macro_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles menu_Macro.Click
        If MacroState = 0 Then ' 0 inactive
            MacroRecording = True
            MacroState = 1
            MacroString = ""
            menu_Macro.Text = "Macro Stop"
            MacroClicked = False
        ElseIf MacroState = 1 Then ' 1 recording
            MacroRecording = False
            MacroState = 0
            menu_Macro.Text = "Macro Record"
            SaveFileDialog1.InitialDirectory = My.Application.Info.DirectoryPath
            SaveFileDialog1.FileName = ""
            SaveFileDialog1.CheckFileExists = False
            SaveFileDialog1.ShowHelp = False
            SaveFileDialog1.Filter = "Macros (*.txt)|*.txt"
            SaveFileDialog1.ShowDialog()
            MacroFile = SaveFileDialog1.FileName
            If MacroFile <> "" Then
                ' Write to a file.
                Try
                    Using sw As StreamWriter = New StreamWriter(MacroFile)
                        sw.Write(MacroString)
                    End Using
                Catch ex As Exception
                    DoCmd1("MSG Macro Not saved " + ex.Message, False)
                End Try
                If MacroClicked Then DoCmd1("MSG Mouseclicks Not included in Macro", False)
            End If
        End If
        RepaintAllScreenLines = True
        ForcePaint()
    End Sub
    Private Sub menu_Return_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles menu_Return.Click
        RepaintAllScreenLines = True
        ForcePaint()
    End Sub
    Dim moWhAct As Boolean = False
    Private Sub XeditPc_MouseWheel(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseWheel, VSB.MouseWheel
        If moWhAct Then Exit Sub
        moWhAct = True
        Logg("mouse wheel start")
        ReshowMsgSrc(False)
        Dim s As String = ""
        If ((Control.ModifierKeys And Keys.Shift) = Keys.Shift) Then
            s = "SHIFT-"
        End If
        If ((Control.ModifierKeys And Keys.Alt) = Keys.Alt) Then
            s = "ALT-"
        End If
        If ((Control.ModifierKeys And Keys.Control) = Keys.Control) Then
            s = "CTRL-"
        End If
        If e.Delta > 0 Then
            DoCmd(s & "MOUSEWHEELUP", True)
        Else
            DoCmd(s & "MOUSEWHEELDOWN", True)
        End If
        ForcePaint()
        Logg("mouse wheel end")
        moWhAct = False
    End Sub
    Private Sub XeditPc_MouseClick(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseClick, VSB.MouseClick
        Dim Button As Integer = e.Button \ &H100000
        Dim s As String = ""
        If ((Control.ModifierKeys And Keys.Shift) = Keys.Shift) Then
            s = "SHIFT-"
        End If
        If ((Control.ModifierKeys And Keys.Alt) = Keys.Alt) Then
            s = "ALT-"
        End If
        If ((Control.ModifierKeys And Keys.Control) = Keys.Control) Then
            If Button > 1 Then s = "CTRL-"
        End If
        If Button > 2 Or s.Length() > 0 Then
            DoCmd1(s & "MOUSEBUTTON" & CStr(Button), False)
            ForcePaint()
        End If
    End Sub
    Private Sub XeditPc_Deactivate(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Deactivate
        Logg("XeditPc_Deactivate")
        RepaintAllScreenLines = True
        ForcePaint()
    End Sub
    Private Function trMsg(ByVal uMsg As Long) As String
        trMsg = ""
        If uMsg = &H0 Then trMsg = "WM_NULL"
        If uMsg = &H1 Then trMsg = "WM_CREATE"
        If uMsg = &H2 Then trMsg = "WM_DESTROY"
        If uMsg = &H3 Then trMsg = "WM_MOVE"
        If uMsg = &H5 Then trMsg = "WM_SIZE"
        If uMsg = &H6 Then trMsg = "WM_ACTIVATE"
        If uMsg = &H7 Then trMsg = "WM_SETFOCUS"
        If uMsg = &H8 Then trMsg = "WM_KILLFOCUS"
        If uMsg = &HA Then trMsg = "WM_ENABLE"
        If uMsg = &HB Then trMsg = "WM_SETREDRAW"
        If uMsg = &HC Then trMsg = "WM_SETTEXT"
        If uMsg = &HD Then trMsg = "WM_GETTEXT"
        If uMsg = &HE Then trMsg = "WM_GETTEXTLENGTH"
        If uMsg = &HF Then trMsg = "WM_PAINT"
        If uMsg = &H10 Then trMsg = "WM_CLOSE"
        If uMsg = &H11 Then trMsg = "WM_QUERYENDSESSION"
        If uMsg = &H12 Then trMsg = "WM_QUIT"
        If uMsg = &H13 Then trMsg = "WM_QUERYOPEN"
        If uMsg = &H14 Then trMsg = "WM_ERASEBKGND"
        If uMsg = &H15 Then trMsg = "WM_SYSCOLORCHANGE"
        If uMsg = &H16 Then trMsg = "WM_ENDSESSION"
        If uMsg = &H18 Then trMsg = "WM_SHOWWINDOW"
        If uMsg = &H1A Then trMsg = "WM_WININICHANGE"
        If uMsg = &H1B Then trMsg = "WM_DEVMODECHANGE"
        If uMsg = &H1C Then trMsg = "WM_ACTIVATEAPP"
        If uMsg = &H1D Then trMsg = "WM_FONTCHANGE"
        If uMsg = &H1E Then trMsg = "WM_TIMECHANGE"
        If uMsg = &H1F Then trMsg = "WM_CANCELMODE"
        If uMsg = &H20 Then trMsg = "WM_SETCURSOR"
        If uMsg = &H21 Then trMsg = "WM_MOUSEACTIVATE"
        If uMsg = &H22 Then trMsg = "WM_CHILDACTIVATE"
        If uMsg = &H23 Then trMsg = "WM_QUEUESYNC"
        If uMsg = &H24 Then trMsg = "WM_GETMINMAXINFO"
        If uMsg = &H26 Then trMsg = "WM_PAINTICON"
        If uMsg = &H27 Then trMsg = "WM_ICONERASEBKGND"
        If uMsg = &H28 Then trMsg = "WM_NEXTDLGCTL"
        If uMsg = &H2A Then trMsg = "WM_SPOOLERSTATUS"
        If uMsg = &H2B Then trMsg = "WM_DRAWITEM"
        If uMsg = &H2C Then trMsg = "WM_MEASUREITEM"
        If uMsg = &H2D Then trMsg = "WM_DELETEITEM"
        If uMsg = &H2E Then trMsg = "WM_VKEYTOITEM"
        If uMsg = &H2F Then trMsg = "WM_CHARTOITEM"
        If uMsg = &H30 Then trMsg = "WM_SETFONT"
        If uMsg = &H31 Then trMsg = "WM_GETFONT"
        If uMsg = &H32 Then trMsg = "WM_SETHOTKEY"
        If uMsg = &H33 Then trMsg = "WM_GETHOTKEY"
        If uMsg = &H37 Then trMsg = "WM_QUERYDRAGICON"
        If uMsg = &H39 Then trMsg = "WM_COMPAREITEM"
        If uMsg = &H3D Then trMsg = "WM_GETOBJECT"
        If uMsg = &H41 Then trMsg = "WM_COMPACTING"
        If uMsg = &H44 Then trMsg = "WM_COMMNOT' IfY"
        If uMsg = &H46 Then trMsg = "WM_WINDOWPOSCHANGING"
        If uMsg = &H47 Then trMsg = "WM_WINDOWPOSCHANGED"
        If uMsg = &H48 Then trMsg = "WM_POWER"
        If uMsg = &H4A Then trMsg = "WM_COPYDATA"
        If uMsg = &H4B Then trMsg = "WM_CANCELJOURNAL"
        If uMsg = &H4E Then trMsg = "WM_NOT' IfY"
        If uMsg = &H50 Then trMsg = "WM_INPUTLANGCHANGEREQUEST"
        If uMsg = &H51 Then trMsg = "WM_INPUTLANGCHANGE"
        If uMsg = &H52 Then trMsg = "WM_TCARD"
        If uMsg = &H53 Then trMsg = "WM_HELP"
        If uMsg = &H54 Then trMsg = "WM_USERCHANGED"
        If uMsg = &H55 Then trMsg = "WM_NOT' IfYFORMAT"
        If uMsg = &H7B Then trMsg = "WM_CONTEXTMENU"
        If uMsg = &H7C Then trMsg = "WM_STYLECHANGING"
        If uMsg = &H7D Then trMsg = "WM_STYLECHANGED"
        If uMsg = &H7E Then trMsg = "WM_DISPLAYCHANGE"
        If uMsg = &H7F Then trMsg = "WM_GETICON"
        If uMsg = &H80 Then trMsg = "WM_SETICON"
        If uMsg = &H81 Then trMsg = "WM_NCCREATE"
        If uMsg = &H82 Then trMsg = "WM_NCDESTROY"
        If uMsg = &H83 Then trMsg = "WM_NCCALCSIZE"
        If uMsg = &H84 Then trMsg = "WM_NCHITTEST"
        If uMsg = &H85 Then trMsg = "WM_NCPAINT"
        If uMsg = &H86 Then trMsg = "WM_NCACTIVATE"
        If uMsg = &H87 Then trMsg = "WM_GETDLGCODE"
        If uMsg = &H88 Then trMsg = "WM_SYNCPAINT"
        If uMsg = &HA0 Then trMsg = "WM_NCMOUSEMOVE"
        If uMsg = &HA1 Then trMsg = "WM_NCLBUTTONDOWN"
        If uMsg = &HA2 Then trMsg = "WM_NCLBUTTONUP"
        If uMsg = &HA3 Then trMsg = "WM_NCLBUTTONDBLCLK"
        If uMsg = &HA4 Then trMsg = "WM_NCRBUTTONDOWN"
        If uMsg = &HA5 Then trMsg = "WM_NCRBUTTONUP"
        If uMsg = &HA6 Then trMsg = "WM_NCRBUTTONDBLCLK"
        If uMsg = &HA7 Then trMsg = "WM_NCMBUTTONDOWN"
        If uMsg = &HA8 Then trMsg = "WM_NCMBUTTONUP"
        If uMsg = &HA9 Then trMsg = "WM_NCMBUTTONDBLCLK"
        If uMsg = &H100 Then trMsg = "WM_KEYFIRST"
        If uMsg = &H100 Then trMsg = "WM_KEYDOWN"
        If uMsg = &H101 Then trMsg = "WM_KEYUP"
        If uMsg = &H102 Then trMsg = "WM_CHAR"
        If uMsg = &H103 Then trMsg = "WM_DEADCHAR"
        If uMsg = &H104 Then trMsg = "WM_SYSKEYDOWN"
        If uMsg = &H105 Then trMsg = "WM_SYSKEYUP"
        If uMsg = &H106 Then trMsg = "WM_SYSCHAR"
        If uMsg = &H107 Then trMsg = "WM_SYSDEADCHAR"
        If uMsg = &H108 Then trMsg = "WM_KEYLAST"
        If uMsg = &H10D Then trMsg = "WM_IME_STARTCOMPOSITION"
        If uMsg = &H10E Then trMsg = "WM_IME_ENDCOMPOSITION"
        If uMsg = &H10F Then trMsg = "WM_IME_KEYLAST"
        If uMsg = &H110 Then trMsg = "WM_INITDIALOG"
        If uMsg = &H111 Then trMsg = "WM_COMMAND"
        If uMsg = &H112 Then trMsg = "WM_SYSCOMMAND"
        If uMsg = &H113 Then trMsg = "WM_TIMER"
        If uMsg = &H114 Then trMsg = "WM_HSCROLL"
        If uMsg = &H115 Then trMsg = "WM_VSCROLL"
        If uMsg = &H116 Then trMsg = "WM_INITMENU"
        If uMsg = &H117 Then trMsg = "WM_INITMENUPOPUP"
        If uMsg = &H11F Then trMsg = "WM_MENUSELECT"
        If uMsg = &H120 Then trMsg = "WM_MENUCHAR"
        If uMsg = &H121 Then trMsg = "WM_ENTERIDLE"
        If uMsg = &H122 Then trMsg = "WM_MENURBUTTONUP"
        If uMsg = &H123 Then trMsg = "WM_MENUDRAG"
        If uMsg = &H124 Then trMsg = "WM_MENUGETOBJECT"
        If uMsg = &H125 Then trMsg = "WM_UNINITMENUPOPUP"
        If uMsg = &H126 Then trMsg = "WM_MENUCOMMAND"
        If uMsg = &H132 Then trMsg = "WM_CTLCOLORMSGBOX"
        If uMsg = &H133 Then trMsg = "WM_CTLCOLOREDIT"
        If uMsg = &H134 Then trMsg = "WM_CTLCOLORLISTBOX"
        If uMsg = &H135 Then trMsg = "WM_CTLCOLORBTN"
        If uMsg = &H136 Then trMsg = "WM_CTLCOLORDLG"
        If uMsg = &H137 Then trMsg = "WM_CTLCOLORSCROLLBAR"
        If uMsg = &H138 Then trMsg = "WM_CTLCOLORSTATIC"
        If uMsg = &H200 Then trMsg = "WM_MOUSEMOVE"
        If uMsg = &H201 Then trMsg = "WM_LBUTTONDOWN"
        If uMsg = &H202 Then trMsg = "WM_LBUTTONUP"
        If uMsg = &H203 Then trMsg = "WM_LBUTTONDBLCLK"
        If uMsg = &H204 Then trMsg = "WM_RBUTTONDOWN"
        If uMsg = &H205 Then trMsg = "WM_RBUTTONUP"
        If uMsg = &H206 Then trMsg = "WM_RBUTTONDBLCLK"
        If uMsg = &H207 Then trMsg = "WM_MBUTTONDOWN"
        If uMsg = &H208 Then trMsg = "WM_MBUTTONUP"
        If uMsg = &H20A Then trMsg = "WM_MOUSEWHEEL"
        If uMsg = &H209 Then trMsg = "WM_MOUSELAST"
        If uMsg = &H210 Then trMsg = "WM_PARENTNOT' IfY"
        If uMsg = &H211 Then trMsg = "WM_ENTERMENULOOP"
        If uMsg = &H212 Then trMsg = "WM_EXITMENULOOP"
        If uMsg = &H213 Then trMsg = "WM_NEXTMENU"
        If uMsg = &H214 Then trMsg = "WM_SIZING"
        If uMsg = &H215 Then trMsg = "WM_CAPTURECHANGED"
        If uMsg = &H216 Then trMsg = "WM_MOVING"
        If uMsg = &H218 Then trMsg = "WM_POWERBROADCAST"
        If uMsg = &H219 Then trMsg = "WM_DEVICECHANGE"
        If uMsg = &H220 Then trMsg = "WM_MDICREATE"
        If uMsg = &H221 Then trMsg = "WM_MDIDESTROY"
        If uMsg = &H222 Then trMsg = "WM_MDIACTIVATE"
        If uMsg = &H223 Then trMsg = "WM_MDIRESTORE"
        If uMsg = &H224 Then trMsg = "WM_MDINEXT"
        If uMsg = &H225 Then trMsg = "WM_MDIMAXIMIZE"
        If uMsg = &H226 Then trMsg = "WM_MDITILE"
        If uMsg = &H227 Then trMsg = "WM_MDICASCADE"
        If uMsg = &H228 Then trMsg = "WM_MDIICONARRANGE"
        If uMsg = &H229 Then trMsg = "WM_MDIGETACTIVE"
        If uMsg = &H230 Then trMsg = "WM_MDISETMENU"
        If uMsg = &H231 Then trMsg = "WM_ENTERSIZEMOVE"
        If uMsg = &H232 Then trMsg = "WM_EXITSIZEMOVE"
        If uMsg = &H233 Then trMsg = "WM_DROPFILES"
        If uMsg = &H234 Then trMsg = "WM_MDIREFRESHMENU"
        If uMsg = &H281 Then trMsg = "WM_IME_SETCONTEXT"
        If uMsg = &H282 Then trMsg = "WM_IME_NOT' IfY"
        If uMsg = &H283 Then trMsg = "WM_IME_CONTROL"
        If uMsg = &H284 Then trMsg = "WM_IME_COMPOSITIONFULL"
        If uMsg = &H285 Then trMsg = "WM_IME_SELECT"
        If uMsg = &H286 Then trMsg = "WM_IME_CHAR"
        If uMsg = &H288 Then trMsg = "WM_IME_REQUEST"
        If uMsg = &H290 Then trMsg = "WM_IME_KEYDOWN"
        If uMsg = &H291 Then trMsg = "WM_IME_KEYUP"
        If uMsg = &H2A1 Then trMsg = "WM_MOUSEHOVER"
        If uMsg = &H2A2 Then trMsg = "WM_MOUSE???"
        If uMsg = &H2A3 Then trMsg = "WM_MOUSELEAVE"
        If uMsg = &H300 Then trMsg = "WM_CUT"
        If uMsg = &H301 Then trMsg = "WM_COPY"
        If uMsg = &H302 Then trMsg = "WM_PASTE"
        If uMsg = &H303 Then trMsg = "WM_CLEAR"
        If uMsg = &H304 Then trMsg = "WM_UNDO"
        If uMsg = &H305 Then trMsg = "WM_RENDERFORMAT"
        If uMsg = &H306 Then trMsg = "WM_RENDERALLFORMATS"
        If uMsg = &H307 Then trMsg = "WM_DESTROYCLIPBOARD"
        If uMsg = &H308 Then trMsg = "WM_DRAWCLIPBOARD"
        If uMsg = &H309 Then trMsg = "WM_PAINTCLIPBOARD"
        If uMsg = &H30A Then trMsg = "WM_VSCROLLCLIPBOARD"
        If uMsg = &H30B Then trMsg = "WM_SIZECLIPBOARD"
        If uMsg = &H30C Then trMsg = "WM_ASKCBFORMATNAME"
        If uMsg = &H30D Then trMsg = "WM_CHANGECBCHAIN"
        If uMsg = &H30E Then trMsg = "WM_HSCROLLCLIPBOARD"
        If uMsg = &H30F Then trMsg = "WM_QUERYNEWPALETTE"
        If uMsg = &H310 Then trMsg = "WM_PALETTEISCHANGING"
        If uMsg = &H311 Then trMsg = "WM_PALETTECHANGED"
        If uMsg = &H312 Then trMsg = "WM_HOTKEY"
        If uMsg = &H317 Then trMsg = "WM_PRINT"
        If uMsg = &H318 Then trMsg = "WM_PRINTCLIENT"
        If uMsg = &H358 Then trMsg = "WM_HANDHELDFIRST"
        If uMsg = &H35F Then trMsg = "WM_HANDHELDLAST"
        If uMsg = &H360 Then trMsg = "WM_AFXFIRST"
        If uMsg = &H37F Then trMsg = "WM_AFXLAST"
        If uMsg = &H380 Then trMsg = "WM_PENWINFIRST"
        If uMsg = &H38F Then trMsg = "WM_PENWINLAST"
    End Function
    Protected Overrides Sub WndProc(ByRef m As System.Windows.Forms.Message)
        MyBase.WndProc(m)
        If m.Msg <> 132 And m.Msg <> 32 And m.Msg <> 512 Then
            Logg("WndProc MSG " & CStr(m.Msg) & " " & trMsg(m.Msg))
        End If
        If m.Msg = 136 Then 'WM_SYNCPAINT
            Logg("WndProc WM_SYNCPAINT")
            RepaintFromLine(1)
        End If
    End Sub
    Public Sub CallDoEvent()
        System.Windows.Forms.Application.DoEvents()
        nrCyclesEv = 0
    End Sub

    Sub RexxCmd(ByVal env As String, ByVal s As String, ByVal e As RexxEvent) Handles Rxs.doCmd
#If CreLogFile Then
        Logg("RexxCmd env/cmd """ & env.ToUpper(CultInf) & """/""" & s & """")
        Logg("RexxCmd """ & env.ToUpper(CultInf) & """ = ""XEDIT"": " & (env.ToUpper(CultInf) = "XEDIT"))
#End If
        If env.ToUpper(CultInf) = "XEDIT" Then
            Me.DoCmd1(s, False)
        Else
            Dim myProcess As Process = New Process()
            Dim en, pa As String, sep As Char, i As Integer
            If s.Substring(0, 1) = """" Then
                sep = """"c
                en = s.Substring(1)
            Else
                sep = " "c
                en = s
            End If
            i = en.IndexOf(sep)
            If i = -1 Then
                pa = ""
            Else
                pa = en.Substring(i + 1)
                en = en.Substring(0, i)
            End If
            'MsgBox(env.ToUpper(CultInf) & " " & s & "!" & en & "!" & pa)
            Try
                myProcess.StartInfo.UseShellExecute = False
                myProcess.StartInfo.CreateNoWindow = True
                myProcess.StartInfo.FileName = en
                myProcess.StartInfo.Arguments = pa
                If env.ToUpper(CultInf) = "NOWAIT" Then myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Minimized
                myProcess.Start()
                ' myProcess = Process.Start(en, pa)
                If env.ToUpper(CultInf) = "NOWAIT" Then
                    rc = 0
                Else
                    myProcess.WaitForExit()
                    rc = myProcess.ExitCode
                End If
            Catch ex As Exception
                rc = -1
            End Try
        End If
        e.rc = rc
    End Sub
End Class