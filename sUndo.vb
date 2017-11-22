Public Class sUndo
    Public UndoT As Integer ' 1 insert 2 delete 3 change
    Public UndoGrp As Integer ' groups modifications line CHANGE *, PASTE, CUT
    Public UndoLineNr As Integer
    Public UndoCursorPos As Short ' pos of cursor
    Public UndoCursorLine As Integer
    Public UndoCurLine As Integer ' currentline
    Public UndoSrc As String
End Class
