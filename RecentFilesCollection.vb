Public Class RecentFilesCollection
    Private recentFilesList As New Collection
    Public Sub New()
        For i As Integer = 1 To 100
            Dim fn As String = GetRegistryKey("XeditPc", "File" & CStr(i), "")
            If fn <> "" Then
                Dim ix As Integer = fn.IndexOf("|")
                Dim opt As String = ""
                If ix > -1 Then
                    opt = fn.Substring(ix + 1)
                    fn = fn.Substring(0, ix).Trim()
                End If
                recentFilesList.Add(New recentFileData(fn, opt))
            Else
                Exit For
            End If
        Next
    End Sub
    Public Function InRecents(fn As String) As Boolean
        For Each r As recentFileData In recentFilesList
            If r.fileName = fn Then Return True
        Next
        Return False
    End Function
    Public Function GetOptions(fn As String) As String
        For Each r As recentFileData In recentFilesList
            If r.fileName = fn Then Return r.Options
        Next
        Return ""
    End Function
    Public Function SetOptions(fn As String, options As String, force As Boolean) As String
        Dim i = 0
        For Each r As recentFileData In recentFilesList
            i += 1
            If r.fileName = fn Then
                If force Then ' remove options, in new options equals blanks
                    r.Options = options
                Else
                    If options <> "" Then
                        r.Options = options ' only if non blank
                    End If
                End If
                Dim ro As String = r.Options
                recentFilesList.Remove(i)
                recentFilesList.Add(r,, 1)
                Return ro
            End If
        Next
        If recentFilesList.Count = 100 Then
            recentFilesList.Remove(100)
        End If
        recentFilesList.Add(New recentFileData(fn, options),, 1)
        Return options
    End Function
    Public Sub GetList(lb As ListBox)
        For Each r As recentFileData In recentFilesList
            Dim f As String = r.fileName
            Dim i As Integer = f.LastIndexOf("\")
            If i > -1 Then f = f.Substring(i + 1)
            If r.Options = "" Then
                lb.Items.Add(f & RFsep & r.fileName)
            Else
                lb.Items.Add(f & RFsep & r.fileName & "  | " & r.Options)
            End If
        Next
    End Sub
    Public Sub Dispose()
        Dim i = 0
        For Each r As recentFileData In recentFilesList
            If r.fileName <> "" Then
                i += 1
                Dim fnx = r.fileName
                If r.Options <> "" Then fnx &= " |" & r.Options
                SaveRegistryKey("XeditPc", "File" & CStr(i), fnx)
            End If
        Next
    End Sub
    Sub EmptyList()
        For i As Integer = 1 To 100
            DelRegistryKeyValue("XeditPc", "File" & CStr(i))
        Next
        recentFilesList.Clear()
    End Sub
End Class
Public Class recentFileData
    Public fileName As String
    Public Options As String
    Sub New(f As String, o As String)
        fileName = f
        Options = o
    End Sub
End Class
