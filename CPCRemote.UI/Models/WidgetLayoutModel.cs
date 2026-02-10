namespace CPCRemote.UI.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Defines the size category for a dashboard widget.
/// Controls which visualization is shown (progress bar vs radial gauge).
/// </summary>
public enum WidgetSize
{
    /// <summary>Wide layout: shows linear progress bar + value text.</summary>
    Wide,
    /// <summary>Square layout: shows radial gauge.</summary>
    Square,
    /// <summary>Tall/large layout: shows radial gauge + sparkline.</summary>
    Tall
}

/// <summary>
/// Persisted layout information for a single dashboard widget.
/// </summary>
public sealed class WidgetLayoutEntry
{
    /// <summary>Sensor label (unique key matching HWInfo gadget sensor).</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>Display order (lower = first).</summary>
    [JsonPropertyName("order")]
    public int Order { get; set; }

    /// <summary>Grid column span.</summary>
    [JsonPropertyName("colSpan")]
    public int ColumnSpan { get; set; } = 1;

    /// <summary>Grid row span.</summary>
    [JsonPropertyName("rowSpan")]
    public int RowSpan { get; set; } = 1;

    /// <summary>Widget size category.</summary>
    [JsonPropertyName("size")]
    public WidgetSize Size { get; set; } = WidgetSize.Square;
}

/// <summary>
/// Root object for persisting the entire dashboard layout to JSON.
/// </summary>
public sealed class DashboardLayout
{
    /// <summary>Layout version for forward-compatibility.</summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>Position of the Quick Actions bar (0 = top, 1 = bottom).</summary>
    [JsonPropertyName("quickActionsPosition")]
    public int QuickActionsPosition { get; set; }

    /// <summary>Individual widget layout entries.</summary>
    [JsonPropertyName("widgets")]
    public List<WidgetLayoutEntry> Widgets { get; set; } = [];
}
