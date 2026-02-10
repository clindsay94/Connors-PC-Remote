namespace CPCRemote.UI.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CPCRemote.UI.Models;
using CPCRemote.UI.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

/// <summary>
/// ViewModel for an individual dashboard widget on the Home Page.
/// Wraps a sensor with layout information (size, position) and visual state.
/// </summary>
public partial class DashboardWidgetViewModel : ObservableObject
{
    // ── Static Color Service ──

    private static CategoryColorService? _colorService;

    /// <summary>
    /// Initializes the static color service reference.
    /// Call once at app startup or when the HomePage is created.
    /// </summary>
    public static void SetColorService(CategoryColorService service)
    {
        _colorService = service;
    }

    // ── Sensor Identity ──

    /// <summary>Sensor label (unique key).</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Full sensor name from HWInfo.</summary>
    public string SensorName { get; init; } = string.Empty;

    /// <summary>Category (CPU, GPU, Memory, etc.).</summary>
    public string Category { get; init; } = "Other";

    /// <summary>Unit of measurement.</summary>
    public string Unit { get; init; } = string.Empty;

    // ── Live Data ──

    /// <summary>Current sensor value.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedValue))]
    [NotifyPropertyChangedFor(nameof(NormalizedValue))]
    public partial float? Value { get; set; }

    // ── Layout ──

    /// <summary>Widget size category controlling the visual presentation.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowRadialGauge))]
    [NotifyPropertyChangedFor(nameof(ShowLinearGauge))]
    [NotifyPropertyChangedFor(nameof(ShowTallLayout))]
    public partial WidgetSize WidgetSize { get; set; } = WidgetSize.Square;

    /// <summary>Display order (lower = first).</summary>
    [ObservableProperty]
    public partial int DisplayOrder { get; set; }

    /// <summary>Grid column span.</summary>
    [ObservableProperty]
    public partial int ColumnSpan { get; set; } = 1;

    /// <summary>Grid row span.</summary>
    [ObservableProperty]
    public partial int RowSpan { get; set; } = 1;

    // ── Edit Mode ──

    /// <summary>Whether the parent dashboard is in edit mode.</summary>
    [ObservableProperty]
    public partial bool IsInEditMode { get; set; }

    // ── Computed Properties ──

    /// <summary>Formatted value string with unit.</summary>
    public string FormattedValue => Value.HasValue
        ? $"{Value.Value:F1} {Unit}"
        : "--";

    /// <summary>Whether this sensor should use a gauge display.</summary>
    public bool IsGaugeSensor => Unit is "%" or "°C";

    /// <summary>Maximum gauge value based on unit type.</summary>
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

    /// <summary>Normalized value (0-1) for progress indicators.</summary>
    public double NormalizedValue => Value.HasValue
        ? System.Math.Clamp(Value.Value / MaxValue, 0, 1)
        : 0;

    /// <summary>Show radial gauge only for Square size with gauge-compatible units.</summary>
    public bool ShowRadialGauge => WidgetSize == WidgetSize.Square && IsGaugeSensor;

    /// <summary>Show linear gauge for Wide size, or Square with non-gauge units.</summary>
    public bool ShowLinearGauge => WidgetSize == WidgetSize.Wide || (WidgetSize == WidgetSize.Square && !IsGaugeSensor);

    /// <summary>Show tall layout: gauge at top + value at bottom.</summary>
    public bool ShowTallLayout => WidgetSize == WidgetSize.Tall;

    /// <summary>Category accent color (loaded from user settings or defaults).</summary>
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

    /// <summary>SolidColorBrush for category accent.</summary>
    public SolidColorBrush CategoryBrush => new(CategoryColor);

    /// <summary>Category gradient resource key.</summary>
    public string CategoryGradientKey => $"{Category}CategoryGradient";

    /// <summary>Category accent resource key.</summary>
    public string CategoryAccentKey => $"{Category}CategoryAccent";

    /// <summary>Glyph icon for the sensor category.</summary>
    public string CategoryGlyph => Category switch
    {
        "CPU" => "\uE950",       // Processor
        "GPU" => "\uE7F8",       // Display
        "Memory" => "\uEDA2",    // Memory
        "Motherboard" => "\uE964", // Devices
        "Storage" => "\uEDA2",   // HardDrive
        "Cooling" => "\uE9CA",   // Fan
        "Network" => "\uE968",   // Network
        _ => "\uE946"            // Diagnostic
    };

    /// <summary>
    /// Refreshes the category color brushes (call when color service reports changes).
    /// </summary>
    public void RefreshCategoryColor()
    {
        OnPropertyChanged(nameof(CategoryColor));
        OnPropertyChanged(nameof(CategoryBrush));
    }

    public bool IsMemoryWidget => Category == "Memory";
}
