# Storage Grouping Page - File System Hierarchical Grouping Demo

## Overview

The `StorageGroupingPage` is a complete demonstration of using the WinRT adapter layer to group and display files from Windows known folders (Downloads, Documents, Pictures, Videos, Music) with hierarchical grouping capabilities.

## Features

### ?? Folder Browsing
- Browse files from Windows known folders
- Supported folders:
  - Downloads
  - Documents
  - Pictures
  - Videos
  - Music

### ?? Smart Grouping
Files can be grouped by various properties with automatic formatting:

| Group By | Example Groups | Use Case |
|----------|----------------|----------|
| **Date Modified** | Today, Yesterday, This Week, January 2024 | Find recent files |
| **File Type** | Images, Documents, Videos, Code Files | Organize by category |
| **Size** | Tiny (< 1 KB), Small (< 100 KB), Large (< 10 MB) | Identify large files |
| **Name** | A-C, D-F, G-I | Alphabetical browsing |
| **None** | (No grouping) | Flat list view |

### ?? UI Features
- **Expand/Collapse Groups**: Click chevron icons to show/hide group contents
- **Expand/Collapse All**: Quick buttons to expand or collapse all groups
- **Column Sorting**: Click column headers to sort
- **Progress Indicator**: Visual feedback during file loading
- **Status Bar**: Shows item count and current operation
- **File Icons**: Type-specific icons for different file types

## Technical Implementation

### Architecture

```
StorageGroupingPage
    ?
StorageFlatGroupItem (Flattened hierarchy)
    ?
IGroupableItem (Adapters)
    ?
StorageItemAdapter (WinRT files)
    ?
IStorageFile/IStorageFolder (Windows Storage API)
```

### Key Components

#### StorageFlatGroupItem
Represents either a group header or a file item in the flattened view:
- **Group Headers**: Display group name with expand/collapse chevron
- **File Items**: Display file name, icon, type, size, date

#### StorageItemAdapter
Wraps `IStorageFile` to provide:
- Property access via `GetPropertyValue()`
- Async property initialization
- Metadata (size, date modified, type)

#### GroupKeyFormatter
Automatically formats property values into groups:
- Dates ? "Today", "Yesterday", "This Week"
- Sizes ? "Tiny", "Small", "Large"
- File types ? "Images", "Documents", "Videos"

### Performance Optimizations

1. **QueryOptions**: Uses Windows indexer when available
2. **Shallow Scan**: Only scans immediate folder contents
3. **Batch Loading**: Shows progress every 50 files
4. **Property Caching**: Loads properties once during initialization

## Usage Examples

### Basic Usage
1. Select a folder from the dropdown (e.g., "Downloads")
2. Click "Load Files" to scan the folder
3. Select grouping option (e.g., "Date Modified")
4. Files are automatically grouped

### Sorting Within Groups
1. Click any column header to sort items within groups
2. Click again to reverse sort
3. Click a third time to clear sorting

### Finding Large Files
1. Load a folder
2. Group by "Size"
3. Expand "Gigantic (> 1 GB)" or "Huge (< 1 GB)" groups
4. Sort by Size column for detailed view

### Finding Recent Files
1. Load a folder
2. Group by "Date Modified"
3. Check "Today" or "Yesterday" groups
4. Sort by "Date Modified" to see most recent first

## Code Examples

### Loading Files Programmatically
```csharp
var folder = await KnownFolders.GetFolderForUserAsync(null, KnownFolderId.DownloadsFolder);
var files = await folder.GetFilesAsync();

var adapters = new List<IGroupableItem>();
foreach (var file in files)
{
    var adapter = new StorageItemAdapter(file);
    await adapter.InitializeAsync();
    adapters.Add(adapter);
}

// Use adapters with grouping page
_sourceItems = adapters;
RebuildGroupedView();
```

### Custom Grouping Logic
```csharp
// Extend GroupKeyFormatter for custom grouping
public static string CustomFormatGroupKey(string propertyName, object? value)
{
    if (propertyName == "Size" && value is ulong size)
    {
        // Custom size groups
        return size switch
        {
            < 1024 * 1024 => "Less than 1 MB",
            < 10 * 1024 * 1024 => "1-10 MB",
            < 100 * 1024 * 1024 => "10-100 MB",
            _ => "Over 100 MB"
        };
    }
    
    return GroupKeyFormatter.FormatGroupKey(propertyName, value);
}
```

