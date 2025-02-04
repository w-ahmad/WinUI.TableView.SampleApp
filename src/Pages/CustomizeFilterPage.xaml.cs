using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class CustomizeFilterPage : Page
{
    public CustomizeFilterPage()
    {
        InitializeComponent();

    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (DataContext is not ExampleViewModel viewModel) return;

        tableView.FilterHandler = new FilterHandler(tableView, viewModel);
        viewModel.Items = new(ExampleViewModel.ItemsList.Take(20));
    }
}

public class FilterHandler : ColumnFilterHandler
{
    private readonly TableView _tableView;
    private readonly ExampleViewModel _viewModel;
    private readonly Dictionary<TableViewColumn, IList<object>> _activeFilters = [];

    public FilterHandler(TableView tableView, ExampleViewModel viewModel) : base(tableView)
    {
        _tableView = tableView;
        _viewModel = viewModel;
    }

    public override void PrepareFilterItems(TableViewColumn column, string? searchText = null)
    {
        var existingItems = _activeFilters.TryGetValue(column, out var selectedValues) ? selectedValues : new List<object>();
        bool isSelected(object value) => !existingItems.Any() || existingItems.Contains(value);
        var items = GetItems(column);

        FilterItems = items.Select(x => (column?.Header?.ToString()) switch
        {
            "Id" => x.Id,
            "First Name" => x.FirstName,
            "Last Name" => x.LastName,
            "Email" => x.Email,
            "Gender" => x.Gender,
            "Dob" => (object)x.Dob,
            _ => null,
        }).Where(x => string.IsNullOrEmpty(searchText) || x?.ToString()?.Contains(searchText, StringComparison.OrdinalIgnoreCase) is true)
          .Distinct()
          .Order()
          .Select(x => x ?? "(Blank)")
          .Select(x => new TableViewFilterItem(!string.IsNullOrEmpty(searchText) || isSelected(x), x))
          .ToList();
    }

    public override void ApplyFilter(TableViewColumn column)
    {
        _activeFilters[column] = SelectedValues;

        _tableView.DeselectAll();
        _viewModel.Items = new(GetItems().Take(20));

        if (!column.IsFiltered)
        {
            column.IsFiltered = true;
            _tableView.FilterDescriptions.Add(new FilterDescription(
                GetPropertyName(column),
                (o) => Filter(column, o)));
        }
    }

    public override void ClearFilter(TableViewColumn? column)
    {
        if (column is null)
        {
            _activeFilters.Clear();
            _tableView.ClearAllSorting();
        }
        else
        {
            _activeFilters.Remove(column);
        }

        _viewModel.Items = new(GetItems().Take(20));
    }

    private IEnumerable<ExampleModel> GetItems(TableViewColumn? excludeColumns = default)
    {
        return ExampleViewModel.ItemsList.Where(x
            => _activeFilters.All(e =>
            {
                if (e.Key == excludeColumns) return true;

                var value = GetPropertyValue(x, e.Key);
                value = string.IsNullOrWhiteSpace(value?.ToString()) ? "(Blank)" : value;
                return e.Value.Contains(value);
            }));
    }

    private static string? GetPropertyName(TableViewColumn column)
    {
        return (column?.Header?.ToString()) switch
        {
            "Id" => nameof(ExampleModel.Id),
            "First Name" => nameof(ExampleModel.FirstName),
            "Last Name" => nameof(ExampleModel.LastName),
            "Email" => nameof(ExampleModel.Email),
            "Gender" => nameof(ExampleModel.Gender),
            "Dob" => nameof(ExampleModel.Dob),
            _ => null,
        };
    }

    private static object? GetPropertyValue(ExampleModel item, TableViewColumn column)
    {
        return (column?.Header?.ToString()) switch
        {
            "Id" => item.Id,
            "First Name" => item.FirstName,
            "Last Name" => item.LastName,
            "Email" => item.Email,
            "Gender" => item.Gender,
            "Dob" => item.Dob,
            _ => null,
        };
    }
}
