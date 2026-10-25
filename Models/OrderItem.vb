Imports System.ComponentModel

Public Class OrderItem
    Implements INotifyPropertyChanged

    Public Event PropertyChanged As PropertyChangedEventHandler _
        Implements INotifyPropertyChanged.PropertyChanged

    Public Property Id As Integer
    Public Property OrderId As Integer
    Public Property MenuItemId As Integer
    Public Property ItemName As String = ""
    Public Property UnitPrice As Decimal

    Private _quantity As Integer
    Public Property Quantity As Integer
        Get
            Return _quantity
        End Get
        Set(value As Integer)
            _quantity = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(NameOf(Quantity)))
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(NameOf(LineTotal)))
        End Set
    End Property

    Public ReadOnly Property LineTotal As Decimal
        Get
            Return UnitPrice * _quantity
        End Get
    End Property
End Class