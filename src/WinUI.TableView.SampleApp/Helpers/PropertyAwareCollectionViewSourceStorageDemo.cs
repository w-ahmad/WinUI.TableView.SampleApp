using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using WinUI.TableView.SampleApp.Adapters;

namespace WinUI.TableView.SampleApp.Helpers;

/// <summary>
/// Enhanced storage demo that respects Windows Property System grouping capabilities.
/// Only enables grouping when the folder supports it via PKEY_IsGroup or known folder types.
/// </summary>
public class PropertyAwareCollectionViewSourceStorageDemo
{
    private readonly ObservableCollection<IStorageItem> _storageItems;
    private readonly CollectionViewSource _collectionViewSource;
    private readonly PropertyAwareCollectionViewSourceBridge _bridge;

    public PropertyAwareCollectionViewSourceStorageDemo()
    {
        _storageItems = new ObservableCollection<IStorageItem>();
        _collectionViewSource = new CollectionViewSource
        {
            Source = _storageItems,
            IsSourceGrouped = false
        };

        _bridge = PropertyAwareCollectionViewSourceBridgeFactory.CreateForStorageItems(_collectionViewSource);
    }

    /// <summary>
    /// Gets the adapted items collection that can be bound to TableView.
    /// </summary>
    public ObservableCollection<IGroupableItem> AdaptedItems => _bridge.AdaptedItems;

    /// <summary>
    /// Gets the property-aware bridge for advanced operations.
    /// </summary>
    public PropertyAwareCollectionViewSourceBridge Bridge => _bridge;

    /// <summary>
    /// Gets whether grouping is enabled based on folder properties.
    /// </summary>
    public bool GroupingEnabled => _bridge.GroupingEnabled;

    /// <summary>
    /// Gets available grouping properties for the current folder.
    /// </summary>
    public string[] AvailableGroupingProperties => _bridge.AvailableGroupingProperties.ToArray();

    /// <summary>
    /// Gets the current source folder.
    /// </summary>
    public StorageFolder? SourceFolder => _bridge.SourceFolder;

    /// <summary>
    /// Loads storage items from a folder and analyzes its grouping capabilities.
    /// </summary>
    public async Task<LoadResult> LoadStorageItemsAsync(StorageFolder folder, int maxItems = 50)
    {
        var result = new LoadResult();
        
        try
        {
            // Set the source folder and analyze its properties
            await _bridge.SetSourceFolderAsync(folder);
            result.FolderAnalyzed = true;
            result.GroupingEnabled = _bridge.GroupingEnabled;
            result.SourceFolder = folder.Path;

            // Clear existing items
            _storageItems.Clear();

            // Load files
            var files = await folder.GetFilesAsync();
            var fileCount = 0;
            foreach (var file in files.Take(maxItems))
            {
                _storageItems.Add(file);
                fileCount++;
            }
            result.FilesLoaded = fileCount;

            // Load folders (limit to prevent overwhelming)
            var folders = await folder.GetFoldersAsync();
            var folderCount = 0;
            foreach (var subfolder in folders.Take(Math.Min(10, maxItems - fileCount)))
            {
                _storageItems.Add(subfolder);
                folderCount++;
            }
            result.FoldersLoaded = folderCount;

            result.TotalItems = fileCount + folderCount;
            result.Success = true;

            // Get available grouping properties after loading
            result.AvailableGroupingProperties = AvailableGroupingProperties;
        }
        catch (UnauthorizedAccessException ex)
        {
            result.ErrorMessage = "Access denied. Please grant permission to access this folder.";
            result.Exception = ex;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Error loading folder: {ex.Message}";
            result.Exception = ex;
        }

        return result;
    }

    /// <summary>
    /// Applies grouping if supported by the current folder.
    /// </summary>
    public async Task<GroupingResult> ApplyGroupingAsync(string propertyName)
    {
        var result = new GroupingResult
        {
            PropertyName = propertyName,
            WasRequested = true
        };

        if (string.IsNullOrEmpty(propertyName))
        {
            _bridge.ClearGrouping();
            result.Applied = true;
            result.Message = "Grouping cleared";
            return result;
        }

        if (!_bridge.GroupingEnabled)
        {
            result.Applied = false;
            result.Message = "Grouping not supported by this folder (PKEY_IsGroup = false)";
            return result;
        }

        var available = AvailableGroupingProperties;
        if (!available.Contains(propertyName))
        {
            result.Applied = false;
            result.Message = $"Property '{propertyName}' not available for grouping in this folder type";
            result.AvailableProperties = available;
            return result;
        }

        var success = await _bridge.SetGroupingAsync(propertyName);
        result.Applied = success;
        result.Message = success 
            ? $"Grouped by {propertyName}"
            : $"Failed to group by {propertyName}";

        return result;
    }

