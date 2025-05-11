using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickLaunch.Core.Actions;
using QuickLaunch.Core.Config;
using QuickLaunch.Core.Utils;
using QuickLaunch.Core.Utils.QuotedString;

namespace QuickLaunch.UI.Parsers;

/// <summary>
/// A simple observable wrapper for the string representation of an ActionRegistration.
/// </summary>
[TypeConverter(typeof(ActionRepresentationConverter))]
public partial class ActionRepresentation : ObservableObject
{
    [ObservableProperty]
    private string _actionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionRepresentation"/> class.
    /// </summary>
    /// <param name="initialActionString">The initial string value.</param>
    public ActionRepresentation(string initialActionString = "")
    {
        _actionString = initialActionString ?? string.Empty; // Ensure non-null
    }

    /// <summary>
    /// Returns the underlying action string.
    /// </summary>
    public override string ToString() => ActionString ?? string.Empty;

    // Optional: Implicit conversion operators for convenience
    public static implicit operator string(ActionRepresentation? representation)
        => representation?.ActionString ?? string.Empty;

    public static implicit operator ActionRepresentation(string? actionString)
        => new ActionRepresentation(actionString ?? string.Empty);
}

/// <summary>
/// Provides a TypeConverter to convert between:
/// 1) string <-> ActionRegistration
/// 2) ActionRepresentation <-> ActionRegistration
/// </summary>
public class ActionRepresentationConverter : TypeConverter
{
    #region ----- Fields. -----

    private readonly StringListParameterConverter _slConverter = new();

    private readonly MaybeQuotedStringTypeConverter _mqsConverter = new();

    #endregion

    #region ----- Constructors. -----

    public ActionRepresentationConverter()
    {
        //this._mqsConverter = _slConverter.MQSLConverter.MQSConverter;
    }

    #endregion

    #region ----- TypeConverter. -----

    // --- CanConvert ---

