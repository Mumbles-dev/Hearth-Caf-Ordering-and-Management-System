Imports System.Globalization
Imports System.Windows.Data

Public Class MoneyConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object,
                            culture As CultureInfo) As Object Implements IValueConverter.Convert
        If TypeOf value Is Decimal Then Return Money.ToText(CDec(value))
        Return ""
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object,
                                culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotSupportedException()
    End Function

End Class