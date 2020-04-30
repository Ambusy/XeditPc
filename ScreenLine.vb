Public Class ScreenLine
    Public CurLinType As Char      ' LineSource, Xcluded, Null, Top, Bot, Ied, Cmd, Msg, Fileidentification
    Public CurLinFixTp As Boolean  ' Line varies with source? (or is constant, ie F, C, M))
    Public CurLinNr As String      ' linenr in source, or ======
    Public CurSrcNr As Integer     ' source line nr
    Public CurNrLines As Integer   ' n° excluded
    Public CurLinSsd As SourceLine ' ptr to Sourcefile
    Public CurLinSrc As String     ' Text (modified?) in screenline
    Public CurLinSrcExp As String     ' Text  in screenline with tabs expanded
    Public CurLinModified As Boolean ' source has been modified and not written to Temp-file?
    Public CurSrcRead As Boolean   ' CurLinSrc in memory?
    Public CurRepaint As Boolean   ' update line on next paint?
    Public CharsOnScr As Short     ' nr of  chars painted on screenline
    Public VerifPartFrom(255) As Short ' how the line is devided in verify parts, max 255 tabs
    Public VerifPartLen(255) As Short
    Public VerifPartHex(255) As Boolean
    Public TabsinOrig As Boolean    ' line has expanded tabs?
    Public Sub CopyFrom(ByVal orig As ScreenLine)
        Me.CurLinType = orig.CurLinType
        Me.CurLinFixTp = orig.CurLinFixTp
        Me.CurLinNr = orig.CurLinNr
        Me.CurSrcNr = orig.CurSrcNr
        Me.CurNrLines = orig.CurNrLines
        Me.CurLinSsd = orig.CurLinSsd
        Me.CurLinSrc = orig.CurLinSrc
        Me.CurLinSrcExp = orig.CurLinSrcExp
        Me.CurLinModified = orig.CurLinModified
        Me.CurSrcRead = orig.CurSrcRead
        Me.CurRepaint = orig.CurRepaint
        Me.CharsOnScr = orig.CharsOnScr
        Array.Copy(orig.VerifPartFrom, Me.VerifPartFrom, Me.VerifPartFrom.Length)
        Array.Copy(orig.VerifPartLen, Me.VerifPartLen, Me.VerifPartFrom.Length)
        Array.Copy(orig.VerifPartHex, Me.VerifPartHex, Me.VerifPartFrom.Length)
        Me.TabsinOrig = orig.TabsinOrig
    End Sub
    Public Overrides Function ToString() As String
        Dim s As String = ""
        s = CurLinType & " " _
       & CStr(CurLinFixTp) & " " _
       & CurLinNr & " " _
       & CurSrcNr.ToString & " " _
       & CurNrLines.ToString & " " _
       & CStr(CurLinModified) & " " _
       & CStr(CurSrcRead) & " " _
       & CStr(CurRepaint) & " " _
       & CharsOnScr.ToString & " " _
       & CurLinSrc & " "
        Return s
    End Function
End Class
