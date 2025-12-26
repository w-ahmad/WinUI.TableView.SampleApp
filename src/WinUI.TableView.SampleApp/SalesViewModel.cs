using Bogus;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace WinUI.TableView.SampleApp;

public partial class SalesViewModel : ObservableObject
{
    public async static Task InitializeItemsAsync()
    {
        await Task.Run(() =>
        {
            var faker = new Faker<SaleExampleModel>()
                .RuleFor(s => s.Employee, f => f.Name.FullName())
                .RuleFor(s => s.Region, f => f.Address.Locale)
                .RuleFor(s => s.Target, f => f.Random.Int(50_000, 250_000))
                .RuleFor(s => s.Sales, (f, s) => f.Random.Int((int)(s.Target * 0.5), (int)(s.Target * 1.2)))
                .RuleFor(s => s.Growth, f => f.Random.Int(-20, 60))
                .RuleFor(s => s.Status, (f, s) => s.Sales >= s.Target ? (s.Growth >= 0 ? "Ahead" : "On Track") : (s.Growth < 0 ? "Behind" : "On Track"));

            SalesList.Clear();
            SalesList = faker.Generate(50);
        });
    }

    [ObservableProperty]
    public partial ObservableCollection<SaleExampleModel>? SalesData { get; set; } 

    public static IList<SaleExampleModel> SalesList { get; set; } = [];
}