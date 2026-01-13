using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace WinUI.TableView.SampleApp;

public partial class SalesViewModel : ObservableObject
{
    public async static Task InitializeItemsAsync()
    {
        await Task.Run(() =>
        {
            SalesList.Clear();

            for (var i = 0; i < 50; i++)
            {
                var target = DataFaker.Integer(50_000, 250_000);
                var sales = DataFaker.Integer((int)(target * 0.5), (int)(target * 1.2));
                var growth = DataFaker.Integer(-20, 60);
                var status = sales >= target ? (growth >= 0 ? "Ahead" : "On Track") : (growth < 0 ? "Behind" : "On Track");

                SalesList.Add(new SaleExampleModel
                {
                    Employee = DataFaker.FullName(),
                    Region = DataFaker.State(),
                    Target = target,
                    Sales = sales,
                    Growth = growth,
                    Status = status
                });
            }
        });
    }

    [ObservableProperty]
    public partial ObservableCollection<SaleExampleModel>? SalesData { get; set; } 

    public static IList<SaleExampleModel> SalesList { get; set; } = [];
}