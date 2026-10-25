Imports System.Globalization
Imports Microsoft.Data.Sqlite

Public Module OrderRepository

    Private Const DateFmt As String = "yyyy-MM-dd HH:mm:ss"

    Private Function ToDbDate(d As DateTime) As String
        Return d.ToString(DateFmt, CultureInfo.InvariantCulture)
    End Function

    Private Function FromDbDate(s As String) As DateTime
        Return DateTime.ParseExact(s, DateFmt, CultureInfo.InvariantCulture)
    End Function

    Private Function MapOrder(r As SqliteDataReader) As Order
        Return New Order With {
            .Id = r.GetInt32(0),
            .OrderNo = r.GetString(1),
            .CreatedAt = FromDbDate(r.GetString(2)),
            .CompletedAt = If(r.IsDBNull(3), CType(Nothing, DateTime?), FromDbDate(r.GetString(3))),
            .Status = r.GetString(4),
            .Subtotal = CDec(r.GetDouble(5)),
            .Discount = CDec(r.GetDouble(6)),
            .Total = CDec(r.GetDouble(7)),
            .PaymentMethod = If(r.IsDBNull(8), "", r.GetString(8)),
            .AmountPaid = CDec(r.GetDouble(9)),
            .ChangeAmount = CDec(r.GetDouble(10)),
            .IsArchived = (r.GetInt32(11) = 1),
            .CustomerName = r.GetString(12),
            .Notes = r.GetString(13)
        }
    End Function

    Private Function NextOrderNo(conn As SqliteConnection, tx As SqliteTransaction) As String
        Using cmd = conn.CreateCommand()
            cmd.Transaction = tx
            cmd.CommandText = "SELECT IFNULL(MAX(Id), 0) FROM Orders"
            Dim lastId As Integer = Convert.ToInt32(cmd.ExecuteScalar())
            Return (1000 + lastId + 1).ToString()
        End Using
    End Function

    ' Shown on the Order page before the order is saved
    Public Function PeekNextOrderNo() As String
        Using conn = GetConnection()
            Return NextOrderNo(conn, Nothing)
        End Using
    End Function

    ' Saves the order + its items in one transaction. New orders start as Pending.
    Public Function CreateOrder(o As Order) As Integer
        Using conn = GetConnection()
            Using tx = conn.BeginTransaction()
                o.OrderNo = NextOrderNo(conn, tx)
                o.CreatedAt = DateTime.Now
                Dim orderId As Integer

                Using cmd = conn.CreateCommand()
                    cmd.Transaction = tx
                    cmd.CommandText =
                        "INSERT INTO Orders (OrderNo, CustomerName, Notes, CreatedAt, Status, Subtotal, " &
                        "Discount, Total, PaymentMethod, AmountPaid, ChangeAmount) " &
                        "VALUES (@no, @cust, @notes, @cr, 'Pending', @sub, @disc, @tot, @pm, @paid, @chg); " &
                        "SELECT last_insert_rowid();"
                    cmd.Parameters.AddWithValue("@no", o.OrderNo)
                    cmd.Parameters.AddWithValue("@cust", o.CustomerName)
                    cmd.Parameters.AddWithValue("@notes", o.Notes)
                    cmd.Parameters.AddWithValue("@cr", ToDbDate(o.CreatedAt))
                    cmd.Parameters.AddWithValue("@sub", CDbl(o.Subtotal))
                    cmd.Parameters.AddWithValue("@disc", CDbl(o.Discount))
                    cmd.Parameters.AddWithValue("@tot", CDbl(o.Total))
                    cmd.Parameters.AddWithValue("@pm", o.PaymentMethod)
                    cmd.Parameters.AddWithValue("@paid", CDbl(o.AmountPaid))
                    cmd.Parameters.AddWithValue("@chg", CDbl(o.ChangeAmount))
                    orderId = Convert.ToInt32(cmd.ExecuteScalar())
                End Using

                For Each it In o.Items
                    Using cmd = conn.CreateCommand()
                        cmd.Transaction = tx
                        cmd.CommandText =
                            "INSERT INTO OrderItems (OrderId, MenuItemId, ItemName, UnitPrice, Quantity, LineTotal) " &
                            "VALUES (@oid, @mid, @nm, @up, @q, @lt)"
                        cmd.Parameters.AddWithValue("@oid", orderId)
                        cmd.Parameters.AddWithValue("@mid", it.MenuItemId)
                        cmd.Parameters.AddWithValue("@nm", it.ItemName)
                        cmd.Parameters.AddWithValue("@up", CDbl(it.UnitPrice))
                        cmd.Parameters.AddWithValue("@q", it.Quantity)
                        cmd.Parameters.AddWithValue("@lt", CDbl(it.LineTotal))
                        cmd.ExecuteNonQuery()
                    End Using
                Next

                tx.Commit()
                o.Id = orderId
                Return orderId
            End Using
        End Using
    End Function

    ' statuses: e.g. {"Pending"} for Order Management, {"Completed","Cancelled"} for history
    Public Function GetOrders(statuses As IEnumerable(Of String), archived As Boolean) As List(Of Order)
        Dim list As New List(Of Order)
        Dim statusList = statuses.ToList()
        If statusList.Count = 0 Then Return list

        Using conn = GetConnection()
            Using cmd = conn.CreateCommand()
                Dim names = statusList.Select(Function(s, i) "@s" & i).ToArray()
                cmd.CommandText =
                    "SELECT Id, OrderNo, CreatedAt, CompletedAt, Status, Subtotal, Discount, Total, " &
                    "PaymentMethod, AmountPaid, ChangeAmount, IsArchived, CustomerName, Notes FROM Orders " &
                    "WHERE IsArchived = @a AND Status IN (" & String.Join(",", names) & ") " &
                    "ORDER BY CreatedAt DESC"
                cmd.Parameters.AddWithValue("@a", If(archived, 1, 0))
                For i = 0 To statusList.Count - 1
                    cmd.Parameters.AddWithValue(names(i), statusList(i))
                Next
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        list.Add(MapOrder(r))
                    End While
                End Using
            End Using
        End Using

        For Each o In list
            o.Items = GetItems(o.Id)
        Next
        Return list
    End Function

    Public Function GetItems(orderId As Integer) As List(Of OrderItem)
        Dim list As New List(Of OrderItem)
        Using conn = GetConnection()
            Using cmd = conn.CreateCommand()
                cmd.CommandText =
                    "SELECT Id, OrderId, MenuItemId, ItemName, UnitPrice, Quantity " &
                    "FROM OrderItems WHERE OrderId = @id"
                cmd.Parameters.AddWithValue("@id", orderId)
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        list.Add(New OrderItem With {
                            .Id = r.GetInt32(0),
                            .OrderId = r.GetInt32(1),
                            .MenuItemId = If(r.IsDBNull(2), 0, r.GetInt32(2)),
                            .ItemName = r.GetString(3),
                            .UnitPrice = CDec(r.GetDouble(4)),
                            .Quantity = r.GetInt32(5)
                        })
                    End While
                End Using
            End Using
        End Using
        Return list
    End Function

    ' status = "Completed" or "Cancelled"
    Public Sub SetStatus(orderId As Integer, status As String)
        Using conn = GetConnection()
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "UPDATE Orders SET Status=@s, CompletedAt=@c WHERE Id=@id"
                cmd.Parameters.AddWithValue("@s", status)
                cmd.Parameters.AddWithValue("@c", ToDbDate(DateTime.Now))
                cmd.Parameters.AddWithValue("@id", orderId)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Public Sub SetArchived(orderId As Integer, archived As Boolean)
        Using conn = GetConnection()
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "UPDATE Orders SET IsArchived=@a WHERE Id=@id"
                cmd.Parameters.AddWithValue("@a", If(archived, 1, 0))
                cmd.Parameters.AddWithValue("@id", orderId)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

End Module