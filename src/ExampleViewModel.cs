using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Reflection;
using Windows.Storage;
using WinUI.TableView.SampleApp.Helpers;

namespace WinUI.TableView.SampleApp;

public partial class ExampleViewModel : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<ExampleModel> Items { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<string> Genders { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<string> Departments { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<string> Designations {get;set;}= [];

    [ObservableProperty]
    public partial ExampleModel? SelectedItem { get; set; }

    public async Task InitializeAsync()
    {
        Items.Clear();
        Genders.Clear();
        Departments.Clear();
        Designations.Clear();

        var lines = await GetDataLines();

        foreach (var line in lines)
        {
            var values = line.Split(',');

            Items.Add(new ExampleModel
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

            if (!Genders.Contains(values[4])) Genders.Add(values[4]);
            if (!Departments.Contains(values[8])) Departments.Add(values[8]);
            if (!Designations.Contains(values[9])) Genders.Add(values[9]);
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
