using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class GroupingExamplePage : Page
{
    public GroupingExamplePage()
    {
        this.InitializeComponent();
        
        // Initialize grouping (matching the selected ComboBox item)
        GroupedTableView.GroupDescriptions.Add(new GroupDescription { PropertyName = "Department" });
    }

    private void GroupByComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Guard against event firing during initialization before controls are created
        if (GroupedTableView == null || GroupByComboBox.SelectedItem is not ComboBoxItem item)
        {
            return;
        }
        
        if (item.Tag is string propertyName)
        {
            // Clear existing grouping
            GroupedTableView.GroupDescriptions.Clear();
            
            if (!string.IsNullOrEmpty(propertyName))
            {
                // Add grouping by selected property
                GroupedTableView.GroupDescriptions.Add(new GroupDescription
                {
                    PropertyName = propertyName
                });
            }
        }
    }

    private void ExpandAll_Click(object sender, RoutedEventArgs e)
    {
        ExpandAllGroups(GroupedTableView);
    }

    private void CollapseAll_Click(object sender, RoutedEventArgs e)
    {
        CollapseAllGroups(GroupedTableView);
    }

    private void TableView_GroupExpandingCollapsing(object sender, TableViewGroupExpandingEventArgs e)
    {
        var action = e.IsExpanding ? "expanding" : "collapsing";
        Debug.WriteLine($"Group '{e.Group.Group}' is {action}");
    }

    /// <summary>
    /// Expands all groups in the specified TableView
    /// </summary>
    private void ExpandAllGroups(TableView tableView)
    {
        if (tableView.CollectionView.CollectionGroups != null)
        {
            ExpandGroupsRecursive(tableView.CollectionView.CollectionGroups.OfType<ICollectionViewGroup>());
            
            // Trigger refresh by re-adding group descriptions
            var groupDescs = tableView.GroupDescriptions.ToList();
            tableView.GroupDescriptions.Clear();
            foreach (var desc in groupDescs)
            {
                tableView.GroupDescriptions.Add(desc);
            }
        }
    }

    /// <summary>
    /// Collapses all groups in the specified TableView
    /// </summary>
    private void CollapseAllGroups(TableView tableView)
    {
        if (tableView.CollectionView.CollectionGroups != null)
        {
            CollapseGroupsRecursive(tableView.CollectionView.CollectionGroups.OfType<ICollectionViewGroup>());
            
            // Trigger refresh by re-adding group descriptions
            var groupDescs = tableView.GroupDescriptions.ToList();
            tableView.GroupDescriptions.Clear();
            foreach (var desc in groupDescs)
            {
                tableView.GroupDescriptions.Add(desc);
            }
        }
    }

    /// <summary>
    /// Recursively expands all groups and their nested groups
    /// </summary>
    private void ExpandGroupsRecursive(IEnumerable<ICollectionViewGroup> groups)
    {
        foreach (var group in groups)
        {
            // Use reflection to set IsExpanded since it's only settable in the concrete type
            var groupType = group.GetType();
            var isExpandedProperty = groupType.GetProperty("IsExpanded");
            if (isExpandedProperty != null && isExpandedProperty.CanWrite)
            {
                isExpandedProperty.SetValue(group, true);
            }
            
            // Recursively expand nested groups
            var nestedGroups = group.GroupItems.OfType<ICollectionViewGroup>();
            if (nestedGroups.Any())
            {
                ExpandGroupsRecursive(nestedGroups);
            }
        }
    }

    /// <summary>
    /// Recursively collapses all groups and their nested groups
    /// </summary>
    private void CollapseGroupsRecursive(IEnumerable<ICollectionViewGroup> groups)
    {
        foreach (var group in groups)
        {
            // Use reflection to set IsExpanded since it's only settable in the concrete type
            var groupType = group.GetType();
            var isExpandedProperty = groupType.GetProperty("IsExpanded");
            if (isExpandedProperty != null && isExpandedProperty.CanWrite)
            {
                isExpandedProperty.SetValue(group, false);
            }
            
            // Recursively collapse nested groups
            var nestedGroups = group.GroupItems.OfType<ICollectionViewGroup>();
            if (nestedGroups.Any())
            {
                CollapseGroupsRecursive(nestedGroups);
            }
        }
    }
}
