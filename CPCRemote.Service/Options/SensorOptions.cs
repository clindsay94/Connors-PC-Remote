namespace CPCRemote.Service.Options;

/// <summary>
/// Configuration options for hardware sensor matching.
/// Allows users to customize which HWiNFO sensor labels map to which stats.
/// </summary>
public sealed class SensorOptions
{
    /// <summary>
    /// Configuration for CPU load sensor.
    /// </summary>
    public SensorMappingOptions CpuLoad { get; set; } = new()
    {
        Patterns = ["total cpu usage", "cpu utilization", "cpu usage"],
        Unit = "%"
    };

    /// <summary>
    /// Configuration for memory load sensor.
    /// </summary>
    public SensorMappingOptions MemoryLoad { get; set; } = new()
    {
        Patterns = ["physical memory load", "memory usage", "memory load", "ram usage"],
        Unit = "%"
    };

    /// <summary>
    /// Configuration for CPU temperature sensor.
    /// </summary>
    public SensorMappingOptions CpuTemp { get; set; } = new()
    {
        Patterns = ["cpu package", "cpu (tctl", "cpu (tdie", "cpu die", "core max", "tdie", "tctl"],
        Unit = "°c",
        RequirePositive = true
    };

    /// <summary>
    /// Configuration for GPU temperature sensor.
    /// </summary>
    public SensorMappingOptions GpuTemp { get; set; } = new()
    {
        Patterns = ["gpu hot spot", "gpu temperature", "gpu core", "gpu edge", "gpu junction"],
        Unit = "°c",
        RequirePositive = true
    };

    /// <summary>
    /// Additional custom sensor mappings that will be included in the /stats response.
    /// Use this to add sensors like individual core temps, GPU memory junction temp, etc.
    /// </summary>
    public List<CustomSensorOptions> CustomSensors { get; set; } = [];
}

/// <summary>
/// Configuration for matching a specific sensor.
/// </summary>
public sealed class SensorMappingOptions
{
    /// <summary>
    /// Patterns to match in the sensor label (case-insensitive).
    /// First match wins.
    /// </summary>
    public string[] Patterns { get; set; } = [];

    /// <summary>
    /// Expected unit of the sensor (e.g., "%", "°c", "RPM").
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// If true, only values > 0 are considered valid.
    /// </summary>
    public bool RequirePositive { get; set; }
}

/// <summary>
/// Configuration for a custom sensor that will be added to the stats response.
/// </summary>
public sealed class CustomSensorOptions
{
    /// <summary>
    /// The JSON property name for this sensor in the /stats response.
    /// Example: "core0Temp", "gpuMemJunctionTemp", "fanSpeed"
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Exact label to match in HWiNFO (case-insensitive).
    /// Example: "Core #0 Temp", "GPU Memory Junction Temperature"
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Expected unit of the sensor (e.g., "°c", "RPM", "%").
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// If true, only values > 0 are considered valid.
    /// </summary>
    public bool RequirePositive { get; set; }
}