    /// <summary>
    /// Forces grouping even if not supported by folder properties.
    /// </summary>
    public GroupingResult ForceGrouping(string propertyName)
    {
        _bridge.ForceEnableGrouping();
        _bridge.SetGrouping(propertyName);

        return new GroupingResult
        {
            PropertyName = propertyName,
            WasRequested = true,
            Applied = true,
            Message = $"Forced grouping by {propertyName} (bypassed PKEY_IsGroup)",
            WasForced = true
        };
    }

    /// <summary>
    /// Adds sample files for testing.
    /// </summary>
    public async Task<int> AddSampleFilesAsync()
    {
        try
        {
            var tempFolder = ApplicationData.Current.TemporaryFolder;
            var addedCount = 0;

            var sampleFiles = new[]
            {
                ("Property_Test_Document.pdf", "Document content"),
                ("Property_Test_Image.jpg", "Image content"),
                ("Property_Test_Video.mp4", "Video content"),
                ("Property_Test_Audio.mp3", "Audio content")
            };

            foreach (var (fileName, content) in sampleFiles)
            {
                try
                {
                    var file = await tempFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(file, content);
                    _storageItems.Add(file);
                    addedCount++;
                }
                catch
                {
                    // Ignore individual file creation errors
                }
            }

            return addedCount;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets detailed statistics about the current state.
    /// </summary>
    public DetailedStats GetDetailedStats()
    {
        var stats = new DetailedStats
        {
            TotalSourceItems = _storageItems.Count,
            AdaptedItems = _bridge.AdaptedItems.Count,
            GroupHeaders = _bridge.AdaptedItems.Count(item => item is GroupHeaderAdapter),
            CurrentGroupProperty = _bridge.CurrentGroupProperty,
            GroupingEnabled = _bridge.GroupingEnabled,
            IsGrouped = _bridge.SupportsGrouping && !string.IsNullOrEmpty(_bridge.CurrentGroupProperty),
            AvailableGroupingProperties = AvailableGroupingProperties,
            SourceFolderPath = _bridge.SourceFolder?.Path
        };

        // Count by file types
        var fileTypes = _storageItems
            .OfType<StorageFile>()
            .GroupBy(f => f.FileType)
            .ToDictionary(g => g.Key, g => g.Count());

        stats.FileTypeCounts = fileTypes;
        stats.FolderCount = _storageItems.Count(item => item is StorageFolder);

        return stats;
    }

    /// <summary>
    /// Clears all items.
    /// </summary>
    public void Clear()
    {
        _storageItems.Clear();
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        _bridge?.Dispose();
    }
}

/// <summary>
/// Result of loading storage items from a folder.
/// </summary>
public class LoadResult
{
    public bool Success { get; set; }
    public bool FolderAnalyzed { get; set; }
    public bool GroupingEnabled { get; set; }
    public string? SourceFolder { get; set; }
    public int FilesLoaded { get; set; }
    public int FoldersLoaded { get; set; }
    public int TotalItems { get; set; }
    public string[]? AvailableGroupingProperties { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }

    public override string ToString()
    {
        if (!Success)
        {
            return $"Failed: {ErrorMessage}";
        }

        var groupingStatus = GroupingEnabled ? "Grouping Enabled" : "Grouping Disabled (PKEY_IsGroup)";
        return $"Loaded {TotalItems} items ({FilesLoaded} files, {FoldersLoaded} folders) - {groupingStatus}";
    }
}

/// <summary>
/// Result of applying grouping.
/// </summary>
public class GroupingResult
{
    public bool WasRequested { get; set; }
    public bool Applied { get; set; }
    public bool WasForced { get; set; }
    public string? PropertyName { get; set; }
    public string? Message { get; set; }
    public string[]? AvailableProperties { get; set; }

    public override string ToString()
    {
        return Message ?? (Applied ? "Grouping applied" : "Grouping failed");
    }
}

/// <summary>
/// Detailed statistics about the collection and property system state.
/// </summary>
public class DetailedStats
{
    public int TotalSourceItems { get; set; }
    public int AdaptedItems { get; set; }
    public int GroupHeaders { get; set; }
    public string? CurrentGroupProperty { get; set; }
    public bool GroupingEnabled { get; set; }
    public bool IsGrouped { get; set; }
    public string[]? AvailableGroupingProperties { get; set; }
    public string? SourceFolderPath { get; set; }
    public Dictionary<string, int> FileTypeCounts { get; set; } = [];
    public int FolderCount { get; set; }

    public override string ToString()
    {
        var groupingStatus = GroupingEnabled ? 
            (IsGrouped ? $"Grouped by {CurrentGroupProperty}" : "Grouping Available") : 
            "Grouping Disabled";
        
        var availableProps = AvailableGroupingProperties?.Length ?? 0;
        return $"Items: {TotalSourceItems}, Adapted: {AdaptedItems}, Groups: {GroupHeaders}, " +
               $"Folders: {FolderCount}, Available Properties: {availableProps} - {groupingStatus}";
    }
}