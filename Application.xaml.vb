Class Application
    Protected Overrides Sub OnStartup(e As StartupEventArgs)
        Db.Initialize()
        SampleData.Seed()        ' TEMPORARY: remove when Menu Management is done
        MyBase.OnStartup(e)
    End Sub

End Class