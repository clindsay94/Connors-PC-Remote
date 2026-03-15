using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace CPCRemote.UI.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b && b)
            {
                return new SolidColorBrush(Colors.Green);
            }
            return new SolidColorBrush(Colors.Red);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color == Colors.Green;
            }
            return false;
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isVisible = value is bool b && b;
            
            if (parameter is string s && s.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
            {
                isVisible = !isVisible;
            }

            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
            {
                bool result = visibility == Visibility.Visible;
                if (parameter is string s && s.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
                {
                    return !result;
                }
                return result;
            }
            return false;
        }
    }

    public class BoolToGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (value is bool b && b) ? "\uE7F1" : "\uE7F2"; // Checkmark : Error/Stop
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is string s && s == "\uE7F1";
        }
    }

    /// <summary>
    /// Converts a boolean value to its negation, with Visibility support.
    /// </summary>
    public class BoolNegationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                bool result = !boolValue;

                if (targetType == typeof(Visibility))
                {
                    return result ? Visibility.Visible : Visibility.Collapsed;
                }

                return result;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }

            return value;
        }
    }

    /// <summary>
    /// Converts IsEditMode to appropriate icon glyph.
    /// </summary>
    public class BoolToEditGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Edit icon when not editing, Checkmark when done
            return (value is bool b && b) ? "\uE73E" : "\uE70F"; // Checkmark : Edit
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is string s && s == "\uE73E";
        }
    }

    /// <summary>
    /// Converts IsEditMode to appropriate button text.
    /// </summary>
    public class BoolToEditTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (value is bool b && b) ? "Done" : "Edit";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is string s && s == "Done";
        }
    }

    /// <summary>
    /// Converts a string to Visibility: non-null/non-empty → Visible, else Collapsed.
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return !string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    /// <summary>
    /// Inverts a boolean and returns Visibility (true → Collapsed, false → Visible).
    /// </summary>
    public class BoolToInverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (value is bool b && b) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is Visibility v && v == Visibility.Collapsed;
        }
    }
}
