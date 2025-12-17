using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using WinUI.TableView.SampleApp.Adapters;

namespace WinUI.TableView.SampleApp.Pages;

/// <summary>
/// Demonstrates production-ready TableView grouping using the flattened hierarchy pattern.
/// This approach provides excellent performance while maintaining full control over the UI.
/// </summary>
public sealed partial class GroupingExamplePage : Page, INotifyPropertyChanged
{
    private readonly ObservableCollection<ExampleFlatGroupItem> _flatItems = [];
    private List<ExampleModel> _sourceItems = [];
    private string _currentGroupProperty = "Department";
    private string? _sortPropertyName;
    private SortDirection? _sortDirection;

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupingExamplePage"/> class.
    /// </summary>
    public GroupingExamplePage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the flattened collection of items for data binding.
    /// </summary>
    public ObservableCollection<ExampleFlatGroupItem> FlatItems => _flatItems;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    #region Event Handlers

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (DataContext is ExampleViewModel viewModel)
        {
            _sourceItems = [.. viewModel.Items];
            RebuildGroupedView();
        }
    }

    private void OnGroupByChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
        {
            _currentGroupProperty = item.Tag?.ToString() ?? string.Empty;
            RebuildGroupedView();
        }
    }

    private void OnToggleGroup(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ExampleFlatGroupItem headerItem &&
            headerItem.IsGroupHeader)
        {
            var index = _flatItems.IndexOf(headerItem);
            if (index >= 0)
            {
                if (headerItem.IsExpanded)
                {
                    CollapseGroup(headerItem, index);
                }
                else
                {
                    ExpandGroup(headerItem, index);
                }
            }
        }
    }

    private void OnExpandAll(object sender, RoutedEventArgs e)
    {
        // Process in forward order for expand
        for (int i = 0; i < _flatItems.Count; i++)
        {
            var item = _flatItems[i];
            if (item.IsGroupHeader && !item.IsExpanded)
            {
                ExpandGroup(item, i);
            }
        }
    }

    private void OnCollapseAll(object sender, RoutedEventArgs e)
    {
        // Process in reverse order for collapse to avoid index issues
        for (int i = _flatItems.Count - 1; i >= 0; i--)
        {
            var item = _flatItems[i];
            if (item.IsGroupHeader && item.IsExpanded)
            {
                CollapseGroup(item, i);
            }
        }
    }

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
            _sortPropertyName = nextDirection.HasValue ? propertyName : null;
            _sortDirection = nextDirection;

            RebuildGroupedView();
            e.Handled = true;
        }
    }

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

    #endregion

    #region Grouping Logic

    /// <summary>
    /// Rebuilds the flattened view based on current grouping and sorting settings.
    /// </summary>
    private void RebuildGroupedView()
    {
        if (_sourceItems.Count == 0)
        {
            _flatItems.Clear();
            return;
        }

        // Remember which groups were expanded
        var expandedGroups = _flatItems
            .Where(f => f.IsGroupHeader && f.IsExpanded)
            .Select(f => f.GroupKey)
            .Where(key => key is not null)
            .ToHashSet();

        _flatItems.Clear();

        if (string.IsNullOrEmpty(_currentGroupProperty))
        {
            // No grouping - show all items flat
            var sortedItems = ApplySorting(_sourceItems);
            foreach (var item in sortedItems)
            {
                _flatItems.Add(new ExampleFlatGroupItem(item, depth: 0, isGroupHeader: false));
            }
        }
        else
        {
            // Group items by property
            var groups = GroupItemsByProperty(_sourceItems, _currentGroupProperty);
            var orderedGroups = groups
                .OrderBy(g => GroupKeyFormatter.GetSortKey(_currentGroupProperty, g.Key));

            foreach (var group in orderedGroups)
            {
                var isExpanded = expandedGroups.Contains(group.Key);

                // Add group header
                var headerItem = new ExampleFlatGroupItem(
                    item: null,
                    depth: 0,
                    isGroupHeader: true,
                    groupKey: group.Key)
                {
                    IsExpanded = isExpanded
                };

                _flatItems.Add(headerItem);

                // Add group items if expanded
                if (isExpanded)
                {
                    var sortedItems = ApplySorting(group.Value);
                    foreach (var item in sortedItems)
                    {
                        _flatItems.Add(new ExampleFlatGroupItem(
                            item: item,
                            depth: 1,
                            isGroupHeader: false));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Groups items by a specified property name.
    /// </summary>
    private Dictionary<string, List<ExampleModel>> GroupItemsByProperty(
        IEnumerable<ExampleModel> items, 
        string propertyName)
    {
        var grouped = new Dictionary<string, List<ExampleModel>>();

        foreach (var item in items)
        {
            var value = GetPropertyValue(item, propertyName);
            var groupKey = GroupKeyFormatter.FormatGroupKey(propertyName, value);

            if (!grouped.TryGetValue(groupKey, out var group))
            {
                group = [];
                grouped[groupKey] = group;
            }

            group.Add(item);
        }

        return grouped;
    }

    /// <summary>
    /// Gets the value of a property from an ExampleModel instance.
    /// </summary>
    private static object? GetPropertyValue(ExampleModel item, string propertyName)
    {
        return propertyName switch
        {
            nameof(ExampleModel.FirstName) => item.FirstName,
            nameof(ExampleModel.LastName) => item.LastName,
            nameof(ExampleModel.Email) => item.Email,
            nameof(ExampleModel.Gender) => item.Gender,
            nameof(ExampleModel.Department) => item.Department,
            nameof(ExampleModel.Designation) => item.Designation,
            nameof(ExampleModel.Dob) => item.Dob,
            nameof(ExampleModel.ActiveAt) => item.ActiveAt,
            nameof(ExampleModel.IsActive) => item.IsActive,
            _ => null
        };
    }

    /// <summary>
    /// Applies sorting to a collection of items based on current sort settings.
    /// </summary>
    private IEnumerable<ExampleModel> ApplySorting(IEnumerable<ExampleModel> items)
    {
        if (_sortDirection is null || string.IsNullOrEmpty(_sortPropertyName))
        {
            return items;
        }

        bool isAscending = _sortDirection == SortDirection.Ascending;
        var comparer = Comparer<object?>.Create((a, b) => CompareValues(a, b));
        
        var sortedItems = items.OrderBy(item => GetPropertyValue(item, _sortPropertyName), comparer);
        
        return isAscending ? sortedItems : sortedItems.Reverse();
    }

    /// <summary>
    /// Compares two values for sorting, handling nulls and different types appropriately.
    /// </summary>
    private static int CompareValues(object? a, object? b)
    {
        // Handle nulls
        if (a is null && b is null) return 0;
        if (a is null) return -1;
        if (b is null) return 1;

        // Handle comparable types
        if (a is IComparable ca && b is IComparable cb && a.GetType() == b.GetType())
        {
            try 
            { 
                return ca.CompareTo(cb); 
            }
            catch 
            { 
                // Fall through to string comparison on error
            }
        }

        // Fall back to string comparison
        return string.Compare(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Group Expansion/Collapse

    /// <summary>
    /// Expands a group by inserting its items after the header.
    /// </summary>
    private void ExpandGroup(ExampleFlatGroupItem headerItem, int headerIndex)
    {
        if (headerItem.GroupKey is null) return;

        headerItem.IsExpanded = true;

        var groupItems = GroupItemsByProperty(_sourceItems, _currentGroupProperty);
        if (groupItems.TryGetValue(headerItem.GroupKey, out var items))
        {
            var sortedItems = ApplySorting(items);
            var insertAt = headerIndex + 1;
            
            foreach (var item in sortedItems)
            {
                _flatItems.Insert(insertAt++, new ExampleFlatGroupItem(
                    item: item,
                    depth: 1,
                    isGroupHeader: false));
            }
        }
    }

    /// <summary>
    /// Collapses a group by removing all items under the header.
    /// </summary>
    private void CollapseGroup(ExampleFlatGroupItem headerItem, int headerIndex)
    {
        headerItem.IsExpanded = false;

        // Remove all items at depth > 0 that come after this header
        var removeFrom = headerIndex + 1;
        while (removeFrom < _flatItems.Count && _flatItems[removeFrom].Depth > 0)
        {
            _flatItems.RemoveAt(removeFrom);
        }
    }

    #endregion
}