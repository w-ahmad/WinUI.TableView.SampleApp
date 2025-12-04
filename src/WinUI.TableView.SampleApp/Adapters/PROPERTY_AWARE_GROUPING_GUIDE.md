# Property-Aware Storage Grouping with PKEY_IsGroup Support

This implementation adds **Windows Property System awareness** to the CollectionViewSource storage grouping, respecting the `PKEY_IsGroup` property key to determine when grouping should be available.

## Key Features

### 1. Windows Property System Integration
- **PKEY_IsGroup Detection** - Respects Windows' native grouping capability flags
- **Folder Type Analysis** - Recognizes special folders (Pictures, Music, Videos, Documents)
- **Extended Properties** - Loads additional metadata for richer grouping options
- **Property-Based Grouping** - Uses system properties like Authors, Keywords, Rating

### 2. Enhanced Adapter
`PropertyAwareStorageItemAdapter` extends `StorageItemAdapter` with:
```csharp
// Check if folder supports grouping
bool supportsGrouping = adapter.SupportsGrouping; // Based on PKEY_IsGroup

// Get available grouping properties for folder type
var properties = adapter.GetAvailableGroupingProperties();
// Results: ["Name", "DateModified", "Artist", "Album", "Genre"] for Music folder

// Access system properties directly
var folderKind = adapter.GetSystemPropertyValue("System.FolderKind"); // "MUSIC"
var authors = adapter.GetSystemPropertyValue("System.ItemAuthors");
```

### 3. Smart Bridge Behavior
`PropertyAwareCollectionViewSourceBridge` provides:
```csharp
// Analyze folder before enabling grouping
await bridge.SetSourceFolderAsync(folder);
bool canGroup = bridge.GroupingEnabled; // Based on folder properties

// Attempt grouping (respects folder capabilities)
bool success = await bridge.SetGroupingAsync("Artist"); // Returns false if not supported

// Force grouping (bypass PKEY_IsGroup)
bridge.ForceEnableGrouping();
bridge.SetGrouping("FileType"); // Always works when forced
```

## Usage Example

### Basic Property-Aware Grouping
```csharp
// Initialize property-aware demo
var demo = new PropertyAwareCollectionViewSourceStorageDemo();
tableView.ItemsSource = demo.AdaptedItems;

// Load folder and analyze capabilities
var result = await demo.LoadStorageItemsAsync(musicFolder);
Console.WriteLine(result); 
// "Loaded 47 items (42 files, 5 folders) - Grouping Enabled"

Console.WriteLine($"Available properties: {string.Join(", ", result.AvailableGroupingProperties)}");
// "Available properties: Name, DateModified, Size, ItemType, FileType, Artist, Album, Genre, Year"

// Apply folder-appropriate grouping
var groupResult = await demo.ApplyGroupingAsync("Artist");
Console.WriteLine(groupResult);
// "Grouped by Artist"

// Try unsupported property in non-music folder
var documentsResult = await demo.ApplyGroupingAsync("Artist");
Console.WriteLine(documentsResult);
// "Property 'Artist' not available for grouping in this folder type"
```

### Folder Type Detection
```csharp
// Music folder
await demo.LoadStorageItemsAsync(musicFolder);
var musicProps = demo.AvailableGroupingProperties;
// ["Name", "DateModified", "Size", "Artist", "Album", "Genre", "Year"]

// Pictures folder  
await demo.LoadStorageItemsAsync(picturesFolder);
var pictureProps = demo.AvailableGroupingProperties;
// ["Name", "DateModified", "Size", "DateTaken", "CameraModel", "Dimensions"]

// Generic folder (no special properties)
await demo.LoadStorageItemsAsync(genericFolder);
var genericProps = demo.AvailableGroupingProperties;
// ["Name", "DateModified", "Size", "ItemType", "FileType"]
```

### PKEY_IsGroup Respect
```csharp
// Folder that doesn't support grouping
await demo.LoadStorageItemsAsync(systemFolder);
Console.WriteLine($"Grouping enabled: {demo.GroupingEnabled}"); // False

var result = await demo.ApplyGroupingAsync("FileType");
Console.WriteLine(result); 
// "Grouping not supported by this folder (PKEY_IsGroup = false)"

// Force grouping if needed
var forceResult = demo.ForceGrouping("FileType");
Console.WriteLine(forceResult);
// "Forced grouping by FileType (bypassed PKEY_IsGroup)"
```

## Integration with Existing Storage Page

