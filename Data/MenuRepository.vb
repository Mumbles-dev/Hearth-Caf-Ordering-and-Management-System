Imports Microsoft.Data.Sqlite

Public Module MenuRepository

    Private Const SelectCols As String =
        "SELECT Id, Name, Category, Price, ImagePath, IsAvailable, IsArchived FROM MenuItems "

    Private Function Map(r As SqliteDataReader) As Product
        Return New Product With {
            .Id = r.GetInt32(0),
            .Name = r.GetString(1),
            .Category = r.GetString(2),
            .Price = CDec(r.GetDouble(3)),
            .ImagePath = If(r.IsDBNull(4), "", r.GetString(4)),
            .IsAvailable = (r.GetInt32(5) = 1),
            .IsArchived = (r.GetInt32(6) = 1)
        }
    End Function

    ' archived = False -> active menu, True -> archive page
    Public Function GetProducts(archived As Boolean) As List(Of Product)
        Dim list As New List(Of Product)
        Using conn = GetConnection()
            Using cmd = conn.CreateCommand()
                cmd.CommandText = SelectCols & "WHERE IsArchived = @a ORDER BY Category, Name"
                cmd.Parameters.AddWithValue("@a", If(archived, 1, 0))
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        list.Add(Map(r))
                    End While
                End Using
            End Using
        End Using
        Return list
    End Function

    Public Function GetCategories() As List(Of String)
        Dim list As New List(Of String)
        Using conn = GetConnection()
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "SELECT DISTINCT Category FROM MenuItems WHERE IsArchived = 0 ORDER BY Category"
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        list.Add(r.GetString(0))
                    End While
                End Using
            End Using
        End Using
        Return list
    End Function

    Public Function AddProduct(p As Product) As Integer
        Using conn = GetConnection()
            Using cmd = conn.CreateCommand()
                cmd.CommandText =
                    "INSERT INTO MenuItems (Name, Category, Price, ImagePath, IsAvailable) " &
                    "VALUES (@n, @c, @p, @i, @av); SELECT last_insert_rowid();"
                cmd.Parameters.AddWithValue("@n", p.Name)
                cmd.Parameters.AddWithValue("@c", p.Category)
                cmd.Parameters.AddWithValue("@p", CDbl(p.Price))
                cmd.Parameters.AddWithValue("@i", p.ImagePath)
                cmd.Parameters.AddWithValue("@av", If(p.IsAvailable, 1, 0))
                Return Convert.ToInt32(cmd.ExecuteScalar())
            End Using
        End Using
    End Function

    Public Sub UpdateProduct(p As Product)
        Using conn = GetConnection()
            Using cmd = conn.CreateCommand()
                cmd.CommandText =
                    "UPDATE MenuItems SET Name=@n, Category=@c, Price=@p, ImagePath=@i, IsAvailable=@av " &
                    "WHERE Id=@id"
                cmd.Parameters.AddWithValue("@n", p.Name)
                cmd.Parameters.AddWithValue("@c", p.Category)
                cmd.Parameters.AddWithValue("@p", CDbl(p.Price))
                cmd.Parameters.AddWithValue("@i", p.ImagePath)
                cmd.Parameters.AddWithValue("@av", If(p.IsAvailable, 1, 0))
                cmd.Parameters.AddWithValue("@id", p.Id)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    ' "Delete" = archive. Restore = SetArchived(id, False)
    Public Sub SetArchived(id As Integer, archived As Boolean)
        Using conn = GetConnection()
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "UPDATE MenuItems SET IsArchived=@a WHERE Id=@id"
                cmd.Parameters.AddWithValue("@a", If(archived, 1, 0))
                cmd.Parameters.AddWithValue("@id", id)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

End Module