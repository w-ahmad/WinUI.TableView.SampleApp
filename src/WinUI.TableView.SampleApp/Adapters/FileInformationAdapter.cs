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
}
