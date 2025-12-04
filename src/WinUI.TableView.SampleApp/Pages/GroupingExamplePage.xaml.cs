using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using WinUI.TableView.SampleApp.Adapters;

namespace WinUI.TableView.SampleApp.Pages;

/// <summary>
/// Represents a flattened item that can be either a group header or a data item.
/// This is similar to FlatNode in the Hierarchy example.
/// </summary>
public sealed class FlatGroupItem : INotifyPropertyChanged
{
    private bool _isExpanded = true;

    public FlatGroupItem(object item, int depth, bool isGroupHeader, string? groupKey = null)
    {
        Item = item;
        Depth = depth;
        IsGroupHeader = isGroupHeader;
        GroupKey = groupKey;
    }

    /// <summary>
    /// The underlying item (either IGroupableItem or a group key object for headers)
    /// </summary>
    public object Item { get; }

    /// <summary>
    /// Depth in the hierarchy (0 for group headers, 1 for data items)
    /// </summary>
    public int Depth { get; }

    /// <summary>
    /// Whether this is a group header row
    /// </summary>
    public bool IsGroupHeader { get; }

    /// <summary>
    /// The group key if this is a group header
    /// </summary>
    public string? GroupKey { get; }

    /// <summary>
    /// Gets the groupable item (only if this is not a group header)
    /// </summary>
    public IGroupableItem? GroupableItem => IsGroupHeader ? null : Item as IGroupableItem;

    /// <summary>
    /// Whether the group is expanded (only relevant for group headers)
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ChevronGlyph)); // Notify that the chevron glyph changed too
            }
        }
    }

    /// <summary>
    /// Indentation margin based on depth
    /// </summary>
    public Thickness Indent => new(left: Depth * 24, top: 0, right: 0, bottom: 0);

    /// <summary>
    /// The data item (only if this is not a group header and the underlying item is an ExampleModel adapter)
    /// </summary>
    public ExampleModel? DataItem => (GroupableItem as ExampleModelAdapter)?.Model;

    /// <summary>
    /// Icon glyph for expand/collapse chevron
    /// </summary>
    public string ChevronGlyph => IsExpanded ? "\uE70E" /*ChevronDown*/ : "\uE76C" /*ChevronRight*/;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Demonstrates grouping using a hierarchy-style flattening approach.
/// This adapts the HierarchyPage pattern for property-based grouping.
/// </summary>
public sealed partial class GroupingExamplePage : Page
{
    public ObservableCollection<FlatGroupItem> FlatItems { get; } = [];
    private string _currentGroupProperty = "Department";
    private string? _sortPropertyName;
    private SortDirection? _sortDirection;
    private readonly Dictionary<string, HashSet<object>> _activeFilters = new();
    private List<IGroupableItem> _sourceItems = [];

    public GroupingExamplePage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Loads data from any collection of IGroupableItem.
    /// This enables working with files, custom models, or any adapted data source.
    /// </summary>
    /// <param name="items">The items to display and group.</param>
    public void LoadData(IEnumerable<IGroupableItem> items)
    {
        _sourceItems = items?.ToList() ?? [];
        RebuildGroupedView();
    }

    /// <summary>
    /// Initializes the filter handler with the view model
    /// </summary>
    private void InitializeFilterHandler(ExampleViewModel viewModel)
    {
        HierarchyGroupTable.FilterHandler = new GroupingExampleFilterHandler(HierarchyGroupTable, this, viewModel);
    }

    /// <summary>
    /// Represents a group with its header and items
    /// </summary>
    private class GroupData
    {
        public required string GroupKey { get; init; }
        public required List<IGroupableItem> Items { get; init; }
        public bool IsExpanded { get; set; } = true;
    }

