using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace WinUI.TableView.SampleApp.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility v && v == Visibility.Visible;
}

public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b && !b ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility v && v == Visibility.Collapsed;
}

public sealed class ExpandGlyphConverter : IValueConverter
{
    // Use Segoe MDL2 Assets chevrons: Right (E76C) when collapsed, Down (E70D) when expanded
    private const string ChevronRight = "\uE76C"; // more consistent right chevron
    private const string ChevronDown  = "\uE70D";

    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b && b ? ChevronDown : ChevronRight;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => string.Equals(value as string, ChevronDown, StringComparison.Ordinal);
}
