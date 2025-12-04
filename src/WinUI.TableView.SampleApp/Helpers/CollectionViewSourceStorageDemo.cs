using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using WinUI.TableView.SampleApp.Adapters;

namespace WinUI.TableView.SampleApp.Pages;

/// <summary>
/// Helper class that demonstrates CollectionViewSource bridge functionality
/// with the existing StorageGroupingPage infrastructure.
/// </summary>
public class CollectionViewSourceStorageDemo
{
    private readonly ObservableCollection<IStorageItem> _storageItems;
    private readonly CollectionViewSource _collectionViewSource;
    private readonly CollectionViewSourceBridge _bridge;

    public CollectionViewSourceStorageDemo()
    {
        // Initialize CollectionViewSource and bridge
        _storageItems = new ObservableCollection<IStorageItem>();
        _collectionViewSource = new CollectionViewSource
        {
            Source = _storageItems,
            IsSourceGrouped = false
        };

        // Create bridge for automatic StorageItem adaptation
        _bridge = CollectionViewSourceBridgeFactory.CreateForStorageItems(_collectionViewSource);
    }

    /// <summary>
    /// Gets the adapted items collection that can be bound to TableView.
    /// </summary>
    public ObservableCollection<IGroupableItem> AdaptedItems => _bridge.AdaptedItems;

    /// <summary>
    /// Gets the underlying CollectionViewSource.
    /// </summary>
    public CollectionViewSource CollectionViewSource => _collectionViewSource;

    /// <summary>
    /// Gets the bridge for advanced operations.
    /// </summary>
    public CollectionViewSourceBridge Bridge => _bridge;

    /// <summary>
    /// Adds storage items to the collection (demonstrates automatic synchronization).
    /// </summary>
    public async Task AddStorageItemsAsync(StorageFolder folder, int maxItems = 50)
    {
        try
        {
            var files = await folder.GetFilesAsync();
            
            foreach (var file in files.Take(maxItems))
            {
                _storageItems.Add(file);
                // Bridge automatically creates StorageItemAdapter and adds to AdaptedItems
            }

            // Also add some folders
            var folders = await folder.GetFoldersAsync();
            foreach (var subfolder in folders.Take(5))
            {
                _storageItems.Add(subfolder);
            }
        }
        catch (Exception)
        {
            // Handle errors gracefully
        }
    }

    /// <summary>
    /// Demonstrates ListView-style grouping.
    /// </summary>
    public void ApplyGrouping(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            _bridge.ClearGrouping();
        }
        else
        {
            _bridge.SetGrouping(propertyName);
        }
    }

    /// <summary>
    /// Adds sample files for demonstration.
    /// </summary>
    public async Task AddSampleFilesAsync()
    {
        try
        {
            var tempFolder = ApplicationData.Current.TemporaryFolder;
            
            var sampleFiles = new[]
            {
                ("Sample_Document.pdf", "Sample document content"),
                ("Sample_Image.jpg", "Sample image content"),
                ("Sample_Video.mp4", "Sample video content"),
                ("Sample_Audio.mp3", "Sample audio content"),
                ("Sample_Archive.zip", "Sample archive content")
            };

            foreach (var (fileName, content) in sampleFiles)
            {
                try
                {
                    var file = await tempFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(file, content);
                    
                    // Add to collection - bridge automatically syncs
                    _storageItems.Add(file);
                }
                catch
                {
                    // Ignore individual file creation errors
                }
            }
        }
        catch (Exception)
        {
            // Handle errors gracefully
        }
    }

    /// <summary>
    /// Removes items by filter predicate.
    /// </summary>
    public void RemoveItemsWhere(Func<IStorageItem, bool> predicate)
    {
        var itemsToRemove = _storageItems.Where(predicate).ToList();
        foreach (var item in itemsToRemove)
        {
            _storageItems.Remove(item);
            // Bridge automatically syncs removal
        }
    }

    /// <summary>
    /// Clears all items.
    /// </summary>
    public void Clear()
    {
        _storageItems.Clear();
        // Bridge automatically syncs clearing
    }

    /// <summary>
    /// Gets collection statistics.
    /// </summary>
    public CollectionStats GetStats()
    {
        var stats = new CollectionStats
        {
            TotalItems = _storageItems.Count,
            AdaptedItems = _bridge.AdaptedItems.Count,
            GroupHeaders = _bridge.AdaptedItems.Count(item => item is GroupHeaderAdapter),
            CurrentGroupProperty = _bridge.CurrentGroupProperty,
            IsGrouped = _bridge.SupportsGrouping && !string.IsNullOrEmpty(_bridge.CurrentGroupProperty)
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
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        _bridge?.Dispose();
    }
}

/// <summary>
/// Statistics about the collection and bridge state.
/// </summary>
public class CollectionStats
{
    public int TotalItems { get; set; }
    public int AdaptedItems { get; set; }
    public int GroupHeaders { get; set; }
    public string? CurrentGroupProperty { get; set; }
    public bool IsGrouped { get; set; }
    public Dictionary<string, int> FileTypeCounts { get; set; } = [];
    public int FolderCount { get; set; }

    public override string ToString()
    {
        var grouped = IsGrouped ? $" (Grouped by {CurrentGroupProperty})" : "";
        return $"Items: {TotalItems}, Adapted: {AdaptedItems}, Groups: {GroupHeaders}, Folders: {FolderCount}{grouped}";
    }
}