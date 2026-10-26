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
        Picture = ImageStore.Load(p.ImagePath, 400)
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

End Class