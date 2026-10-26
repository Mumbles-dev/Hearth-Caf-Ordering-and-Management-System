Public Module TextMatch

    ' How many single-letter edits turn a into b ("cofee" -> "coffee" = 1)
    Public Function Distance(a As String, b As String) As Integer
        If a.Length = 0 Then Return b.Length
        If b.Length = 0 Then Return a.Length

        Dim prev(b.Length) As Integer
        Dim curr(b.Length) As Integer
        For j = 0 To b.Length
            prev(j) = j
        Next

        For i = 1 To a.Length
            curr(0) = i
            For j = 1 To b.Length
                Dim cost = If(a(i - 1) = b(j - 1), 0, 1)
                curr(j) = Math.Min(Math.Min(curr(j - 1) + 1, prev(j) + 1), prev(j - 1) + cost)
            Next
            Array.Copy(curr, prev, curr.Length)
        Next
        Return prev(b.Length)
    End Function

End Module