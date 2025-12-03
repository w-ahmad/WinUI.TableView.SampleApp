using System;
using WinUI.TableView;

namespace WinUI.TableView.SampleApp.Helpers;

/// <summary>
/// Custom GroupDescription that integrates with GroupKeyFormatter for smart grouping.
/// </summary>
public class CustomGroupDescription : GroupDescription
{
    private readonly string _propertyName;
    private readonly Func<string, object?, object>? _groupKeyFormatter;

    /// <summary>
    /// Creates a new CustomGroupDescription.
    /// </summary>
    /// <param name="propertyName">Property to group by (e.g., "DateModified", "Size", "FileType")</param>
    /// <param name="groupKeyFormatter">Optional formatter function (e.g., GroupKeyFormatter.FormatGroupKey)</param>
    public CustomGroupDescription(string propertyName, Func<string, object?, object>? groupKeyFormatter = null)
    {
        _propertyName = propertyName;
        _groupKeyFormatter = groupKeyFormatter;
        PropertyName = propertyName;
    }

    /// <summary>
    /// Gets the group key for an item using the formatter if provided.
    /// </summary>
    public override object GetGroupKey(object? item)
    {
        if (item is not Adapters.IGroupableItem groupableItem)
        {
            return string.Empty;
        }

        // Get the raw property value
        var propertyValue = groupableItem.GetPropertyValue(_propertyName);

        // Apply formatter if provided (e.g., GroupKeyFormatter.FormatGroupKey)
        if (_groupKeyFormatter != null)
        {
            return _groupKeyFormatter(_propertyName, propertyValue) ?? string.Empty;
        }

        return propertyValue ?? string.Empty;
    }

    /// <summary>
    /// Compares two group keys for sorting.
    /// Uses the sort key logic from GroupKeyFormatter if available.
    /// </summary>
    public override int Compare(object? x, object? y)
    {
        // For date groups, we want reverse chronological (newest first)
        if (_propertyName == "DateModified")
        {
            var xStr = x?.ToString() ?? string.Empty;
            var yStr = y?.ToString() ?? string.Empty;
            var xKey = Adapters.GroupKeyFormatter.GetSortKey(_propertyName, xStr);
            var yKey = Adapters.GroupKeyFormatter.GetSortKey(_propertyName, yStr);
            
            // Reverse order for dates (Today before Yesterday)
            return Comparer<object>.Default.Compare(yKey, xKey);
        }

        // For other properties, use natural order
        return Comparer<object>.Default.Compare(x, y);
    }
}
