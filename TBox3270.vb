' Textbox as used in the 3270-type terminal emulating
Public Class TBox3270
    Inherits TextBox
    Public MaxTextLength As Integer = 0
    Public PadChar As Char = " "c
    Public OverlayOrInsertMode As Boolean = True
    Sub New()
    End Sub
End Class
