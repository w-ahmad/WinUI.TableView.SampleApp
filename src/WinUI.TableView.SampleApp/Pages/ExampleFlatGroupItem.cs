using Microsoft.UI.Xaml;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinUI.TableView.SampleApp.Pages;

/// <summary>
/// Represents a flattened item for ExampleModel hierarchical grouping.
/// This follows the same proven pattern as StorageGroupingPage.
/// </summary>
public sealed class ExampleFlatGroupItem : INotifyPropertyChanged
{
    private bool _isExpanded = true;

    public ExampleFlatGroupItem(ExampleModel? item, int depth, bool isGroupHeader, string? groupKey = null)
    {
        Item = item;
        Depth = depth;
        IsGroupHeader = isGroupHeader;
        GroupKey = groupKey;
    }

    /// <summary>
    /// Gets the underlying ExampleModel item (null for group headers).
    /// </summary>
    public ExampleModel? Item { get; }

    /// <summary>
    /// Gets the depth in the hierarchy (0 for group headers, 1 for data items).
    /// </summary>
    public int Depth { get; }

    /// <summary>
    /// Gets a value indicating whether this is a group header row.
    /// </summary>
    public bool IsGroupHeader { get; }

    /// <summary>
    /// Gets the group key if this is a group header.
    /// </summary>
    public string? GroupKey { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the group is expanded (only relevant for group headers).
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
                OnPropertyChanged(nameof(ChevronGlyph));
            }
        }
    }

    /// <summary>
    /// Gets the indentation margin based on depth.
    /// </summary>
    public Thickness Indent => new(left: Depth * 24, top: 0, right: 0, bottom: 0);

    /// <summary>
    /// Gets the icon glyph for expand/collapse chevron.
    /// </summary>
    public string ChevronGlyph => IsExpanded ? "\uE70E" /*ChevronDown*/ : "\uE76C" /*ChevronRight*/;

    // Display properties for UI binding
    public string DisplayName => IsGroupHeader ? 
        (GroupKey ?? "(Unknown)") : 
        $"{Item?.FirstName} {Item?.LastName}".Trim();

    public string FileIcon => IsGroupHeader ? "\uE8B7" /*Folder*/ : "\uE77B" /*Person*/;

    public string Email => IsGroupHeader ? string.Empty : Item?.Email ?? string.Empty;
    public string Department => IsGroupHeader ? string.Empty : Item?.Department ?? string.Empty;
    public string Gender => IsGroupHeader ? string.Empty : Item?.Gender ?? string.Empty;
    public string Designation => IsGroupHeader ? string.Empty : Item?.Designation ?? string.Empty;
    public string DateOfBirth => IsGroupHeader ? string.Empty : 
        Item?.Dob.ToString("MM/dd/yyyy") ?? string.Empty;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}