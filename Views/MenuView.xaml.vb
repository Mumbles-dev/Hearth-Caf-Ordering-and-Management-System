Imports System.Collections.ObjectModel
Imports System.Globalization
Imports System.IO
Imports System.Windows.Data
Imports System.Windows.Input
Imports System.Windows.Media
Imports Microsoft.Win32
Imports System.Linq

Class MenuView

    Private _all As New List(Of Product)          ' every active product
    Private _view As ListCollectionView
    Private _categories As New List(Of String)
    Private _category As String = ""              ' chip filter, "" = all
    Private _editing As Product                   ' Nothing = adding a new item
    Private _imagePath As String = ""             ' saved photo (relative path)
    Private _pendingImage As String = ""          ' newly chosen file, copied when you press Save
    Private _suppress As Boolean = False          ' ignore list selection events we trigger ourselves

    ' Category field state
    Private ReadOnly _options As New ObservableCollection(Of CategoryOption)
    Private _lastTyped As String = ""             ' what the user had typed before the last keystroke
    Private _busy As Boolean = False              ' true while we change the text ourselves
    Private _hi As Integer = -1                   ' highlighted row in the dropdown
    Private _suggestion As String = ""

    Public Sub New()
        InitializeComponent()
        LblPrice.Text = $"Price ({Money.Symbol})"
        CategoryDropList.ItemsSource = _options
        ReloadAll()
        ClearForm()
    End Sub

    ' ================= Loading the list =================

    Private Sub ReloadAll(Optional selectId As Integer = 0)
        _suppress = True

        _all = MenuRepository.GetProducts(False)
        _categories = _all.Select(Function(p) p.Category).Distinct().OrderBy(Function(c) c).ToList()
        If _category <> "" AndAlso Not _categories.Contains(_category) Then _category = ""

        Dim chips As New List(Of CategoryChip)
        chips.Add(New CategoryChip With {
            .Key = "", .Label = $"All items  {_all.Count}", .IsSelected = (_category = "")})
        For Each cat In _categories
            Dim count As Integer = Enumerable.Count(_all, Function(p) p.Category = cat)
            chips.Add(New CategoryChip With {
                .Key = cat, .Label = $"{cat}  {count}", .IsSelected = (cat = _category)})
        Next
        ChipsList.ItemsSource = chips

        _view = New ListCollectionView(_all)
        _view.Filter = AddressOf RowFilter
        ProductListBox.ItemsSource = _view

        Dim selected = _all.FirstOrDefault(Function(p) p.Id = selectId)
        ProductListBox.SelectedItem = selected

        UpdateStats()
        UpdateFooter()
        _suppress = False

        If selected IsNot Nothing Then LoadForm(selected)
    End Sub

    Private Function RowFilter(obj As Object) As Boolean
        Dim p = DirectCast(obj, Product)
        If _category <> "" AndAlso p.Category <> _category Then Return False

        Dim q = TxtSearch.Text.Trim()
        If q <> "" AndAlso p.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 Then Return False
        Return True
    End Function

    Private Sub UpdateStats()
        StatTotal.Text = _all.Count.ToString()

        Dim topCats = _all.GroupBy(Function(p) p.Category) _
                          .OrderByDescending(Function(g) g.Count()).Take(2).ToList()

        If topCats.Count > 0 Then
            StatName2.Text = topCats(0).Key
            StatValue2.Text = topCats(0).Count().ToString()
            StatBox2.Visibility = Visibility.Visible
        Else
            StatBox2.Visibility = Visibility.Collapsed
        End If

        If topCats.Count > 1 Then
            StatName3.Text = topCats(1).Key
            StatValue3.Text = topCats(1).Count().ToString()
            StatBox3.Visibility = Visibility.Visible
        Else
            StatBox3.Visibility = Visibility.Collapsed
        End If
    End Sub

    Private Sub UpdateFooter()
        TxtFooter.Text = If(_view.Count = _all.Count,
                            $"Showing all {_all.Count} menu items",
                            $"Showing {_view.Count} of {_all.Count} menu items")
    End Sub

    Private Sub TxtSearch_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _view Is Nothing Then Return
        _view.Refresh()
        UpdateFooter()
    End Sub

    Private Sub Chip_Checked(sender As Object, e As RoutedEventArgs)
        If _view Is Nothing Then Return
        _category = Convert.ToString(DirectCast(sender, RadioButton).Tag)
        _view.Refresh()
        UpdateFooter()
    End Sub

    Private Sub ProductListBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        If _suppress Then Return
        Dim p = TryCast(ProductListBox.SelectedItem, Product)
        If p IsNot Nothing Then LoadForm(p)
    End Sub

    ' ================= The form =================

    Private Sub BtnAdd_Click(sender As Object, e As RoutedEventArgs)
        ClearForm()
        TxtName.Focus()
    End Sub

    Private Sub BtnCancel_Click(sender As Object, e As RoutedEventArgs)
        ClearForm()
    End Sub

    ' Blank form = "add" mode
    Private Sub ClearForm()
        _suppress = True
        ProductListBox.SelectedItem = Nothing
        _suppress = False

        _editing = Nothing
        _imagePath = ""
        _pendingImage = ""
        TxtFormTitle.Text = "Add menu item"
        TxtName.Clear()
        SetCategoryText("")
        TxtPrice.Clear()
        TxtDescription.Clear()
        TglAvailable.IsChecked = True
        BtnArchive.Visibility = Visibility.Collapsed
        ShowPhoto()
    End Sub

    ' Filled form = "edit" mode
    Private Sub LoadForm(p As Product)
        _editing = p
        _imagePath = p.ImagePath
        _pendingImage = ""
        TxtFormTitle.Text = "Edit menu item"
        TxtName.Text = p.Name
        SetCategoryText(p.Category)
        TxtPrice.Text = p.Price.ToString("0.00", CultureInfo.InvariantCulture)
        TxtDescription.Text = p.Description
        TglAvailable.IsChecked = p.IsAvailable
        BtnArchive.Visibility = Visibility.Visible
        ShowPhoto()
    End Sub

    ' ================= Photo =================

    Private Sub ShowPhoto()
        Dim source = If(_pendingImage <> "", _pendingImage, _imagePath)
        Dim bmp = ImageStore.Load(source)
        If bmp Is Nothing Then
            PhotoBox.Background = Nothing
            TxtImageInfo.Text = "No photo"
        Else
            PhotoBox.Background = New ImageBrush(bmp) With {.Stretch = Stretch.UniformToFill}
            TxtImageInfo.Text = $"{Path.GetExtension(source).TrimStart("."c).ToUpperInvariant()} · " &
                                $"{bmp.PixelWidth} × {bmp.PixelHeight}"
        End If
    End Sub

    Private Sub BtnChangePhoto_Click(sender As Object, e As RoutedEventArgs)
        Dim dlg As New OpenFileDialog With {
            .Title = "Choose a photo",
            .Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files|*.*"
        }
        If dlg.ShowDialog() = True Then
            _pendingImage = dlg.FileName
            ShowPhoto()
        End If
    End Sub

    ' ================= Category: autocomplete, dropdown, status =================

    ' Sets the text from code (no autocomplete, no dropdown)
    Private Sub SetCategoryText(value As String)
        _busy = True
        TxtCategory.Text = value
        TxtCategory.CaretIndex = value.Length
        _busy = False
        _lastTyped = value
        CategoryPopup.IsOpen = False
        UpdateCategoryStatus()
    End Sub

    ' What the user actually typed (without the grey, not-yet-accepted completion)
    Private Function TypedPart() As String
        Dim t = TxtCategory.Text
        If TxtCategory.SelectionLength > 0 Then t = t.Substring(0, TxtCategory.SelectionStart)
        Return t.Trim()
    End Function

    Private Sub TxtCategory_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _busy Then Return

        Dim typed = TxtCategory.Text
        Dim grew = typed.Length > _lastTyped.Length

        ' Inline completion: typing "Cof" fills in "Coffee" with "fee" selected.
        ' Only for an exact-case prefix, since categories are case-sensitive.
        If TxtCategory.IsKeyboardFocusWithin AndAlso grew AndAlso typed.Trim() <> "" AndAlso
           TxtCategory.CaretIndex = typed.Length Then

            Dim completion = _categories.FirstOrDefault(
                Function(c) c.Length > typed.Length AndAlso c.StartsWith(typed, StringComparison.Ordinal))

            If completion IsNot Nothing Then
                _busy = True
                TxtCategory.Text = completion
                TxtCategory.Select(typed.Length, completion.Length - typed.Length)
                _busy = False
            End If
        End If

        _lastTyped = typed
        UpdateCategoryStatus()
        RefreshCategoryPopup()
    End Sub

    Private Sub TxtCategory_PreviewKeyDown(sender As Object, e As KeyEventArgs)
        Select Case e.Key
            Case Key.Down
                If Not CategoryPopup.IsOpen Then
                    OpenCategoryList(TypedPart() = "")
                Else
                    SetHighlight(Math.Min(_hi + 1, _options.Count - 1))
                End If
                e.Handled = True

            Case Key.Up
                If CategoryPopup.IsOpen Then
                    SetHighlight(Math.Max(_hi - 1, 0))
                    e.Handled = True
                End If

            Case Key.Enter
                If CategoryPopup.IsOpen AndAlso _hi >= 0 AndAlso _hi < _options.Count Then
                    PickCategory(_options(_hi).Name)
                Else
                    ' Accept the grey completion (if any) and keep exactly what is in the box
                    CategoryPopup.IsOpen = False
                    TxtCategory.CaretIndex = TxtCategory.Text.Length
                    UpdateCategoryStatus()
                End If
                e.Handled = True

            Case Key.Tab
                ' Tab accepts the grey completion, then moves on as usual
                TxtCategory.CaretIndex = TxtCategory.Text.Length

            Case Key.Escape
                If CategoryPopup.IsOpen Then
                    CategoryPopup.IsOpen = False
                    e.Handled = True
                End If
        End Select
    End Sub

    Private Sub TxtCategory_LostKeyboardFocus(sender As Object, e As KeyboardFocusChangedEventArgs)
        CategoryPopup.IsOpen = False

        ' A completion that was never accepted is dropped, so only what you typed is kept
        If TxtCategory.SelectionLength > 0 Then
            Dim typed = TxtCategory.Text.Substring(0, TxtCategory.SelectionStart)
            _busy = True
            TxtCategory.Text = typed
            _busy = False
            _lastTyped = typed
        End If
        UpdateCategoryStatus()
    End Sub

    ' ----- Matching -----

    ' Best matches first: same name in other case, starts with, contains, similar spelling
    Private Function FindMatches(typed As String) As List(Of String)
        Dim t = typed.Trim()
        If t = "" Then Return New List(Of String)()

        Dim lowered = t.ToLowerInvariant()
        Dim maxEdits = If(t.Length >= 6, 2, 1)
        Dim found As New List(Of KeyValuePair(Of Integer, String))

        For Each c In _categories
            Dim score As Integer
            If String.Equals(c, t, StringComparison.OrdinalIgnoreCase) Then
                score = 0
            ElseIf c.StartsWith(t, StringComparison.OrdinalIgnoreCase) Then
                score = 1
            ElseIf c.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0 Then
                score = 2
            ElseIf t.Length >= 4 AndAlso IsCloseSpelling(c.ToLowerInvariant(), lowered, maxEdits) Then
                score = 3
            Else
                Continue For
            End If
            found.Add(New KeyValuePair(Of Integer, String)(score, c))
        Next

        Return found.OrderBy(Function(k) k.Key).ThenBy(Function(k) k.Value) _
                    .Select(Function(k) k.Value).Take(6).ToList()
    End Function

    Private Function IsCloseSpelling(category As String, typed As String, maxEdits As Integer) As Boolean
        If TextMatch.Distance(category, typed) <= maxEdits Then Return True
        ' Typo made while still typing: "cofe" is close to the start of "coffee"
        If category.Length > typed.Length Then
            Return TextMatch.Distance(category.Substring(0, typed.Length), typed) <= maxEdits
        End If
        Return False
    End Function

    ' ----- Status line under the field -----

    Private Sub UpdateCategoryStatus()
        Dim full = TxtCategory.Text.Trim()
        _suggestion = ""
        BtnUseSuggestion.Visibility = Visibility.Collapsed

        If full = "" Then
            CategoryHelp.Visibility = Visibility.Collapsed
            Return
        End If

        CategoryHelp.Visibility = Visibility.Visible

        If _categories.Contains(full) Then
            TxtCategoryHelp.Text = "✓ Existing category"
            TxtCategoryHelp.Foreground = DirectCast(FindResource("OkFgBrush"), Brush)
            Return
        End If

        Dim best = FindMatches(TypedPart()).FirstOrDefault(Function(c) c <> full)
        If best IsNot Nothing Then
            _suggestion = best
            TxtCategoryHelp.Text = $"Did you mean '{best}'?"
            TxtCategoryHelp.Foreground = DirectCast(FindResource("PrimaryBrush"), Brush)
            BtnUseSuggestion.Visibility = Visibility.Visible
        Else
            TxtCategoryHelp.Text = "New category. It will be created when you save."
            TxtCategoryHelp.Foreground = DirectCast(FindResource("MutedBrush"), Brush)
        End If
    End Sub

    Private Sub BtnUseSuggestion_Click(sender As Object, e As RoutedEventArgs)
        If _suggestion <> "" Then PickCategory(_suggestion)
    End Sub

    ' ----- Dropdown -----

    ' Opens by itself while typing, if there is something close to show
    Private Sub RefreshCategoryPopup()
        Dim t = TypedPart()
        If Not TxtCategory.IsKeyboardFocusWithin OrElse t = "" Then
            CategoryPopup.IsOpen = False
            Return
        End If

        Dim matches = FindMatches(t)
        ' Nothing to show, or the only match is exactly what is already in the box
        If matches.Count = 0 OrElse (matches.Count = 1 AndAlso matches(0) = TxtCategory.Text.Trim()) Then
            CategoryPopup.IsOpen = False
            Return
        End If

        OpenCategoryList(False)
    End Sub

    Private Sub OpenCategoryList(showAll As Boolean)
        Dim names = If(showAll, _categories.ToList(), FindMatches(TypedPart()))

        _options.Clear()
        For Each n In names
            _options.Add(New CategoryOption With {.Name = n})
        Next

        If _options.Count = 0 Then
            CategoryPopup.IsOpen = False
            Return
        End If

        PopupCard.Width = Math.Max(TxtCategory.ActualWidth - 10, 200)
        SetHighlight(If(showAll, names.IndexOf(TxtCategory.Text.Trim()), -1))
        CategoryPopup.IsOpen = True
    End Sub

    Private Sub SetHighlight(index As Integer)
        _hi = index
        For i = 0 To _options.Count - 1
            _options(i).IsHighlighted = (i = index)
        Next

        If index >= 0 AndAlso index < _options.Count Then
            Dim row = TryCast(CategoryDropList.ItemContainerGenerator.ContainerFromIndex(index), FrameworkElement)
            If row IsNot Nothing Then row.BringIntoView()
        End If
    End Sub

    Private Sub BtnCategoryDrop_Click(sender As Object, e As RoutedEventArgs)
        OpenCategoryList(True)
    End Sub

    Private Sub PickCategory(name As String)
        SetCategoryText(name)
    End Sub

    Private Sub CategoryPick_Click(sender As Object, e As RoutedEventArgs)
        Dim opt = DirectCast(DirectCast(sender, FrameworkElement).DataContext, CategoryOption)
        PickCategory(opt.Name)
    End Sub

    Private Sub CategoryDelete_Click(sender As Object, e As RoutedEventArgs)
        Dim name = DirectCast(DirectCast(sender, FrameworkElement).DataContext, CategoryOption).Name
        CategoryPopup.IsOpen = False

        Dim count As Integer = System.Linq.Enumerable.Count(_all, Function(p) p.Category = name)
        Dim answer = MessageBox.Show(
            $"Delete the category '{name}'?{vbCrLf}{count} menu item(s) in it will be moved to " &
            $"'{MenuRepository.DefaultCategory}'.",
            "Delete category", MessageBoxButton.YesNo, MessageBoxImage.Warning)
        If answer <> MessageBoxResult.Yes Then Return

        MenuRepository.DeleteCategory(name)
        ReloadAll(If(_editing IsNot Nothing, _editing.Id, 0))
        If _editing Is Nothing AndAlso TxtCategory.Text.Trim() = name Then SetCategoryText("")
    End Sub

    ' ================= Save / archive =================

    Private Sub BtnSave_Click(sender As Object, e As RoutedEventArgs)
        Dim name = TxtName.Text.Trim()
        Dim category = TxtCategory.Text.Trim()
        Dim editId = If(_editing Is Nothing, 0, _editing.Id)

        If name = "" Then
            Fail("Please enter an item name.", TxtName)
            Return
        End If
        If category = "" Then
            Fail("Please enter or pick a category.", TxtCategory)
            Return
        End If

        Dim price As Decimal
        Dim priceText = TxtPrice.Text.Replace(Money.Symbol, "").Replace(",", "").Trim()
        If Not Decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.InvariantCulture, price) _
           OrElse price < 0 Then
            Fail("Please enter a valid price, for example 4.50.", TxtPrice)
            Return
        End If

        Try
            Dim image = _imagePath
            If _pendingImage <> "" Then image = ImageStore.SaveCopy(_pendingImage)

            Dim p As New Product With {
                .Id = editId,
                .Name = name,
                .Category = category,
                .Price = price,
                .Description = TxtDescription.Text.Trim(),
                .ImagePath = image,
                .IsAvailable = TglAvailable.IsChecked.GetValueOrDefault()
            }

            Dim savedId As Integer
            If _editing Is Nothing Then
                savedId = MenuRepository.AddProduct(p)
            Else
                MenuRepository.UpdateProduct(p)
                savedId = p.Id
            End If

            ReloadAll(savedId)
        Catch ex As Exception
            MessageBox.Show("Could not save the item:" & vbCrLf & ex.Message,
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub BtnArchive_Click(sender As Object, e As RoutedEventArgs)
        If _editing Is Nothing Then Return

        Dim answer = MessageBox.Show(
            $"Archive '{_editing.Name}'?{vbCrLf}It will be removed from the menu and the order page. " &
            "You can restore it from the archive.",
            "Archive item", MessageBoxButton.YesNo, MessageBoxImage.Question)
        If answer <> MessageBoxResult.Yes Then Return

        MenuRepository.SetArchived(_editing.Id, True)
        ReloadAll()
        ClearForm()
    End Sub

    Private Sub Fail(message As String, target As Control)
        MessageBox.Show(message, "Menu item", MessageBoxButton.OK, MessageBoxImage.Warning)
        target.Focus()
    End Sub

End Class