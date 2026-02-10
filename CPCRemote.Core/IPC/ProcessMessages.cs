namespace CPCRemote.Core.IPC;

using System.Text.Json.Serialization;

// ══════════════════════════════════════════════════════════
// Process Management IPC Messages
// ══════════════════════════════════════════════════════════

/// <summary>
/// Information about a running process.
/// </summary>
public sealed record ProcessInfoDto
{
    /// <summary>Process ID.</summary>
    [JsonPropertyName("pid")]
    public int Pid { get; init; }

    /// <summary>Process name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Memory usage in MB.</summary>
    [JsonPropertyName("memoryMb")]
    public double MemoryMb { get; init; }

    /// <summary>Whether this is a protected system process that cannot be killed.</summary>
    [JsonPropertyName("isProtected")]
    public bool IsProtected { get; init; }
}

// ── Get Top Processes ──

/// <summary>Request to get the top processes by memory usage.</summary>
public sealed record GetTopProcessesRequest : IpcRequest
{
    /// <summary>Maximum number of processes to return (default 20).</summary>
    [JsonPropertyName("count")]
    public int Count { get; init; } = 20;
}

/// <summary>Response containing top processes.</summary>
public sealed record GetTopProcessesResponse : IpcResponse
{
    /// <summary>Array of process information.</summary>
    [JsonPropertyName("processes")]
    public ProcessInfoDto[] Processes { get; init; } = [];
}

// ── Kill Process ──

/// <summary>Request to terminate a process by PID.</summary>
public sealed record KillProcessRequest : IpcRequest
{
    /// <summary>Process ID to terminate.</summary>
    [JsonPropertyName("pid")]
    public int Pid { get; init; }
}

/// <summary>Response confirming whether the process was killed.</summary>
public sealed record KillProcessResponse : IpcResponse
{
    /// <summary>Name of the process that was killed.</summary>
    [JsonPropertyName("processName")]
    public string? ProcessName { get; init; }
}
