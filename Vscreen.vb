Public Class Vscreen
    Dim Tboxes As New Collection
    Dim textboxWithFocus As TBox3270 = Nothing
    Dim cursorLine, cursorCol As Integer
    Public ClickToEnter As Boolean = False

    Private Sub Vscreen_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim Height = 10
        Dim Width = 10
        Dim measureWidth As Integer = 9
        Dim measureHeight As Integer = 21
        Dim TypObj As Char = " "c
        Dim protectedField As Label
        Dim textField As TBox3270
        Dim numberOfFields As Integer = 0
        Dim numberOfTextboxes As Integer = 0
        Dim numberOfChars As Integer
        cursorLine = 0
        For ln As Integer = 1 To VSCREENlines
            Dim fieldnr As Integer = 0
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
                    protectedField = New TLbl3270 With {
                        .AutoSize = True,
                        .Location = New System.Drawing.Point(cl * measureWidth, (ln - 1) * measureHeight),
                        .Name = "Labl" & CStr(numberOfFields),
                        .Size = New System.Drawing.Size(measureWidth * numberOfChars, measureHeight - 3),
                        .TabIndex = numberOfFields - 1,
                        .Text = "",
                        .Tag = CStr(ln) + " " + CStr(cl),
                        .Font = Label1.Font,
                        .Visible = True,
                        .ForeColor = getColor(VSCREENarea(ln - 1, cl - 1, 2)),
                        .BackColor = System.Drawing.Color.Black
                    }
                    TypObj = "L"c
                    Dim w = protectedField.Width + protectedField.Left
                    Dim h = protectedField.Height + protectedField.Top
                    If w > Width Then
                        Width = w
                    End If
                    If h > Height Then
                        Height = h
                    End If
                    Me.Controls.Add(protectedField)
                    If ClickToEnter Then AddHandler protectedField.MouseClick, AddressOf TLbl3270_Click
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
                    numberOfTextboxes += 1
                    If fieldnr = 0 Then fieldnr = numberOfTextboxes ' remember seqnr of each first field on a line
                    textField = New TBox3270 With {
                        .Location = New System.Drawing.Point(cl * measureWidth, (ln - 1) * measureHeight),
                        .Name = "Txtb" & CStr(numberOfFields),
                        .Size = New System.Drawing.Size(measureWidth * numberOfChars, measureHeight - 3),
                        .TabIndex = numberOfFields - 1,
                        .Linenumber = ln,
                        .Fieldnumber = numberOfTextboxes,
                        .Text = "",
                        .Font = TextBox1.Font,
                        .BorderStyle = BorderStyle.FixedSingle,
                        .ForeColor = getColor(VSCREENarea(ln - 1, cl - 1, 2)),
                        .BackColor = Color.Black,
                        .Tag = CStr(ln) + " " + CStr(cl),
                        .MaxTextLength = numberOfChars,
                        .PadChar = "_"c
                    }
                    If cursorLine = 0 Then
                        cursorCol = cl + 1
                        cursorLine = ln + 1
                    End If
                    textField.Visible = True
                    If ln - 1 = VSCREENcursorline And (VSCREENcursorcol >= cl And VSCREENcursorcol <= cl + numberOfChars) Then
                        textboxWithFocus = textField
                    End If
                    TypObj = "T"c
                    Dim w = textField.Width + textField.Left
                    Dim h = textField.Height + textField.Top
                    If w > Width Then
                        Width = w
                    End If
                    If h > Height Then
                        Height = h
                    End If
                    Me.Controls.Add(textField)
                    AddHandler textField.Enter, AddressOf TBox3270_Enter
                    AddHandler textField.MouseClick, AddressOf TBox3270_Enter
                    AddHandler textField.KeyDown, AddressOf TBox3270_KeyDown
                    AddHandler textField.KeyPress, AddressOf TBox3270_KeyPress
                    Tboxes.Add(textField)
                ElseIf VSCREENarea(ln - 1, cl - 1, 1) = "T" AndAlso TypObj = "T"c Then
                    textField.Text += VSCREENarea(ln - 1, cl - 1, 0)
                    textField.InitialText = textField.Text
                End If
            Next
        Next
        Me.Height = Height + 25 + measureHeight ' for message line
        Me.Width = Width
        If VSCREENFirstScreen Then ' first time executed in this Xedit session?
            VSCREENFirstScreen = False
            Me.Top = Screen.PrimaryScreen.Bounds.Height - Me.Height
            Me.Left = Screen.PrimaryScreen.Bounds.Width - Me.Width
            VSCREENLocOffsetX = Me.Top
            VSCREENLocOffsetY = Me.Left
        Else
            Me.Top = VSCREENLocOffsetX
            Me.Left = VSCREENLocOffsetY
        End If
        Me.TopMost = True
        Timer1.Enabled = True
    End Sub
    Function getColor(typ As Char) As Color
        Dim fc As Color = Color.White
        If typ = "1"c Then fc = Color.Blue
        If typ = "2"c Then fc = Color.Red
        If typ = "3"c Then fc = Color.Pink
        If typ = "4"c Then fc = Color.Green
        If typ = "5"c Then fc = Color.Turquoise
        If typ = "6"c Then fc = Color.Yellow
        If typ = "7"c Then fc = Color.White
        Return fc
    End Function
    Private Sub Vscreen_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If Not WantClose Then ' user cancelled form
            storeVarT("WAITREAD.0", "0")
        End If
        VSCREENLocOffsetX = Me.Top
        VSCREENLocOffsetY = Me.Left
    End Sub
    Dim WantClose As Boolean = False
    Dim KDeventArgs As KeyEventArgs
    Private Sub Vscreen_KeyDown(sender As Object, eventArgs As KeyEventArgs) Handles MyBase.KeyDown
        Dim KeyPfTxt As String = ""
        KDeventArgs = eventArgs
        Dim Shift As Integer = eventArgs.KeyData \ &H10000
        If Shift <= 1 Then ' not for ALT and CTRL
            If eventArgs.KeyCode = 13 Then KeyPfTxt = "ENTER"
            Select Case eventArgs.KeyCode
                Case System.Windows.Forms.Keys.F1 To System.Windows.Forms.Keys.F12
                    KeyPfTxt = "PFKEY " + CStr(eventArgs.KeyCode - 111)
            End Select
        End If
        If KeyPfTxt <> "" Then
            CloseScreen(KeyPfTxt)
        End If
    End Sub
    Private Sub CloseScreen(KeyPfTxt As String)
        storeVarT("WAITREAD.0", CStr(Tboxes.Count + 2))
        storeVarT("WAITREAD.1", KeyPfTxt)
        storeVarT("WAITREAD.2", "CURSOR " + CStr(cursorLine) + " " + CStr(cursorCol))
        For i As Integer = 1 To Tboxes.Count
            Dim t As TBox3270 = Tboxes(i)
            Dim ix As Integer = t.Tag.indexof(" ")
            Dim ln, cl As Integer
            ln = Convert.ToInt32(t.Tag.substring(0, ix))
            cl = Convert.ToInt32(t.Tag.substring(ix + 1))
            storeVarT("WAITREAD." & CStr(i + 2), "DATA " + CStr(ln) + " " + CStr(cl) + " " + t.Text)
        Next
        WantClose = True
        MyBase.Close()
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
    Private Sub TLbl3270_Click(sender As TLbl3270, e As EventArgs)
        Dim ix As Integer = sender.Tag.indexof(" ")
        Dim ln, cl As Integer
        ln = Convert.ToInt32(sender.Tag.substring(0, ix))
        cl = Convert.ToInt32(sender.Tag.substring(ix + 1))
        If cl = 1 Then CloseScreen("ENTER")
    End Sub
    Private Sub TBox3270_Enter(sender As TBox3270, e As EventArgs)
        Dim ix As Integer = sender.Tag.indexof(" ")
        Dim ln, cl As Integer
        ln = Convert.ToInt32(sender.Tag.substring(0, ix))
        cl = Convert.ToInt32(sender.Tag.substring(ix + 1))
        cursorCol = cl + 1
        cursorLine = ln + 1
        If sender.Text.Trim = "" Then
            sender.SelectionStart = 0
            sender.SelectionLength = 0
        End If
    End Sub
    Dim copiedText As String = ""
    Private Sub TBox3270_KeyPress(myBox As TBox3270, e As KeyPressEventArgs)
        Dim st As Integer = myBox.SelectionStart
        Dim sl As Integer = myBox.SelectionLength
        If e.KeyChar = vbBack Then
            e.Handled = True
            Exit Sub
        End If
        If e.KeyChar = ChrW(24) Then ' ctrl-x
            copiedText = myBox.Text.Substring(st, sl)
            myBox.Text = myBox.Text.Substring(0, st) + myBox.Text.Substring(st + sl)
            e.Handled = True
            Exit Sub
        End If
        If e.KeyChar = ChrW(3) Then ' ctrl-c
            copiedText = myBox.Text.Substring(st, sl)
            e.Handled = True
            Exit Sub
        End If
        If e.KeyChar = ChrW(22) Then ' crtrl-v
            myBox.Text = myBox.Text.Trim + copiedText
            e.Handled = True
            Exit Sub
        End If
        If e.KeyChar = ChrW(27) Then ' escape
            myBox.Text = myBox.InitialText
            e.Handled = True
            Exit Sub
        End If

        If myBox.OverlayOrInsertMode Then
            If st < myBox.MaxTextLength Then
                If sl > 0 Then
                    myBox.Text = myBox.Text.Substring(0, st) + " " + myBox.Text.Substring(st + sl)
                End If
                myBox.Text = (myBox.Text.Substring(0, st) + e.KeyChar + myBox.Text.Substring(st + 1)).PadRight(myBox.MaxTextLength, " "c)
                st += 1
            Else
                Beep()
            End If
            e.Handled = True
        Else
            If sl > 0 Then
                myBox.Text = myBox.Text.Substring(0, st) + myBox.Text.Substring(st + sl)
            End If
            If myBox.Text.Length = myBox.MaxTextLength Then
                If myBox.Text(myBox.MaxTextLength - 1) = " "c Or myBox.Text(myBox.MaxTextLength - 1) = myBox.PadChar Then
                    myBox.Text = myBox.Text.Substring(0, myBox.MaxTextLength - 1)
                End If
            End If
            If myBox.Text.Length < myBox.MaxTextLength Then
                myBox.Text = (myBox.Text.Substring(0, st) + e.KeyChar + myBox.Text.Substring(st)).PadRight(myBox.MaxTextLength, " "c)
                st += 1
            Else
                Beep()
            End If
            e.Handled = True
        End If
        myBox.SelectionStart = st
        myBox.SelectionLength = 0
    End Sub

    Private Sub Vscreen_MouseClick(sender As Object, e As MouseEventArgs) Handles MyBase.MouseClick
        Dim i = 1
    End Sub

    Private Sub TBox3270_KeyDown(myBox As TBox3270, e As KeyEventArgs)
        Dim st As Integer = myBox.SelectionStart
        Dim sl As Integer = myBox.SelectionLength
        If e.KeyCode = Keys.Insert Then
            myBox.OverlayOrInsertMode = Not myBox.OverlayOrInsertMode
            e.Handled = True
        End If
        If e.KeyCode = Keys.Back Then ' backspace
            If sl > 0 Then ' delete selected
                myBox.Text = (myBox.Text.Substring(0, st) + myBox.Text.Substring(st + sl)).PadRight(myBox.MaxTextLength, " "c)
            Else
                If st > 0 Then ' there is a char before the cursor
                    myBox.Text = (myBox.Text.Substring(0, st - 1) + myBox.Text.Substring(st)).PadRight(myBox.MaxTextLength, " "c)
                    st = Math.Max(0, st - 1)
                End If
            End If
            e.Handled = True
        End If
        If e.KeyCode = Keys.Delete Then ' delete
            If sl > 0 Then ' delete selected
                myBox.Text = (myBox.Text.Substring(0, st) + myBox.Text.Substring(st + sl)).PadRight(myBox.MaxTextLength, " "c)
                st = Math.Max(0, st - sl)
            Else
                If st < myBox.MaxTextLength Then
                    myBox.Text = (myBox.Text.Substring(0, st) + myBox.Text.Substring(st + 1)).PadRight(myBox.MaxTextLength, " "c)
                End If
            End If
            e.Handled = True
        End If
        If e.KeyCode = Keys.Down Then ' arrow down
            For i As Integer = 1 To Tboxes.Count
                Dim t As TBox3270 = Tboxes(i)
                If t.Linenumber > myBox.Linenumber Then
                    t.Focus()
                    t.SelectAll()
                    Exit For
                End If
            Next
            e.Handled = True
        End If
        If e.KeyCode = Keys.Up Then ' arrow up
            For i As Integer = Tboxes.Count To 1 Step -1
                Dim t As TBox3270 = Tboxes(i)
                If t.Linenumber < myBox.Linenumber Then
                    For j As Integer = i - 1 To 1 Step -1
                        Dim tv As TBox3270 = Tboxes(j)
                        If tv.Linenumber = t.Linenumber Then
                            t = Tboxes(j)
                            t.SelectAll()
                        Else
                            Exit For
                        End If
                    Next
                    t.Focus()
                    Exit For
                End If
            Next
            e.Handled = True
        End If

        If e.Handled Then
            myBox.SelectionStart = st
            myBox.SelectionLength = 0
        End If
    End Sub
End Class