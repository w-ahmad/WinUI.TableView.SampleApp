# WinRT Adapter Layer - Phase 1 Implementation

## Overview

This directory contains the adapter layer that enables hierarchical grouping to work with multiple data sources, including WinRT Storage APIs (`IStorageItem`, `IStorageFile`, `IStorageFolder`) and custom application models.

## Architecture

The adapter pattern allows the `HierarchyStyleGroupingPage` to work with any data type through a common interface, making it compatible with:
- Custom application models (e.g., `ExampleModel`)
- WinRT Storage items (files and folders from known folders)
- High-performance file collections using `FileInformationFactory`

## Files

### `IGroupableItem.cs`
The core interface that all data adapters implement. Provides:
- `Name`: Display name for the item
- `GetPropertyValue(string)`: Dynamic property access for grouping/filtering
- `DateModified`: Last modified date
- `Size`: Item size in bytes
- `ItemType`: Type descriptor (File, Folder, Person, etc.)
- `FileType`: File extension (if applicable)
- `UnderlyingItem`: Access to the original object

### `ExampleModelAdapter.cs`
Adapter for existing `ExampleModel` objects. Maps person-related properties to the `IGroupableItem` interface.

**Usage:**
```csharp
var model = new ExampleModel { FirstName = "John", LastName = "Doe" };
var adapter = new ExampleModelAdapter(model);
string name = adapter.Name; // "John Doe"
object? dept = adapter.GetPropertyValue("Department");
```

### `StorageItemAdapter.cs`
Adapter for WinRT `IStorageItem` (files and folders). Provides async initialization for property loading.

**Usage:**
```csharp
IStorageFile file = await folder.GetFileAsync("document.txt");
var adapter = new StorageItemAdapter(file);
await adapter.InitializeAsync(); // Load properties
ulong size = adapter.Size;
DateTimeOffset modified = adapter.DateModified;
```

**Important:** Always call `InitializeAsync()` before accessing `Size` or `DateModified` properties.

### `FileInformationAdapter.cs`
High-performance adapter for `FileInformation` objects from `Windows.Storage.BulkAccess.FileInformationFactory`.

This is the **recommended adapter for large file collections** because:
- Properties are pre-loaded and cached
- Supports thumbnail access
- No async initialization needed
- Optimized for batch operations

**Usage:**
```csharp
var query = folder.CreateFileQuery(CommonFileQuery.OrderByName);
var factory = new FileInformationFactory(query, ThumbnailMode.ListView, 100);
var fileInfos = await factory.GetFilesAsync(0, 100);

foreach (var fileInfo in fileInfos)
{
    var adapter = new FileInformationAdapter(fileInfo);
    // Properties are immediately available
    string name = adapter.Name;
    ulong size = adapter.Size;
    var thumbnail = adapter.Thumbnail; // Access thumbnail if loaded
}
```

### `GroupKeyFormatter.cs`
Static helper class for formatting property values into user-friendly group keys.

**Supported Grouping Patterns:**

#### Date Grouping
Formats `DateTimeOffset`, `DateTime`, and `DateOnly` values into relative time groups:
- Today
- Yesterday
- This Week
- Last Week
- This Month
- Last Month
- [Month Year] (e.g., "January 2024")
- [Year] (e.g., "2023")

#### Size Grouping
Formats file sizes into intuitive categories:
- Empty (0 bytes)
- Tiny (< 1 KB)
- Small (< 100 KB)
- Medium (< 1 MB)
- Large (< 10 MB)
- Very Large (< 100 MB)
- Huge (< 1 GB)
- Gigantic (> 1 GB)

#### File Type Grouping
Groups file extensions into categories:
- Documents (.doc, .pdf, .txt, etc.)
- Spreadsheets (.xls, .csv, etc.)
- Presentations (.ppt, etc.)
- Images (.jpg, .png, .gif, etc.)
- Videos (.mp4, .avi, etc.)
- Audio (.mp3, .wav, etc.)
- Archives (.zip, .rar, etc.)
- Code Files (.cs, .xaml, .json, etc.)
- Applications (.exe, .dll, etc.)

#### Alphabetical Grouping
Groups by first letter in ranges:
- A - C, D - F, G - I, J - L, M - O, P - R, S - U, V - X, Y - Z, # (Other)

**Usage:**
```csharp
// Format a date
var date = DateTimeOffset.Now.AddDays(-5);
string group = GroupKeyFormatter.FormatGroupKey("DateModified", date); // "This Week"

// Format a size
ulong size = 5 * 1024 * 1024; // 5 MB
string sizeGroup = GroupKeyFormatter.FormatGroupKey("Size", size); // "Large (< 10 MB)"

// Format a file extension
string typeGroup = GroupKeyFormatter.FormatGroupKey("FileType", ".jpg"); // "Images"

// Get sort key for proper ordering
string sortKey = GroupKeyFormatter.GetSortKey("DateModified", group);
```

## Integration with HierarchyStyleGroupingPage

The next phase will update `HierarchyStyleGroupingPage` to use these adapters:

1. Replace `ExampleModel` with `IGroupableItem` throughout
2. Use `GroupKeyFormatter` for smart grouping
3. Support async loading for storage items
4. Enable grouping by storage-specific properties (DateModified, Size, FileType)

## Next Steps (Phase 2)

- [ ] Update `FlatGroupItem` to use `IGroupableItem`
- [ ] Modify `GroupItemsByProperty` to work with adapters
- [ ] Integrate `GroupKeyFormatter` for automatic formatting
- [ ] Add async support for loading storage items
- [ ] Create example page demonstrating file system grouping

## Performance Considerations

### For Small Collections (< 100 items)
Use `StorageItemAdapter` with direct folder queries:
```csharp
var files = await folder.GetFilesAsync();
var adapters = new List<IGroupableItem>();
foreach (var file in files)
{
    var adapter = new StorageItemAdapter(file);
    await adapter.InitializeAsync();
    adapters.Add(adapter);
}
```

### For Large Collections (100+ items)
Use `FileInformationAdapter` with `FileInformationFactory`:
```csharp
var query = folder.CreateFileQuery(CommonFileQuery.OrderByName);
var factory = new FileInformationFactory(query, ThumbnailMode.ListView, 100);
var fileInfos = await factory.GetFilesAsync(0, 1000);
var adapters = fileInfos.Select(f => new FileInformationAdapter(f)).ToList();
```

### For Very Large Collections (1000+ items)
Consider implementing incremental loading with `ISupportIncrementalLoading` (future enhancement).

## Design Patterns Used

- **Adapter Pattern**: Wraps different data types with a common interface
- **Strategy Pattern**: `GroupKeyFormatter` provides different formatting strategies
- **Factory Pattern**: Future enhancement will add factory methods for creating adapters

## Testing Checklist

- [x] Interface defined with all required properties
- [x] ExampleModelAdapter maps all relevant properties
- [x] StorageItemAdapter handles files and folders
- [x] FileInformationAdapter provides high-performance access
- [x] GroupKeyFormatter handles all common grouping scenarios
- [x] Sort keys ensure proper group ordering
- [ ] Unit tests for adapters (Phase 2)
- [ ] Integration tests with HierarchyStyleGroupingPage (Phase 2)
- [ ] Performance tests with large file collections (Phase 3)
