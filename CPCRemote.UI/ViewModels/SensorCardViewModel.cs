namespace CPCRemote.UI.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CPCRemote.UI.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

/// <summary>
/// ViewModel for an individual sensor card on the dynamic dashboard.
/// </summary>
public partial class SensorCardViewModel : ObservableObject
{
    // ── Static Color Service ──

    private static CategoryColorService? _colorService;

    /// <summary>
    /// Initializes the static color service reference.
    /// </summary>
    public static void SetColorService(CategoryColorService service)
    {
        _colorService = service;
    }

    /// <summary>
    /// Sensor label (unique identifier from HWInfo).
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Full sensor name from HWInfo.
    /// </summary>
    public string SensorName { get; init; } = string.Empty;

    /// <summary>
    /// Category (CPU, GPU, Memory, Motherboard, Storage, Cooling, Network, Other).
    /// </summary>
    public string Category { get; init; } = "Other";

    /// <summary>
    /// Unit of measurement (°C, %, MHz, W, V, RPM).
    /// </summary>
    public string Unit { get; init; } = string.Empty;

    /// <summary>
    /// HWInfo color code (if specified).
    /// </summary>
    public string HwInfoColor { get; init; } = string.Empty;

    /// <summary>
    /// Current sensor value.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedValue))]
    public partial float? Value { get; set; }

    /// <summary>
    /// User-defined display order (lower = first).
    /// </summary>
    [ObservableProperty]
    public partial int DisplayOrder { get; set; }

    /// <summary>
    /// Whether the sensor is visible on the dashboard.
    /// </summary>
    [ObservableProperty]
    public partial bool IsVisible { get; set; } = true;

    /// <summary>
    /// Whether the dashboard is in edit mode (set by parent).
    /// </summary>
    [ObservableProperty]
    public partial bool IsInEditMode { get; set; }

    /// <summary>
    /// Formatted value string with unit.
    /// </summary>
    public string FormattedValue => Value.HasValue
        ? $"{Value.Value:F1} {Unit}"
        : "--";

    /// <summary>
    /// Whether this sensor should display as a gauge (load/temp) vs numeric.
    /// </summary>
    public bool IsGaugeSensor => Unit is "%" or "°C";

    /// <summary>
    /// Maximum value for gauge display (based on unit type).
    /// </summary>
    public double MaxValue => Unit switch
    {
        "%" => 100,
        "°C" => 100,
        "MHz" => 6000,
        "W" => 500,
        "V" => 2,
        "RPM" => 5000,
        _ => 100
    };

    /// <summary>
    /// Gets the accent color for this sensor's category (from settings or defaults).
    /// </summary>
    public Color CategoryColor => _colorService?.GetColor(Category) ?? Category switch
    {
        "CPU" => Color.FromArgb(255, 255, 107, 107),
        "GPU" => Color.FromArgb(255, 0, 230, 118),
        "Memory" => Color.FromArgb(255, 92, 107, 192),
        "Motherboard" => Color.FromArgb(255, 255, 193, 7),
        "Storage" => Color.FromArgb(255, 121, 134, 203),
        "Cooling" => Color.FromArgb(255, 77, 208, 225),
        "Network" => Color.FromArgb(255, 224, 64, 251),
        _ => Color.FromArgb(255, 144, 164, 174)
    };

    /// <summary>
    /// Gets the gradient brush resource key for this category.
    /// </summary>
    public string CategoryGradientKey => $"{Category}CategoryGradient";

    /// <summary>
    /// Gets the accent brush resource key for this category.
    /// </summary>
    public string CategoryAccentKey => $"{Category}CategoryAccent";

    /// <summary>
    /// Creates a solid color brush from the category color.
    /// </summary>
    public SolidColorBrush CategoryBrush => new(CategoryColor);

    /// <summary>
    /// Refreshes the category color brushes (call when color service reports changes).
    /// </summary>
    public void RefreshCategoryColor()
    {
        OnPropertyChanged(nameof(CategoryColor));
        OnPropertyChanged(nameof(CategoryBrush));
    }
}