    /// <summary>
    /// Rebuilds the flattened view based on current grouping property
    /// </summary>
    internal void RebuildGroupedView()
    {
        // Use source items if available, otherwise try to get from DataContext
        if (_sourceItems.Count == 0 && DataContext is ExampleViewModel viewModel)
        {
            // Wrap ExampleModel items in adapters for backward compatibility
            _sourceItems = viewModel.Items.Select(m => (IGroupableItem)new ExampleModelAdapter(m)).ToList();
        }

        if (_sourceItems.Count == 0)
        {
            return;
        }

        // Remember which groups were expanded
        var expandedGroups = FlatItems
            .Where(f => f.IsGroupHeader && f.IsExpanded)
            .Select(f => f.GroupKey)
            .ToHashSet();

        FlatItems.Clear();

        // Apply filters to get the base set of items
        var filteredItems = ApplyFilters(_sourceItems);

        if (string.IsNullOrEmpty(_currentGroupProperty))
        {
            // No grouping - just show all items flat
            var items = ApplySorting(filteredItems);
            foreach (var item in items)
            {
                FlatItems.Add(new FlatGroupItem(item, depth: 0, isGroupHeader: false));
            }
            return;
        }

        // Group items by the selected property
        var groups = GroupItemsByProperty(filteredItems, _currentGroupProperty);

        // Flatten the groups into the view (similar to HierarchyPage.RebuildFlat)
        // Order groups using smart sort keys from GroupKeyFormatter
        var orderedGroups = groups.OrderBy(g => GroupKeyFormatter.GetSortKey(_currentGroupProperty, g.GroupKey));
        
        foreach (var group in orderedGroups)
        {
            // Restore previous expansion state, default to true
            var isExpanded = expandedGroups.Contains(group.GroupKey);
            group.IsExpanded = isExpanded;

            // Add group header at depth 0
            var headerItem = new FlatGroupItem(
                item: group.GroupKey,
                depth: 0,
                isGroupHeader: true,
                groupKey: group.GroupKey)
            {
                IsExpanded = group.IsExpanded
            };

            FlatItems.Add(headerItem);

            // Add group items at depth 1 (only if expanded), with sorting applied
            if (group.IsExpanded)
            {
                var sortedItems = ApplySorting(group.Items);
                foreach (var dataItem in sortedItems)
                {
                    FlatItems.Add(new FlatGroupItem(
                        item: dataItem,
                        depth: 1,
                        isGroupHeader: false));
                }
            }
        }
    }

    /// <summary>
    /// Groups items by a property name
    /// </summary>
    private List<GroupData> GroupItemsByProperty(IEnumerable<IGroupableItem> items, string propertyName)
    {
        var groupedDict = new Dictionary<string, List<IGroupableItem>>();

        foreach (var item in items)
        {
            var propertyValue = item.GetPropertyValue(propertyName);
            
            // Use GroupKeyFormatter for smart grouping
            var groupKey = GroupKeyFormatter.FormatGroupKey(propertyName, propertyValue);

            if (!groupedDict.ContainsKey(groupKey))
            {
                groupedDict[groupKey] = [];
            }

            groupedDict[groupKey].Add(item);
        }

        return groupedDict.Select(kvp => new GroupData
        {
            GroupKey = kvp.Key,
            Items = kvp.Value
        }).ToList();
    }

    /// <summary>
    /// Applies active filters to a collection of items
    /// </summary>
    private IEnumerable<IGroupableItem> ApplyFilters(IEnumerable<IGroupableItem> items)
    {
        if (_activeFilters.Count == 0)
        {
            return items;
        }

        return items.Where(item =>
        {
            // Item must match ALL active filters
            foreach (var filter in _activeFilters)
            {
                var propertyValue = item.GetPropertyValue(filter.Key);
                var normalizedValue = string.IsNullOrWhiteSpace(propertyValue?.ToString()) ? "(Blank)" : propertyValue;
                
                if (!filter.Value.Contains(normalizedValue))
                {
                    return false;
                }
            }
            return true;
        });
    }

    /// <summary>
    /// Gets a property value for filtering purposes (legacy support for ExampleModel)
    /// </summary>
    private static object? GetPropertyValueForFilter(ExampleModel item, string propertyName)
    {
        return propertyName switch
        {
            "FirstName" => item.FirstName,
            "LastName" => item.LastName,
            "Email" => item.Email,
            "Gender" => item.Gender,
            "Department" => item.Department,
            "Designation" => item.Designation,
            "Dob" => item.Dob,
            "ActiveAt" => item.ActiveAt,
            "IsActive" => item.IsActive,
            _ => null
        };
    }

