using System;

namespace WinUI.TableView.SampleApp.Adapters;

/// <summary>
/// Adapter for ExampleModel to implement IGroupableItem interface.
/// Allows existing ExampleModel objects to work with the generic grouping system.
/// </summary>
public class ExampleModelAdapter : IGroupableItem
{
    private readonly ExampleModel _model;

    public ExampleModelAdapter(ExampleModel model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <inheritdoc/>
    public string Name => $"{_model.FirstName} {_model.LastName}".Trim();

    /// <inheritdoc/>
    public object? GetPropertyValue(string propertyName)
    {
        return propertyName switch
        {
            "Name" => Name,
            "FirstName" => _model.FirstName,
            "LastName" => _model.LastName,
            "Email" => _model.Email,
            "Gender" => _model.Gender,
            "Department" => _model.Department,
            "Designation" => _model.Designation,
            "Dob" => _model.Dob,
            "ActiveAt" => _model.ActiveAt,
            "IsActive" => _model.IsActive,
            "Address" => _model.Address,
            "DateModified" => DateModified,
            "Size" => Size,
            "ItemType" => ItemType,
            "FileType" => FileType,
            _ => null
        };
    }

    /// <inheritdoc/>
    public DateTimeOffset DateModified => DateTimeOffset.Now; // ExampleModel doesn't have a modified date

    /// <inheritdoc/>
    public ulong Size => 0; // Not applicable for person records

    /// <inheritdoc/>
    public string ItemType => "Person";

    /// <inheritdoc/>
    public string? FileType => null; // Not applicable

    /// <inheritdoc/>
    public object UnderlyingItem => _model;

    /// <summary>
    /// Gets the underlying ExampleModel.
    /// </summary>
    public ExampleModel Model => _model;
}
