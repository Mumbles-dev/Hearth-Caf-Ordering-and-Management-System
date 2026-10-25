Imports System.IO
Imports System.Windows.Media
Imports System.Windows.Media.Imaging

Class MainWindow
    Private _orderView As OrderView
    Public Sub New()
        InitializeComponent()
        RefreshBranding()
        TxtDate.Text = DateTime.Now.ToString("dddd, MMM d, yyyy")
        NavOrder.IsChecked = True      ' fires Nav_Checked -> opens the Order page
    End Sub

    ' Loads the system name + logo from the Settings table.
    ' Public so the Settings page can call it after the user changes them (Step 11).
    Public Sub RefreshBranding()
        TxtBrand.Text = SettingsRepository.GetValue("SystemName", "Hearth")

        Dim logoPath = SettingsRepository.GetValue("LogoPath", "")
        If logoPath <> "" AndAlso File.Exists(logoPath) Then
            Dim bmp As New BitmapImage()
            bmp.BeginInit()
            bmp.CacheOption = BitmapCacheOption.OnLoad   ' don't keep the file locked
            bmp.UriSource = New Uri(logoPath)
            bmp.EndInit()
            LogoBox.Background = New ImageBrush(bmp) With {.Stretch = Stretch.UniformToFill}
            LogoGlyph.Visibility = Visibility.Collapsed
        Else
            LogoBox.Background = DirectCast(FindResource("PrimaryLightBrush"), Brush)
            LogoGlyph.Visibility = Visibility.Visible
        End If
    End Sub

    Private Sub Nav_Checked(sender As Object, e As RoutedEventArgs)
        Dim rb = TryCast(sender, RadioButton)
        If rb Is Nothing Then Return

        ' A fresh view each time, so data is reloaded whenever you open a page
        Select Case rb.Name
            Case "NavOrder"
                If _orderView Is Nothing Then
                    _orderView = New OrderView()
                Else
                    _orderView.ReloadProducts()
                End If
                ShowPage(_orderView, "Order")
            Case "NavOrders"
                ShowPage(New OrderManagementView(), "Order Management")
            Case "NavMenu"
                ShowPage(New MenuView(), "Menu Management")
            Case "NavDashboard"
                ShowPage(New DashboardView(), "Dashboard")
            Case "NavSettings"
                ShowPage(New SettingsView(), "Settings")
        End Select
    End Sub

    Private Sub ShowPage(view As UserControl, title As String)
        PageHost.Content = view
        TxtPageTitle.Text = title
    End Sub

    Private Sub BtnExit_Click(sender As Object, e As RoutedEventArgs)
        Dim answer = MessageBox.Show("Exit the ordering system?", "Exit",
                                     MessageBoxButton.YesNo, MessageBoxImage.Question)
        If answer = MessageBoxResult.Yes Then
            Application.Current.Shutdown()
        End If
    End Sub

End Class