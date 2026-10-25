Imports System.ComponentModel
Imports System.IO
Imports System.Windows.Media.Imaging

Public Class ProductTile
    Implements INotifyPropertyChanged

    Public Event PropertyChanged As PropertyChangedEventHandler _
        Implements INotifyPropertyChanged.PropertyChanged

    Public ReadOnly Property Product As Product
    Public ReadOnly Property Picture As BitmapImage

    Public Sub New(p As Product)
        Product = p
        Picture = LoadPicture(p.ImagePath)
    End Sub

    Private _qty As Integer
    Public Property Qty As Integer
        Get
            Return _qty
        End Get
        Set(value As Integer)
            _qty = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(NameOf(Qty)))
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(NameOf(InCart)))
        End Set
    End Property

    Public ReadOnly Property InCart As Boolean
        Get
            Return _qty > 0
        End Get
    End Property

    Private Shared Function LoadPicture(path As String) As BitmapImage
        If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then Return Nothing
        Try
            Dim bmp As New BitmapImage()
            bmp.BeginInit()
            bmp.CacheOption = BitmapCacheOption.OnLoad   ' don't lock the file
            bmp.DecodePixelWidth = 400
            bmp.UriSource = New Uri(path)
            bmp.EndInit()
            bmp.Freeze()
            Return bmp
        Catch
            Return Nothing
        End Try
    End Function

End Class