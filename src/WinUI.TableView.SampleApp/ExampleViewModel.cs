using Bogus;
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
        await Task.Run(() =>
        {
            var startId = 1;
            var startDate = new DateOnly(1970, 1, 1);

            var faker = new Faker<ExampleModel>()
                .RuleFor(e => e.Id, f => startId++)
                .RuleFor(e => e.FirstName, f => f.Name.FirstName())
                .RuleFor(e => e.LastName, f => f.Name.LastName())
                .RuleFor(e => e.Email, (f, e) => f.Internet.Email(e.FirstName, e.LastName))
                .RuleFor(e => e.Gender, f => f.Person.Gender.ToString())
                .RuleFor(e => e.Dob, f => DateOnly.FromDateTime(f.Date.Past(50, new DateTime(startDate.Year, startDate.Month, startDate.Day))))
                .RuleFor(e => e.IsActive, f => f.Random.Bool())
                .RuleFor(e => e.ActiveAt, f => TimeOnly.FromDateTime(f.Date.SoonOffset(1).DateTime))
                .RuleFor(e => e.Department, f => f.Commerce.Department())
                .RuleFor(e => e.Designation, f => f.Name.JobTitle())
                .RuleFor(e => e.Address, f => f.Address.FullAddress())
                .RuleFor(e => e.Avatar, f => f.Internet.Avatar());

            ItemsList.Clear();
            ItemsList.AddRange(faker.Generate(1_000));
        });
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
