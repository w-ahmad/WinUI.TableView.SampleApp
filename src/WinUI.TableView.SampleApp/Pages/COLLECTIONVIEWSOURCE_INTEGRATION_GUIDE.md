# CollectionViewSource Storage Grouping Integration

## Integration with Existing StorageGroupingPage

You can easily integrate the CollectionViewSource functionality into the existing `StorageGroupingPage` by adding this bridge functionality.

### 1. Add CollectionViewSource Demo to StorageGroupingPage

Add these members to `StorageGroupingPage.xaml.cs`:

```csharp
using WinUI.TableView.SampleApp.Helpers;

public sealed partial class StorageGroupingPage : Page, INotifyPropertyChanged
{
    // Add CollectionViewSource demo alongside existing functionality
    private CollectionViewSourceStorageDemo? _collectionViewSourceDemo;
    private bool _useCollectionViewSourceMode;

    // Add toggle for CollectionViewSource mode
    public bool UseCollectionViewSourceMode
    {
        get => _useCollectionViewSourceMode;
        set
        {
            if (_useCollectionViewSourceMode != value)
            {
                _useCollectionViewSourceMode = value;
                OnPropertyChanged();
                SwitchToCollectionViewSourceMode(value);
            }
        }
    }

    private void SwitchToCollectionViewSourceMode(bool useCollectionViewSource)
    {
        if (useCollectionViewSource)
        {
            // Initialize CollectionViewSource demo
            _collectionViewSourceDemo = new CollectionViewSourceStorageDemo();
            
            // Switch TableView to use bridge
            StorageTable.ItemsSource = _collectionViewSourceDemo.AdaptedItems;
            
            StatusMessage = "Switched to CollectionViewSource mode";
        }
        else
        {
            // Switch back to original flat items
            StorageTable.ItemsSource = FlatItems;
            
            // Clean up demo
            _collectionViewSourceDemo?.Dispose();
            _collectionViewSourceDemo = null;
            
            StatusMessage = "Switched to hierarchical grouping mode";
        }
    }
}
```

### 2. Update LoadFilesAsync Method

Modify the load method to support both modes:

```csharp
private async Task LoadFilesAsync()
{
    // ... existing folder loading code ...

    if (_useCollectionViewSourceMode && _collectionViewSourceDemo != null)
    {
        // CollectionViewSource mode - use bridge
        await _collectionViewSourceDemo.AddStorageItemsAsync(folder, 100);
        
        // Apply current grouping
        var groupProperty = (GroupByComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        _collectionViewSourceDemo.ApplyGrouping(groupProperty ?? "");
        
        var stats = _collectionViewSourceDemo.GetStats();
        StatusMessage = $"Loaded via CollectionViewSource: {stats}";
    }
    else
    {
        // Original hierarchical mode
        // ... existing code for loading into _sourceItems and FlatItems ...
    }
}
```

### 3. Update Grouping Method

Modify grouping to support both modes:

```csharp
private void OnGroupByChanged(object sender, SelectionChangedEventArgs e)
{
    if (sender is not ComboBox comboBox || comboBox.SelectedItem is not ComboBoxItem item)
    {
        return;
    }

    var groupProperty = item.Tag?.ToString() ?? "";

    if (_useCollectionViewSourceMode && _collectionViewSourceDemo != null)
    {
        // CollectionViewSource bridge mode
        _collectionViewSourceDemo.ApplyGrouping(groupProperty);
        
        var stats = _collectionViewSourceDemo.GetStats();
        StatusMessage = string.IsNullOrEmpty(groupProperty) 
            ? $"CollectionViewSource grouping disabled: {stats}" 
            : $"CollectionViewSource grouped by {item.Content}: {stats}";
    }
    else
    {
        // Original hierarchical mode
        _currentGroupProperty = groupProperty;
        
        if (_sourceItems.Count > 0)
        {
            RebuildGroupedView();
            StatusMessage = string.IsNullOrEmpty(_currentGroupProperty) 
                ? "Grouping disabled" 
                : $"Grouped by {item.Content}";
        }
    }
}
```

### 4. Add Toggle Button to XAML

Add this to the action buttons section in `StorageGroupingPage.xaml`:

