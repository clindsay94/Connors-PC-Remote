namespace CPCRemote.Core.IPC;

using System.Text.Json.Serialization;

/// <summary>
/// Request to get current PC hardware statistics.
/// </summary>
public sealed record GetStatsRequest : IpcRequest;

/// <summary>
/// Response containing PC hardware statistics organized by component.
/// </summary>
public sealed record GetStatsResponse : IpcResponse
{
    /// <summary>
    /// Gets or sets the CPU statistics.
    /// </summary>
    [JsonPropertyName("cpu")]
    public CpuStats? Cpu { get; init; }

    /// <summary>
    /// Gets or sets the memory statistics.
    /// </summary>
    [JsonPropertyName("memory")]
    public MemoryStats? Memory { get; init; }

    /// <summary>
    /// Gets or sets the GPU statistics.
    /// </summary>
    [JsonPropertyName("gpu")]
    public GpuStats? Gpu { get; init; }

    /// <summary>
    /// Gets or sets the motherboard statistics.
    /// </summary>
    [JsonPropertyName("motherboard")]
    public MotherboardStats? Motherboard { get; init; }
}

/// <summary>
/// CPU statistics from HWiNFO.
/// </summary>
public sealed record CpuStats
{
    /// <summary>
    /// Total CPU utilization percentage.
    /// </summary>
    [JsonPropertyName("utility")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? Utility { get; init; }

    /// <summary>
    /// CPU package/die temperature (Tctl/Tdie).
    /// </summary>
    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? Temperature { get; init; }

    /// <summary>
    /// CPU die average temperature.
    /// </summary>
    [JsonPropertyName("dieAvgTemp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? DieAvgTemp { get; init; }

    /// <summary>
    /// CPU IOD hotspot temperature.
    /// </summary>
    [JsonPropertyName("iodHotspot")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? IodHotspot { get; init; }

    /// <summary>
    /// CPU package power in watts.
    /// </summary>
    [JsonPropertyName("packagePower")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? PackagePower { get; init; }

    /// <summary>
    /// CPU PPT (Package Power Tracking) in watts.
    /// </summary>
    [JsonPropertyName("ppt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? Ppt { get; init; }

    /// <summary>
    /// Average core clock speed in MHz.
    /// </summary>
    [JsonPropertyName("coreClock")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? CoreClock { get; init; }

    /// <summary>
    /// Average effective clock speed in MHz.
    /// </summary>
    [JsonPropertyName("effectiveClock")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? EffectiveClock { get; init; }

    /// <summary>
    /// Per-core effective clock speeds in MHz (Core 0-7).
    /// </summary>
    [JsonPropertyName("coreEffectiveClocks")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float[]? CoreEffectiveClocks { get; init; }
}

/// <summary>
/// Memory statistics from HWiNFO.
/// </summary>
public sealed record MemoryStats
{
    /// <summary>
    /// Physical memory load percentage.
    /// </summary>
    [JsonPropertyName("load")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? Load { get; init; }

    /// <summary>
    /// DDR5 DIMM temperatures in Celsius (indexed by slot).
    /// </summary>
    [JsonPropertyName("dimmTemps")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DimmTemp[]? DimmTemps { get; init; }
}

/// <summary>
/// Temperature reading for a single DIMM.
/// </summary>
public sealed record DimmTemp
{
    /// <summary>
    /// DIMM slot index (e.g., 1, 2, 3, 4).
    /// </summary>
    [JsonPropertyName("slot")]
    public int Slot { get; init; }

    /// <summary>
    /// SPD Hub temperature in Celsius.
    /// </summary>
    [JsonPropertyName("temp")]
    public float Temp { get; init; }
}

/// <summary>
/// GPU statistics from HWiNFO.
/// </summary>
public sealed record GpuStats
{
    /// <summary>
    /// GPU core temperature in Celsius.
    /// </summary>
    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? Temperature { get; init; }

    /// <summary>
    /// GPU memory junction temperature in Celsius.
    /// </summary>
    [JsonPropertyName("memJunctionTemp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? MemJunctionTemp { get; init; }

    /// <summary>
    /// GPU power consumption in watts.
    /// </summary>
    [JsonPropertyName("power")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? Power { get; init; }

    /// <summary>
    /// GPU clock speed in MHz.
    /// </summary>
    [JsonPropertyName("clock")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? Clock { get; init; }

    /// <summary>
    /// GPU effective clock speed in MHz.
    /// </summary>
    [JsonPropertyName("effectiveClock")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? EffectiveClock { get; init; }

    /// <summary>
    /// GPU memory usage percentage.
    /// </summary>
    [JsonPropertyName("memoryUsage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? MemoryUsage { get; init; }

    /// <summary>
    /// GPU core load percentage.
    /// </summary>
    [JsonPropertyName("coreLoad")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? CoreLoad { get; init; }
}

/// <summary>
/// Motherboard statistics from HWiNFO.
/// </summary>
public sealed record MotherboardStats
{
    /// <summary>
    /// CPU core voltage (Vcore).
    /// </summary>
    [JsonPropertyName("vcore")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? Vcore { get; init; }

    /// <summary>
    /// SOC voltage (VDDCR_SOC).
    /// </summary>
    [JsonPropertyName("vsoc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? Vsoc { get; init; }
}
