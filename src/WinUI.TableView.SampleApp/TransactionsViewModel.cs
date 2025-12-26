using Bogus;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinUI.TableView.SampleApp;

public partial class TransactionsViewModel : ObservableObject
{
    public async static Task InitializeItemsAsync()
    {
        await Task.Run(() =>
        {
            var startId = 7_475_327;
            var startDate = new DateTime(2010, 1, 1);

            var faker = new Faker<TransactionModel>()
                .RuleFor(t => t.Id, f => startId++)
                .RuleFor(t => t.Date, f => startDate.AddMinutes(f.IndexFaker))
                .RuleFor(t => t.ClientId, f => f.Random.Int(100, 2000))
                .RuleFor(t => t.CardId, f => f.Random.Int(1, 6000))
                .RuleFor(t => t.Amount, f => f.Finance.Amount() * (f.Random.Bool(0.1f) ? -1 : 1))
                .RuleFor(t => t.UseChip, f => f.Random.Bool(0.2f) ? "Online Transaction" : "Swipe Transaction")
                .RuleFor(t => t.MerchantId, f => f.Random.Int(1000, 99999))
                .RuleFor(t => t.MerchantCity, f => f.Address.City())
                .RuleFor(t => t.MerchantState, f => f.Address.StateAbbr())
                .RuleFor(t => t.Zip, f => f.Address.ZipCode())
                .RuleFor(t => t.Mcc, f => f.Random.Int(1700, 9500));

            TransacationsList = faker.Generate(1_000_000);
        });
    }

    [ObservableProperty]
    public partial IList<TransactionModel>? TransacationsData { get; set; }

    public static IList<TransactionModel> TransacationsList { get; set; } = [];
}