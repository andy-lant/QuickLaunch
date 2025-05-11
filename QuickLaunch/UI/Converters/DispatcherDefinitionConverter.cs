using System;
using System.Globalization;
using System.Windows.Data;
using QuickLaunch.Core.Config;

namespace QuickLaunch.UI.Converters;
/// <summary>
/// Converts between objects in the ComboBox selection and DispatcherDefinition.
/// Used to handle placeholder items in a ComboBox bound to a DispatcherDefinition property.
/// </summary>
public class DispatcherDefinitionConverter : IValueConverter
{
    /// <summary>
    /// Converts the source DispatcherDefinition (or null) to an object suitable for ComboBox SelectedItem.
    /// In this case, it's a direct pass-through.
    /// </summary>
    /// <param name="value">The source value (DispatcherDefinition or null).</param>
    /// <param name="targetType">The target type (usually object).</param>
    /// <param name="parameter">Converter parameter (not used).</param>
    /// <param name="culture">Culture info.</param>
    /// <returns>The input value itself.</returns>
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {

        // We expect the source property (SelectedCommand.Dispatcher) to be
        // either a DispatcherDefinition or null. Just return it as is.
        if (value is DispatcherDefinition || value is null)
        {
            return value;
        }
        // other types are not converted, return null
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Converts the selected item from the ComboBox back to a DispatcherDefinition or null.
    /// If the selected item is not a DispatcherDefinition (e.g., it's the placeholder), return null.
    /// </summary>
    /// <param name="value">The value from the ComboBox SelectedItem.</param>
    /// <param name="targetType">The target type (DispatcherDefinition).</param>
    /// <param name="parameter">Converter parameter (not used).</param>
    /// <param name="culture">Culture info.</param>
    /// <returns>A DispatcherDefinition if the input value is one, otherwise null.</returns>
    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Check if the selected item from the ComboBox is actually a DispatcherDefinition
        if (value is DispatcherDefinition dispatcherDefinition)
        {
            // If yes, return it so it can be assigned to SelectedCommand.Dispatcher
            return dispatcherDefinition;
        }

        // If the selected item is null or the placeholder (or anything else),
        // return null. This prevents the binding from trying to assign
        // the placeholder object to the DispatcherDefinition property.
        return null;
    }
}
