Public Class Vscreen
    Dim Tboxes As New Collection
    Dim textboxWithFocus As TextBox = Nothing
    Dim cursorLine, cursorCol As Integer
    Private Sub Vscreen_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim measureWidth As Integer = 8
        Dim measureHeight As Integer = 19
        Dim TypObj As Char = " "c
        Dim protectedField As Label
        Dim textField As TextBox
        Dim numberOfFields As Integer = 0
        Dim numberOfChars As Integer
        cursorLine = 0
        For ln As Integer = 1 To VSCREENlines
            For cl As Integer = 1 To VSCREENcols
                If VSCREENarea(ln - 1, cl - 1, 1) = vbNullChar Then
                    VSCREENarea(ln - 1, cl - 1, 1) = " "c
                End If
                If VSCREENarea(ln - 1, cl - 1, 1) = "P"c Then
                    numberOfChars = 0
                    For icl As Integer = cl To VSCREENcols - 1
                        If VSCREENarea(ln - 1, icl, 1) = "T"c Then
                            numberOfChars += 1
                        Else
                            Exit For
                        End If
                    Next
                    numberOfFields += 1
                    protectedField = New Label()
                    protectedField.AutoSize = True
                    protectedField.Location = New System.Drawing.Point(cl * measureWidth, ln * measureHeight)
                    protectedField.Name = "Labl" & CStr(numberOfFields)
                    protectedField.Size = New System.Drawing.Size(measureWidth * numberOfChars, measureHeight)
                    protectedField.TabIndex = numberOfFields - 1
                    protectedField.Text = ""
                    protectedField.Visible = True
                    protectedField.ForeColor = getColor(VSCREENarea(ln - 1, cl - 1, 2))
                    TypObj = "L"c
                    Me.Controls.Add(protectedField)
                ElseIf VSCREENarea(ln - 1, cl - 1, 1) = "T"c AndAlso TypObj = "L"c Then
                    protectedField.Text += VSCREENarea(ln - 1, cl - 1, 0)
                ElseIf VSCREENarea(ln - 1, cl - 1, 1) = "U"c Then
                    numberOfChars = 0
                    For icl As Integer = cl To VSCREENcols - 1
                        If VSCREENarea(ln - 1, icl, 1) = "T"c Then
                            numberOfChars += 1
                        Else
                            Exit For
                        End If
                    Next
                    numberOfFields += 1
                    textField = New TextBox()
                    textField.Location = New System.Drawing.Point(cl * measureWidth, ln * measureHeight)
                    textField.Name = "Txtb" & CStr(numberOfFields)
                    textField.Size = New System.Drawing.Size(measureWidth * numberOfChars, measureHeight)
                    textField.TabIndex = numberOfFields - 1
                    textField.Text = ""
                    textField.ForeColor = getColor(VSCREENarea(ln - 1, cl - 1, 2))
                    textField.Tag = CStr(ln) + " " + CStr(cl)
                    If cursorLine = 0 Then
                        cursorCol = cl + 1
                        cursorLine = ln + 1
                    End If
                    textField.Visible = True
                    If ln - 1 = VSCREENcursorline And (VSCREENcursorcol >= cl & VSCREENcursorcol <= cl + numberOfChars) Then
                        textboxWithFocus = textField
                    End If
                    TypObj = "T"c
                    Me.Controls.Add(textField)
                    AddHandler textField.Enter, AddressOf TextBox1_Enter
                    AddHandler textField.MouseEnter, AddressOf TextBox1_Enter
                    Tboxes.Add(textField)
                ElseIf VSCREENarea(ln - 1, cl - 1, 1) = "T" AndAlso TypObj = "T"c Then
                    textField.Text += VSCREENarea(ln - 1, cl - 1, 0)
                End If
            Next
        Next
        Timer1.Enabled = True
    End Sub
    Function getColor(typ As Char) As Color
        Dim fc As Color = Color.Black
        If typ = "1"c Then fc = Color.Blue
        If typ = "2"c Then fc = Color.Red
        If typ = "3"c Then fc = Color.Pink
        If typ = "4"c Then fc = Color.Green
        If typ = "5"c Then fc = Color.Turquoise
        If typ = "6"c Then fc = Color.Yellow
        If typ = "7"c Then fc = Color.Black
        Return fc
    End Function
    Private Sub Vscreen_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If Not WantClose Then ' user cancelled form
            storeVarT("WAITREAD.0", "0")
        End If
    End Sub
    Dim WantClose As Boolean = False
    Private Sub Vscreen_KeyDown(sender As Object, eventArgs As KeyEventArgs) Handles MyBase.KeyDown
        Dim KeyPfTxt As String = ""
        Dim Shift As Integer = eventArgs.KeyData \ &H10000
        If Shift <= 1 Then ' not for ALT and CTRL
            If eventArgs.KeyCode = 13 Then KeyPfTxt = "ENTER"
            Select Case eventArgs.KeyCode
                Case System.Windows.Forms.Keys.F1 To System.Windows.Forms.Keys.F12
                    KeyPfTxt = "PFKEY " + CStr(eventArgs.KeyCode - 111)
            End Select
        End If
        If KeyPfTxt <> "" Then
            storeVarT("WAITREAD.0", CStr(Tboxes.Count + 2))
            storeVarT("WAITREAD.1", KeyPfTxt)
            storeVarT("WAITREAD.2", "CURSOR " + CStr(cursorLine) + " " + CStr(cursorCol))
            For i As Integer = 1 To Tboxes.Count
                Dim t As TextBox = Tboxes(i)
                Dim ix As Integer = t.Tag.indexof(" ")
                Dim ln, cl As Integer
                ln = Convert.ToInt32(t.Tag.substring(0, ix))
                cl = Convert.ToInt32(t.Tag.substring(ix + 1))
                storeVarT("WAITREAD." & CStr(i + 2), "DATA " + CStr(ln) + " " + CStr(cl) + " " + t.Text)
            Next
            WantClose = True
            MyBase.Close()
        End If
    End Sub
    Private Sub storeVarT(ByVal ky As String, ByVal S1 As String)
        Dim cvr As New DefVariable
        Dim execName As String = "", n As String = ""
        Dim k As Integer
        VSCREENRexx.StoreVar(VSCREENRexx.SourceNameIndexPosition(ky, Rexx.tpSymbol.tpVariable, cvr), S1, k, execName, n)
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If Not IsNothing(textboxWithFocus) Then
            textboxWithFocus.Focus()
        End If
        Timer1.Enabled = False
    End Sub

    Private Sub TextBox1_Enter(sender As TextBox, e As EventArgs)
        Dim ix As Integer = sender.Tag.indexof(" ")
        Dim ln, cl As Integer
        ln = Convert.ToInt32(sender.Tag.substring(0, ix))
        cl = Convert.ToInt32(sender.Tag.substring(ix + 1))
        cursorCol = cl + 1
        cursorLine = ln + 1
    End Sub
End Class