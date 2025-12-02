# Hierarchy-Style Grouping Prototype

## Overview

This prototype demonstrates an alternative approach to grouping in TableView by adapting the **hierarchy flattening pattern** from `HierarchyPage`. Instead of using the built-in `GroupDescriptions` and `CollectionViewGroup`, this approach manually flattens grouped data into a single observable collection, giving you fine-grained control over expand/collapse behavior.

## Key Concepts

### Similarities with Hierarchy View

Both the Hierarchy View and this Grouping approach share:

1. **Tree Structure Flattening**: Converting a nested structure into a flat list
   - Hierarchy: `Node` ? `Children` ? `FlatNode` collection
   - Grouping: `GroupKey` ? `Items` ? `FlatGroupItem` collection

2. **Expand/Collapse State**: Tracking whether nodes/groups are expanded
   - Hierarchy: `FlatNode.IsExpanded`
   - Grouping: `FlatGroupItem.IsExpanded`

3. **Depth/Indentation**: Visual hierarchy through indentation
   - Hierarchy: `FlatNode.Depth` * IndentSize
   - Grouping: `FlatGroupItem.Depth` * 24px

4. **Recursive Flattening**: Building the flat view through recursion
   - Hierarchy: `AddNodeAndChildren()` in `RebuildFlat()`
   - Grouping: `RebuildGroupedView()` with depth levels

### Differences from Built-in Grouping

| Aspect | Built-in Grouping | Hierarchy-Style Grouping |
|--------|-------------------|--------------------------|
| **Implementation** | Declarative (`GroupDescriptions`) | Imperative (manual flattening) |
| **Group Headers** | Special `TableViewGroupHeaderRow` | Regular rows with templates |
| **Expand/Collapse** | Built into CollectionView | Manual insert/remove from FlatItems |
| **Control** | Limited to CollectionView logic | Full control over every aspect |
| **Complexity** | Simple to use | More code, but more flexible |

## Architecture

### FlatGroupItem Class

The core wrapper class that represents both group headers and data items:

```csharp
public sealed class FlatGroupItem : INotifyPropertyChanged
{
    public object Item { get; }           // The actual item (group key or data)
    public int Depth { get; }             // 0 for headers, 1 for items
    public bool IsGroupHeader { get; }    // True for group headers
    public bool IsExpanded { get; set; }  // Expansion state
    public string? GroupKey { get; }      // Group identifier
    
    // Computed properties
    public Thickness Indent => new(Depth * 24, 0, 0, 0);
    public ExampleModel? DataItem => IsGroupHeader ? null : Item as ExampleModel;
    public string ChevronGlyph => IsExpanded ? "?" : "?";
}
```

### Flattening Algorithm

The `RebuildGroupedView()` method follows this pattern:

```
1. Remember which groups were expanded (preservation across rebuilds)
2. Clear the FlatItems collection
3. Group source items by property (e.g., Department)
4. For each group:
   a. Add group header at depth 0
   b. If expanded:
      - Add each item at depth 1
```

### Expand/Collapse Logic

**Expand:**
```csharp
void ExpandGroup(headerItem, headerIndex) {
    headerItem.IsExpanded = true;
    // Get items for this group
    // Insert them after the header at depth 1
}
```

**Collapse:**
```csharp
void CollapseGroup(headerItem, headerIndex) {
    headerItem.IsExpanded = false;
    // Remove all items at depth > 0 after this header
}
```

## Benefits of This Approach

### 1. **Manual Control**
You control exactly when and how items are added/removed from the view. No black-box CollectionView logic.

### 2. **Performance for Large Groups**
Only expanded groups' items are in the collection, reducing memory and rendering overhead.

### 3. **Easier Customization**
- Custom group header UI through templates
- Easy to add group-level actions (expand all, collapse all)
- Can add group metadata (item count, aggregates, etc.)

### 4. **Consistent Pattern**
If you're already using HierarchyPage, this uses the same mental model and code patterns.

### 5. **State Preservation**
Easy to save/restore expansion state across navigation, filtering, or sorting.

## When to Use Each Approach

### Use Built-in Grouping When:
- ? You want simple, declarative grouping
- ? Standard group headers are sufficient
- ? You don't need complex expand/collapse logic
- ? You're okay with CollectionView handling everything

### Use Hierarchy-Style Grouping When:
- ? You need full control over expand/collapse
- ? You want to customize group header appearance significantly
- ? You're working with very large datasets (lazy loading groups)
- ? You need to integrate with existing hierarchy patterns
- ? You want to add custom group-level functionality

## Potential Enhancements

1. **Multi-level Grouping**: Support grouping by multiple properties (e.g., Department ? Gender ? Designation)
2. **Lazy Loading**: Load group items only when expanded
3. **Group Aggregates**: Show sums, averages, counts in headers
4. **Drag & Drop**: Move items between groups
5. **Virtual Scrolling**: Only render visible groups/items
6. **Persistent State**: Save expansion state to settings/storage

## Code Files

- **`HierarchyStyleGroupingPage.xaml.cs`**: Main page logic
- **`HierarchyStyleGroupingPage.xaml`**: UI definition with templates
- **`FlatGroupItem`**: Wrapper class for flattened items
- **`InverseBoolToVisibilityConverter`**: Helper converter for XAML

## Comparison Example

### Built-in Grouping (Declarative)
```xaml
<TableView ItemsSource="{Binding Items}">
    <TableView.GroupDescriptions>
        <GroupDescription PropertyName="Department" />
    </TableView.GroupDescriptions>
</TableView>
```

### Hierarchy-Style Grouping (Imperative)
```csharp
// Build flattened view
FlatItems.Clear();
var groups = items.GroupBy(x => x.Department);
foreach (var group in groups) {
    FlatItems.Add(new FlatGroupItem(group.Key, 0, true));
    if (isExpanded) {
        foreach (var item in group) {
            FlatItems.Add(new FlatGroupItem(item, 1, false));
        }
    }
}
```

```xaml
<TableView ItemsSource="{Binding FlatItems}">
    <!-- Custom templates for headers vs items -->
</TableView>
```

## Summary

The Hierarchy-Style Grouping approach trades some simplicity for significantly more control and flexibility. It's ideal when you need to go beyond what the built-in grouping provides, or when you want consistency with an existing hierarchy implementation.

The pattern is:
1. **Wrap** items in a `FlatGroupItem` class
2. **Flatten** groups into a single ObservableCollection
3. **Expand/Collapse** by inserting/removing items
4. **Customize** through templates and bindings

This gives you the power to implement exactly the grouping behavior you need, with full transparency into how it works.
