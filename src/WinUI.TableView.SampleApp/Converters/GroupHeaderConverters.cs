using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using WinUI.TableView.SampleApp.Adapters;

namespace WinUI.TableView.SampleApp.Converters;

/// <summary>
/// Converter that determines if an IGroupableItem is a group header.
/// </summary>
public class IsGroupHeaderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isGroupHeader = false;

        if (value is GroupHeaderAdapter)
        {
            isGroupHeader = true;
        }
        else if (value is IGroupableItem item)
        {
            // Check if the item has the IsGroupHeader property
            var isGroupHeaderValue = item.GetPropertyValue("IsGroupHeader");
            if (isGroupHeaderValue is bool boolValue)
            {
                isGroupHeader = boolValue;
            }
        }

        // Return Visibility if requested, otherwise return bool
        if (parameter?.ToString() == "Visibility")
        {
            return isGroupHeader ? Visibility.Visible : Visibility.Collapsed;
        }

        return isGroupHeader;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter that inverts IsGroupHeaderConverter result.
/// </summary>
public class IsNotGroupHeaderConverter : IValueConverter
{
    private static readonly IsGroupHeaderConverter _innerConverter = new();

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var result = _innerConverter.Convert(value, targetType, null, language);
        
        if (result is bool boolValue)
        {
            bool isNotGroupHeader = !boolValue;
            
            // Return Visibility if requested, otherwise return bool
            if (parameter?.ToString() == "Visibility")
            {
                return isNotGroupHeader ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return isNotGroupHeader;
        }
        
        // Default to true (not a group header) if not a bool
        if (parameter?.ToString() == "Visibility")
        {
            return Visibility.Visible;
        }
        
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}