using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace WinUI.TableView.SampleApp.Adapters;

/// <summary>
/// Adapter for WinRT IStorageItem (files and folders) to implement IGroupableItem interface.
/// Allows storage items to work with the generic grouping system.
/// </summary>
public class StorageItemAdapter : IGroupableItem
{
    private readonly IStorageItem _item;
    private BasicProperties? _properties;
    private bool _isInitialized;

    public StorageItemAdapter(IStorageItem item)
    {
        _item = item ?? throw new ArgumentNullException(nameof(item));
    }

    /// <summary>
    /// Initializes the adapter by loading properties asynchronously.
    /// Must be called before accessing Size or DateModified properties.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        _properties = await _item.GetBasicPropertiesAsync();
        _isInitialized = true;
    }

    /// <inheritdoc/>
    public string Name => _item.Name;

    /// <inheritdoc/>
    public object? GetPropertyValue(string propertyName)
    {
        return propertyName switch
        {
            "Name" => Name,
            "DateModified" => DateModified,
            "Size" => Size,
            "ItemType" => ItemType,
            "FileType" => FileType,
            "Path" => _item.Path,
            _ => null
        };
    }

    /// <inheritdoc/>
    public DateTimeOffset DateModified => _properties?.DateModified ?? DateTimeOffset.MinValue;

    /// <inheritdoc/>
    public ulong Size => _properties?.Size ?? 0;

    /// <inheritdoc/>
    public string ItemType => _item is StorageFile ? "File" : "Folder";

    /// <inheritdoc/>
    public string? FileType => (_item as StorageFile)?.FileType;

    /// <inheritdoc/>
    public object UnderlyingItem => _item;

    /// <summary>
    /// Gets the underlying IStorageItem.
    /// </summary>
    public IStorageItem StorageItem => _item;

    /// <summary>
    /// Gets whether this adapter has been initialized.
    /// </summary>
    public bool IsInitialized => _isInitialized;

    // Display properties for UI binding
    public string SizeDisplay => FormatSize(Size);
    public string DateModifiedDisplay => DateModified.ToString("g");
    public string FileIcon => GetFileIcon();

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
        if (ItemType == "Folder") return "\uE8B7";
        
        var fileType = FileType?.ToLowerInvariant() ?? string.Empty;
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
