using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickLaunch.Core.Utils;

namespace QuickLaunch.Core.Actions;


/// <summary>
/// Simple record to hold information about an action parameter.
/// </summary>
/// <param name="Name">The name of the parameter (e.g., "Path", "Url").</param>
/// <param name="Type">The expected data type of the parameter.</param>
/// <param name="Description">Optional description of the parameter.</param>
public record ActionParameterInfo(string Name, Type Type, bool IsOptional = false, string? Description = null)
{
    public bool ValidateValue(object? value)
    {
        return ValidateValue(value, out var _);
    }
    public bool ValidateValue(object? value, out List<string> errors)
    {
        List<string> currentErrors = new();
        if (IsOptional && value is null)
        {
            // ignore
        }
        else if (!Type.IsInstanceOfType(value))
        {
            currentErrors.Add($"Invalid value type {value?.GetType().FullName ?? "null"} for parameter {Name}, expected {Type.FullName}");
        }
        else if (Type == typeof(string) && value is string str && string.IsNullOrWhiteSpace(str))
        {
            currentErrors.Add($"Invalid null or white space string value for parameter {Name}, expected {Type.FullName}");
        }
        errors = currentErrors;
        return currentErrors.Count == 0;
    }
}


/// <summary>
/// Defines a contract for actions that can be executed.
/// </summary>
public interface IAction
{
    static ActionType ActionType { get { throw new NotImplementedException(); } }

    /// <summary>
    /// Executes the action using its configured parameters.
    /// </summary>
    void Execute();
}

public partial class ActionType : ObservableObject
{

    [ObservableProperty]
    public string _name;

    [ObservableProperty]
    public string _description;

    [ObservableProperty]
    public Type _associatedType; // The concrete class implementing IAction (e.g., typeof(OpenFileAction))

    private readonly ActionParameterInfo[] _parameters;
    public ActionParameterInfo[] Parameters { get => _parameters; }

    /// <summary>
    /// Initializes a new instance of the ActionType class.
    /// </summary>
    /// <param name="name">The unique identifier name for the action type (e.g., "OpenFile").</param>
    /// <param name="description">A user-friendly description of the action type.</param>
    /// <param name="type">The .NET type that implements IAction for this action type.</param>
    /// <exception cref="ArgumentException">Thrown if the provided type does not implement IAction or lacks the static ExpectedParameters property.</exception>
    public ActionType(string name, string description, Type type, IEnumerable<ActionParameterInfo> parameters)
    {
        ArgumentExceptionHelper.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentExceptionHelper.ThrowIfNullOrEmpty(description, nameof(description));
        ArgumentNullException.ThrowIfNull(type, nameof(type));


        // Validate that the type implements IAction
        if (!typeof(IAction).IsAssignableFrom(type))
        {
            throw new ArgumentException($"The provided type '{type.FullName}' does not implement IAction.", nameof(type));
        }

        // Assign properties using the backing fields
        _name = name;
        _description = description;
        _associatedType = type;
        _parameters = parameters.ToArray() ?? Array.Empty<ActionParameterInfo>();
    }

}