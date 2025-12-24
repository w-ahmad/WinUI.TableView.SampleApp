using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace WinUI.TableView.SampleApp;

/// <summary>
/// Represents a single transaction record from the CSV sample data.
/// </summary>
public partial class TransactionExampleModel : ObservableObject
{
    [ObservableProperty]
    public partial int Id { get; set; }

    [ObservableProperty]
    public partial DateTime Date { get; set; }

    [ObservableProperty]
    public partial int ClientId { get; set; }

    [ObservableProperty]
    public partial int CardId { get; set; }

    [ObservableProperty]
    public partial decimal Amount { get; set; }

    [ObservableProperty]
    public partial string? UseChip { get; set; }

    [ObservableProperty]
    public partial int MerchantId { get; set; }

    [ObservableProperty]
    public partial string? MerchantCity { get; set; }

    [ObservableProperty]
    public partial string? MerchantState { get; set; }

    [ObservableProperty]
    public partial string Zip { get; set; }

    [ObservableProperty]
    public partial int Mcc { get; set; }

    [ObservableProperty]
    public partial string? Errors { get; set; }

    internal static bool TryParseCsv(string csvLine, [NotNullWhen(true)] out TransactionExampleModel? record)
    {
        try
        {
            var parts = csvLine.Split(',');

            var id = int.Parse(parts[0].Trim(), CultureInfo.InvariantCulture);
            var date = DateTime.Parse(parts[1].Trim(), CultureInfo.InvariantCulture);
            var clientId = int.Parse(parts[2].Trim(), CultureInfo.InvariantCulture);
            var cardId = int.Parse(parts[3].Trim(), CultureInfo.InvariantCulture);

            // Amount field like "$-77.00" or "$14.57"
            var amountText = parts[4].Trim().Replace("$", "", StringComparison.Ordinal);
            var amount = decimal.Parse(amountText, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);

            var useChip = string.IsNullOrWhiteSpace(parts[5]) ? null : parts[5].Trim();
            var merchantId = int.Parse(parts[6].Trim(), CultureInfo.InvariantCulture);
            var merchantCity = string.IsNullOrWhiteSpace(parts[7]) ? null : parts[7].Trim();
            var merchantState = string.IsNullOrWhiteSpace(parts[8]) ? null : parts[8].Trim();

            // Zip in sample includes ".0" (e.g. "58523.0"), preserve meaningful digits
            var zipRaw = parts[9].Trim();
            if (zipRaw.EndsWith(".0", StringComparison.Ordinal))
            {
                zipRaw = zipRaw.Substring(0, zipRaw.Length - 2);
            }
            var zip = string.IsNullOrWhiteSpace(zipRaw) ? null : zipRaw;

            var mcc = int.Parse(parts[10].Trim(), CultureInfo.InvariantCulture);
            var errors = parts[11].Trim();
            if (string.IsNullOrEmpty(errors))
            {
                errors = null;
            }

            record = new TransactionExampleModel
            {
                Id = id,
                Date = date,
                ClientId = clientId,
                CardId = cardId,
                Amount = amount,
                UseChip = useChip,
                MerchantId = merchantId,
                MerchantCity = merchantCity,
                MerchantState = merchantState,
                Zip = zip,
                Mcc = mcc,
                Errors = errors
            };

            return true;
        }
        catch
        {
            record = null;
            return false;
        }
    }
}

