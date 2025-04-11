using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using Windows.Storage;
using WinUI.TableView.SampleApp.Helpers;

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
        var lines = await GetDataLines();

        foreach (var line in lines.Take(100))
        {
            var values = line.Split(',');

            ItemsList.Add(new ExampleModel
            {
                Id = int.Parse(values[0]),
                FirstName = values[1],
                LastName = values[2],
                Email = values[3],
                Gender = values[4],
                Dob = DateOnly.ParseExact(values[5], "mm/dd/yyyy", CultureInfo.InvariantCulture),
                IsActive = bool.Parse(values[6]),
                ActiveAt = TimeOnly.ParseExact(values[7], "h:mm tt", CultureInfo.InvariantCulture),
                Department = values[8],
                Designation = values[9],
                Address = values[10],
                Avatar = values[11],
            });
        }
    }

    private static async Task<IList<string>> GetDataLines()
    {
        StorageFile? file;

#if WINDOWS
        if (!NativeHelper.IsAppPackaged)
        {
            var sourcePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!, "Assets/data.csv"));
            file = await StorageFile.GetFileFromPathAsync(sourcePath);
        }
        else
#endif
        { 
            var sourceUri = new Uri("ms-appx:///Assets/data.csv");
            file = await StorageFile.GetFileFromApplicationUriAsync(sourceUri);
        }

        return await FileIO.ReadLinesAsync(file);
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
