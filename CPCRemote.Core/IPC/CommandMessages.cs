namespace CPCRemote.Core.IPC;

using System.Text.Json.Serialization;

using CPCRemote.Core.Enums;

/// <summary>
/// Request to execute a power command on the PC.
/// </summary>
public sealed record ExecuteCommandRequest : IpcRequest
{
    /// <summary>
    /// Gets or sets the command type to execute.
    /// </summary>
    [JsonPropertyName("commandType")]
    public TrayCommandType CommandType { get; init; }
}

/// <summary>
/// Response after executing a command.
/// </summary>
public sealed record ExecuteCommandResponse : IpcResponse;

/// <summary>
/// Generic error response for any failed operation.
/// </summary>
public sealed record ErrorResponse : IpcResponse
{
    /// <summary>
    /// Gets or sets the exception type name if available.
    /// </summary>
    [JsonPropertyName("exceptionType")]
    public string? ExceptionType { get; init; }

    /// <summary>
    /// Gets or sets the stack trace if available (debug builds only).
    /// </summary>
    [JsonPropertyName("stackTrace")]
    public string? StackTrace { get; init; }
}
