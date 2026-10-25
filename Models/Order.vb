Public Class Order
    Public Property Id As Integer
    Public Property OrderNo As String = ""
    Public Property CustomerName As String = ""
    Public Property Notes As String = ""
    Public Property CreatedAt As DateTime
    Public Property CompletedAt As DateTime?
    Public Property Status As String = "Pending"   ' Pending / Completed / Cancelled
    Public Property Subtotal As Decimal
    Public Property Discount As Decimal
    Public Property Total As Decimal
    Public Property PaymentMethod As String = ""
    Public Property AmountPaid As Decimal
    Public Property ChangeAmount As Decimal
    Public Property IsArchived As Boolean
    Public Property Items As New List(Of OrderItem)
End Class