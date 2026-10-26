Imports System.IO
Imports Microsoft.Data.Sqlite

Public Module Db

    Public ReadOnly DbPath As String =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ordering.db")

    Public Function GetConnection() As SqliteConnection
        Dim conn As New SqliteConnection($"Data Source={DbPath}")
        conn.Open()
        Using cmd = conn.CreateCommand()
            cmd.CommandText = "PRAGMA foreign_keys = ON;"
            cmd.ExecuteNonQuery()
        End Using
        Return conn
    End Function

    Public Sub Initialize()
        Const schema As String = "
        CREATE TABLE IF NOT EXISTS MenuItems (
            Id          INTEGER PRIMARY KEY AUTOINCREMENT,
            Name        TEXT    NOT NULL,
            Category    TEXT    NOT NULL,
            Price       REAL    NOT NULL,
            ImagePath   TEXT
            Description TEXT    NOT NULL DEFAULT '',
            IsAvailable INTEGER NOT NULL DEFAULT 1,
            IsArchived  INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS Orders (
            Id            INTEGER PRIMARY KEY AUTOINCREMENT,
            OrderNo       TEXT    NOT NULL UNIQUE,
            CustomerName  TEXT    NOT NULL DEFAULT '',
            Notes         TEXT    NOT NULL DEFAULT '',
            CreatedAt     TEXT    NOT NULL,
            CompletedAt   TEXT,
            Status        TEXT    NOT NULL DEFAULT 'Pending',
            Subtotal      REAL    NOT NULL,
            Discount      REAL    NOT NULL DEFAULT 0,
            Total         REAL    NOT NULL,
            PaymentMethod TEXT,
            AmountPaid    REAL    NOT NULL DEFAULT 0,
            ChangeAmount  REAL    NOT NULL DEFAULT 0,
            IsArchived    INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS OrderItems (
            Id         INTEGER PRIMARY KEY AUTOINCREMENT,
            OrderId    INTEGER NOT NULL,
            MenuItemId INTEGER,
            ItemName   TEXT    NOT NULL,
            UnitPrice  REAL    NOT NULL,
            Quantity   INTEGER NOT NULL,
            LineTotal  REAL    NOT NULL,
            FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS Settings (
            Key   TEXT PRIMARY KEY,
            Value TEXT
        );

        INSERT OR IGNORE INTO Settings (Key, Value) VALUES ('SystemName', 'My Cafe');
        INSERT OR IGNORE INTO Settings (Key, Value) VALUES ('LogoPath', '');
        "
        Using conn = GetConnection()
            Using cmd = conn.CreateCommand()
                cmd.CommandText = schema
                cmd.ExecuteNonQuery()
            End Using

            ' Upgrades for databases created by older versions of the app
            EnsureColumn(conn, "MenuItems", "Description", "TEXT NOT NULL DEFAULT ''")
            DropColumnIfExists(conn, "MenuItems", "SKU")     ' SKU was removed from the design
        End Using
    End Sub

    Private Function ColumnExists(conn As SqliteConnection, table As String, column As String) As Boolean
        Using cmd = conn.CreateCommand()
            cmd.CommandText = $"PRAGMA table_info({table})"
            Using r = cmd.ExecuteReader()
                While r.Read()
                    If String.Equals(r.GetString(1), column, StringComparison.OrdinalIgnoreCase) Then Return True
                End While
            End Using
        End Using
        Return False
    End Function

    ' Adds a column only if the table doesn't have it yet
    Private Sub EnsureColumn(conn As SqliteConnection, table As String, column As String, definition As String)
        If ColumnExists(conn, table, column) Then Return
        Using cmd = conn.CreateCommand()
            cmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition}"
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    Private Sub DropColumnIfExists(conn As SqliteConnection, table As String, column As String)
        If Not ColumnExists(conn, table, column) Then Return
        Try
            Using cmd = conn.CreateCommand()
                cmd.CommandText = $"ALTER TABLE {table} DROP COLUMN {column}"
                cmd.ExecuteNonQuery()
            End Using
        Catch
            ' If this SQLite version can't drop columns, the unused column is harmless
        End Try
    End Sub

End Module