using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;
using WinUI.TableView.SampleApp.Adapters;

namespace WinUI.TableView.SampleApp.Pages;

/// <summary>
/// Represents a flattened item for file system hierarchical grouping.
/// </summary>
public sealed class StorageFlatGroupItem : INotifyPropertyChanged
{
    private bool _isExpanded = true;

    public StorageFlatGroupItem(IGroupableItem? item, int depth, bool isGroupHeader, string? groupKey = null)
    {
        Item = item;
        Depth = depth;
        IsGroupHeader = isGroupHeader;
        GroupKey = groupKey;
    }

    public IGroupableItem? Item { get; }
    public int Depth { get; }
    public bool IsGroupHeader { get; }
    public string? GroupKey { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ChevronGlyph));
            }
        }
    }

    public Thickness Indent => new(left: Depth * 24, top: 0, right: 0, bottom: 0);
    public string ChevronGlyph => IsExpanded ? "\uE70E" : "\uE76C";

    // Display properties for data rows
    public string FileName => Item?.Name ?? string.Empty;
    public string FileIcon => Item?.ItemType == "Folder" ? "\uE8B7" : GetFileIcon(Item?.FileType);
    public string ItemTypeDisplay => IsGroupHeader ? string.Empty : Item?.ItemType ?? string.Empty;
    public string SizeDisplay => IsGroupHeader ? string.Empty : FormatSize(Item?.Size ?? 0);
    public string DateModifiedDisplay => IsGroupHeader ? string.Empty : Item?.DateModified.ToString("g") ?? string.Empty;
    public string FileExtension => IsGroupHeader ? string.Empty : Item?.FileType ?? string.Empty;

    private static string GetFileIcon(string? fileType)
    {
        if (string.IsNullOrEmpty(fileType)) return "\uE8A5"; // Document

        return fileType.ToLowerInvariant() switch
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

    private static string FormatSize(ulong bytes)
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

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Demonstrates file system grouping using WinRT Storage APIs.
/// </summary>
public sealed partial class StorageGroupingPage : Page, INotifyPropertyChanged
{
    public ObservableCollection<StorageFlatGroupItem> FlatItems { get; } = [];
    
    private List<IGroupableItem> _sourceItems = [];
    private string _currentGroupProperty = "DateModified";
    private string? _sortPropertyName;
    private SortDirection? _sortDirection;
    
    private bool _isLoading;
    private string _loadingStatus = string.Empty;
    private string _statusMessage = "Click 'Load Files' to browse a folder";
    private string _itemCountText = string.Empty;

    public StorageGroupingPage()
    {
        InitializeComponent();
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
    }

    public string LoadingStatus
    {
        get => _loadingStatus;
        set
        {
            if (_loadingStatus != value)
            {
                _loadingStatus = value;
                OnPropertyChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public string ItemCountText
    {
        get => _itemCountText;
        set
        {
            if (_itemCountText != value)
            {
                _itemCountText = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Loads files from the selected known folder.
    /// </summary>
    private async void OnLoadFiles(object sender, RoutedEventArgs e)
    {
        await LoadFilesAsync();
    }

    private async Task LoadFilesAsync()
    {
        if (FolderComboBox.SelectedItem is not ComboBoxItem selectedFolder)
        {
            return;
        }

        IsLoading = true;
        LoadingStatus = "Loading files...";
        _sourceItems.Clear();
        FlatItems.Clear();

        try
        {
            var folderName = selectedFolder.Tag?.ToString() ?? "Downloads";
            var folder = await GetKnownFolderAsync(folderName);

            if (folder == null)
            {
                StatusMessage = $"Could not access {folderName} folder";
                return;
            }

            LoadingStatus = $"Scanning {folderName}...";

            // Use QueryOptions for better performance with large folders
            var queryOptions = new QueryOptions(CommonFileQuery.OrderByName, [])
            {
                FolderDepth = FolderDepth.Shallow,
                IndexerOption = IndexerOption.UseIndexerWhenAvailable
            };

            var query = folder.CreateFileQueryWithOptions(queryOptions);
            var files = await query.GetFilesAsync();

            LoadingStatus = $"Loading {files.Count} file properties...";

            // Load files with progress updates
            var adapters = new List<IGroupableItem>();
            int processed = 0;
            
            foreach (var file in files)
            {
                var adapter = new StorageItemAdapter(file);
                await adapter.InitializeAsync();
                adapters.Add(adapter);
                
                processed++;
                if (processed % 50 == 0)
                {
                    LoadingStatus = $"Loaded {processed}/{files.Count} files...";
                }
            }

            _sourceItems = adapters;
            RebuildGroupedView();

            StatusMessage = $"Loaded {files.Count} files from {folderName}";
            ItemCountText = $"{files.Count} items";
        }
        catch (UnauthorizedAccessException)
        {
            StatusMessage = "Access denied. Please grant permission to access this folder.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<StorageFolder?> GetKnownFolderAsync(string folderName)
    {
        try
        {
            return folderName switch
            {
                "Downloads" => await KnownFolders.GetFolderForUserAsync(null, KnownFolderId.DownloadsFolder),
                "Documents" => await KnownFolders.GetFolderForUserAsync(null, KnownFolderId.DocumentsLibrary),
                "Pictures" => await KnownFolders.GetFolderForUserAsync(null, KnownFolderId.PicturesLibrary),
                "Videos" => await KnownFolders.GetFolderForUserAsync(null, KnownFolderId.VideosLibrary),
                "Music" => await KnownFolders.GetFolderForUserAsync(null, KnownFolderId.MusicLibrary),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Rebuilds the flattened view based on current grouping property.
    /// </summary>
    private void RebuildGroupedView()
    {
        if (_sourceItems.Count == 0)
        {
            return;
        }

        // Remember expanded groups
        var expandedGroups = FlatItems
            .Where(f => f.IsGroupHeader && f.IsExpanded)
            .Select(f => f.GroupKey)
            .ToHashSet();

        FlatItems.Clear();

        if (string.IsNullOrEmpty(_currentGroupProperty))
        {
            // No grouping - show all items flat
            var sortedItems = ApplySorting(_sourceItems);
            foreach (var item in sortedItems)
            {
                FlatItems.Add(new StorageFlatGroupItem(item, depth: 0, isGroupHeader: false));
            }
            return;
        }

        // Group items by property
        var groups = GroupItemsByProperty(_sourceItems, _currentGroupProperty);

        // Flatten groups into view
        var orderedGroups = groups.OrderBy(g => GroupKeyFormatter.GetSortKey(_currentGroupProperty, g.Key));

        foreach (var group in orderedGroups)
        {
            var isExpanded = expandedGroups.Contains(group.Key);

            // Add group header
            var headerItem = new StorageFlatGroupItem(
                item: null,
                depth: 0,
                isGroupHeader: true,
                groupKey: group.Key)
            {
                IsExpanded = isExpanded
            };

            FlatItems.Add(headerItem);

            // Add group items if expanded
            if (isExpanded)
            {
                var sortedItems = ApplySorting(group.Value);
                foreach (var item in sortedItems)
                {
                    FlatItems.Add(new StorageFlatGroupItem(
                        item: item,
                        depth: 1,
                        isGroupHeader: false));
                }
            }
        }
    }

    private Dictionary<string, List<IGroupableItem>> GroupItemsByProperty(IEnumerable<IGroupableItem> items, string propertyName)
    {
        var grouped = new Dictionary<string, List<IGroupableItem>>();

        foreach (var item in items)
        {
            var value = item.GetPropertyValue(propertyName);
            var groupKey = GroupKeyFormatter.FormatGroupKey(propertyName, value);

            if (!grouped.ContainsKey(groupKey))
            {
                grouped[groupKey] = [];
            }

            grouped[groupKey].Add(item);
        }

        return grouped;
    }

    private IEnumerable<IGroupableItem> ApplySorting(IEnumerable<IGroupableItem> items)
    {
        if (_sortDirection is null || string.IsNullOrEmpty(_sortPropertyName))
        {
            return items;
        }

        bool isAscending = _sortDirection == SortDirection.Ascending;

        var sortedItems = items.OrderBy(item => item.GetPropertyValue(_sortPropertyName),
            Comparer<object?>.Create((a, b) =>
            {
                if (a is null && b is null) return 0;
                if (a is null) return -1;
                if (b is null) return 1;

                if (a is IComparable ca && b is IComparable cb && a.GetType() == b.GetType())
                {
                    try { return ca.CompareTo(cb); }
                    catch { }
                }

                return string.Compare(a?.ToString(), b?.ToString(), StringComparison.OrdinalIgnoreCase);
            }));

        return isAscending ? sortedItems : sortedItems.Reverse();
    }

    private void OnToggleGroup(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not StorageFlatGroupItem headerItem || !headerItem.IsGroupHeader)
        {
            return;
        }

        var index = FlatItems.IndexOf(headerItem);
        if (index < 0) return;

        if (headerItem.IsExpanded)
        {
            CollapseGroup(headerItem, index);
        }
        else
        {
            ExpandGroup(headerItem, index);
        }
    }

    private void ExpandGroup(StorageFlatGroupItem headerItem, int headerIndex)
    {
        headerItem.IsExpanded = true;

        var groupItems = GroupItemsByProperty(_sourceItems, _currentGroupProperty);
        if (!groupItems.TryGetValue(headerItem.GroupKey ?? string.Empty, out var items))
        {
            return;
        }

        var sortedItems = ApplySorting(items);
        var insertAt = headerIndex + 1;
        foreach (var item in sortedItems)
        {
            FlatItems.Insert(insertAt++, new StorageFlatGroupItem(
                item: item,
                depth: 1,
                isGroupHeader: false));
        }
    }

    private void CollapseGroup(StorageFlatGroupItem headerItem, int headerIndex)
    {
        headerItem.IsExpanded = false;

        var removeFrom = headerIndex + 1;
        while (removeFrom < FlatItems.Count && FlatItems[removeFrom].Depth > 0)
        {
            FlatItems.RemoveAt(removeFrom);
        }
    }

    private void OnExpandAll(object sender, RoutedEventArgs e)
    {
        for (int i = 0; i < FlatItems.Count; i++)
        {
            var item = FlatItems[i];
            if (item.IsGroupHeader && !item.IsExpanded)
            {
                ExpandGroup(item, i);
            }
        }
    }

    private void OnCollapseAll(object sender, RoutedEventArgs e)
    {
        for (int i = FlatItems.Count - 1; i >= 0; i--)
        {
            var item = FlatItems[i];
            if (item.IsGroupHeader && item.IsExpanded)
            {
                CollapseGroup(item, i);
            }
        }
    }

    private void OnFolderChanged(object sender, SelectionChangedEventArgs e)
    {
        // Files will be loaded when user clicks "Load Files"
        StatusMessage = "Click 'Load Files' to browse this folder";
        _sourceItems.Clear();
        FlatItems.Clear();
        ItemCountText = string.Empty;
    }

    private void OnGroupByChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox comboBox || comboBox.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        _currentGroupProperty = item.Tag?.ToString() ?? "";
        
        if (_sourceItems.Count > 0)
        {
            RebuildGroupedView();
            StatusMessage = string.IsNullOrEmpty(_currentGroupProperty) 
                ? "Grouping disabled" 
                : $"Grouped by {item.Content}";
        }
    }

    private void OnTableSorting(object sender, TableViewSortingEventArgs e)
    {
        if (e.Column?.Tag is not string propertyName)
        {
            return;
        }

        var nextDirection = e.Column.SortDirection switch
        {
            SortDirection.Ascending => SortDirection.Descending,
            SortDirection.Descending => (SortDirection?)null,
            _ => SortDirection.Ascending
        };

        e.Column.SortDirection = nextDirection;
        _sortPropertyName = nextDirection.HasValue ? propertyName : null;
        _sortDirection = nextDirection;

        RebuildGroupedView();
        e.Handled = true;
    }

    private void OnTableClearSorting(object sender, TableViewClearSortingEventArgs e)
    {
        if (e.Column is not null)
        {
            e.Column.SortDirection = null;
        }

        _sortPropertyName = null;
        _sortDirection = null;

        RebuildGroupedView();
        e.Handled = true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
