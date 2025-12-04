# CollectionViewSource Bridge for WinUI TableView

## Overview

The `CollectionViewSourceBridge` enables WinUI TableView to work seamlessly with `CollectionViewSource`, providing ListView-style grouping while maintaining TableView's performance and features.

## Key Benefits

- **ListView Compatibility**: Use familiar `CollectionViewSource` and `PropertyGroupDescription` patterns
- **Automatic Synchronization**: Changes in the source collection automatically update the TableView
- **Backward Compatibility**: Existing `IGroupableItem` adapters continue to work
- **Type Safety**: Factory methods for common data types with compile-time safety
- **Performance**: Efficient collection change tracking without full rebuilds

## Basic Usage

### 1. Create CollectionViewSource

```csharp
// Your data collection
var storageItems = new ObservableCollection<IStorageItem>();

// Standard CollectionViewSource
var collectionViewSource = new CollectionViewSource
{
    Source = storageItems,
    IsSourceGrouped = false
};
```

### 2. Create Bridge

```csharp
// Create bridge for IStorageItem collections
var bridge = CollectionViewSourceBridgeFactory.CreateForStorageItems(collectionViewSource);

// Bind TableView to adapted items
tableView.ItemsSource = bridge.AdaptedItems;
```

### 3. Apply Grouping

```csharp
// Group by file type (ListView-style)
collectionViewSource.GroupDescriptions.Clear();
collectionViewSource.GroupDescriptions.Add(new PropertyGroupDescription("FileType"));

// Clear grouping
collectionViewSource.GroupDescriptions.Clear();
```

## Factory Methods

### Pre-built Adapters

```csharp
// For IStorageItem collections
var bridge = CollectionViewSourceBridgeFactory.CreateForStorageItems(collectionViewSource);

// For ExampleModel collections  
var bridge = CollectionViewSourceBridgeFactory.CreateForExampleModels(collectionViewSource);

// Auto-detect adapter type
var bridge = CollectionViewSourceBridgeFactory.CreateAutoDetect(collectionViewSource);
```

### Custom Adapters

```csharp
// Custom adapter with type safety
var bridge = CollectionViewSourceBridgeFactory.CreateCustom<MyDataType>(
    collectionViewSource,
    item => new MyCustomAdapter(item));

// Direct instantiation with custom factory
var bridge = new CollectionViewSourceBridge(
    collectionViewSource,
    item => CreateMyAdapter(item));
```

## Advanced Scenarios

### Custom Grouping Logic

```csharp
// Group by file size ranges
collectionViewSource.GroupDescriptions.Add(
    new PropertyGroupDescription("Size", new SizeRangeValueConverter()));

// Multiple grouping levels
collectionViewSource.GroupDescriptions.Add(new PropertyGroupDescription("ItemType"));
collectionViewSource.GroupDescriptions.Add(new PropertyGroupDescription("FileType"));
```

### Dynamic Collection Updates

```csharp
// Collections automatically sync
storageItems.Add(newFile);        // Automatically appears in TableView
storageItems.RemoveAt(0);         // Automatically removed from TableView
storageItems.Clear();             // TableView cleared automatically
```

### Bridge Properties

```csharp
// Check grouping support
if (bridge.SupportsGrouping)
{
    var currentProperty = bridge.CurrentGroupProperty; // e.g., "FileType"
}

// Access underlying CollectionViewSource
var cvs = bridge.CollectionViewSource;
var view = cvs.View; // ICollectionView for advanced scenarios
```

## Integration with TableView Features

### Column Sorting

```csharp
// TableView column sorting works normally
<controls:TableViewTextColumn Header="Name"
                              Binding="{Binding Name}"
                              CanUserSort="True"/>
```

### Selection

```csharp
// Access selected adapted items
var selectedAdapter = (IGroupableItem)tableView.SelectedItem;
var originalItem = selectedAdapter.UnderlyingItem; // Original data object
```

### Custom Templates

```csharp
// Use adapted properties in templates
<controls:TableViewTemplateColumn Header="Custom">
    <controls:TableViewTemplateColumn.CellTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="{Binding FileIcon}" FontFamily="Segoe MDL2 Assets"/>
                <TextBlock Text="{Binding Name}" Margin="8,0,0,0"/>
            </StackPanel>
        </DataTemplate>
    </controls:TableViewTemplateColumn.CellTemplate>
</controls:TableViewTemplateColumn>
```

## Comparison: ListView vs TableView with Bridge

| Feature | ListView with CollectionViewSource | TableView with Bridge |
|---------|-----------------------------------|----------------------|
| Grouping | ? PropertyGroupDescription | ? PropertyGroupDescription |
| Sorting | ? SortDescriptions | ? Column headers |
| Virtualization | ? Built-in | ? Enhanced performance |
| Columns | ? Single template | ? Multiple typed columns |
| Custom cells | ?? DataTemplate only | ? Rich cell types |
| Performance | ?? Standard | ? Optimized for tables |

## Error Handling

```csharp
try
{
    var bridge = CollectionViewSourceBridgeFactory.CreateForStorageItems(collectionViewSource);
    tableView.ItemsSource = bridge.AdaptedItems;
}
catch (ArgumentNullException ex)
{
    // Handle null CollectionViewSource
}
catch (InvalidOperationException ex)
{
    // Handle invalid source type
}
```

## Memory Management

```csharp
// Dispose bridge when done (removes event handlers)
protected override void OnNavigatedFrom(NavigationEventArgs e)
{
    bridge?.Dispose();
    base.OnNavigatedFrom(e);
}
```

## Performance Notes

- **Collection Changes**: Bridge efficiently handles `INotifyCollectionChanged` events
- **Adapter Creation**: Factory methods cache adapters when possible
- **Memory Usage**: Adapters hold references to original objects, ensure proper disposal
- **Large Collections**: Consider virtualization strategies for 10,000+ items

## Migration from ListView

### Before (ListView)
```xml
<ListView ItemsSource="{x:Bind MyCollectionViewSource.View}">
    <ListView.GroupStyle>
        <GroupStyle/>
    </ListView.GroupStyle>
</ListView>
```

### After (TableView with Bridge)
```csharp
// In code-behind
var bridge = CollectionViewSourceBridgeFactory.CreateForStorageItems(MyCollectionViewSource);
MyTableView.ItemsSource = bridge.AdaptedItems;
```

```xml
<controls:TableView x:Name="MyTableView">
    <controls:TableView.Columns>
        <!-- Define specific columns -->
    </controls:TableView.Columns>
</controls:TableView>
```

## See Also

- [IGroupableItem Interface Documentation](./IGroupableItem.md)
- [TableView Grouping Guide](./TableView-Grouping.md)
- [Performance Best Practices](./Performance-Guide.md)
- [CollectionViewSourceExample.xaml](../Pages/CollectionViewSourceExample.xaml) - Complete working example