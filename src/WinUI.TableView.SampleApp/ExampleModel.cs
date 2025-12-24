using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace WinUI.TableView.SampleApp;

public partial class ExampleModel : ObservableObject
{
    [ObservableProperty]
    public partial int Id { get; set; }

    [ObservableProperty]
    [Display(ShortName = "First Name")]
    public partial string? FirstName { get; set; }

    [ObservableProperty]
    [Display(ShortName = "Last Name")]
    public partial string? LastName { get; set; }

    [ObservableProperty]
    public partial string? Email { get; set; }

    [ObservableProperty]
    public partial string? Gender { get; set; }

    [ObservableProperty]
    public partial DateOnly Dob { get; set; }

    [ObservableProperty]
    [Display(ShortName = "Active At")]
    public partial TimeOnly ActiveAt { get; set; }

    [ObservableProperty]
    [Display(ShortName = "Is Active")]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    public partial string? Department { get; set; }

    [ObservableProperty]
    public partial string? Designation { get; set; }

    [ObservableProperty]
    public partial string? Address { get; set; }

    [ObservableProperty]
    [Display(AutoGenerateField = false)]
    public partial string? Avatar { get; set; }

    internal static bool TryParseCsv(string csvLine, [NotNullWhen(true)] out ExampleModel? record)
    {
        try
        {
            var parts = csvLine.Split(',');
            record = new ExampleModel
            {
                Id = int.Parse(parts[0]),
                FirstName = parts[1],
                LastName = parts[2],
                Email = parts[3],
                Gender = parts[4],
                Dob = DateOnly.ParseExact(parts[5], "mm/dd/yyyy", CultureInfo.InvariantCulture),
                IsActive = bool.Parse(parts[6]),
                ActiveAt = TimeOnly.ParseExact(parts[7], "h:mm tt", CultureInfo.InvariantCulture),
                Department = parts[8],
                Designation = parts[9],
                Address = parts[10],
                Avatar = parts[11],
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
