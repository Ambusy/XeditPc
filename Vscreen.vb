Imports System.Threading

Public Class Vscreen
    Dim Tboxes As New Collection
    Dim textboxWithFocus As TextBox = Nothing
    Dim cursorLine, cursorCol As Integer
    Public ClickToEnter As Boolean = False
    Dim measureWidth As Integer = 8
    Dim measureHeight As Integer = 24
    Dim measureHeightB As Integer = 21
    Dim myBackColor As System.Drawing.Color = System.Drawing.Color.White

    Private Sub Vscreen_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim Height = 10
        Dim Width = 10
        Dim TypObj As Char = " "c
        Dim protectedField As New Label
        Dim textField As TextBox
        Dim buttonField As Button
        Dim numberOfFields As Integer = 0
        Dim numberOfTextboxes As Integer = 0
        Dim numberOfChars As Integer
        Dim numberOfButtons As Integer = 0

        cursorLine = 0
        protectedField.Text = "" ' anders waarschuwing
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
                    protectedField = New Label With {
                        .AutoSize = True,
                        .Location = New System.Drawing.Point((cl - 1) * measureWidth, (ln - 1) * measureHeight),
                        .Name = "Labl" & CStr(numberOfFields),
                        .Size = New System.Drawing.Size(measureWidth * numberOfChars, measureHeightB),
                        .TabIndex = numberOfFields - 1,
                        .Text = "",
                        .Tag = CStr(ln) + " " + CStr(cl),
                        .Font = Label1.Font,
                        .Visible = True,
                        .ForeColor = getColor(VSCREENarea(ln - 1, cl - 1, 2)),
                        .BackColor = Color.White
                    }
                    ' Debug.WriteLine("L " + CStr(cl - 1) & " " & CStr(protectedField.Left) + " " + CStr(protectedField.Width) + " " + CStr(numberOfChars))
                    TypObj = "L"c
                    Dim w = protectedField.Width + protectedField.Left ' totale omvang venster bepalen
                    Dim h = protectedField.Height + protectedField.Top
                    If w > Width Then
                        Width = w
                    End If
                    If h > Height Then
                        Height = h
                    End If
                    Me.Controls.Add(protectedField)
                    'If ClickToEnter Then AddHandler protectedField.MouseClick, AddressOf TLbl3270_Click
                ElseIf VSCREENarea(ln - 1, cl - 1, 1) = "T"c AndAlso TypObj = "L"c Then
                    protectedField.Text += VSCREENarea(ln - 1, cl - 1, 0) ' kopieer litteral text
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
                    textField = New TextBox With {
                        .Location = New System.Drawing.Point((cl - 1) * measureWidth, (ln - 1) * measureHeight),
                        .Name = "ScrOb" & CStr(numberOfFields),
                        .Size = New System.Drawing.Size(measureWidth * numberOfChars, measureHeightB),
                        .TabIndex = numberOfFields - 1,
                        .Tag = CStr(ln) + " " + CStr(cl) + " " + CStr(numberOfTextboxes),
                        .Text = "",
                        .Font = TextBox1.Font,
                        .BorderStyle = BorderStyle.FixedSingle,
                        .ForeColor = getColor(VSCREENarea(ln - 1, cl - 1, 2)),
                        .BackColor = myBackColor
                    }
                    'Debug.WriteLine("T " + CStr(cl - 1) & " " & CStr(textField.Left) + " " + CStr(textField.Width) + " " + CStr(numberOfChars))
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
                    AddHandler textField.KeyPress, AddressOf TBox_KeyPress
                    AddHandler textField.TextChanged, AddressOf TBox_textChanged
                    AddHandler textField.KeyDown, AddressOf TBox_Keydown
                    AddHandler textField.GotFocus, AddressOf tbox_GotFocus
                    AddHandler textField.LostFocus, AddressOf tbox_LostFocus
                    Tboxes.Add(textField)
                    If VSCREENfieldText.ContainsKey(textField.Name) Then
                        textField.Text = VSCREENfieldText(textField.Name)
                    Else                        'on first display of screen
                        Dim str As String = "" ' Else can ce empty, initial add is moved to VscreenPprocs in Xedit
                        For xi = 1 To numberOfChars
                            str += VSCREENarea(ln - 1, cl - 1 + xi, 0)
                        Next
                        VSCREENfieldText.Add(textField.Name, str)
                        VSCREENfieldTextInitial.Add(textField.Name, str)
                        textField.Text = str
                    End If
                ElseIf VSCREENarea(ln - 1, cl - 1, 1) = "B"c Then
                    numberOfChars = 0
                    Dim butText As String = ""
                    For icl As Integer = cl To VSCREENcols - 1
                        If VSCREENarea(ln - 1, icl, 1) = "T"c Then
                            numberOfChars += 1
                            butText += VSCREENarea(ln - 1, icl, 0)
                        Else
                            Exit For
                        End If
                    Next
                    numberOfFields += 1
                    numberOfButtons += 1
                    buttonField = New Button With {
                        .Location = New System.Drawing.Point((cl - 1) * measureWidth, (ln - 1) * measureHeight),
                        .Name = "ScrOb" & CStr(numberOfFields),
                        .Size = New System.Drawing.Size(measureWidth * numberOfChars, measureHeightB),
                        .TabIndex = numberOfFields - 1,
                        .Tag = "PFKEYx",
                        .Text = butText,
                        .Font = TextBox1.Font,
                        .ForeColor = getColor(VSCREENarea(ln - 1, cl - 1, 2)),
                        .BackColor = myBackColor,
                        .Visible = True
                    }
                    TypObj = "B"c
                    Dim w = buttonField.Width + buttonField.Left
                    Dim h = buttonField.Height + buttonField.Top
                    If w > Width Then
                        Width = w
                    End If
                    If h > Height Then
                        Height = h
                    End If
                    Me.Controls.Add(buttonField)
                    AddHandler buttonField.Click, AddressOf TBox_buttonclick
                    'Tboxes.Add(buttonField)
                End If
            Next
        Next
        For Each box As TextBox In Tboxes
            box.Text = box.Text.TrimEnd
        Next
        Me.Height = Height + 25 + measureHeight ' for message line
        Me.Width = Width + 25
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
        If typ = "7"c Then fc = Color.Black
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
                    KeyPfTxt = "F" + CStr(eventArgs.KeyCode - 111)
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
            Dim t As TextBox = Tboxes(i)
            Dim elems As String() = t.Tag.split(" ")
            Dim ix As Integer = t.Tag.indexof(" ")
            Dim ln, cl As Integer
            ln = Convert.ToInt32(elems(0))
            cl = Convert.ToInt32(elems(1))
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
    Private Sub TBox_textChanged(myBox As TextBox, e As EventArgs)
        If VSCREENfieldText.ContainsKey(myBox.Name) Then
            If VSCREENfieldText(myBox.Name).TrimEnd <> myBox.Text.TrimEnd Then
                VSCREENfieldText(myBox.Name) = myBox.Text
                If myBox.Width / measureWidth = myBox.Text.TrimEnd.Length - 1 Then '
                    'Beep()
                    Dim freq As Integer = 400
                    Dim duration As Integer = 50
                    System.Console.Beep(freq, duration)
                End If
            End If
            End If
    End Sub
    Private isOverwriteMode As Boolean = False
    Private Sub TBox_Keydown(myBox As TextBox, e As KeyEventArgs)
        If myBox.BackColor <> Color.White Then
            myBox.BackColor = Color.White
        End If
        If e.KeyCode = Keys.Down Then ' escape
            Dim elems As String() = myBox.Tag.split(" ")
            Dim ln As Integer = Convert.ToInt32(elems(0))
            For Each box As TextBox In Tboxes
                elems = box.Tag.split(" ")
                Dim ln1 As Integer = Convert.ToInt32(elems(0))
                If ln1 = ln + 1 Then
                    box.Focus()
                    e.Handled = True
                    Exit Sub
                End If
            Next
        End If
        If e.KeyCode = Keys.Up Then ' escape
            Dim elems As String() = myBox.Tag.split(" ")
            Dim ln As Integer
            ln = Convert.ToInt32(elems(0))
            For Each box As TextBox In Tboxes
                elems = box.Tag.split(" ")
                Dim ln1 As Integer = Convert.ToInt32(elems(0))
                If ln1 = ln - 1 Then
                    box.Focus()
                    e.Handled = True
                    Exit Sub
                End If
            Next
        End If
        If e.KeyCode = Keys.Insert Then
            isOverwriteMode = Not isOverwriteMode
            e.Handled = True
        End If
    End Sub
    Private Sub TBox_KeyPress(myBox As TextBox, e As KeyPressEventArgs)
        If e.KeyChar = ChrW(Keys.Escape) Then ' escape
            myBox.Text = CStr(VSCREENfieldTextInitial(myBox.Name))
            e.Handled = True
            Exit Sub
        End If
        If isOverwriteMode Then
            ' If in overwrite mode 
            If myBox.SelectionStart < myBox.TextLength Then
                ' If cursor is not at the end of the text ,  Replace the character at the current cursor position, keep SelStart, is set to 0 when text changes
                Dim selstart As Integer = myBox.SelectionStart
                Dim originalText As String = myBox.Text
                Dim newChar As Char = e.KeyChar
                Dim newText As String = originalText.Remove(myBox.SelectionStart, 1) ' Remove the character
                newText = newText.Insert(myBox.SelectionStart, newChar.ToString()) ' Insert the new character
                myBox.Text = newText
                myBox.SelectionStart = selstart + 1 ' Move cursor forward
                e.Handled = True ' Mark event as handled to prevent default character insertion
            End If
        End If
    End Sub
    Private Sub TBox_buttonclick(myBox As Button, e As EventArgs)
        If myBox.Text <> "" Then
            CloseScreen(myBox.Text)
        End If
    End Sub
    Private Sub tbox_GotFocus(sender As Object, e As EventArgs)
        sender.BackColor = Color.Linen
    End Sub

    Private Sub tbox_LostFocus(sender As Object, e As EventArgs)
        sender.BackColor = Color.White
    End Sub
#If DEBUG Or tracen Then
    Sub DumpArea(Optional ix As Integer = 0)
        For ln As Integer = 1 To VSCREENlines
            Dim lne As String = ""
            For cl As Integer = 1 To VSCREENcols
                If VSCREENarea(ln - 1, cl - 1, ix) = vbNullChar Then
                    lne += " "
                Else
                    lne += VSCREENarea(ln - 1, cl - 1, ix)
                End If
            Next
            Debug.WriteLine(lne)
        Next
    End Sub
#End If
End Class