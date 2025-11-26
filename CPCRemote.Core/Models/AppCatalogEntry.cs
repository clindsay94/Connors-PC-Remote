namespace CPCRemote.Core.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a single application entry in the app catalog.
/// </summary>
public sealed class AppCatalogEntry
{
    /// <summary>
    /// The slot identifier (App1-App10) that maps to SmartThings capability.
    /// </summary>
    [JsonPropertyName("slot")]
    public required string Slot { get; set; }

    /// <summary>
    /// Display name shown in logs and UI.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Full path to the executable or file to launch.
    /// </summary>
    [JsonPropertyName("path")]
    public required string Path { get; set; }

    /// <summary>
    /// Optional command-line arguments to pass when launching.
    /// </summary>
    [JsonPropertyName("arguments")]
    public string? Arguments { get; set; }

    /// <summary>
    /// Optional working directory for the process.
    /// </summary>
    [JsonPropertyName("workingDirectory")]
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Optional category for organization (e.g., "Games", "Productivity", "Media").
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>
    /// Whether to run the application as administrator (elevated).
    /// </summary>
    [JsonPropertyName("runAsAdmin")]
    public bool RunAsAdmin { get; set; }

    /// <summary>
    /// Whether this slot is enabled. Disabled slots won't launch.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
}
