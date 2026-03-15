using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using CPCRemote.UI.Converters;
using NUnit.Framework;

namespace CPCRemote.Tests;

[TestFixture]
public class ConverterLogicTests
{
    [Test]
    public void BoolToColorConverter_ConvertBack_ReturnsExpected()
    {
        var converter = new BoolToColorConverter();
        Assert.That(converter.ConvertBack(new SolidColorBrush(Colors.Green), typeof(bool), null, null), Is.True);
        Assert.That(converter.ConvertBack(new SolidColorBrush(Colors.Red), typeof(bool), null, null), Is.False);
        Assert.That(converter.ConvertBack(null, typeof(bool), null, null), Is.False);
    }

    [Test]
    public void BoolToVisibilityConverter_ConvertBack_ReturnsExpected()
    {
        var converter = new BoolToVisibilityConverter();
        Assert.That(converter.ConvertBack(Visibility.Visible, typeof(bool), null, null), Is.True);
        Assert.That(converter.ConvertBack(Visibility.Collapsed, typeof(bool), null, null), Is.False);
        Assert.That(converter.ConvertBack(Visibility.Visible, typeof(bool), "Inverse", null), Is.False);
        Assert.That(converter.ConvertBack(Visibility.Collapsed, typeof(bool), "Inverse", null), Is.True);
    }

    [Test]
    public void BoolToGlyphConverter_ConvertBack_ReturnsExpected()
    {
        var converter = new BoolToGlyphConverter();
        Assert.That(converter.ConvertBack("\uE7F1", typeof(bool), null, null), Is.True);
        Assert.That(converter.ConvertBack("\uE7F2", typeof(bool), null, null), Is.False);
    }

    [Test]
    public void BoolToEditGlyphConverter_ConvertBack_ReturnsExpected()
    {
        var converter = new BoolToEditGlyphConverter();
        Assert.That(converter.ConvertBack("\uE73E", typeof(bool), null, null), Is.True);
        Assert.That(converter.ConvertBack("\uE70F", typeof(bool), null, null), Is.False);
    }

    [Test]
    public void BoolToEditTextConverter_ConvertBack_ReturnsExpected()
    {
        var converter = new BoolToEditTextConverter();
        Assert.That(converter.ConvertBack("Done", typeof(bool), null, null), Is.True);
        Assert.That(converter.ConvertBack("Edit", typeof(bool), null, null), Is.False);
    }

    [Test]
    public void StringToVisibilityConverter_ConvertBack_ReturnsDoNothing()
    {
        var converter = new StringToVisibilityConverter();
        Assert.That(converter.ConvertBack(Visibility.Visible, typeof(string), null, null), Is.EqualTo(Binding.DoNothing));
    }

    [Test]
    public void BoolToInverseVisibilityConverter_ConvertBack_ReturnsExpected()
    {
        var converter = new BoolToInverseVisibilityConverter();
        Assert.That(converter.ConvertBack(Visibility.Collapsed, typeof(bool), null, null), Is.True);
        Assert.That(converter.ConvertBack(Visibility.Visible, typeof(bool), null, null), Is.False);
    }

    [Test]
    public void EnumToVisibilityConverter_ConvertBack_ReturnsDoNothing()
    {
        var converter = new EnumToVisibilityConverter();
        Assert.That(converter.ConvertBack(Visibility.Visible, typeof(object), "SomeValue", null), Is.EqualTo(Binding.DoNothing));
    }
}
