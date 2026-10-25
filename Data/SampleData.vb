Public Module SampleData

    Public Sub Seed()
        If MenuRepository.GetProducts(False).Count > 0 Then Return
        Add("Espresso", "Coffee", 3D)
        Add("Americano", "Coffee", 3.5D)
        Add("Cappuccino", "Coffee", 4.5D)
        Add("Flat white", "Coffee", 4.5D)
        Add("Iced latte", "Coffee", 5D)
        Add("Matcha latte", "Tea", 5.5D)
        Add("Butter croissant", "Bakery", 3.5D)
        Add("Almond croissant", "Bakery", 4.5D)
        Add("Banana bread", "Bakery", 4D)
    End Sub

    Private Sub Add(name As String, category As String, price As Decimal)
        MenuRepository.AddProduct(New Product With {.Name = name, .Category = category, .Price = price})
    End Sub

End Module