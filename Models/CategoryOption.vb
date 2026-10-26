Imports System.ComponentModel

Public Class CategoryOption
    Implements INotifyPropertyChanged

    Public Event PropertyChanged As PropertyChangedEventHandler _
        Implements INotifyPropertyChanged.PropertyChanged

    Public Property Name As String = ""

    Private _isHighlighted As Boolean
    Public Property IsHighlighted As Boolean
        Get
            Return _isHighlighted
        End Get
        Set(value As Boolean)
            _isHighlighted = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(NameOf(IsHighlighted)))
        End Set
    End Property

End Class