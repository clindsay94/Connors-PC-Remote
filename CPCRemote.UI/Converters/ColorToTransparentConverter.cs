using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Windows.UI;

namespace CPCRemote.UI.Converters;

/// <summary>
/// Converts a Windows.UI.Color to a new Color with modified Alpha (transparency).
/// The converter parameter specifies the Alpha factor (0.0 to 1.0).
/// </summary>
public sealed class ColorToTransparentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Color color)
        {
            byte alpha = 255; // Default full opacity

            // Parse parameter as float factor (0.0 - 1.0) or byte (0-255)
            if (parameter is string paramStr && double.TryParse(paramStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double factor))
            {
                alpha = (byte)(255 * Math.Clamp(factor, 0.0, 1.0));
            }
            else if (parameter is double d)
            {
                alpha = (byte)(255 * Math.Clamp(d, 0.0, 1.0));
            }

            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        return Colors.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
