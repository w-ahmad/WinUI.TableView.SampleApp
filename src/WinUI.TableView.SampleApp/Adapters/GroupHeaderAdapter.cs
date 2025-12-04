using System;

namespace WinUI.TableView.SampleApp.Adapters;

/// <summary>
/// Adapter for group header rows in hierarchical views.
/// </summary>
public class GroupHeaderAdapter : IGroupableItem
{
    private readonly string _groupKey;
    private readonly int _itemCount;

    public GroupHeaderAdapter(string groupKey, int itemCount)
    {
        _groupKey = groupKey ?? string.Empty;
        _itemCount = itemCount;
    }

    /// <inheritdoc/>
    public string Name => _groupKey;

    /// <inheritdoc/>
    public object? GetPropertyValue(string propertyName)
    {
        return propertyName switch
        {
            "Name" => Name,
            "GroupKey" => _groupKey,
            "ItemCount" => _itemCount,
            "IsGroupHeader" => true,
            _ => null
        };
    }

    /// <inheritdoc/>
    public DateTimeOffset DateModified => DateTimeOffset.MinValue;

    /// <inheritdoc/>
    public ulong Size => 0;

    /// <inheritdoc/>
    public string ItemType => "Group";

    /// <inheritdoc/>
    public string? FileType => null;

    /// <inheritdoc/>
    public object UnderlyingItem => this;

    /// <inheritdoc/>
    public string SizeDisplay => $"({_itemCount} items)";

    /// <inheritdoc/>
    public string DateModifiedDisplay => string.Empty;

    /// <inheritdoc/>
    public string FileIcon => "\uE8FD"; // Group icon

    /// <summary>
    /// Gets the group key.
    /// </summary>
    public string GroupKey => _groupKey;

    /// <summary>
    /// Gets the number of items in this group.
    /// </summary>
    public int ItemCount => _itemCount;

    /// <summary>
    /// Gets whether this is a group header (always true for this adapter).
    /// </summary>
    public bool IsGroupHeader => true;
}