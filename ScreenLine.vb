Public Class ScreenLine
    Public CurLinType As Char      ' LineSource, Xcluded, Null, Top, Bot, Ied, Cmd, Msg, Fileidentification
    Public CurLinFixTp As Boolean  ' Line varies with source? (or is constant, ie F, C, M))
    Public CurLinNr As String      ' linenr in source, or ======
    Public CurSrcNr As Integer     ' source line nr
    Public CurNrLines As Integer   ' n° excluded
    Public CurLinSsd As SourceLine ' ptr to Sourcefile
    Public CurLinSrc As String     ' Text (modified?) in screenline
    Public CurLinModified As Boolean ' source has been modified and not written to Temp-file?
    Public CurSrcRead As Boolean   ' CurLinSrc in memory?
    Public CurRepaint As Boolean   ' update line on next paint?
    Public CharsOnScr As Short     ' nr of  chars painted on screenline
    Public TabPosIns As Boolean    ' array has values in it
    Public TabPosScr(2048) As Short ' pos of char on line in respect with tabs, no more than pixels horizontally!
    Public Sub CopyFrom(ByVal orig As ScreenLine)
        Me.CurLinType = orig.CurLinType
        Me.CurLinFixTp = orig.CurLinFixTp
        Me.CurLinNr = orig.CurLinNr
        Me.CurSrcNr = orig.CurSrcNr
        Me.CurNrLines = orig.CurNrLines
        Me.CurLinSsd = orig.CurLinSsd
        Me.CurLinSrc = orig.CurLinSrc
        Me.CurLinModified = orig.CurLinModified
        Me.CurSrcRead = orig.CurSrcRead
        Me.CurRepaint = orig.CurRepaint
        Me.CharsOnScr = orig.CharsOnScr
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
