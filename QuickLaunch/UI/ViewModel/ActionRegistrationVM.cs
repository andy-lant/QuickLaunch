using System;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickLaunch.Core.Config;
using QuickLaunch.UI.Parsers;

namespace QuickLaunch.UI.ViewModel;


public partial class ActionRegistrationVM : ObservableObject
{
    private readonly ActionRepresentationConverter _converter = new();

    private ActionRegistration? _actionData = null;

    // Receives value from the Control's DP via OnActionRegistrationChanged
    public ActionRegistration? ActionData
    {
        get => _actionData;
        set
        {
            // Use SetProperty for change notification and loop prevention
            if (SetProperty(ref _actionData, value))
            {
                // When ActionData is updated from outside, format it to update ActionRepresentation
                ActionRepresentation newRep = value is null ? new() : (ActionRepresentation?)_converter.ConvertFrom(value) ?? new();

                // Update the ActionRepresentation property only if the string content changed
                if (ActionRepresentation?.ActionString != newRep.ActionString)
                {
                    // Use the property setter to ensure notification
                    this.ActionRepresentation = newRep;
                }
            }
        }
    }

    private ActionRepresentation _actionRepresentation = new(); // Initialize

    public ActionRepresentation ActionRepresentation
    {
        get => _actionRepresentation;
        set
        {
            // Check if the underlying string is actually different
            if (_actionRepresentation?.ActionString != value?.ActionString)
            {
                // Update the backing field and notify the UI (TextBox)
                // DO NOT parse back to _actionData here.
                SetProperty(ref _actionRepresentation, value ?? new());
            }
        }
    }

    // Derived Properties

    ///// <summary>
    ///// The Action Type
    ///// </summary>
    //public ActionType? ActionType => ActionData?.ActionType;

    ///// <summary>
    ///// The action's parameters.
    ///// </summary>
    //public ObservableCollection<object> ActionParameters { get; private set; }

    // ----- Constructors. -----

    public ActionRegistrationVM()
    {
    }

    // ----- Methods. -----


    /// <summary>
    /// Attempt to update ActionRegistration from the current ActionRepresentation.
    /// </summary>
    public void AttemptUpdateSource()
    {
        Console.WriteLine("Attempting to parse VM representation and update source DP...");

        // Ask the ViewModel to parse its current string representation
        if (TryParseCurrentRepresentation(out ActionRegistration? parsedAction))
        {
            // Parsing successful.
            var currentDpValue = this.ActionData;

            // Compare if the parsed value is actually different from the current DP value
            // NOTE: Implement robust comparison based on your ActionRegistration class.
            // Comparing via the formatted string is a common way if Create always makes new instances.
            string? currentFormatted = currentDpValue == null ? null : _converter.FormatActionRegistration(currentDpValue);
            string? parsedFormatted = parsedAction == null ? null : _converter.FormatActionRegistration(parsedAction);

            if (currentFormatted != parsedFormatted)
            {
                Console.WriteLine($"Parsing successful. Updating ActionRegistration DP from '{currentFormatted ?? "null"}' to '{parsedFormatted ?? "null"}'.");
                // Update the Dependency Property using SetValue.
                // This will trigger the TwoWay binding back to TestWindow.Action.Action
                // AND will call OnActionRegistrationChanged again.
                ActionData = parsedAction;
            }
            else
            {
                Console.WriteLine("Parsing successful, but result is same as current DP value. No update needed.");
            }
        }
        else
        {
            // Parsing failed (TryParseCurrentRepresentation returned false or threw).
            // The VM's TryParse method should have logged the error.
            Console.WriteLine("Parsing failed. ActionRegistration Dependency Property not updated.");
            // Consider providing user feedback (e.g., TextBox border color, error message).
            // Optionally revert the TextBox text to the last known good value:
            // Input.Text = _viewModel.ActionData == null ? "" : _localConverter.FormatActionRegistration(_viewModel.ActionData);
        }
    }

    /// <summary>
    /// Attempts to parse the current ActionRepresentation string.
    /// </summary>
    /// <param name="parsedAction">The resulting ActionRegistration if parsing succeeds.</param>
    /// <returns>True if parsing was successful, False otherwise.</returns>
    public bool TryParseCurrentRepresentation(out ActionRegistration? parsedAction)
    {
        parsedAction = null;
        if (this.ActionRepresentation == null) return false;

        try
        {
            // Use the converter to parse the string held by ActionRepresentation
            // Pass the ActionRepresentation object itself to ConvertTo
            parsedAction = (ActionRegistration?)_converter.ConvertTo(this.ActionRepresentation, typeof(ActionRegistration));
            // Consider null/empty string case: should it return null or throw?
            // Assuming converter returns null for empty/invalid strings that don't throw FormatException.
            return true;
        }
        catch (Exception ex) when (ex is FormatException || ex is ArgumentException || ex is NotSupportedException)
        {
            // Log expected parsing/conversion errors
            Console.WriteLine($"Error parsing action string '{this.ActionRepresentation.ActionString}': {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            // Log unexpected errors
            Console.WriteLine($"Unexpected error parsing action string '{this.ActionRepresentation.ActionString}': {ex}");
            // Depending on severity, you might want to rethrow or handle differently
            return false;
        }
    }
}
