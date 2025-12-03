using System;

namespace WinUI.TableView.SampleApp.Adapters;

/// <summary>
/// Interface for items that can be grouped in hierarchical views.
/// Abstracts away the underlying data model (ExampleModel, IStorageItem, etc.)
/// </summary>
public interface IGroupableItem
{
    /// <summary>
    /// Gets the display name of the item.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a property value by name for grouping/filtering purposes.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The property value, or null if not found.</returns>
    object? GetPropertyValue(string propertyName);

    /// <summary>
    /// Gets the date the item was last modified.
    /// </summary>
    DateTimeOffset DateModified { get; }

    /// <summary>
    /// Gets the size of the item in bytes (0 for non-file items).
    /// </summary>
    ulong Size { get; }

    /// <summary>
    /// Gets the type of the item (e.g., "File", "Folder", "Person", etc.).
    /// </summary>
    string ItemType { get; }

    /// <summary>
    /// Gets the file extension (if applicable), or null.
    /// </summary>
    string? FileType { get; }

    /// <summary>
    /// Gets the underlying data object.
    /// </summary>
    object UnderlyingItem { get; }

    /// <summary>
    /// Gets a formatted display string for the size.
    /// </summary>
    string SizeDisplay { get; }

    /// <summary>
    /// Gets a formatted display string for the date modified.
    /// </summary>
    string DateModifiedDisplay { get; }

    /// <summary>
    /// Gets the icon glyph for the file/folder type.
    /// </summary>
    string FileIcon { get; }
}