```xml
<!-- Action Buttons -->
<StackPanel Grid.Column="4" Orientation="Horizontal" HorizontalAlignment="Right" Spacing="8">
    <Button Content="Load Files" Click="OnLoadFiles">
        <Button.KeyboardAccelerators>
            <KeyboardAccelerator Key="F5"/>
        </Button.KeyboardAccelerators>
    </Button>
    <Button Content="Expand All" Click="OnExpandAll"/>
    <Button Content="Collapse All" Click="OnCollapseAll"/>
    
    <!-- NEW: CollectionViewSource Toggle -->
    <Border Width="1" Background="{ThemeResource DividerStrokeColorDefaultBrush}"/>
    <ToggleButton Content="CollectionViewSource Mode" 
                  IsChecked="{x:Bind UseCollectionViewSourceMode, Mode=TwoWay}"
                  ToolTipService.ToolTip="Toggle between hierarchical grouping and CollectionViewSource bridge"/>
</StackPanel>
```

### 5. Add CollectionViewSource Demo Buttons

Add these buttons for testing CollectionViewSource functionality:

```xml
<!-- CollectionViewSource-specific buttons -->
<StackPanel Orientation="Horizontal" 
           Spacing="8" 
           Visibility="{x:Bind UseCollectionViewSourceMode, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
    <Button Content="Add Samples" Click="OnAddSampleFiles"/>
    <Button Content="Remove .txt Files" Click="OnRemoveTxtFiles"/>
    <Button Content="Clear All" Click="OnClearAllFiles"/>
</StackPanel>
```

### 6. Add Event Handlers

Add these methods to the code-behind:

```csharp
private async void OnAddSampleFiles(object sender, RoutedEventArgs e)
{
    if (_collectionViewSourceDemo != null)
    {
        await _collectionViewSourceDemo.AddSampleFilesAsync();
        var stats = _collectionViewSourceDemo.GetStats();
        StatusMessage = $"Added sample files: {stats}";
    }
}

private void OnRemoveTxtFiles(object sender, RoutedEventArgs e)
{
    if (_collectionViewSourceDemo != null)
    {
        _collectionViewSourceDemo.RemoveItemsWhere(item => 
            item is StorageFile file && file.FileType.Equals(".txt", StringComparison.OrdinalIgnoreCase));
            
        var stats = _collectionViewSourceDemo.GetStats();
        StatusMessage = $"Removed .txt files: {stats}";
    }
}

private void OnClearAllFiles(object sender, RoutedEventArgs e)
{
    if (_collectionViewSourceDemo != null)
    {
        _collectionViewSourceDemo.Clear();
        StatusMessage = "Cleared all files via CollectionViewSource";
    }
}
```

## Usage Demonstration

### Toggle Between Modes

1. **Hierarchical Grouping Mode (Original)**
   - Uses `FlatItems` with `StorageFlatGroupItem`
   - Manual expand/collapse of groups
   - Complex flattened hierarchy management

2. **CollectionViewSource Mode (New)**
   - Uses `CollectionViewSourceBridge` with automatic grouping
   - ListView-style API: `SetGrouping(propertyName)`
   - Automatic collection synchronization

### Test CollectionViewSource Features

```csharp
// Load files in CollectionViewSource mode
await demo.AddStorageItemsAsync(folder);

// Apply different groupings
demo.ApplyGrouping("FileType");    // Group by file extensions
demo.ApplyGrouping("DateModified"); // Group by relative dates  
demo.ApplyGrouping("Size");        // Group by size categories
demo.ApplyGrouping("");           // Clear grouping

// Dynamic collection updates
await demo.AddSampleFilesAsync();  // Add sample files
demo.RemoveItemsWhere(f => f.Name.Contains("temp")); // Remove specific files
demo.Clear(); // Clear all

// Get statistics
var stats = demo.GetStats();
Console.WriteLine(stats); // "Items: 25, Adapted: 30, Groups: 5, Folders: 2 (Grouped by FileType)"
```

## Benefits Demonstrated

### 1. API Simplicity
```csharp
// Complex hierarchical grouping
RebuildGroupedView();
GroupItemsByProperty();
ApplySorting();
FlattenGroups();

// vs CollectionViewSource bridge
bridge.SetGrouping("FileType");
```

### 2. Automatic Synchronization
```csharp
// Add item to source collection
storageItems.Add(newFile);
// Bridge automatically:
// - Creates adapter
// - Assigns to correct group  
// - Updates TableView display
```

### 3. ListView Familiarity
```csharp
// Standard CollectionViewSource pattern
var cvs = new CollectionViewSource { Source = items };
bridge.SetGrouping("PropertyName"); // Like PropertyGroupDescription
```

## Result

This integration shows how the CollectionViewSource bridge provides:
- ? **Side-by-side comparison** with hierarchical grouping
- ? **ListView-style API** with TableView performance
- ? **Automatic collection synchronization**  
- ? **Smart group formatting** (file types, dates, sizes)
- ? **Dynamic add/remove operations**
- ? **Real-time statistics and feedback**

The toggle demonstrates that both approaches can coexist, allowing developers to choose the pattern that best fits their needs.