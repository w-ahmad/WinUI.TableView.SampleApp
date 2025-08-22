using Microsoft.UI.Xaml.Data;

namespace WinUI.TableView.SampleApp.Converters;

public sealed class BoolToDoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b && b ? 1.0 : 0.0;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is double d && d > 0.5;
}
