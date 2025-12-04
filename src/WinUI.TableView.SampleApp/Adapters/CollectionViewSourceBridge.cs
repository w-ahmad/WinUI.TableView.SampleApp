using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Microsoft.UI.Xaml.Data;

namespace WinUI.TableView.SampleApp.Adapters;

/// <summary>
/// Bridge that enables TableView to work with CollectionViewSource like ListView does.
/// Maintains backward compatibility with existing IGroupableItem adapters.
/// Note: WinUI CollectionViewSource has different APIs than WPF, so this bridges the concepts.
/// </summary>
public class CollectionViewSourceBridge : INotifyPropertyChanged
{
    private readonly CollectionViewSource _collectionViewSource;
    private readonly Func<object, IGroupableItem> _adapterFactory;
    private ObservableCollection<IGroupableItem>? _adaptedItems;
    private INotifyCollectionChanged? _sourceCollection;
    private string? _currentGroupProperty;

    public CollectionViewSourceBridge(
        CollectionViewSource collectionViewSource,
        Func<object, IGroupableItem> adapterFactory)
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
    /// Gets whether the source collection supports grouping.
    /// </summary>
    public bool SupportsGrouping => _collectionViewSource.IsSourceGrouped || !string.IsNullOrEmpty(_currentGroupProperty);

    /// <summary>
    /// Gets the current grouping property.
    /// </summary>
    public string? CurrentGroupProperty => _currentGroupProperty;

    /// <summary>
    /// Sets grouping by property name (simulates PropertyGroupDescription).
    /// Creates a hierarchical view with group headers and items.
    /// </summary>
    public void SetGrouping(string? propertyName)
    {
        _currentGroupProperty = propertyName;
        OnPropertyChanged(nameof(SupportsGrouping));
        OnPropertyChanged(nameof(CurrentGroupProperty));
        
        RebuildGroupedView();
    }

    /// <summary>
    /// Rebuilds the adapted items with hierarchical grouping.
    /// </summary>
    private void RebuildGroupedView()
    {
        if (_adaptedItems == null || _collectionViewSource.Source is not IEnumerable source)
        {
            return;
        }

        var sourceItems = source.Cast<object>().ToList();
        _adaptedItems.Clear();

        if (string.IsNullOrEmpty(_currentGroupProperty))
        {
            // No grouping - add all items flat
            foreach (var item in sourceItems)
            {
                var adapter = _adapterFactory(item);
                _adaptedItems.Add(adapter);
            }
            return;
        }

        // Create grouped view
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
            // Add group header (using a special GroupHeaderAdapter)
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
        // For simplicity, rebuild the entire view when the source collection changes
        // This could be optimized for better performance with large collections
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

    /// <summary>
    /// Disposes resources and unsubscribes from events.
    /// </summary>
    public void Dispose()
    {
        UnsubscribeFromSourceCollection();
    }
}

/// <summary>
/// Static factory methods for creating CollectionViewSource bridges with common adapter types.
/// </summary>
public static class CollectionViewSourceBridgeFactory
{
    /// <summary>
    /// Creates a bridge for IStorageItem collections.
    /// </summary>
    public static CollectionViewSourceBridge CreateForStorageItems(CollectionViewSource collectionViewSource)
    {
        return new CollectionViewSourceBridge(
            collectionViewSource,
            item => new StorageItemAdapter((Windows.Storage.IStorageItem)item));
    }

    /// <summary>
    /// Creates a bridge for ExampleModel collections.
    /// </summary>
    public static CollectionViewSourceBridge CreateForExampleModels(CollectionViewSource collectionViewSource)
    {
        return new CollectionViewSourceBridge(
            collectionViewSource,
            item => new ExampleModelAdapter((ExampleModel)item));
    }

    /// <summary>
    /// Creates a bridge with a custom adapter factory.
    /// </summary>
    public static CollectionViewSourceBridge CreateCustom<T>(
        CollectionViewSource collectionViewSource,
        Func<T, IGroupableItem> adapterFactory) where T : class
    {
        return new CollectionViewSourceBridge(
            collectionViewSource,
            item => adapterFactory((T)item));
    }

    /// <summary>
    /// Creates a bridge that automatically detects the source type and creates appropriate adapters.
    /// </summary>
    public static CollectionViewSourceBridge CreateAutoDetect(CollectionViewSource collectionViewSource)
    {
        return new CollectionViewSourceBridge(
            collectionViewSource,
            item => CreateAdapterForItem(item));
    }

    private static IGroupableItem CreateAdapterForItem(object item)
    {
        return item switch
        {
            Windows.Storage.IStorageItem storageItem => new StorageItemAdapter(storageItem),
            ExampleModel exampleModel => new ExampleModelAdapter(exampleModel),
            IGroupableItem groupableItem => groupableItem, // Already an adapter
            _ => new GenericObjectAdapter(item) // Fallback for any object
        };
    }
}