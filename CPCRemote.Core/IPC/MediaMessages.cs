namespace CPCRemote.Core.IPC;

using System.Text.Json.Serialization;

// ══════════════════════════════════════════════════════════
// Media & Volume Control IPC Messages
// ══════════════════════════════════════════════════════════

/// <summary>
/// Media action types for playback control.
/// </summary>
public enum MediaAction
{
    PlayPause,
    Next,
    Previous,
    Stop
}

// ── Get Volume ──

/// <summary>Request to get current system volume level and mute state.</summary>
public sealed record GetVolumeRequest : IpcRequest;

/// <summary>Response containing current volume level and mute state.</summary>
public sealed record GetVolumeResponse : IpcResponse
{
    /// <summary>Volume level 0-100.</summary>
    [JsonPropertyName("level")]
    public int Level { get; init; }

    /// <summary>Whether the system is muted.</summary>
    [JsonPropertyName("isMuted")]
    public bool IsMuted { get; init; }
}

// ── Set Volume ──

/// <summary>Request to set the system volume level.</summary>
public sealed record SetVolumeRequest : IpcRequest
{
    /// <summary>Target volume level 0-100.</summary>
    [JsonPropertyName("level")]
    public int Level { get; init; }
}

/// <summary>Response confirming the volume was set.</summary>
public sealed record SetVolumeResponse : IpcResponse;

// ── Toggle Mute ──

/// <summary>Request to toggle mute on/off.</summary>
public sealed record ToggleMuteRequest : IpcRequest;

/// <summary>Response confirming mute toggle, including new state.</summary>
public sealed record ToggleMuteResponse : IpcResponse
{
    /// <summary>New mute state after toggle.</summary>
    [JsonPropertyName("isMuted")]
    public bool IsMuted { get; init; }
}

// ── Send Media Key ──

/// <summary>Request to send a media key (play/pause, next, etc.).</summary>
public sealed record SendMediaKeyRequest : IpcRequest
{
    /// <summary>The media action to perform.</summary>
    [JsonPropertyName("action")]
    public MediaAction Action { get; init; }
}

/// <summary>Response confirming the media key was sent.</summary>
public sealed record SendMediaKeyResponse : IpcResponse;
