namespace CPCRemote.Core.IPC;

using System.Text.Json.Serialization;

/// <summary>
/// Request to get the current sensor configuration.
/// </summary>
public sealed record GetSensorConfigRequest : IpcRequest;

/// <summary>
/// Response containing the current sensor configuration.
/// </summary>
public sealed record GetSensorConfigResponse : IpcResponse
{
    /// <summary>
    /// The current sensor configuration.
    /// </summary>
    [JsonPropertyName("config")]
    public SensorConfigDto? Config { get; init; }
}

/// <summary>
/// Request to save sensor configuration.
/// </summary>
public sealed record SaveSensorConfigRequest : IpcRequest
{
    /// <summary>
    /// The sensor configuration to save.
    /// </summary>
    [JsonPropertyName("config")]
    public required SensorConfigDto Config { get; init; }
}

/// <summary>
/// Response after saving sensor configuration.
/// </summary>
public sealed record SaveSensorConfigResponse : IpcResponse;

/// <summary>
/// DTO for sensor configuration that can be serialized over IPC.
/// </summary>
public sealed record SensorConfigDto
{
    /// <summary>
    /// Configuration for CPU load sensor.
    /// </summary>
    [JsonPropertyName("cpuLoad")]
    public SensorMappingDto CpuLoad { get; init; } = new();

    /// <summary>
    /// Configuration for memory load sensor.
    /// </summary>
    [JsonPropertyName("memoryLoad")]
    public SensorMappingDto MemoryLoad { get; init; } = new();

    /// <summary>
    /// Configuration for CPU temperature sensor.
    /// </summary>
    [JsonPropertyName("cpuTemp")]
    public SensorMappingDto CpuTemp { get; init; } = new();

    /// <summary>
    /// Configuration for GPU temperature sensor.
    /// </summary>
    [JsonPropertyName("gpuTemp")]
    public SensorMappingDto GpuTemp { get; init; } = new();

    /// <summary>
    /// Additional custom sensor mappings.
    /// </summary>
    [JsonPropertyName("customSensors")]
    public List<CustomSensorDto> CustomSensors { get; init; } = [];
}

/// <summary>
/// DTO for sensor mapping configuration.
/// </summary>
public sealed record SensorMappingDto
{
    /// <summary>
    /// Patterns to match in the sensor label (case-insensitive).
    /// </summary>
    [JsonPropertyName("patterns")]
    public string[] Patterns { get; init; } = [];

    /// <summary>
    /// Expected unit of the sensor.
    /// </summary>
    [JsonPropertyName("unit")]
    public string Unit { get; init; } = string.Empty;

    /// <summary>
    /// If true, only values > 0 are considered valid.
    /// </summary>
    [JsonPropertyName("requirePositive")]
    public bool RequirePositive { get; init; }
}

/// <summary>
/// DTO for custom sensor configuration.
/// </summary>
public sealed record CustomSensorDto
{
    /// <summary>
    /// The JSON property name for this sensor in the /stats response.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Exact label to match in HWiNFO (case-insensitive).
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Expected unit of the sensor.
    /// </summary>
    [JsonPropertyName("unit")]
    public string Unit { get; init; } = string.Empty;

    /// <summary>
    /// If true, only values > 0 are considered valid.
    /// </summary>
    [JsonPropertyName("requirePositive")]
    public bool RequirePositive { get; init; }
}
