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
using Windows.Storage.BulkAccess;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using WinUI.TableView.SampleApp.Adapters;

namespace WinUI.TableView.SampleApp.Pages;

/// <summary>
/// Demonstrates production-ready file system grouping using the CollectionViewSourceBridge pattern.
/// This implementation provides optimal performance for large file collections while maintaining
/// smooth scrolling and responsive UI.
/// </summary>
public sealed partial class StorageGroupingPageVirtualized : Page, INotifyPropertyChanged
{
    private readonly CollectionViewSourceBridge _groupingBridge;
    private readonly ObservableCollection<IGroupableItem> _sourceItems = [];
    
    private string _currentGroupProperty = "DateModified";
    private bool _isLoading;
    private string _loadingStatus = string.Empty;
    private string _statusMessage = "Click 'Load Files' to browse a folder";
    private string _itemCountText = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageGroupingPageVirtualized"/> class.
    /// </summary>
    public StorageGroupingPageVirtualized()
    {
        InitializeComponent();
        
        // Setup CollectionViewSourceBridge for automatic grouping management
        var collectionViewSource = new Microsoft.UI.Xaml.Data.CollectionViewSource
        {
            Source = _sourceItems,
            IsSourceGrouped = false
        };
        
        _groupingBridge = CollectionViewSourceBridgeFactory.CreateForStorageItems(collectionViewSource);
        
        // Bind TableView to the bridge's adapted items
        StorageTable.ItemsSource = _groupingBridge.AdaptedItems;
        
        // Setup initial grouping
        UpdateGrouping();
    }

    #region Properties

    /// <summary>
    /// Gets or sets a value indicating whether files are currently being loaded.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the current loading status message.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the status message displayed to the user.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the item count text.
    /// </summary>
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

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles the Load Files button click event.
    /// </summary>
    private async void OnLoadFiles(object sender, RoutedEventArgs e)
    {
        await LoadFilesAsync();
    }

    /// <summary>
    /// Handles folder selection changes.
    /// </summary>
    private void OnFolderChanged(object sender, SelectionChangedEventArgs e)
    {
        StatusMessage = "Click 'Load Files' to browse this folder";
        _sourceItems.Clear();
        ItemCountText = string.Empty;
    }

    /// <summary>
    /// Handles grouping property changes.
    /// </summary>
    private void OnGroupByChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
        {
            _currentGroupProperty = item.Tag?.ToString() ?? string.Empty;
            UpdateGrouping();
            
            StatusMessage = string.IsNullOrEmpty(_currentGroupProperty) 
                ? "Grouping disabled" 
                : $"Grouped by {item.Content}";
        }
    }

    /// <summary>
    /// Handles table sorting events.
    /// </summary>
    private void OnTableSorting(object sender, TableViewSortingEventArgs e)
    {
        if (e.Column?.Tag is string propertyName)
        {
            SortDirection? nextDirection = e.Column.SortDirection switch
            {
                SortDirection.Ascending => SortDirection.Descending,
                SortDirection.Descending => null,
                _ => SortDirection.Ascending
            };

            e.Column.SortDirection = nextDirection;
            // Note: Sorting implementation would be added here if needed
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles clearing table sorting.
    /// </summary>
    private void OnTableClearSorting(object sender, TableViewClearSortingEventArgs e)
    {
        if (e.Column is not null)
        {
            e.Column.SortDirection = null;
        }
        
        // Note: Sort clearing implementation would be added here if needed
        e.Handled = true;
    }

    #endregion

    #region File Loading

    /// <summary>
    /// Loads files from the selected folder using FileInformationFactory for optimal performance.
    /// </summary>
    private async Task LoadFilesAsync()
    {
        if (FolderComboBox.SelectedItem is not ComboBoxItem selectedFolder)
        {
            return;
        }

        IsLoading = true;
        LoadingStatus = "Loading files...";
        _sourceItems.Clear();

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

            // Use FileInformationFactory for optimal performance with large file collections
            var query = folder.CreateFileQuery(CommonFileQuery.OrderByName);
            var factory = new FileInformationFactory(query, Windows.Storage.FileProperties.ThumbnailMode.ListView);

            // Get file count first
            var fileCount = await query.GetItemCountAsync();
            LoadingStatus = $"Loading {fileCount} files...";

            // Load files in batches with pre-loaded properties
            const int maxFiles = 10000; // Reasonable limit for demonstration
            var fileInfos = await factory.GetFilesAsync(0, Math.Min(fileCount, maxFiles));

            // Create adapters - FileInformationAdapter doesn't need async initialization
            var adapters = fileInfos.Select(f => new FileInformationAdapter(f) as IGroupableItem);

            // Update source collection - bridge will automatically handle grouping
            foreach (var adapter in adapters)
            {
                _sourceItems.Add(adapter);
            }

            StatusMessage = $"Loaded {fileInfos.Count} files from {folderName}";
            ItemCountText = $"{fileInfos.Count} items";
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

    /// <summary>
    /// Gets a known folder by name.
    /// </summary>
    private static async Task<StorageFolder?> GetKnownFolderAsync(string folderName)
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

    #endregion

    #region Grouping Management

    /// <summary>
    /// Updates the current grouping using the CollectionViewSourceBridge.
    /// </summary>
    private void UpdateGrouping()
    {
        _groupingBridge.SetGrouping(string.IsNullOrEmpty(_currentGroupProperty) ? null : _currentGroupProperty);
    }

    #endregion
}
