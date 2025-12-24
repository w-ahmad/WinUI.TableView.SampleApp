using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace WinUI.TableView.SampleApp;

public partial class SaleExampleModel : ObservableObject
{
    [ObservableProperty]
    public partial string? Employee { get; set; }

    [ObservableProperty]
    public partial string? Region { get; set; }

    [ObservableProperty]
    public partial int Sales { get; set; }

    [ObservableProperty]
    public partial int Target { get; set; }

    [ObservableProperty]
    public partial int Growth { get; set; }

    [ObservableProperty]
    public partial string? Status { get; set; }

    internal static bool TryParseCsv(string csvLine, [NotNullWhen(true)] out SaleExampleModel? record)
    {
        try
        {
            var parts = csvLine.Split(',');
            record = new SaleExampleModel
            {
                Employee = parts[0].Trim(),
                Region = parts[1].Trim(),
                Sales = int.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                Target = int.Parse(parts[3].Trim(), CultureInfo.InvariantCulture),
                Growth = int.Parse(parts[4].Trim(), CultureInfo.InvariantCulture),
                Status = parts[5].Trim()
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

