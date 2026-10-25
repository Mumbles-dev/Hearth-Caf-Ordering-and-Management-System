Imports System.Globalization

Public Module Money

    Public Const Symbol As String = "$"

    Public Function ToText(value As Decimal) As String
        Return Symbol & value.ToString("N2", CultureInfo.InvariantCulture)
    End Function

    ' Reads what the cashier typed. Anything invalid counts as 0.
    Public Function Parse(text As String) As Decimal
        If String.IsNullOrWhiteSpace(text) Then Return 0D
        Dim cleaned = text.Replace(Symbol, "").Replace(",", "").Trim()
        Dim v As Decimal
        If Decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, v) Then Return v
        Return 0D
    End Function

End Module