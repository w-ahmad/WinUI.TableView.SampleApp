using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

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
    /// The underlying item (either ExampleModel or a group key object)
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
            }
        }
    }

    /// <summary>
    /// Indentation margin based on depth
    /// </summary>
    public Thickness Indent => new(left: Depth * 24, top: 0, right: 0, bottom: 0);

    /// <summary>
    /// The data item (only if this is not a group header)
    /// </summary>
    public ExampleModel? DataItem => IsGroupHeader ? null : Item as ExampleModel;

    /// <summary>
    /// Icon glyph for expand/collapse chevron
    /// </summary>
    public string ChevronGlyph => IsExpanded ? "\uE70D" /*ChevronDown*/ : "\uE70E" /*ChevronRight*/;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Demonstrates grouping using a hierarchy-style flattening approach.
/// This adapts the HierarchyPage pattern for property-based grouping.
/// </summary>
public sealed partial class HierarchyStyleGroupingPage : Page
{
    public ObservableCollection<FlatGroupItem> FlatItems { get; } = [];
    private string _currentGroupProperty = "Department";

    public HierarchyStyleGroupingPage()
    {
        InitializeComponent();
        RebuildGroupedView();
    }

    /// <summary>
    /// Represents a group with its header and items
    /// </summary>
    private class GroupData
    {
        public required string GroupKey { get; init; }
        public required List<ExampleModel> Items { get; init; }
        public bool IsExpanded { get; set; } = true;
    }

    /// <summary>
    /// Rebuilds the flattened view based on current grouping property
    /// </summary>
    private void RebuildGroupedView()
    {
        if (DataContext is not ExampleViewModel viewModel)
        {
            return;
        }

        // Remember which groups were expanded
        var expandedGroups = FlatItems
            .Where(f => f.IsGroupHeader && f.IsExpanded)
            .Select(f => f.GroupKey)
            .ToHashSet();

        FlatItems.Clear();

        if (string.IsNullOrEmpty(_currentGroupProperty))
        {
            // No grouping - just show all items flat
            foreach (var item in viewModel.Items)
            {
                FlatItems.Add(new FlatGroupItem(item, depth: 0, isGroupHeader: false));
            }
            return;
        }

        // Group items by the selected property
        var groups = GroupItemsByProperty(viewModel.Items, _currentGroupProperty);

        // Flatten the groups into the view (similar to HierarchyPage.RebuildFlat)
        foreach (var group in groups.OrderBy(g => g.GroupKey))
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

            // Add group items at depth 1 (only if expanded)
            if (group.IsExpanded)
            {
                foreach (var dataItem in group.Items)
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
    private List<GroupData> GroupItemsByProperty(IEnumerable<ExampleModel> items, string propertyName)
    {
        var groupedDict = new Dictionary<string, List<ExampleModel>>();

        foreach (var item in items)
        {
            var groupKey = GetPropertyValue(item, propertyName)?.ToString() ?? "(Blank)";

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
    /// Gets a property value by name using reflection
    /// </summary>
    private static object? GetPropertyValue(ExampleModel item, string propertyName)
    {
        return propertyName switch
        {
            "Department" => item.Department,
            "Gender" => item.Gender,
            "Designation" => item.Designation,
            _ => null
        };
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
        if (DataContext is not ExampleViewModel viewModel)
        {
            return;
        }

        headerItem.IsExpanded = true;

        // Get all items for this group
        var groupItems = GroupItemsByProperty(viewModel.Items, _currentGroupProperty)
            .FirstOrDefault(g => g.GroupKey == headerItem.GroupKey);

        if (groupItems is null)
        {
            return;
        }

        // Insert items right after the header
        var insertAt = headerIndex + 1;
        foreach (var dataItem in groupItems.Items)
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
        RebuildGroupedView();
    }
}
