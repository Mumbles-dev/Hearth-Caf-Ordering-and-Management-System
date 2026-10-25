Public Module SettingsRepository

    Public Function GetValue(key As String, Optional fallback As String = "") As String
        Using conn = GetConnection()
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "SELECT Value FROM Settings WHERE Key=@k"
                cmd.Parameters.AddWithValue("@k", key)
                Dim result = cmd.ExecuteScalar()
                If result Is Nothing OrElse IsDBNull(result) Then Return fallback
                Return result.ToString()
            End Using
        End Using
    End Function

    Public Sub SetValue(key As String, value As String)
        Using conn = GetConnection()
            Using cmd = conn.CreateCommand()
                cmd.CommandText =
                    "INSERT INTO Settings (Key, Value) VALUES (@k, @v) " &
                    "ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value"
                cmd.Parameters.AddWithValue("@k", key)
                cmd.Parameters.AddWithValue("@v", value)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

End Module