### Enhanced StorageGroupingPage Integration
```csharp
public sealed partial class StorageGroupingPage : Page
{
    private PropertyAwareCollectionViewSourceStorageDemo? _propertyAwareDemo;

    private async Task LoadFilesWithPropertyAwarenessAsync()
    {
        _propertyAwareDemo = new PropertyAwareCollectionViewSourceStorageDemo();
        
        // Load folder and analyze properties
        var result = await _propertyAwareDemo.LoadStorageItemsAsync(selectedFolder);
        
        if (result.Success)
        {
            // Update UI with property-aware capabilities
            StorageTable.ItemsSource = _propertyAwareDemo.AdaptedItems;
            UpdateGroupingOptionsUI(result.AvailableGroupingProperties);
            
            StatusMessage = result.ToString();
            
            // Show grouping availability
            GroupingAvailabilityText.Text = result.GroupingEnabled 
                ? "? Folder supports grouping" 
                : "? Folder does not support grouping (PKEY_IsGroup)";
        }
    }

    private void UpdateGroupingOptionsUI(string[] availableProperties)
    {
        GroupByComboBox.Items.Clear();
        
        foreach (var property in availableProperties)
        {
            var displayName = property switch
            {
                "Artist" => "Artist",
                "Album" => "Album", 
                "Genre" => "Genre",
                "DateTaken" => "Date Taken",
                "CameraModel" => "Camera Model",
                _ => property
            };
            
            GroupByComboBox.Items.Add(new ComboBoxItem 
            { 
                Content = displayName, 
                Tag = property 
            });
        }
        
        GroupByComboBox.Items.Add(new ComboBoxItem { Content = "None", Tag = "" });
    }

    private async void OnGroupByChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_propertyAwareDemo == null) return;
        
        var item = (ComboBoxItem)GroupByComboBox.SelectedItem;
        var property = item?.Tag?.ToString();
        
        var result = await _propertyAwareDemo.ApplyGroupingAsync(property ?? "");
        StatusMessage = result.ToString();
        
        // Show force option if grouping failed
        if (!result.Applied && !string.IsNullOrEmpty(property))
        {
            ForceGroupingButton.Visibility = Visibility.Visible;
            ForceGroupingButton.Content = $"Force Group by {item?.Content}";
        }
        else
        {
            ForceGroupingButton.Visibility = Visibility.Collapsed;
        }
    }

    private void OnForceGrouping(object sender, RoutedEventArgs e)
    {
        if (_propertyAwareDemo == null) return;
        
        var item = (ComboBoxItem)GroupByComboBox.SelectedItem;
        var property = item?.Tag?.ToString();
        
        if (!string.IsNullOrEmpty(property))
        {
            var result = _propertyAwareDemo.ForceGrouping(property);
            StatusMessage = result.ToString();
            ForceGroupingButton.Visibility = Visibility.Collapsed;
        }
    }
}
```

### XAML Enhancement
```xml
<!-- Grouping Status Display -->
<TextBlock x:Name="GroupingAvailabilityText" 
           Foreground="{ThemeResource TextFillColorSecondaryBrush}"
           Margin="0,4,0,0"/>

<!-- Force Grouping Button (shown when grouping fails) -->
<Button x:Name="ForceGroupingButton"
        Click="OnForceGrouping"
        Visibility="Collapsed"
        Style="{StaticResource AccentButtonStyle}"/>

<!-- Enhanced Status with Property Info -->
<TextBlock Text="{x:Bind PropertyAwareStatusMessage, Mode=OneWay}"
           TextWrapping="Wrap"/>
```

## Property System Benefits

### 1. Windows Shell Consistency
- Behaves like Windows File Explorer for grouping availability
- Respects folder-specific grouping capabilities  
- Uses native Windows property system metadata

### 2. Intelligent Property Detection
- **Music Folders**: Artist, Album, Genre, Year, Rating
- **Picture Folders**: Date Taken, Camera Model, Dimensions, GPS
- **Video Folders**: Length, Resolution, Frame Rate
- **Document Folders**: Authors, Keywords, Categories, Tags

### 3. Graceful Fallbacks
- Falls back to basic properties when extended properties unavailable
- Continues working even if property system access fails
- Provides force override for special cases

### 4. Performance Optimization
- Loads extended properties asynchronously
- Caches property analysis results
- Only queries relevant properties per folder type

## Result

This property-aware implementation provides:

- ? **Native Windows Integration** - Respects PKEY_IsGroup like File Explorer
- ? **Smart Property Detection** - Shows relevant properties per folder type  
- ? **Graceful Degradation** - Works even when property system fails
- ? **Force Override** - Allows bypassing restrictions when needed
- ? **Performance Optimized** - Async property loading and caching
- ? **Developer Friendly** - Clear success/failure feedback

The implementation successfully bridges TableView with Windows Property System for authentic shell-like grouping behavior.