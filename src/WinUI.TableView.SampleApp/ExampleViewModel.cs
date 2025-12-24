using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace WinUI.TableView.SampleApp;

public partial class ExampleViewModel : ObservableObject
{
    public ExampleViewModel()
    {
        foreach (var item in ItemsList)
        {
            Items.Add(item);
            Genders.Add(item.Gender);
            Departments.Add(item.Department);
            Designations.Add(item.Designation);
        }
    }

    public async static Task InitializeItemsAsync()
    {
        await foreach (var line in GetDataLines())
        {
            if (ExampleModel.TryParseCsv(line, out var record))
            {
                ItemsList.Add(record);
            }
        }
    }

    private static IAsyncEnumerable<string> GetDataLines()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets/data.csv");
        return File.ReadLinesAsync(path);
    }

    public static List<ExampleModel> ItemsList { get; } = [];

    [ObservableProperty]
    public partial ObservableCollection<ExampleModel> Items { get; set; } = [];

    public SortedSet<string?> Genders { get; set; } = [];

    public SortedSet<string?> Departments { get; set; } = [];

    public SortedSet<string?> Designations { get; set; } = [];

    [ObservableProperty]
    public partial ExampleModel? SelectedItem { get; set; }
}
