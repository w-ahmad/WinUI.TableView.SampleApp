using System;

namespace WinUI.TableView.SampleApp.Adapters;

/// <summary>
/// Generic adapter for any object type that doesn't have a specific adapter.
/// Provides basic property access through reflection.
/// </summary>
public class GenericObjectAdapter : IGroupableItem
{
    private readonly object _item;
    private readonly Type _itemType;

    public GenericObjectAdapter(object item)
    {
        _item = item ?? throw new ArgumentNullException(nameof(item));
        _itemType = _item.GetType();
    }

    /// <inheritdoc/>
    public string Name => GetStringProperty("Name") ?? 
                         GetStringProperty("Title") ?? 
                         GetStringProperty("DisplayName") ?? 
                         _item.ToString() ?? 
                         "Unknown";

    /// <inheritdoc/>
    public object? GetPropertyValue(string propertyName)
    {
        try
        {
            var property = _itemType.GetProperty(propertyName);
            return property?.GetValue(_item);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public DateTimeOffset DateModified => GetDateTimeOffsetProperty("DateModified") ?? 
                                         GetDateTimeOffsetProperty("LastWriteTime") ?? 
                                         GetDateTimeOffsetProperty("ModifiedDate") ?? 
                                         DateTimeOffset.MinValue;

    /// <inheritdoc/>
    public ulong Size => GetULongProperty("Size") ?? 
                        GetULongProperty("Length") ?? 
                        GetULongProperty("FileSize") ?? 
                        0;

    /// <inheritdoc/>
    public string ItemType => GetStringProperty("ItemType") ?? 
                             GetStringProperty("Type") ?? 
                             GetStringProperty("Kind") ?? 
                             _itemType.Name;

    /// <inheritdoc/>
    public string? FileType => GetStringProperty("FileType") ?? 
                              GetStringProperty("Extension") ?? 
                              GetStringProperty("FileExtension");

    /// <inheritdoc/>
    public object UnderlyingItem => _item;

    /// <inheritdoc/>
    public string SizeDisplay => FormatSize(Size);

    /// <inheritdoc/>
    public string DateModifiedDisplay => DateModified.ToString("g");

    /// <inheritdoc/>
    public string FileIcon => GetFileIcon();

    private string? GetStringProperty(string propertyName)
    {
        try
        {
            var property = _itemType.GetProperty(propertyName);
            return property?.GetValue(_item)?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private DateTimeOffset? GetDateTimeOffsetProperty(string propertyName)
    {
        try
        {
            var property = _itemType.GetProperty(propertyName);
            var value = property?.GetValue(_item);
            
            return value switch
            {
                DateTimeOffset dto => dto,
                DateTime dt => new DateTimeOffset(dt),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private ulong? GetULongProperty(string propertyName)
    {
        try
        {
            var property = _itemType.GetProperty(propertyName);
            var value = property?.GetValue(_item);
            
            return value switch
            {
                ulong ul => ul,
                long l when l >= 0 => (ulong)l,
                int i when i >= 0 => (ulong)i,
                uint ui => ui,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private string FormatSize(ulong bytes)
    {
        if (bytes == 0) return "0 bytes";
        string[] sizes = { "bytes", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }

    private string GetFileIcon()
    {
        var fileType = FileType?.ToLowerInvariant() ?? string.Empty;
        var itemType = ItemType?.ToLowerInvariant() ?? string.Empty;

        if (itemType.Contains("folder") || itemType.Contains("directory"))
        {
            return "\uE8B7"; // Folder icon
        }
        
        return fileType switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "\uEB9F", // Image
            ".mp4" or ".avi" or ".mkv" or ".mov" => "\uE8B2", // Video
            ".mp3" or ".wav" or ".flac" => "\uE8D6", // Music
            ".zip" or ".rar" or ".7z" => "\uE8B5", // Archive
            ".exe" or ".msi" => "\uE756", // Application
            ".pdf" => "\uE8A5", // PDF
            _ => "\uE8A5" // Default document
        };
    }
}