    /// <summary>
    /// Public wrapper for GetPropertyValueForFilter (used by filter handler)
    /// </summary>
    internal object? GetPropertyValueForFilterPublic(ExampleModel item, string propertyName)
        => GetPropertyValueForFilter(item, propertyName);

    /// <summary>
    /// Updates active filters (called by filter handler)
    /// </summary>
    internal void UpdateActiveFilters(string propertyName, HashSet<object> values)
    {
        _activeFilters[propertyName] = values;
    }

    /// <summary>
    /// Removes a filter (called by filter handler)
    /// </summary>
    internal void RemoveActiveFilter(string propertyName)
    {
        _activeFilters.Remove(propertyName);
    }

    /// <summary>
    /// Clears all filters (called by filter handler)
    /// </summary>
    internal void ClearAllFilters()
    {
        _activeFilters.Clear();
    }

    /// <summary>
    /// Applies sorting to a collection of items based on current sort state
    /// </summary>
    private IEnumerable<IGroupableItem> ApplySorting(IEnumerable<IGroupableItem> items)
    {
        if (_sortDirection is null || string.IsNullOrEmpty(_sortPropertyName))
        {
            return items;
        }

        // Sort using property values from the adapter
        bool isAscending = _sortDirection == SortDirection.Ascending;

        var sortedItems = items.OrderBy(item => item.GetPropertyValue(_sortPropertyName), 
            Comparer<object?>.Create((a, b) =>
            {
                // Handle nulls
                if (a is null && b is null) return 0;
                if (a is null) return -1;
                if (b is null) return 1;

                // Handle comparable types
                if (a is IComparable ca && b is IComparable cb && a.GetType() == b.GetType())
                {
                    try { return ca.CompareTo(cb); }
                    catch { /* Fall through to string comparison */ }
                }

                // Fall back to string comparison
                return string.Compare(a?.ToString(), b?.ToString(), StringComparison.OrdinalIgnoreCase);
            }));

        return isAscending ? sortedItems : sortedItems.Reverse();
    }

    /// <summary>
    /// Handles toggling a group's expand/collapse state
    /// </summary>
    private void OnToggleGroup(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not FlatGroupItem headerItem || !headerItem.IsGroupHeader)
        {
            return;
        }

        var index = FlatItems.IndexOf(headerItem);
        if (index < 0)
        {
            return;
        }

        if (headerItem.IsExpanded)
        {
            CollapseGroup(headerItem, index);
        }
        else
        {
            ExpandGroup(headerItem, index);
        }
    }

    /// <summary>
    /// Expands a group by inserting its items after the header
    /// </summary>
    private void ExpandGroup(FlatGroupItem headerItem, int headerIndex)
    {
        headerItem.IsExpanded = true;

        // Apply filters first, then group
        var filteredItems = ApplyFilters(_sourceItems);
        var groupItems = GroupItemsByProperty(filteredItems, _currentGroupProperty)
            .FirstOrDefault(g => g.GroupKey == headerItem.GroupKey);

        if (groupItems is null)
        {
            return;
        }

        // Apply sorting and insert items right after the header
        var sortedItems = ApplySorting(groupItems.Items);
        var insertAt = headerIndex + 1;
        foreach (var dataItem in sortedItems)
        {
            FlatItems.Insert(insertAt++, new FlatGroupItem(
                item: dataItem,
                depth: 1,
                isGroupHeader: false));
        }
    }

    /// <summary>
    /// Collapses a group by removing all items under the header
    /// </summary>
    private void CollapseGroup(FlatGroupItem headerItem, int headerIndex)
    {
        headerItem.IsExpanded = false;

        // Remove all items at depth > 0 that come after this header
        var removeFrom = headerIndex + 1;
        while (removeFrom < FlatItems.Count && FlatItems[removeFrom].Depth > 0)
        {
            FlatItems.RemoveAt(removeFrom);
        }
    }

    /// <summary>
    /// Expands all groups
    /// </summary>
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

    /// <summary>
    /// Collapses all groups
    /// </summary>
    private void OnCollapseAll(object sender, RoutedEventArgs e)
    {
        // Go backwards to avoid index issues
        for (int i = FlatItems.Count - 1; i >= 0; i--)
        {
            var item = FlatItems[i];
            if (item.IsGroupHeader && item.IsExpanded)
            {
                CollapseGroup(item, i);
            }
        }
    }

    /// <summary>
    /// Handles changing the grouping property
    /// </summary>
    private void OnGroupByChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox comboBox || comboBox.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        _currentGroupProperty = item.Tag?.ToString() ?? "";
        RebuildGroupedView();
    }

    /// <summary>
    /// Called when DataContext changes
    /// </summary>
    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (DataContext is ExampleViewModel viewModel)
        {
            InitializeFilterHandler(viewModel);
        }
        RebuildGroupedView();
    }

    /// <summary>
    /// Handles column sorting
    /// </summary>
    private void OnTableSorting(object sender, TableViewSortingEventArgs e)
    {
        if (e.Column?.Tag is not string propertyName)
        {
            return;
        }

        // Toggle sort direction: null ? Ascending ? Descending ? null
        var nextDirection = e.Column.SortDirection switch
        {
            SortDirection.Ascending => SortDirection.Descending,
            SortDirection.Descending => (SortDirection?)null,
            _ => SortDirection.Ascending
        };

        // Update column sort direction
        e.Column.SortDirection = nextDirection;

        // Update our tracking fields
        _sortPropertyName = nextDirection.HasValue ? propertyName : null;
        _sortDirection = nextDirection;

        // Rebuild the view with new sort
        RebuildGroupedView();

        e.Handled = true;
    }

    /// <summary>
    /// Handles clearing sorting
    /// </summary>
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
}

