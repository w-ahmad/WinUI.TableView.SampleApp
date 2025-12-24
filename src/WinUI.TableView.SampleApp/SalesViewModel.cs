using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace WinUI.TableView.SampleApp;

public partial class SalesViewModel : ObservableObject
{
    public async static Task InitializeItemsAsync()
    {
        await foreach (var line in GetSalesDataLines())
        {
            if (SaleExampleModel.TryParseCsv(line, out var record))
            {
                SalesList.Add(record);
            }
        }
    }

    [ObservableProperty]
    public partial ObservableCollection<SaleExampleModel>? SalesData { get; set; } 

    public static IList<SaleExampleModel> SalesList { get; set; } = [];

    private static IAsyncEnumerable<string> GetSalesDataLines()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets/sales_data.csv");
        return File.ReadLinesAsync(path);
    }
}