using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using QuickLaunch.Core.Config;

namespace QuickLaunch.UI.ViewModel;

public class NullToVisibilityConverter : IValueConverter
{
    public Visibility NullValue { get; set; } = Visibility.Collapsed;
    public Visibility NotNullValue { get; set; } = Visibility.Visible;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Handle specific case for DispatcherActionItem binding where Action might be null
        if (value is DispatcherActionEntry actionItem)
        {
            return actionItem.Action == null ? NullValue : NotNullValue;
        }
        return value == null ? NullValue : NotNullValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class NullToBooleanConverter : IValueConverter
{
    public bool NullValue { get; set; } = false;
    public bool NotNullValue { get; set; } = true;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Handle specific case for DispatcherActionItem binding
        if (value is DispatcherActionEntry actionItem)
        {
            return actionItem.Action != null ? NotNullValue : NullValue;
        }
        return value == null ? NullValue : NotNullValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