/// <summary>
/// Custom filter handler for the grouping example page that filters the underlying data
/// </summary>
public class GroupingExampleFilterHandler : ColumnFilterHandler
{
    private readonly GroupingExamplePage _page;
    private readonly ExampleViewModel _viewModel;
    private readonly TableView _tableView;

    public GroupingExampleFilterHandler(TableView tableView, GroupingExamplePage page, ExampleViewModel viewModel) 
        : base(tableView)
    {
        _tableView = tableView;
        _page = page;
        _viewModel = viewModel;
    }

    public override IList<TableViewFilterItem> GetFilterItems(TableViewColumn column, string? searchText)
    {
        if (column?.Tag is not string propertyName)
        {
            return [];
        }

        // Get all unique values from the full dataset (not filtered)
        var allValues = _viewModel.Items
            .Select(item => _page.GetPropertyValueForFilterPublic(item, propertyName))
            .Select(v => string.IsNullOrWhiteSpace(v?.ToString()) ? "(Blank)" : v)
            .Distinct()
            .OrderBy(v => v?.ToString())
            .ToList();

        // Determine which values are currently selected
        var selectedValues = SelectedValues.TryGetValue(column, out var selected) ? selected : [];
        bool isSelected(object? value) => !column.IsFiltered || selectedValues.Contains(value!);

        return allValues
            .Where(v => string.IsNullOrEmpty(searchText) || 
                       v?.ToString()?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true)
            .Select(v => new TableViewFilterItem(isSelected(v), v!))
            .ToList();
    }

    public override void ApplyFilter(TableViewColumn column)
    {
        if (column?.Tag is not string propertyName)
        {
            return;
        }

        if (!column.IsFiltered)
        {
            column.IsFiltered = true;
        }

        // Update the page's active filters
        _page.UpdateActiveFilters(propertyName, SelectedValues[column].ToHashSet());

        // Rebuild the view with filters applied
        _page.RebuildGroupedView();
    }

    public override void ClearFilter(TableViewColumn? column)
    {
        if (column is not null)
        {
            column.IsFiltered = false;
            SelectedValues.Remove(column);

            if (column.Tag is string propertyName)
            {
                _page.RemoveActiveFilter(propertyName);
            }
        }
        else
        {
            // Clear all filters
            SelectedValues.Clear();
            _page.ClearAllFilters();
            
            foreach (var col in _tableView.Columns)
            {
                if (col is not null)
                {
                    col.IsFiltered = false;
                }
            }
        }

        _page.RebuildGroupedView();
    }
}
