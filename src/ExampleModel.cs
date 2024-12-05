using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WinUI.TableView.SampleApp;

public partial class ExampleModel : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    [property: Display(ShortName = "First Name")]
    private string? _firstName;

    [ObservableProperty]
    [property: Display(ShortName = "Last Name")]
    private string? _lastName;

    [ObservableProperty]
    private string? _email;

    [ObservableProperty]
    private string? _gender;

    [ObservableProperty]
    private DateOnly _dob;

    [ObservableProperty]
    [property: Display(ShortName = "Active At")]
    private TimeOnly _activeAt;

    [ObservableProperty]
    [property: Display(ShortName = "Is Active")]
    private bool _isActive;

    [ObservableProperty]
    private string? _department;

    [ObservableProperty]
    private string? _designation;

    [ObservableProperty]
    private string? _address;

    [ObservableProperty]
    [property: Display(AutoGenerateField = false)]
    private string? _avatar;
}
