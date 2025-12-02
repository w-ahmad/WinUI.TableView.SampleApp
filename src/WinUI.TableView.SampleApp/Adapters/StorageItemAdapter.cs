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
}
