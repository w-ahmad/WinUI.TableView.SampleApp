using System;
using System.Threading.Tasks;
using Windows.Storage.BulkAccess;
using Windows.Storage.FileProperties;

namespace WinUI.TableView.SampleApp.Adapters;

/// <summary>
/// Adapter for WinRT FileInformation/FolderInformation (from BulkAccess) to implement IGroupableItem interface.
/// This is the high-performance adapter for large file collections.
/// FileInformation provides pre-loaded properties and thumbnail support.
/// </summary>
public class FileInformationAdapter : IGroupableItem
{
    private readonly FileInformation? _fileInfo;
    private readonly FolderInformation? _folderInfo;

    public FileInformationAdapter(FileInformation fileInfo)
    {
        _fileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
    }

    public FileInformationAdapter(FolderInformation folderInfo)
    {
        _folderInfo = folderInfo ?? throw new ArgumentNullException(nameof(folderInfo));
    }

    /// <summary>
    /// No async initialization needed - FileInformation provides lazy-loaded properties.
    /// </summary>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public string Name => _fileInfo?.Name ?? _folderInfo?.Name ?? string.Empty;

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
            "Path" => _fileInfo?.Path ?? _folderInfo?.Path,
            "DisplayName" => _fileInfo?.DisplayName ?? _folderInfo?.DisplayName,
            "DisplayType" => _fileInfo?.DisplayType ?? _folderInfo?.DisplayType,
            _ => null
        };
    }

    /// <inheritdoc/>
    public DateTimeOffset DateModified => 
        _fileInfo?.BasicProperties.DateModified ?? 
        _folderInfo?.BasicProperties.DateModified ?? 
        DateTimeOffset.MinValue;

    /// <inheritdoc/>
    public ulong Size => 
        _fileInfo?.BasicProperties.Size ?? 
        _folderInfo?.BasicProperties.Size ?? 
        0;

    /// <inheritdoc/>
    public string ItemType => _fileInfo is not null ? "File" : "Folder";

    /// <inheritdoc/>
    public string? FileType => _fileInfo?.FileType;

    /// <inheritdoc/>
    public object UnderlyingItem => (object?)_fileInfo ?? _folderInfo!;

    /// <summary>
    /// Gets the underlying FileInformation (if this is a file).
    /// </summary>
    public FileInformation? FileInfo => _fileInfo;

    /// <summary>
    /// Gets the underlying FolderInformation (if this is a folder).
    /// </summary>
    public FolderInformation? FolderInfo => _folderInfo;

    /// <summary>
    /// Gets the thumbnail for this item (if available).
    /// </summary>
    public StorageItemThumbnail? Thumbnail => _fileInfo?.Thumbnail ?? _folderInfo?.Thumbnail;

    // ✅ Display properties for UI binding
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
