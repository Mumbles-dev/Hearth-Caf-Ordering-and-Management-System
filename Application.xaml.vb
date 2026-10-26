Class Application
    Protected Overrides Sub OnStartup(e As StartupEventArgs)
        Db.Initialize()
        SampleData.Seed()
        MyBase.OnStartup(e)
    End Sub

End Class