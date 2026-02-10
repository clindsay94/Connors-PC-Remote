namespace CPCRemote.Core.IPC;

using System.Text.Json.Serialization;

/// <summary>
/// Request to get user's sensor display preferences.
/// </summary>
public sealed record GetSensorPreferencesRequest : IpcRequest;

/// <summary>
/// Response containing user's sensor display preferences.
/// </summary>
public sealed record GetSensorPreferencesResponse : IpcResponse
{
    /// <summary>
    /// User preferences for sensor display.
    /// </summary>
    [JsonPropertyName("preferences")]
    public SensorPreferenceDto[] Preferences { get; init; } = [];
}

/// <summary>
/// Request to save user's sensor display preferences.
/// </summary>
public sealed record SaveSensorPreferencesRequest : IpcRequest
{
    /// <summary>
    /// Preferences to save.
    /// </summary>
    [JsonPropertyName("preferences")]
    public required SensorPreferenceDto[] Preferences { get; init; }
}

/// <summary>
/// Response after saving sensor preferences.
/// </summary>
public sealed record SaveSensorPreferencesResponse : IpcResponse;

/// <summary>
/// User preference for an individual sensor's display.
/// </summary>
public sealed record SensorPreferenceDto
{
    /// <summary>
    /// Sensor label (unique identifier).
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// User-defined display order (lower = first).
    /// </summary>
    [JsonPropertyName("displayOrder")]
    public int DisplayOrder { get; init; }

    /// <summary>
    /// Whether the sensor is visible on the dashboard.
    /// </summary>
    [JsonPropertyName("isVisible")]
    public bool IsVisible { get; init; } = true;

    /// <summary>
    /// Optional user-defined custom color override (hex code).
    /// </summary>
    [JsonPropertyName("customColor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CustomColor { get; init; }
}
