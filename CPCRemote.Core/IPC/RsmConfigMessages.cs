namespace CPCRemote.Core.IPC;

using System.Text.Json.Serialization;

/// <summary>
/// Data transfer object for RSM (Remote Shutdown Manager) configuration.
/// </summary>
public class RsmConfigDto
{
    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;

    [JsonPropertyName("port")]
    public int Port { get; set; }

    [JsonPropertyName("secret")]
    public string? Secret { get; set; }
}

/// <summary>
/// Request to update the RSM configuration.
/// </summary>
public record SaveRsmConfigRequest : IpcRequest
{
    [JsonPropertyName("config")]
    public required RsmConfigDto Config { get; init; }
}

/// <summary>
/// Response to a SaveRsmConfigRequest.
/// </summary>
public record SaveRsmConfigResponse : IpcResponse;
