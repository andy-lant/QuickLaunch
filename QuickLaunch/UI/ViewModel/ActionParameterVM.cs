// ParameterViewModel.cs
#nullable enable

using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickLaunch.Core.Actions;
using QuickLaunch.Core.Config; // For ActionParameterInfo

namespace QuickLaunch.UI.ViewModel;

/// <summary>
/// ViewModel representing a single parameter for an ActionRegistration.
/// Used by ActionRegistrationEditorControl to dynamically generate UI.
/// </summary>
public partial class ActionParameterVM : ObservableObject
{

    [ObservableProperty]
    private ActionParameter _parameter;

    public string Name => Parameter.Key;

    public ActionParameterInfo ParameterInfo => Parameter.ParameterInfo;
    public string? Description => ParameterInfo.Description;
    public bool IsOptional => ParameterInfo.IsOptional;
    public Type ParameterType => ParameterInfo.Type;

    public string Summary => $"{Name}={ValueString}";

    public string _valueString;

    public object? _valueObject;

    /// <summary>
    /// Gets or sets the string representation of the parameter's value.
    /// All values are handled as strings initially for simplicity with TextBox binding.
    /// Type conversions happen during validation/saving.
    /// </summary>
    public string ValueString
    {
        get => _valueString;
    }

    public object? ValueObject
    {
        get => _valueObject;
    }

    /// <summary>
    /// Indicates whether a "Browse..." button should be shown for this parameter.
    /// Determined based on parameter name conventions (e.g., "Path", "FilePath", "Directory").
    /// </summary>
    public bool HasBrowseButton { get; }

    /// <summary>
    /// Determines if the parameter type is boolean. Used for template selection.
    /// </summary>
    public bool IsBooleanType => ParameterType == typeof(bool);

    /// <summary>
    /// Gets the display name, adding "*" for required parameters.
    /// </summary>
    public string DisplayName => IsOptional ? Name : Name + "*";

    // ----- Constructors. -----

    /// <summary>
    /// Initializes a new instance of the ParameterViewModel class.
    /// </summary>
    /// <param name="info">The ActionParameterInfo describing the parameter.</param>
    /// <param name="initialValue">The initial string value from the ActionRegistration.Parameters dictionary.</param>
    /// <param name="updateCallback">A function to call when the StringValue changes, passing the parameter name and new value.</param>
    public ActionParameterVM(ActionParameter parameter)
    {
        _parameter = parameter;
        _valueString = parameter.Value?.ToString() ?? "";  // FIXME: use converter
        _valueObject = parameter.Value;

        // Determine if a browse button is needed based on name convention
        HasBrowseButton = Name.Contains("Path", StringComparison.OrdinalIgnoreCase) ||
                          Name.Contains("File", StringComparison.OrdinalIgnoreCase) ||
                          Name.Contains("Directory", StringComparison.OrdinalIgnoreCase) ||
                          Name.Contains("Folder", StringComparison.OrdinalIgnoreCase);

        // If it's boolean, ensure initial value is valid ("true" or "false")
        if (IsBooleanType && !string.Equals(_valueString, "true", StringComparison.OrdinalIgnoreCase) && !string.Equals(_valueString, "false", StringComparison.OrdinalIgnoreCase))
        {
            _valueString = "false"; // Default boolean to false if initial value is invalid/missing
                                    // Immediately update the underlying dictionary via 
            _valueObject = false;
        }
    }

    // ----- Event handlers. -----
    private void OnParameterChanged(object? sender, PropertyChangedEventArgs e)
    {

    }

}
#nullable disable
