using CPCRemote.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CPCRemote.UI.Controls;

/// <summary>
/// A dynamic sensor card control that displays HWInfo sensor data
/// with category-based colors and animated visualizations.
/// </summary>
public sealed partial class SensorCard : UserControl
{
    public SensorCard()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the sensor label.
    /// </summary>
    public string Label => ViewModel?.Label ?? string.Empty;

    /// <summary>
    /// Gets the full sensor name.
    /// </summary>
    public string SensorName => ViewModel?.SensorName ?? string.Empty;

    /// <summary>
    /// Gets the sensor category.
    /// </summary>
    public string Category => ViewModel?.Category ?? "Other";

    /// <summary>
    /// Gets the unit of measurement.
    /// </summary>
    public string Unit => ViewModel?.Unit ?? string.Empty;

    /// <summary>
    /// Gets the current value.
    /// </summary>
    public float? Value => ViewModel?.Value;

    /// <summary>
    /// Gets the formatted value with unit.
    /// </summary>
    public string FormattedValue => ViewModel?.FormattedValue ?? "--";

    /// <summary>
    /// Gets whether this is a gauge sensor (% or temperature).
    /// </summary>
    public bool IsGaugeSensor => ViewModel?.IsGaugeSensor ?? false;

    /// <summary>
    /// Gets the inverse of IsGaugeSensor for visibility binding.
    /// </summary>
    public Visibility IsGaugeSensorInverted => IsGaugeSensor ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// Gets the maximum value for gauge display.
    /// </summary>
    public double MaxValue => ViewModel?.MaxValue ?? 100;

    /// <summary>
    /// Gets the category accent color brush.
    /// </summary>
    public SolidColorBrush CategoryBrush => ViewModel?.CategoryBrush ?? new SolidColorBrush(Microsoft.UI.Colors.Gray);

    /// <summary>
    /// The ViewModel for this sensor card.
    /// </summary>
    public SensorCardViewModel? ViewModel
    {
        get => DataContext as SensorCardViewModel;
        set => DataContext = value;
    }
}
