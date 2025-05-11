#nullable enable

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QuickLaunch.UI.Converters;

/// <summary>
/// Converts a boolean value to a Visibility value, inverting the logic.
/// True becomes Collapsed, False becomes Visible.
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public class InverseBoolToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean to an inverted Visibility value.
    /// </summary>
    /// <param name="value">The boolean value to convert.</param>
    /// <param name="targetType">The type of the binding target property (should be Visibility).</param>
    /// <param name="parameter">The converter parameter (not used).</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>Visibility.Collapsed if value is true; otherwise, Visibility.Visible.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Ensure the target type is Visibility
        if (targetType != typeof(Visibility))
        {
            throw new InvalidOperationException("The target must be a Visibility type.");
        }

        // Check if the input value is a boolean
        if (value is bool boolValue)
        {
            // Inverse logic: true -> Collapsed, false -> Visible
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }

        // Default fallback if input is not a boolean (or null)
        // Consider if null should be treated as true or false based on your needs.
        // Here, null is treated like false (resulting in Visible).
        return Visibility.Visible;
    }

    /// <summary>
    /// Converts a Visibility value back to an inverted boolean value.
    /// </summary>
    /// <param name="value">The Visibility value to convert.</param>
    /// <param name="targetType">The type of the binding source property (should be bool).</param>
    /// <param name="parameter">The converter parameter (not used).</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>False if value is Visible; otherwise, true.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Ensure the target type is bool
        if (targetType != typeof(bool))
        {
            throw new InvalidOperationException("The target must be a boolean type.");
        }

        // Check if the input value is a Visibility enum member
        if (value is Visibility visibilityValue)
        {
            // Inverse logic: Visible -> false, Collapsed/Hidden -> true
            return visibilityValue != Visibility.Visible;
        }

        // Default fallback if input is not a Visibility value
        return true; // Or false, depending on desired default
    }
}
