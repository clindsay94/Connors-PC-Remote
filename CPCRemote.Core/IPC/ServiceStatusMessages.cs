namespace CPCRemote.Core.IPC;

using System.Text.Json.Serialization;

/// <summary>
/// Request to get the current service status.
/// </summary>
public sealed record ServiceStatusRequest : IpcRequest;

/// <summary>
/// Response containing the service status information.
/// </summary>
public sealed record ServiceStatusResponse : IpcResponse
{
    /// <summary>
    /// Gets or sets the service version string.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; init; }

    /// <summary>
    /// Gets or sets the service uptime in seconds.
    /// </summary>
    [JsonPropertyName("uptimeSeconds")]
    public double UptimeSeconds { get; init; }

    /// <summary>
    /// Gets or sets the HTTP listener address (e.g., "http://+:5005/").
    /// </summary>
    [JsonPropertyName("httpListenerAddress")]
    public string? HttpListenerAddress { get; init; }

    /// <summary>
    /// Gets or sets whether the HTTP listener is actively listening.
    /// </summary>
    [JsonPropertyName("isListening")]
    public bool IsListening { get; init; }

    /// <summary>
    /// Gets or sets whether hardware monitoring is available.
    /// </summary>
    [JsonPropertyName("isHardwareMonitoringAvailable")]
    public bool IsHardwareMonitoringAvailable { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when the service started (UTC).
    /// </summary>
    [JsonPropertyName("startTimeUtc")]
    public DateTime StartTimeUtc { get; init; }
}
