Imports System.Collections.ObjectModel
Imports System.Globalization
Imports System.Windows.Data

Class OrderView

    Private ReadOnly _tiles As New List(Of ProductTile)
    Private ReadOnly _cart As New ObservableCollection(Of OrderItem)
    Private _view As ListCollectionView
    Private _category As String = ""          ' "" = all items
    Private _discountIsPercent As Boolean = False
    Private _ready As Boolean = False

    Public Sub New()
        InitializeComponent()
        CartList.ItemsSource = _cart
        LoadProducts()
        ResetOrder()
        _ready = True
        UpdateTotals()
    End Sub

    ' ---------- Products ----------

    ' Public so the main window can refresh the menu when you come back to this page
    Public Sub ReloadProducts()
        LoadProducts()
        CartChanged()
    End Sub

    Private Sub LoadProducts()
        _category = ""
        _tiles.Clear()
        For Each p In MenuRepository.GetProducts(False).Where(Function(x) x.IsAvailable)
            _tiles.Add(New ProductTile(p))
        Next

        ' Category chips: "All items" + one per category, with counts
        Dim chips As New List(Of CategoryChip)
        chips.Add(New CategoryChip With {.Key = "", .Label = $"All items  {_tiles.Count}", .IsSelected = True})
        For Each grp In _tiles.GroupBy(Function(t) t.Product.Category).OrderBy(Function(x) x.Key)
            chips.Add(New CategoryChip With {.Key = grp.Key, .Label = $"{grp.Key}  {grp.Count()}"})
        Next
        ChipsList.ItemsSource = chips

        _view = New ListCollectionView(_tiles)
        _view.Filter = AddressOf TileFilter
        ProductList.ItemsSource = _view
        UpdateProductInfo()
    End Sub

    Private Function TileFilter(obj As Object) As Boolean
        Dim t = DirectCast(obj, ProductTile)
        If _category <> "" AndAlso t.Product.Category <> _category Then Return False
        Dim q = TxtSearch.Text.Trim()
        If q <> "" AndAlso t.Product.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 Then Return False
        Return True
    End Function

    Private Sub RefreshProducts()
        If _view Is Nothing Then Return
        _view.Refresh()
        UpdateProductInfo()
    End Sub

    Private Sub UpdateProductInfo()
        TxtCount.Text = $"{_view.Count} product{If(_view.Count = 1, "", "s")} available"
        TxtNoProducts.Visibility = If(_view.Count = 0, Visibility.Visible, Visibility.Collapsed)
    End Sub

    Private Sub TxtSearch_TextChanged(sender As Object, e As TextChangedEventArgs)
        RefreshProducts()
    End Sub

    Private Sub Chip_Checked(sender As Object, e As RoutedEventArgs)
        Dim rb = DirectCast(sender, RadioButton)
        _category = Convert.ToString(rb.Tag)
        RefreshProducts()
    End Sub

    Private Sub Product_Click(sender As Object, e As RoutedEventArgs)
        Dim tile = DirectCast(DirectCast(sender, FrameworkElement).DataContext, ProductTile)
        AddToCart(tile.Product)
    End Sub

    ' ---------- Cart ----------

    Private Sub AddToCart(p As Product)
        Dim existing = _cart.FirstOrDefault(Function(i) i.MenuItemId = p.Id)
        If existing IsNot Nothing Then
            existing.Quantity += 1
        Else
            _cart.Add(New OrderItem With {
                .MenuItemId = p.Id, .ItemName = p.Name, .UnitPrice = p.Price, .Quantity = 1})
        End If
        CartChanged()
    End Sub

    Private Sub BtnPlus_Click(sender As Object, e As RoutedEventArgs)
        Dim item = DirectCast(DirectCast(sender, FrameworkElement).DataContext, OrderItem)
        item.Quantity += 1
        CartChanged()
    End Sub

    Private Sub BtnMinus_Click(sender As Object, e As RoutedEventArgs)
        Dim item = DirectCast(DirectCast(sender, FrameworkElement).DataContext, OrderItem)
        If item.Quantity > 1 Then item.Quantity -= 1
        CartChanged()
    End Sub

    Private Sub BtnRemove_Click(sender As Object, e As RoutedEventArgs)
        Dim item = DirectCast(DirectCast(sender, FrameworkElement).DataContext, OrderItem)
        _cart.Remove(item)
        CartChanged()
    End Sub

    ' Keeps the product cards (badge + outline) and the totals in sync with the cart
    Private Sub CartChanged()
        For Each tile In _tiles
            Dim id = tile.Product.Id
            Dim line = _cart.FirstOrDefault(Function(i) i.MenuItemId = id)
            tile.Qty = If(line Is Nothing, 0, line.Quantity)
        Next
        TxtCartEmpty.Visibility = If(_cart.Count = 0, Visibility.Visible, Visibility.Collapsed)
        UpdateTotals()
    End Sub

    ' ---------- Totals & discount ----------

    Private Function CalcDiscount(subtotal As Decimal) As Decimal
        Dim v = Math.Max(Money.Parse(TxtDiscount.Text), 0D)
        Dim d = If(_discountIsPercent, subtotal * Math.Min(v, 100D) / 100D, Math.Min(v, subtotal))
        Return Math.Round(d, 2)
    End Function

    Private Sub UpdateTotals()
        If Not _ready Then Return

        Dim subtotal = _cart.Sum(Function(i) i.LineTotal)
        Dim itemCount = _cart.Sum(Function(i) i.Quantity)
        Dim discount = CalcDiscount(subtotal)
        Dim total = subtotal - discount
        Dim change = Math.Max(Money.Parse(TxtCash.Text) - total, 0D)

        TxtSubtotalLabel.Text = $"Subtotal · {itemCount} item{If(itemCount = 1, "", "s")}"
        TxtSubtotal.Text = Money.ToText(subtotal)
        TxtDiscountAmt.Text = "-" & Money.ToText(discount)
        TxtTotal.Text = Money.ToText(total)
        TxtChange.Text = Money.ToText(change)
        BtnConfirm.Content = "Confirm payment · " & Money.ToText(total)
        BtnConfirm.IsEnabled = _cart.Count > 0
    End Sub

    Private Sub Totals_Changed(sender As Object, e As TextChangedEventArgs)
        UpdateTotals()
    End Sub

    Private Sub BtnDiscType_Click(sender As Object, e As RoutedEventArgs)
        _discountIsPercent = Not _discountIsPercent
        BtnDiscType.Content = If(_discountIsPercent, "%", Money.Symbol)
        UpdateTotals()
    End Sub

    ' ---------- Payment ----------

    Private Sub BtnConfirm_Click(sender As Object, e As RoutedEventArgs)
        If _cart.Count = 0 Then Return

        Dim subtotal = _cart.Sum(Function(i) i.LineTotal)
        Dim discount = CalcDiscount(subtotal)
        Dim total = subtotal - discount
        Dim cash = Money.Parse(TxtCash.Text)

        If cash < total Then
            MessageBox.Show($"Cash received is less than the total ({Money.ToText(total)}).",
                            "Payment", MessageBoxButton.OK, MessageBoxImage.Warning)
            TxtCash.Focus()
            Return
        End If

        Dim o As New Order With {
            .CustomerName = TxtCustomer.Text.Trim(),
            .Notes = TxtNotes.Text.Trim(),
            .Subtotal = subtotal,
            .Discount = discount,
            .Total = total,
            .PaymentMethod = "Cash",
            .AmountPaid = cash,
            .ChangeAmount = cash - total
        }
        For Each line In _cart
            o.Items.Add(New OrderItem With {
                .MenuItemId = line.MenuItemId, .ItemName = line.ItemName,
                .UnitPrice = line.UnitPrice, .Quantity = line.Quantity})
        Next

        Try
            OrderRepository.CreateOrder(o)
        Catch ex As Exception
            MessageBox.Show("Could not save the order:" & vbCrLf & ex.Message,
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            Return
        End Try

        MessageBox.Show($"Order #{o.OrderNo} placed.{vbCrLf}Change due: {Money.ToText(o.ChangeAmount)}",
                        "Order placed", MessageBoxButton.OK, MessageBoxImage.Information)
        ResetOrder()
    End Sub

    ' Clears everything for the next customer
    Private Sub ResetOrder()
        _cart.Clear()
        TxtCustomer.Clear()
        TxtNotes.Clear()
        TxtCash.Clear()
        TxtDiscount.Clear()
        _discountIsPercent = False
        BtnDiscType.Content = Money.Symbol
        TxtOrderNo.Text = "Order #" & OrderRepository.PeekNextOrderNo()
        TxtOrderDate.Text = "Today, " & DateTime.Now.ToString("h:mm tt", CultureInfo.InvariantCulture)
        CartChanged()
    End Sub

End Class