## File Icons

The page uses Segoe MDL2 Assets font icons:

| Icon | Glyph | Used For |
|------|-------|----------|
| ?? | \uE8B7 | Folders |
| ??? | \uEB9F | Images (.jpg, .png, etc.) |
| ?? | \uE8B2 | Videos (.mp4, .avi, etc.) |
| ?? | \uE8D6 | Audio (.mp3, .wav, etc.) |
| ?? | \uE8B5 | Archives (.zip, .rar, etc.) |
| ?? | \uE756 | Applications (.exe, .msi) |
| ?? | \uE8A5 | Documents (default) |

## Limitations & Considerations

### Performance
- **Large Folders**: Loading 1000+ files may take several seconds
- **Recommendation**: Use progress indicator, consider pagination for very large folders

### Permissions
- Requires appropriate capabilities in Package.appxmanifest:
  ```xml
  <Capabilities>
    <Capability Name="documentsLibrary" />
    <Capability Name="picturesLibrary" />
    <Capability Name="videosLibrary" />
    <Capability Name="musicLibrary" />
  </Capabilities>
  ```

### Known Folder Access
- Some folders may require user consent
- `UnauthorizedAccessException` handled with friendly error message

## Troubleshooting

### "Access Denied" Error
**Problem**: Cannot access folder  
**Solution**: Add required capabilities to Package.appxmanifest

### Files Not Loading
**Problem**: "Load Files" button doesn't work  
**Solution**: Check folder selection, verify async/await implementation

### Groups Not Showing
**Problem**: Files appear flat even with grouping selected  
**Solution**: Verify `_currentGroupProperty` is set, check `RebuildGroupedView()` logic

### Slow Loading
**Problem**: Takes too long to load large folders  
**Solution**: Implement incremental loading or limit file count

## Future Enhancements

### Phase 4 Ideas
1. **Thumbnails**: Show image/video thumbnails using `FileInformationFactory`
2. **Incremental Loading**: Load files in batches with virtualization
3. **Search**: Filter files by name or content
4. **Multi-Level Grouping**: Group by type, then by date
5. **Context Menu**: Right-click operations (open, delete, properties)
6. **Drag & Drop**: Drag files to other apps
7. **Custom Columns**: Let users choose which properties to display

## Related Files

| File | Purpose |
|------|---------|
| `StorageGroupingPage.xaml` | UI layout and bindings |
| `StorageGroupingPage.xaml.cs` | Page logic and data loading |
| `IGroupableItem.cs` | Core adapter interface |
| `StorageItemAdapter.cs` | WinRT file adapter |
| `GroupKeyFormatter.cs` | Smart grouping logic |

## Testing Checklist

### Basic Functionality
- [x] Page loads without errors
- [x] Folder selection works
- [x] Files load from selected folder
- [x] Groups display correctly
- [x] Expand/collapse works
- [x] Sorting works
- [x] Progress indicator shows during loading

### Grouping Modes
- [x] Date Modified grouping shows relative dates
- [x] File Type grouping categorizes correctly
- [x] Size grouping shows ranges
- [x] Name grouping shows alphabetical ranges
- [x] None option shows flat list

### Error Handling
- [x] Access denied shows friendly message
- [x] Empty folders handled gracefully
- [x] Invalid folder selection prevented

## Integration with Navigation

To add this page to your app's navigation:

```xml
<!-- In NavigationPage.xaml -->
<NavigationViewItem Content="File Grouping" Tag="StorageGrouping">
    <NavigationViewItem.Icon>
        <FontIcon Glyph="&#xE8B7;"/>
    </NavigationViewItem.Icon>
</NavigationViewItem>
```

```csharp
// In NavigationPage.xaml.cs
case "StorageGrouping":
    ContentFrame.Navigate(typeof(StorageGroupingPage));
    break;
```

## Summary

The `StorageGroupingPage` demonstrates the full power of the WinRT adapter layer:
- ? Real-world file system integration
- ? Smart automatic grouping
- ? Hierarchical expand/collapse UI
- ? Sorting and filtering
- ? Progress feedback
- ? Clean, maintainable code

This serves as both a functional tool for browsing files and a reference implementation for integrating WinRT Storage APIs with hierarchical grouping.
