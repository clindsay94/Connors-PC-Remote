namespace CPCRemote.Core.IPC;

using System.Text.Json.Serialization;

/// <summary>
/// Request to get sensors currently selected in HWInfo's gadget.
/// </summary>
public sealed record GetGadgetSensorsRequest : IpcRequest;

/// <summary>
/// Response containing sensors from HWInfo's gadget registry.
/// </summary>
public sealed record GetGadgetSensorsResponse : IpcResponse
{
    /// <summary>
    /// List of sensors currently in the HWInfo gadget.
    /// </summary>
    [JsonPropertyName("sensors")]
    public GadgetSensorDto[] Sensors { get; init; } = [];
}

/// <summary>
/// Represents a sensor from HWInfo's gadget.
/// </summary>
public sealed record GadgetSensorDto
{
    /// <summary>
    /// Position in the gadget (0-indexed).
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; init; }

    /// <summary>
    /// Full sensor name from HWInfo (e.g., "CPU [#0]: AMD Ryzen 5 5600G").
    /// </summary>
    [JsonPropertyName("sensorName")]
    public string SensorName { get; init; } = string.Empty;

    /// <summary>
    /// Sensor label (e.g., "CPU (Tctl/Tdie)").
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Current sensor value.
    /// </summary>
    [JsonPropertyName("value")]
    public float Value { get; init; }

    /// <summary>
    /// Unit of measurement (e.g., "°C", "%", "MHz").
    /// </summary>
    [JsonPropertyName("unit")]
    public string Unit { get; init; } = string.Empty;

    /// <summary>
    /// Auto-detected category (CPU, GPU, Memory, Motherboard, Storage, Cooling, Network, Other).
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// HWInfo color code for the sensor.
    /// </summary>
    [JsonPropertyName("color")]
    public string Color { get; init; } = string.Empty;

    /// <summary>
    /// Whether the user has set this sensor as visible (from saved preferences).
    /// Defaults to true if no preferences are saved.
    /// </summary>
    [JsonPropertyName("isVisible")]
    public bool IsVisible { get; init; } = true;

    /// <summary>
    /// User-defined display order (from saved preferences).
    /// Defaults to the gadget index if no preferences are saved.
    /// </summary>
    [JsonPropertyName("displayOrder")]
    public int DisplayOrder { get; init; }
}
