using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;
using Windows.Storage;

namespace WinUI.TableView.SampleApp.Adapters;

/// <summary>
/// Enhanced CollectionViewSource bridge that respects Windows Property System grouping capabilities.
/// Only enables grouping when the source folder has PKEY_IsGroup = true or is a known groupable folder type.
/// </summary>
public class PropertyAwareCollectionViewSourceBridge : INotifyPropertyChanged, IDisposable
{
    private readonly CollectionViewSource _collectionViewSource;
    private readonly Func<object, PropertyAwareStorageItemAdapter> _adapterFactory;
    private ObservableCollection<IGroupableItem>? _adaptedItems;
    private INotifyCollectionChanged? _sourceCollection;
    private string? _currentGroupProperty;
    private bool _groupingEnabled = true;
    private StorageFolder? _sourceFolder;

    public PropertyAwareCollectionViewSourceBridge(
        CollectionViewSource collectionViewSource,
        Func<object, PropertyAwareStorageItemAdapter> adapterFactory)
    {
        _collectionViewSource = collectionViewSource ?? throw new ArgumentNullException(nameof(collectionViewSource));
        _adapterFactory = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));

        InitializeAdaptedItems();
    }

    /// <summary>
    /// Gets the adapted items collection that TableView can bind to.
    /// </summary>
    public ObservableCollection<IGroupableItem> AdaptedItems => _adaptedItems ??= new ObservableCollection<IGroupableItem>();

    /// <summary>
    /// Gets the underlying CollectionViewSource for advanced scenarios.
    /// </summary>
    public CollectionViewSource CollectionViewSource => _collectionViewSource;

    /// <summary>
    /// Gets whether the source collection supports grouping based on PKEY_IsGroup.
    /// </summary>
    public bool SupportsGrouping => _groupingEnabled && !string.IsNullOrEmpty(_currentGroupProperty);

    /// <summary>
    /// Gets whether grouping is enabled by the source folder's properties.
    /// </summary>
    public bool GroupingEnabled => _groupingEnabled;

    /// <summary>
    /// Gets the current grouping property.
    /// </summary>
    public string? CurrentGroupProperty => _currentGroupProperty;

    /// <summary>
    /// Gets the source folder that was analyzed for grouping capabilities.
    /// </summary>
    public StorageFolder? SourceFolder => _sourceFolder;

    /// <summary>
    /// Gets available grouping properties for the current folder context.
    /// </summary>
    public IEnumerable<string> AvailableGroupingProperties
    {
        get
        {
            if (!_groupingEnabled || _adaptedItems == null)
            {
                return Enumerable.Empty<string>();
            }

            // Get properties from the first adapter that has extended properties loaded
            var firstAdapter = _adaptedItems
                .OfType<PropertyAwareStorageItemAdapter>()
                .FirstOrDefault(a => a.AreExtendedPropertiesLoaded);

            return firstAdapter?.GetAvailableGroupingProperties() ?? GetDefaultGroupingProperties();
        }
    }

    private static IEnumerable<string> GetDefaultGroupingProperties()
    {
        return new[] { "Name", "DateModified", "Size", "ItemType", "FileType" };
    }

    /// <summary>
    /// Sets the source folder and analyzes its grouping capabilities.
    /// </summary>
    public async Task SetSourceFolderAsync(StorageFolder folder)
    {
        _sourceFolder = folder;
        await AnalyzeFolderGroupingCapabilitiesAsync(folder);
    }

    /// <summary>
    /// Analyzes whether the folder supports grouping based on Windows Property System.
    /// </summary>
    private async Task AnalyzeFolderGroupingCapabilitiesAsync(StorageFolder folder)
    {
        try
        {
            // Create a temporary adapter to check folder properties
            var folderAdapter = new PropertyAwareStorageItemAdapter(folder);
            await folderAdapter.LoadExtendedPropertiesAsync();

            // Check if grouping is supported
            _groupingEnabled = folderAdapter.SupportsGrouping;

            OnPropertyChanged(nameof(GroupingEnabled));
            OnPropertyChanged(nameof(SupportsGrouping));
            OnPropertyChanged(nameof(AvailableGroupingProperties));
        }
        catch (Exception)
        {
            // If analysis fails, default to enabling grouping
            _groupingEnabled = true;
        }
    }

    /// <summary>
    /// Sets grouping by property name if grouping is enabled by folder properties.
    /// </summary>
    public async Task<bool> SetGroupingAsync(string? propertyName)
    {
        if (!_groupingEnabled)
        {
            return false;
        }

        _currentGroupProperty = propertyName;
        OnPropertyChanged(nameof(SupportsGrouping));
        OnPropertyChanged(nameof(CurrentGroupProperty));

        await RebuildGroupedViewAsync();
        return true;
    }

    /// <summary>
    /// Sets grouping synchronously (for compatibility with existing code).
    /// </summary>
    public bool SetGrouping(string? propertyName)
    {
        if (!_groupingEnabled)
        {
            return false;
        }

        _currentGroupProperty = propertyName;
        OnPropertyChanged(nameof(SupportsGrouping));
        OnPropertyChanged(nameof(CurrentGroupProperty));

        // For sync version, rebuild without waiting for property loading
        RebuildGroupedView();
        return true;
    }

    /// <summary>
    /// Rebuilds the adapted items with hierarchical grouping (async version).
    /// </summary>
    private async Task RebuildGroupedViewAsync()
    {
        if (_adaptedItems == null || _collectionViewSource.Source is not IEnumerable source)
        {
            return;
        }

        var sourceItems = source.Cast<object>().ToList();
        _adaptedItems.Clear();

        if (!_groupingEnabled || string.IsNullOrEmpty(_currentGroupProperty))
        {
            // No grouping - add all items flat
            foreach (var item in sourceItems)
            {
                var adapter = _adapterFactory(item);
                await adapter.LoadExtendedPropertiesAsync();
                _adaptedItems.Add(adapter);
            }
            return;
        }

        // Load properties for all adapters first
        var adapters = new List<PropertyAwareStorageItemAdapter>();
        foreach (var item in sourceItems)
        {
            var adapter = _adapterFactory(item);
            await adapter.LoadExtendedPropertiesAsync();
            adapters.Add(adapter);
        }

        // Create grouped view
        var groups = adapters
            .GroupBy(adapter =>
            {
                var value = adapter.GetPropertyValue(_currentGroupProperty);
                return GroupKeyFormatter.FormatGroupKey(_currentGroupProperty, value);
            })
            .OrderBy(g => GroupKeyFormatter.GetSortKey(_currentGroupProperty, g.Key));

        foreach (var group in groups)
        {
            // Add group header
            var groupHeader = new GroupHeaderAdapter(group.Key, group.Count());
            _adaptedItems.Add(groupHeader);

            // Add group items
            var sortedItems = group.OrderBy(adapter => adapter.Name);
            foreach (var adapter in sortedItems)
            {
                _adaptedItems.Add(adapter);
            }
        }
    }

    /// <summary>
    /// Rebuilds the adapted items with hierarchical grouping (sync version).
    /// </summary>
    private void RebuildGroupedView()
    {
        if (_adaptedItems == null || _collectionViewSource.Source is not IEnumerable source)
        {
            return;
        }

        var sourceItems = source.Cast<object>().ToList();
        _adaptedItems.Clear();

        if (!_groupingEnabled || string.IsNullOrEmpty(_currentGroupProperty))
        {
            // No grouping - add all items flat
            foreach (var item in sourceItems)
            {
                var adapter = _adapterFactory(item);
                _adaptedItems.Add(adapter);
            }
            return;
        }

        // Create grouped view (without waiting for extended properties)
        var groups = sourceItems
            .GroupBy(item =>
            {
                var adapter = _adapterFactory(item);
                var value = adapter.GetPropertyValue(_currentGroupProperty);
                return GroupKeyFormatter.FormatGroupKey(_currentGroupProperty, value);
            })
            .OrderBy(g => GroupKeyFormatter.GetSortKey(_currentGroupProperty, g.Key));

        foreach (var group in groups)
        {
            // Add group header
            var groupHeader = new GroupHeaderAdapter(group.Key, group.Count());
            _adaptedItems.Add(groupHeader);

            // Add group items
            var sortedItems = group
                .Select(item => _adapterFactory(item))
                .OrderBy(adapter => adapter.Name);

            foreach (var adapter in sortedItems)
            {
                _adaptedItems.Add(adapter);
            }
        }
    }

    /// <summary>
    /// Clears any grouping.
    /// </summary>
    public void ClearGrouping()
    {
        SetGrouping(null);
    }

    /// <summary>
    /// Forces re-enabling of grouping (bypasses PKEY_IsGroup check).
    /// </summary>
    public void ForceEnableGrouping()
    {
        _groupingEnabled = true;
        OnPropertyChanged(nameof(GroupingEnabled));
        OnPropertyChanged(nameof(SupportsGrouping));
    }

    /// <summary>
    /// Forces disabling of grouping.
    /// </summary>
    public void ForceDisableGrouping()
    {
        _groupingEnabled = false;
        ClearGrouping();
        OnPropertyChanged(nameof(GroupingEnabled));
        OnPropertyChanged(nameof(SupportsGrouping));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void InitializeAdaptedItems()
    {
        UnsubscribeFromSourceCollection();
        if (_adaptedItems != null)
        {
            _adaptedItems.Clear();
        }

        if (_collectionViewSource.Source is IEnumerable source)
        {
            // Subscribe to collection changes
            if (source is INotifyCollectionChanged notifyCollection)
            {
                _sourceCollection = notifyCollection;
                _sourceCollection.CollectionChanged += OnSourceCollectionChanged;
            }

            // Build initial grouped view
            RebuildGroupedView();
        }

        OnPropertyChanged(nameof(SupportsGrouping));
        OnPropertyChanged(nameof(CurrentGroupProperty));
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Rebuild view when collection changes
        RebuildGroupedView();
    }

    private void UnsubscribeFromSourceCollection()
    {
        if (_sourceCollection != null)
        {
            _sourceCollection.CollectionChanged -= OnSourceCollectionChanged;
            _sourceCollection = null;
        }
    }

    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        UnsubscribeFromSourceCollection();
    }
}

/// <summary>
/// Factory for creating property-aware CollectionViewSource bridges.
/// </summary>
public static class PropertyAwareCollectionViewSourceBridgeFactory
{
    /// <summary>
    /// Creates a property-aware bridge for IStorageItem collections.
    /// </summary>
    public static PropertyAwareCollectionViewSourceBridge CreateForStorageItems(CollectionViewSource collectionViewSource)
    {
        return new PropertyAwareCollectionViewSourceBridge(
            collectionViewSource,
            item => new PropertyAwareStorageItemAdapter((IStorageItem)item));
    }

    /// <summary>
    /// Creates a property-aware bridge with a custom adapter factory.
    /// </summary>
    public static PropertyAwareCollectionViewSourceBridge CreateCustom<T>(
        CollectionViewSource collectionViewSource,
        Func<T, PropertyAwareStorageItemAdapter> adapterFactory) where T : class
    {
        return new PropertyAwareCollectionViewSourceBridge(
            collectionViewSource,
            item => adapterFactory((T)item));
    }
}