namespace CPCRemote.Core.IPC;

using System.Text.Json.Serialization;

/// <summary>
/// Base class for all IPC messages exchanged between the UI and Service.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(GetStatsRequest), "GetStatsRequest")]
[JsonDerivedType(typeof(GetStatsResponse), "GetStatsResponse")]
[JsonDerivedType(typeof(GetAppsRequest), "GetAppsRequest")]
[JsonDerivedType(typeof(GetAppsResponse), "GetAppsResponse")]
[JsonDerivedType(typeof(SaveAppRequest), "SaveAppRequest")]
[JsonDerivedType(typeof(SaveAppResponse), "SaveAppResponse")]
[JsonDerivedType(typeof(DeleteAppRequest), "DeleteAppRequest")]
[JsonDerivedType(typeof(DeleteAppResponse), "DeleteAppResponse")]
[JsonDerivedType(typeof(ServiceStatusRequest), "ServiceStatusRequest")]
[JsonDerivedType(typeof(ServiceStatusResponse), "ServiceStatusResponse")]
[JsonDerivedType(typeof(ExecuteCommandRequest), "ExecuteCommandRequest")]
[JsonDerivedType(typeof(ExecuteCommandResponse), "ExecuteCommandResponse")]
[JsonDerivedType(typeof(ErrorResponse), "ErrorResponse")]
public abstract record IpcMessage
{
    /// <summary>
    /// Gets or sets the correlation ID for request-response pairing.
    /// </summary>
    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString("N");
}

/// <summary>
/// Base class for IPC requests.
/// </summary>
public abstract record IpcRequest : IpcMessage;

/// <summary>
/// Base class for IPC responses.
/// </summary>
public abstract record IpcResponse : IpcMessage
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>
    /// Gets or sets an optional error message if the operation failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }
}
