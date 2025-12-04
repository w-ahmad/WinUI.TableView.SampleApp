using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace WinUI.TableView.SampleApp.Adapters;

/// <summary>
/// Enhanced StorageItemAdapter that supports Windows Property System for grouping detection.
/// Uses PKEY_IsGroup and other property keys to determine grouping capabilities.
/// </summary>
public class PropertyAwareStorageItemAdapter : StorageItemAdapter
{
    private readonly IStorageItem _item;
    private Dictionary<string, object?>? _extendedProperties;
    private bool _propertiesLoaded;

    public PropertyAwareStorageItemAdapter(IStorageItem item) : base(item)
    {
        _item = item ?? throw new ArgumentNullException(nameof(item));
    }

    /// <summary>
    /// Loads extended properties including Windows Property System keys.
    /// </summary>
    public async Task LoadExtendedPropertiesAsync()
    {
        if (_propertiesLoaded)
        {
            return;
        }

        _extendedProperties = new Dictionary<string, object?>();

        try
        {
            // Load basic properties first
            await InitializeAsync();

            // Load Windows Property System properties if this is a folder
            if (_item is StorageFolder folder)
            {
                await LoadFolderPropertiesAsync(folder);
            }
            else if (_item is StorageFile file)
            {
                await LoadFilePropertiesAsync(file);
            }
        }
        catch (Exception)
        {
            // If property loading fails, continue with basic properties
            _extendedProperties ??= new Dictionary<string, object?>();
        }
        finally
        {
            _propertiesLoaded = true;
        }
    }

    private async Task LoadFolderPropertiesAsync(StorageFolder folder)
    {
        try
        {
            var propertiesToRetrieve = new List<string>
            {
                "System.IsGroup",           // PKEY_IsGroup - indicates if folder supports grouping
                "System.Kind",              // PKEY_Kind - folder kind (Pictures, Music, etc.)
                "System.Category",          // PKEY_Category - category of folder
                "System.ItemType",          // PKEY_ItemType - type of items in folder
                "System.FolderKind",        // PKEY_FolderKind - special folder designation
                "System.ParentFolder",      // PKEY_ParentFolder - parent folder info
                "System.ItemAuthors"        // PKEY_ItemAuthors - for grouping by author
            };

            var properties = await folder.Properties.RetrievePropertiesAsync(propertiesToRetrieve);

            foreach (var kvp in properties)
            {
                _extendedProperties[kvp.Key] = kvp.Value;
            }
        }
        catch (Exception)
        {
            // Some properties might not be available - continue gracefully
        }
    }

    private async Task LoadFilePropertiesAsync(StorageFile file)
    {
        try
        {
            var propertiesToRetrieve = new List<string>
            {
                "System.Kind",              // PKEY_Kind - file kind
                "System.Category",          // PKEY_Category - category
                "System.Rating",            // PKEY_Rating - for grouping by rating
                "System.ItemAuthors",       // PKEY_ItemAuthors - for grouping by author
                "System.Keywords",          // PKEY_Keywords - for grouping by keywords
                "System.ContentType",       // PKEY_ContentType - MIME type
                "System.PerceivedType"      // PKEY_PerceivedType - perceived file type
            };

            var properties = await file.Properties.RetrievePropertiesAsync(propertiesToRetrieve);

            foreach (var kvp in properties)
            {
                _extendedProperties[kvp.Key] = kvp.Value;
            }
        }
        catch (Exception)
        {
            // Some properties might not be available - continue gracefully
        }
    }

    /// <summary>
    /// Gets whether this folder/location supports grouping based on PKEY_IsGroup.
    /// </summary>
    public bool SupportsGrouping
    {
        get
        {
            if (!_propertiesLoaded || _extendedProperties == null)
            {
                // Default to true if properties aren't loaded yet
                return true;
            }

            // Check PKEY_IsGroup first
            if (_extendedProperties.TryGetValue("System.IsGroup", out var isGroupValue))
            {
                if (isGroupValue is bool boolValue)
                {
                    return boolValue;
                }
            }

            // Fall back to heuristics for special folder types
            if (_item is StorageFolder)
            {
                if (_extendedProperties.TryGetValue("System.FolderKind", out var folderKindValue))
                {
                    // Special folders like Pictures, Music, Videos typically support grouping
                    var folderKind = folderKindValue?.ToString()?.ToUpperInvariant();
                    return folderKind switch
                    {
                        "PICTURES" => true,
                        "MUSIC" => true,
                        "VIDEOS" => true,
                        "DOCUMENTS" => true,
                        "DOWNLOADS" => true,
                        _ => true // Default to supporting grouping for unknown folder types
                    };
                }
            }

            // Default to supporting grouping
            return true;
        }
    }

