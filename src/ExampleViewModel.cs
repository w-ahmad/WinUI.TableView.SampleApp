using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Reflection;
using Windows.Storage;
using WinUI.TableView.SampleApp.Helpers;

namespace WinUI.TableView.SampleApp;

public partial class ExampleViewModel : ObservableObject
{
    private readonly List<ExampleModel> _items = [];

    public ObservableCollection<ExampleModel> Items => new(_items);

    public SortedSet<string> Genders { get; set; } = [];

    public SortedSet<string> Departments { get; set; } = [];

    public SortedSet<string> Designations { get; set; } = [];

    [ObservableProperty]
    public partial ExampleModel? SelectedItem { get; set; }

    public async Task InitializeAsync()
    {
        var lines = await GetDataLines();

        foreach (var line in lines.Take(100))
        {
            var values = line.Split(',');

            _items.Add(new ExampleModel
            {
                Id = int.Parse(values[0]),
                FirstName = values[1],
                LastName = values[2],
                Email = values[3],
                Gender = values[4],
                Dob = DateOnly.Parse(values[5]),
                IsActive = bool.Parse(values[6]),
                ActiveAt = TimeOnly.Parse(values[7]),
                Department = values[8],
                Designation = values[9],
                Address = values[10],
                Avatar = values[11],
            });

            Genders.Add(values[4]);
            Departments.Add(values[8]);
            Designations.Add(values[9]);
        }
    }

    private static async Task<IList<string>> GetDataLines()
    {
        StorageFile? file;

        if (!NativeHelper.IsAppPackaged)
        {
            var sourcePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!, "Assets/data.csv"));
            file = await StorageFile.GetFileFromPathAsync(sourcePath);
        }
        else
        {
            var sourceUri = new Uri("ms-appx:///" + "Assets/data.csv");
            file = await StorageFile.GetFileFromApplicationUriAsync(sourceUri);
        }

        return await FileIO.ReadLinesAsync(file);
    }
}
