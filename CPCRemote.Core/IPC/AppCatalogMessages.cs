namespace CPCRemote.Core.IPC;

using System.Text.Json.Serialization;

using CPCRemote.Core.Models;

/// <summary>
/// Request to get all configured applications in the catalog.
/// </summary>
public sealed record GetAppsRequest : IpcRequest;

/// <summary>
/// Response containing the list of configured applications.
/// </summary>
public sealed record GetAppsResponse : IpcResponse
{
    /// <summary>
    /// Gets or sets the list of application catalog entries.
    /// </summary>
    [JsonPropertyName("apps")]
    public IReadOnlyList<AppCatalogEntry> Apps { get; init; } = [];
}

/// <summary>
/// Request to save or update an application entry.
/// </summary>
public sealed record SaveAppRequest : IpcRequest
{
    /// <summary>
    /// Gets or sets the application entry to save.
    /// </summary>
    [JsonPropertyName("app")]
    public required AppCatalogEntry App { get; init; }
}

/// <summary>
/// Response after saving an application entry.
/// </summary>
public sealed record SaveAppResponse : IpcResponse
{
    /// <summary>
    /// Gets or sets the saved application entry (with any server-side modifications).
    /// </summary>
    [JsonPropertyName("app")]
    public AppCatalogEntry? App { get; init; }
}

/// <summary>
/// Request to delete an application entry by slot.
/// </summary>
public sealed record DeleteAppRequest : IpcRequest
{
    /// <summary>
    /// Gets or sets the slot identifier to delete.
    /// </summary>
    [JsonPropertyName("slot")]
    public required string Slot { get; init; }
}

/// <summary>
/// Response after deleting an application entry.
/// </summary>
public sealed record DeleteAppResponse : IpcResponse;
