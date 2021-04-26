Public Class EdtSession
    Public EditFileName As String
    Public EditFile As FileStream
    Public BakfileAlreadyCreated As Boolean ' Created .BAK (on first SAVE only)
    Public CursorDisplayColumn, PrevEditPosScr, prevPressPos As Short ' of cursor
    Public CursorDisplayLine, PrevEditLineScr, prevPressLine As Integer ' of cursor
    Public CmdLineNr, MsgLineNrF, MsgLineNrT, CurLineNr As Short ' from SET CMDLINE MSGLINE CURLINE 
    Public Msgs, ScrOverlayed As New Collection ' messages/source to be displayed
    Public MsgOverlay As Boolean ' Msg in overlay?
    'Public NrBeforeCurL As Short ' nr of lines of source BEFORE curline
    Public nSrcOnScrn As Short ' Nr of source dependent lines on screen
    Public SrcOnScrn(255) As Short ' indices in Srclist of lines on screen that contain sources (excl title, fixed lines, etc)
    Public Tabs(255) As Short
    Public FileChanged As Boolean ' file has changed?
    Public SessionInited As Boolean ' file has passed init phase and has been shown?
    Public SeqOfFirstSourcelineOnScreen As Integer ' Sourcenr of 1st line on screen
    Public IsUnicode As Boolean = False
    Public FileUsesEndlineLF As Boolean
    Public FileUsesEndlineCR As Boolean

    Public EncodingType As Char ' A Ascii U Unicode 8 Utf8
    Public LinEndOff As Boolean ' from SET LINEND ON char
    Public LinEndChar As Char
    Public EditZoneLeft As Short ' from SET ZONE min max
    Public EditZoneRight As Short
    Public EditDisplayMin As Short ' from SET DISPLAY min max
    Public EditDisplayMax As Short
    Public EditAutoSave, AutosaveModifications, AutoSavedTimes As Integer ' >0: save
    Public AutoSavNames As New Collection
    Public RecfmV As Boolean ' from SET RECFM
    Public Lrecl As Short ' from SET LRECL
    Public Trunc As Short ' from SET TRUNC
    Public InsOvertype As Boolean ' true = insert
    Public ScopeAllDisplay As Boolean ' true = Scope all
    Public Wrap As Boolean ' true = WRAP on
    Public Shadow As Boolean ' true = SHADOW on
    Public ShowEol As Boolean ' from SHOWEOL
    Public CaseMU As Boolean ' from SET CASE U/M I/R
    Public CaseIR As Boolean
    Public Stay As Boolean ' from SET STAY
    Public HexM As Boolean ' from SET HEX
    Public MsgMode As Boolean ' from SET MSGMode
    Public ExpTabs As Boolean ' from SET EXPANDTAB 

    Public EditTextHeight, EditTextWidth As Single, mMouseDown, mMouseDownCmd As Boolean, RectHeight As Double
    Public mseSelLeft, mseSelLeftVer As Short ' selected by mouse (and CTRL) 
    Public mseSelTop As Integer
    Public mseSelRight, mseSelRightVer As Short
    Public mseSelBot As Integer
    Public mSelect As Boolean ' there is selected text!
    Public mSelRctg As Boolean ' selected with CTRL = rectangle, not whole lines

    Public color_select As Color = Color.White ' selected area
    Public color_selectbg As Color = Color.Blue
    Public color_command As Color = Color.Black ' command
    Public color_commandbg As Color = Color.White
    Public color_linenr As Color = Color.Black ' linenumbers general
    Public color_curline As Color = Color.Blue ' curline linenumber
    Public color_linenrbg As Color = Color.White
    Public color_text As Color = Color.Black ' main text
    Public color_textcursor As Color = Color.Indigo ' line with cursor
    Public color_textbg As Color = Color.White
    Public color_cursor As Color = Color.Red ' cursor symbol

    Public Verif As New Collection ' Verify pairs
    Public VerifyOn As Boolean ' Set Verify On/Off
    Public SourceList As New Collection
    Public Settings As New Collection
    Public LineCommands As New Collection
    Public PendingCommands As New Collection
    Public IedLines As New Collection
    Public Synonyms As New Collection
    Public ReservedLines As New Collection

    Public UndoS As New Collection ' undo stack
    Public UnDoCnt, chgCount, UndoSet As Integer ' n° undo's; n° of changes made by user
    Public DoUnDo, IncrUnDoCnt As Boolean ' Don't stack changes while UNDOing; increment Undo block counter once per User action
    Public UndoLineP As Integer, UndoPosP As Short ' pos of cursor for previous change

    Dim RepeatFactorPresent As Boolean
    Dim RepeatFactor As Integer
    Dim LinecmdText As String
    Public Sub New()
        MyBase.New()
        CaseMU = True
        InsOvertype = True
        EncodingType = "8"c ' utf8
        Shadow = True
        SessionInited = False
        LinEndChar = "#"c
        Dim vp As VerifyPair
        vp = New VerifyPair
        Dim i As Integer = 1024
        vp.VerFrom = 1
        vp.VerTo = i
        vp.VerHex = False
        Verif.Add(vp)
    End Sub
    Protected Overrides Sub Finalize()
        If EditFile IsNot Nothing Then
            EditFile.Dispose()
            EditFile.Close()
        End If

        Verif.Clear()
        Verif = Nothing

        Msgs.Clear()
        Msgs = Nothing

        ScrOverlayed.Clear()
        ScrOverlayed = Nothing

        SourceList.Clear()
        SourceList = Nothing

        Settings.Clear()
        Settings = Nothing

        LineCommands.Clear()
        LineCommands = Nothing

        PendingCommands.Clear()
        PendingCommands = Nothing

        IedLines.Clear()
        IedLines = Nothing

        Synonyms.Clear()
        Synonyms = Nothing

        UndoS.Clear()
        UndoS = Nothing

        AutoSavNames.Clear()
        AutoSavNames = Nothing

        ReservedLines.Clear()
        ReservedLines = Nothing

        MyBase.Finalize()
    End Sub
    Private Sub strpNum()
        Dim i, j As Integer
        For i = 1 To LinecmdText.Length()  ' strip number after commandname
            If Mid(LinecmdText, i, 1) >= "0" And Mid(LinecmdText, i, 1) <= "9" Then
                If i > 1 Then
                    RepeatFactor = XeditPc.CIntUserCor(Mid(LinecmdText, i))
                    RepeatFactorPresent = True
                    LinecmdText = Left(LinecmdText, i - 1)
                Else
                    For j = 1 To LinecmdText.Length()  ' strip number before commandname
                        If Mid(LinecmdText, j, 1) < "0" Or Mid(LinecmdText, j, 1) > "9" Then
                            RepeatFactor = XeditPc.CIntUserCor(Left(LinecmdText, j - 1))
                            RepeatFactorPresent = True
                            LinecmdText = Mid(LinecmdText, j)
                            Exit For
                        End If
                    Next
                End If
                Exit For
            End If
        Next
        LinecmdText = LinecmdText.ToUpper(CultInf)
    End Sub
    Public Sub AddLineCmd(ByVal lNr As Integer, ByVal lCmdp As String)
        Dim i, j As Integer
        Dim mLnCmd As LineCommand = Nothing
        RepeatFactorPresent = False
        LinecmdText = lCmdp
        strpNum()
        j = LineCommands.Count()
        For i = 1 To LineCommands.Count()
            mLnCmd = DirectCast(LineCommands.Item(i), LineCommand)
            If mLnCmd.Linenr = lNr Then
                j = -i
                Exit For
            End If
            If mLnCmd.Linenr > lNr Then
                j = i - 1
                Exit For
            End If
        Next
        If j >= 0 Then
            mLnCmd = New LineCommand
        End If
        mLnCmd.Linenr = lNr
        mLnCmd.LinecmdText = LinecmdText
        mLnCmd.RepeatFactorPresent = RepeatFactorPresent
        mLnCmd.RepeatFactor = RepeatFactor
        If j >= 0 Then
            If j = 0 Then
                If LineCommands.Count() = 0 Then
                    LineCommands.Add(mLnCmd)
                Else
                    LineCommands.Add(mLnCmd, , 1)
                End If
            Else
                LineCommands.Add(mLnCmd, , , j)
            End If
        End If
    End Sub
    Public Sub ModLineCmd(ByVal lNr As Integer, ByVal lCmdp As String)
        Dim i As Integer
        Dim mLnCmd As LineCommand
        RepeatFactorPresent = False
        LinecmdText = lCmdp
        strpNum()
        For i = 1 To LineCommands.Count()
            mLnCmd = DirectCast(LineCommands.Item(i), LineCommand)
            If mLnCmd.Linenr = lNr Then
                mLnCmd.LinecmdText = LinecmdText
                mLnCmd.RepeatFactorPresent = RepeatFactorPresent
                mLnCmd.RepeatFactor = RepeatFactor
                Exit For
            End If
        Next
    End Sub
    Public Function FindLineCmd(ByRef CurLinNr As Integer, ByRef j As Integer) As Boolean
        Dim i As Integer, mLnCmd As LineCommand
        j = 0
        For i = 1 To LineCommands.Count()
            mLnCmd = DirectCast(LineCommands.Item(i), LineCommand)
            If mLnCmd.Linenr = CurLinNr Then
                j = i
                mRecentLineCmdFound = i
                Exit For
            End If
        Next
        FindLineCmd = (j > 0)
    End Function
    Public Function SearchPendingLnr(ByVal lNr As Integer) As Integer
        Dim i, j As Integer
        Dim mLnCmd As LineCommand
        j = 0
        For i = 1 To PendingCommands.Count()
            mLnCmd = DirectCast(PendingCommands.Item(i), LineCommand)
            If mLnCmd.Linenr = lNr Then
                j = i
                Exit For
            End If
        Next
        SearchPendingLnr = j
    End Function
    Public Function SearchLineLnr(ByVal lNr As Integer) As Integer
        Dim i, j As Integer
        Dim mLnCmd As LineCommand
        j = 0
        For i = 1 To LineCommands.Count()
            mLnCmd = DirectCast(LineCommands.Item(i), LineCommand)
            If mLnCmd.Linenr = lNr Then
                j = i
                Exit For
            End If
        Next
        SearchLineLnr = j
    End Function
    Public Function SearchLineCm(ByVal cmd As String, Optional ByVal fromE As Integer = 1, Optional ByVal toE As Integer = 1) As Integer
        Dim i, j As Integer
        Dim mLnCmd As LineCommand
        j = 0
        For i = 1 To LineCommands.Count()
            mLnCmd = DirectCast(LineCommands.Item(i), LineCommand)
            If mLnCmd.LinecmdText = cmd Then
                If mLnCmd.Linenr >= fromE And mLnCmd.Linenr <= toE Then
                    j = i
                    Exit For
                End If
            End If
        Next
        SearchLineCm = j
    End Function
    Public Function SearchPendingCm(ByVal cmd As String, Optional ByVal fromE As Integer = 1, Optional ByVal toE As Integer = 9999999) As Integer
        Dim i, j As Integer
        Dim mLnCmd As LineCommand
        j = 0
        For i = 1 To PendingCommands.Count()
            mLnCmd = DirectCast(PendingCommands.Item(i), LineCommand)
            If mLnCmd.LinecmdText = cmd Then
                If mLnCmd.Linenr >= fromE And mLnCmd.Linenr <= toE Then
                    j = i
                    Exit For
                End If
            End If
        Next
        SearchPendingCm = j
    End Function
    Public Sub AddPendingCmd(ByVal lNr As Integer, ByVal lCmdp As String)
        Dim j As Integer
        Dim mLnCmd As LineCommand
        LinecmdText = lCmdp
        strpNum()
        j = SearchPendingLnr(lNr)
        If j > 0 Then
            mLnCmd = DirectCast(PendingCommands.Item(j), LineCommand)
        Else
            mLnCmd = New LineCommand
            mLnCmd.Linenr = lNr
            mLnCmd.LinecmdText = LinecmdText
            mLnCmd.RepeatFactorPresent = RepeatFactorPresent
            mLnCmd.RepeatFactor = RepeatFactor
            PendingCommands.Add(mLnCmd)
        End If
    End Sub
    Public Sub ChangeRememberedLinenrs(ByVal Delta As Integer, ByVal Linenr As Integer)
        Dim i As Integer
        Dim mLnCmd As LineCommand
        For i = 1 To LineCommands.Count()
            mLnCmd = DirectCast(LineCommands.Item(i), LineCommand)
            If mLnCmd.Linenr > Linenr Then
                mLnCmd.Linenr += Delta
            End If
        Next
        For i = 1 To PendingCommands.Count()
            mLnCmd = DirectCast(PendingCommands.Item(i), LineCommand)
            If mLnCmd.Linenr > Linenr Then
                mLnCmd.Linenr += Delta
            End If
        Next
        For i = 1 To IedLines.Count()
            Dim ie As IedLine = DirectCast(IedLines.Item(i), IedLine)
            If ie.Linenr > Linenr Then
                ie.Linenr += Delta
            End If
        Next
    End Sub
End Class
