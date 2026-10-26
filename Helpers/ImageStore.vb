Imports System.IO
Imports System.Windows.Media.Imaging

Public Module ImageStore

    Private ReadOnly Root As String = AppDomain.CurrentDomain.BaseDirectory
    Private Const FolderName As String = "ProductImages"

    ' "ProductImages\abc.jpg" -> full path. Full paths are returned unchanged.
    Public Function FullPath(filePath As String) As String
        If String.IsNullOrWhiteSpace(filePath) Then Return ""
        Return If(Path.IsPathRooted(filePath), filePath, Path.Combine(Root, filePath))
    End Function

    ' Copies a chosen picture into the app's folder and returns its relative path
    Public Function SaveCopy(sourceFile As String) As String
        Dim folder = Path.Combine(Root, FolderName)
        Directory.CreateDirectory(folder)
        Dim fileName = Guid.NewGuid().ToString("N") & Path.GetExtension(sourceFile).ToLowerInvariant()
        File.Copy(sourceFile, Path.Combine(folder, fileName))
        Return Path.Combine(FolderName, fileName)
    End Function

    ' decodeWidth = 0 loads the full size; otherwise a smaller copy (saves memory)
    Public Function Load(filePath As String, Optional decodeWidth As Integer = 0) As BitmapImage
        Dim full = FullPath(filePath)
        If full = "" OrElse Not File.Exists(full) Then Return Nothing
        Try
            Dim bmp As New BitmapImage()
            bmp.BeginInit()
            bmp.CacheOption = BitmapCacheOption.OnLoad     ' don't lock the file
            If decodeWidth > 0 Then bmp.DecodePixelWidth = decodeWidth
            bmp.UriSource = New Uri(full)
            bmp.EndInit()
            bmp.Freeze()
            Return bmp
        Catch
            Return Nothing
        End Try
    End Function

End Module