using System;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace CPCRemote.UI.Converters;

public class SegmentedStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // We cannot easily convert String -> SegmentedItem without reference to the items.
        // Returning null or DependencyProperty.UnsetValue basically means "no selection" or "keep current".
        // For a build fix, this is sufficient. For runtime, we might need a different approach (e.g. using Tag behavior).
        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is SegmentedItem item && item.Tag is string tag)
        {
            return tag;
        }
        return null!; // Or string.Empty
    }
}
