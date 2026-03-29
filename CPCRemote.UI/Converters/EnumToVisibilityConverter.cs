using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace CPCRemote.UI.Converters;

/// <summary>
/// Converts an Enum value to Visibility.Visible if it matches the ConverterParameter.
/// Otherwise returns Collapsed.
/// </summary>
public sealed class EnumToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is null || parameter is null)
            return Visibility.Collapsed;

        string valueStr = value.ToString() ?? string.Empty;
        string paramStr = parameter.ToString() ?? string.Empty;

        return valueStr.Equals(paramStr, StringComparison.OrdinalIgnoreCase) 
            ? Visibility.Visible 
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return DependencyProperty.UnsetValue;
    }
}
