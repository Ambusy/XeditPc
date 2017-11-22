Public Class SourceLine
    Public SrcFileIx As Char
    Public SrcStart As Long
    Public SrcLength As Integer
    Public SrcSelect As Integer
    Public SrcPoint As String
    Sub New()
        MyBase.New()
        SrcPoint = ""
    End Sub
End Class
