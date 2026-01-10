using Microsoft.UI.Xaml.Data;
using System;

namespace CPCRemote.UI.Converters;

public sealed class NullableFloatToDoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is float f)
        {
            return (double)f;
        }
        if (value is double d)
        {
            return d;
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