    /// <summary>
    /// Determines if this converter can convert from ActionRegistration to the destinationType.
    /// </summary>
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string)
            || destinationType == typeof(ActionRegistration) // *** Handles ActionRegistration ***
            || base.CanConvertTo(context, destinationType);
    }

    /// <summary>
    /// Determines if this converter can convert from string or ActionRegistration to ActionRepresentation.
    /// </summary>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type? sourceType)
    {
        return sourceType == typeof(string)
            || sourceType == typeof(ActionRegistration) // *** Handles ActionRegistration ***
            || base.CanConvertTo(context, sourceType);
    }

    // --- ConvertFrom (Source object -> ActionRepresentation) ---

    /// <summary>
    /// Converts the given object (string or ActionRegistration) to an ActionRepresentation.
    /// </summary>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        // Check if the input is an ActionRegistration we can format
        if (value is ActionRegistration registration)
        {
            // Use internal helper to format the ActionRegistration to its string form
            string formattedString = FormatActionRegistration(registration);

            // Create and return a new ActionRepresentation containing the formatted string
            return new ActionRepresentation(formattedString);
        }
        else if (value is string str)
        {
            // If the input is a string, create a new ActionRepresentation directly
            return new ActionRepresentation(str);
        }
        // Handle converting a null ActionRegistration
        else if (value == null)
        {
            return new ActionRepresentation();
        }

        // If input wasn't ActionRegistration or destinationType isn't handled, delegate to base
        return base.ConvertFrom(context, culture, value);
    }

    // --- ConvertTo (ActionRepresentation -> Destination object) ---

    /// <summary>
    /// Converts an ActionRepresentationto the specified destination type (string or ActionRegistration).
    /// </summary>
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        string? inputString = null;

        // *** Check if input is ActionRepresentation ***
        if (value is ActionRepresentation rep)
        {
            // Extract the string payload
            inputString = rep.ActionString;
        }
        // Also handle direct string input
        else if (value is string s)
        {
            inputString = s;
        }

        // If we got a string from either source type, try to parse it
        if (inputString != null)
        {
            if (destinationType == typeof(string))
            {
                // Return the string representation directly
                return inputString;
            }
            else if (destinationType == typeof(ActionRegistration))
            {
                if (string.IsNullOrWhiteSpace(inputString))
                {
                    return null; // Represent empty/whitespace as 'no action' (null ActionRegistration)
                }

                // Use the internal helper to parse the string and create the ActionRegistration
                try
                {
                    return ParseActionString(inputString);
                }
                catch (Exception ex) when (ex is FormatException || ex is ArgumentException)
                {
                    // Propagate parsing/validation exceptions
                    throw;
                }
                catch (Exception ex)
                {
                    // Wrap other unexpected errors
                    throw new NotSupportedException($"Error converting '{inputString}' to ActionRegistration. {ex.Message}", ex);
                }
            }
        }

        // If input was neither string nor ActionRepresentation, delegate to base class
        return base.ConvertTo(context, culture, value, destinationType);
    }


    // --- Internal Helper Methods (for parsing/formatting the string) ---

    /// <summary>Parses the action string into an ActionRegistration.</summary>
    private ActionRegistration ParseActionString(string inputString)
    {
        // (Implementation remains the same as previous answer)
        // 1. Trim input string
        // 2. Find first space to separate ActionName from ParamsString
        // 3. Lookup ActionType using ActionDispatcherFactory.LookupActionType(ActionName) (Throw FormatException if not found)
        // 4. Parse ParamsString using ParseParameters helper (Throw FormatException if parsing fails)
        // 5. Call ActionRegistration.Create(actionType, parsedParams) (Handles param validation/conversion, throws ArgumentException on failure)
        // 6. Return the created ActionRegistration

        inputString = (inputString ?? string.Empty).Trim();
        ArgumentExceptionHelper.ThrowIfNullOrWhiteSpace(inputString, nameof(inputString));

        // --- 1. Split Action Name from Parameter String ---
        // Find the index of the first whitespace character to separate ActionName from parameters.
        int firstWhitespaceIndex = -1;
        for (int i = 0; i < inputString.Length; i++)
        {
            if (char.IsWhiteSpace(inputString[i]))
            {
                firstWhitespaceIndex = i;
                break;
            }
        }
        string actionName = (firstWhitespaceIndex == -1) ? inputString : inputString.Substring(0, firstWhitespaceIndex);
        string paramsString = (firstWhitespaceIndex == -1) ? string.Empty : inputString.Substring(firstWhitespaceIndex + 1).Trim();

        if (string.IsNullOrWhiteSpace(actionName))
        {
            throw new FormatException("Action name cannot be empty or whitespace.");
        }

        // --- 2. Lookup ActionType ---
        // Assumes ActionDispatcherFactory is accessible and has this static method
        ActionType? actionType = ActionFactory.LookupActionType(actionName);
        if (actionType == null)
        {
            // Provide specific error for unknown action type
            throw new FormatException($"Unknown ActionType specified: '{actionName}'.");
        }

        // --- 3. Parse the Parameter String ---
        // This helper handles Key=Value pairs, quoting, and escapes.
        // It returns null if the *syntax* of the parameter string is invalid.
        List<ActionParameter>? parsedStringParams = ParseParameters(paramsString, actionType);
        if (parsedStringParams == null)
        {
            // Parameter string syntax error
            throw new FormatException($"Failed to parse parameters segment: '{paramsString}'. Check format (e.g., Key=Value Key2=\"Value with spaces\").");
        }

        // --- 4. Create and Validate ActionRegistration ---
        // Delegate final validation, parameter type conversion, and object creation
        // to the static factory method on ActionRegistration.
        // Assumes ActionRegistration.Create exists and performs validation,
        // throwing ArgumentException on failure.
        return ActionRegistration.Create(actionType, parsedStringParams);
    }

    /// <summary>Formats the ActionRegistration into its canonical string representation.</summary>
    public string FormatActionRegistration(ActionRegistration actionReg)
    {
        // (Implementation remains the same as previous answer)
        // 1. Start StringBuilder with actionReg.ActionType.Name
        // 2. Iterate through actionReg.Parameters (ordered by key for consistency)
        // 3. For each non-null parameter value:
        //    a. Append space, Key, '='
        //    b. Convert value to string
        //    c. Check if quoting is needed (contains space, quote, equals, or is empty)
        //    d. If quoting needed, append quote, escaped value (replace " with \"), quote
        //    e. Else, append raw value string
        // 4. Return StringBuilder.ToString()

        var sb = new StringBuilder();
        sb.Append(actionReg.ActionType.Name);

        // Order parameters for consistent output
        foreach (var param in actionReg.Parameters.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (param.Value != null) // Let's handle null values gracefully if they can exist
            {
                sb.Append(' ');
                sb.Append(param.Key);
                sb.Append('=');

                TypeConverter converter = TypeDescriptor.GetConverter(param.Type);
                string stringValue;
                if (converter != null && converter.CanConvertTo(typeof(string)))
                {
                    stringValue = converter.ConvertToInvariantString(param.Value) ?? string.Empty;
                }
                else
                {
                    stringValue = Convert.ToString(param.Value, CultureInfo.InvariantCulture) ?? string.Empty;
                }

                if (param.Type == typeof(StringListParameter))
                {
                    // Convert StringListParameter to string using the string list converter
                    sb.Append(_slConverter.ConvertToInvariantString(param.Value));
                }
                else
                {
                    // Convert other types to string using the MaybeQuotedString converter
                    sb.Append(_mqsConverter.ConvertToInvariantString(new MaybeQuotedString(stringValue)));
                }
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Parses a string containing key-value parameters.
    /// Format examples: "Key1=Value1 Key2=\"Value with spaces\" Key3=EmptyString=\"\""
    /// Handles quoted values and basic escapes (\", \\).
    /// Parameter keys are treated case-insensitively for duplicate checks.
    /// </summary>
    /// <param name="paramsString">The parameter string to parse.</param>
    /// <returns>A list of (Key, Value) string tuples on success, or null if a syntax error is detected.</returns>
    private List<ActionParameter>? ParseParameters(string paramsString, ActionType actionType)
    {
        var parameters = new List<ActionParameter>();
        // An empty or whitespace parameter string is valid (means no parameters)
        if (string.IsNullOrWhiteSpace(paramsString))
        {
            return parameters;
        }

        int pos = 0;
        int len = paramsString.Length;
        // Use case-sensitive comparison for checking duplicate keys.
        var keysFound = new HashSet<string>();

        while (pos < len)
        {
            // Skip leading whitespace before the key
            while (pos < len && char.IsWhiteSpace(paramsString[pos])) pos++;
            // Break if we reached the end after skipping whitespace
            if (pos >= len) break;

            // --- Parse Key ---
            int equalsPos = paramsString.IndexOf('=', pos);
            // Key must exist, not be empty/whitespace, and '=' must be found after the key.
            if (equalsPos == -1 || equalsPos == pos || char.IsWhiteSpace(paramsString[equalsPos - 1]))
            {
                throw new FormatException($"Invalid parameter format near position {pos}: Expected 'Key=Value' format. Missing '=', empty key, or whitespace before '='.");
            }
            // Trim trailing space from key only
            string key = paramsString.Substring(pos, equalsPos - pos).TrimEnd();
            // Key itself cannot be empty or contain spaces
            if (string.IsNullOrEmpty(key) || key.Contains(' '))
            {
                throw new FormatException($"Invalid parameter key '{key}' near position {pos}: Key cannot be empty or contain spaces.");
            }

            // Check for duplicate keys
            if (!keysFound.Add(key))
            {
                throw new FormatException($"Duplicate parameter key detected: '{key}'.");
            }

            var paramInfo = actionType.Parameters.FirstOrDefault(p => p.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (paramInfo is null)
            {
                throw new FormatException($"Unknown parameter key {key} near position {pos}: Key does not match any known parameter.");
            }

            // Move position past the '=' sign
            pos = equalsPos + 1;

            if (paramInfo.Type == typeof(StringListParameter))
            {
                if (!_slConverter.MQSLConverter.TryParsePrefix(paramsString, pos, out var parsedStringList, out int consumed) ||
                    parsedStringList == null)
                {
                    throw new FormatException($"Invalid parameter key '{key}' near position {pos}: Invalid valid string list value after '='.");
                }
                var parameter = ActionParameter.Create(paramInfo, new StringListParameter(parsedStringList));
                parameters.Add(parameter);
                pos += consumed;
            }
            else
            {
                if (!_mqsConverter.TryParsePrefix(paramsString.Substring(pos), out var parsedString, out int consumed) ||
                     parsedString is null)
                {
                    throw new FormatException($"Invalid parameter key '{key}' near position {pos}: Expected a valid value after '='.");
                }
                var parameter = ActionParameter.CreateFromString(paramInfo, new[] { parsedString.Value });
                parameters.Add(parameter);
                pos += consumed;
            }

            // Skip whitespace before the next potential key (or end of string)

            while (pos < len && char.IsWhiteSpace(paramsString[pos])) pos++;
        }

        // If the loop finished without returning null, parsing was successful
        return parameters;
    }

    #endregion
}

