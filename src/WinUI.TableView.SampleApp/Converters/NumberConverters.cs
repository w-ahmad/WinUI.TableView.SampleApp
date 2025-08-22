using Microsoft.UI.Xaml.Data;
using System;

namespace WinUI.TableView.SampleApp.Converters;

public sealed class DoubleToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is null) return string.Empty;
        if (!double.TryParse(value.ToString(), out var d)) return value.ToString();
        var format = parameter as string ?? "G";
        return d.ToString(format);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is string s && double.TryParse(s, out var d)) return d;
        return 0d;
    }
}