    /// <summary>
    /// Gets available grouping properties based on folder content and Windows Property System.
    /// </summary>
    public IEnumerable<string> GetAvailableGroupingProperties()
    {
        if (!SupportsGrouping)
        {
            yield break;
        }

        // Always available basic properties
        yield return "Name";
        yield return "DateModified";
        yield return "Size";
        yield return "ItemType";
        yield return "FileType";

        if (!_propertiesLoaded || _extendedProperties == null)
        {
            yield break;
        }

        // Property-based grouping options
        if (_extendedProperties.ContainsKey("System.ItemAuthors"))
        {
            yield return "Authors";
        }

        if (_extendedProperties.ContainsKey("System.Category"))
        {
            yield return "Category";
        }

        if (_extendedProperties.ContainsKey("System.Keywords"))
        {
            yield return "Keywords";
        }

        if (_extendedProperties.ContainsKey("System.Rating"))
        {
            yield return "Rating";
        }

        // Folder-specific properties
        if (_item is StorageFolder && _extendedProperties.TryGetValue("System.FolderKind", out var folderKind))
        {
            var kind = folderKind?.ToString()?.ToUpperInvariant();
            switch (kind)
            {
                case "PICTURES":
                    yield return "DateTaken";
                    yield return "CameraModel";
                    yield return "Dimensions";
                    break;
                case "MUSIC":
                    yield return "Artist";
                    yield return "Album";
                    yield return "Genre";
                    yield return "Year";
                    break;
                case "VIDEOS":
                    yield return "Length";
                    yield return "Resolution";
                    break;
            }
        }
    }

    /// <summary>
    /// Enhanced property value retrieval that includes Windows Property System properties.
    /// </summary>
    public new object? GetPropertyValue(string propertyName)
    {
        // First try base implementation
        var baseValue = base.GetPropertyValue(propertyName);
        if (baseValue != null)
        {
            return baseValue;
        }

        // Try extended properties
        return GetExtendedPropertyValue(propertyName);
    }

    /// <summary>
    /// Gets property values from the Windows Property System.
    /// </summary>
    public object? GetExtendedPropertyValue(string propertyName)
    {
        if (_propertiesLoaded && _extendedProperties != null)
        {
            // Map common property names to Windows Property System keys
            var systemPropertyKey = propertyName switch
            {
                "Authors" => "System.ItemAuthors",
                "Category" => "System.Category",
                "Keywords" => "System.Keywords",
                "Rating" => "System.Rating",
                "Kind" => "System.Kind",
                "PerceivedType" => "System.PerceivedType",
                "ContentType" => "System.ContentType",
                "FolderKind" => "System.FolderKind",
                _ => propertyName
            };

            if (_extendedProperties.TryGetValue(systemPropertyKey, out var value))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the Windows Property System key value directly.
    /// </summary>
    public object? GetSystemPropertyValue(string propertyKey)
    {
        if (_propertiesLoaded && _extendedProperties != null)
        {
            _extendedProperties.TryGetValue(propertyKey, out var value);
            return value;
        }
        return null;
    }

    /// <summary>
    /// Gets whether extended properties have been loaded.
    /// </summary>
    public bool AreExtendedPropertiesLoaded => _propertiesLoaded;

    /// <summary>
    /// Gets the folder kind if this is a folder.
    /// </summary>
    public string? FolderKind => GetSystemPropertyValue("System.FolderKind")?.ToString();

    /// <summary>
    /// Gets whether this is a special/known folder type.
    /// </summary>
    public bool IsSpecialFolder => !string.IsNullOrEmpty(FolderKind);
}