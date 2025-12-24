using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace WinUI.TableView.SampleApp;

public partial class TransactionsViewModel : ObservableObject
{
    public async static Task InitializeItemsAsync()
    {
        await foreach (var line in GetTransactionDataLines())
        {
            if (TransactionExampleModel.TryParseCsv(line, out var record))
            {
                TransacationsList.Add(record);
            }
        }
    }

    [ObservableProperty]
    public partial bool IsReady { get; set; }

    [ObservableProperty]
    public partial IList<TransactionExampleModel>? TransacationsData { get; set; } 

    public static IList<TransactionExampleModel> TransacationsList { get; set; } = [];

    private static IAsyncEnumerable<string> GetTransactionDataLines()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets/transactions_data.csv");
        return File.ReadLinesAsync(path);
    }
}