namespace CPCRemote.Core.IPC;

using System.Text.Json.Serialization;

/// <summary>
/// Request to get current PC hardware statistics.
/// </summary>
public sealed record GetStatsRequest : IpcRequest;

/// <summary>
/// Response containing PC hardware statistics.
/// </summary>
public sealed record GetStatsResponse : IpcResponse
{
    /// <summary>
    /// Gets or sets the CPU load percentage (0-100).
    /// </summary>
    [JsonPropertyName("cpu")]
    public float? Cpu { get; init; }

    /// <summary>
    /// Gets or sets the memory usage percentage (0-100).
    /// </summary>
    [JsonPropertyName("memory")]
    public float? Memory { get; init; }

    /// <summary>
    /// Gets or sets the CPU temperature in Celsius.
    /// </summary>
    [JsonPropertyName("cpuTemp")]
    public float? CpuTemp { get; init; }

    /// <summary>
    /// Gets or sets the GPU temperature in Celsius.
    /// </summary>
    [JsonPropertyName("gpuTemp")]
    public float? GpuTemp { get; init; }
}
