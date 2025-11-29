namespace CPCRemote.Core.Models;

/// <summary>
/// Configuration options for Wake-on-LAN (WoL) functionality.
/// </summary>
/// <remarks>
/// <para>
/// Wake-on-LAN sends a "magic packet" to wake a sleeping or powered-off PC.
/// The magic packet contains the target MAC address repeated 16 times.
/// </para>
/// <para>
/// Example configuration in appsettings.json:
/// <code>
/// "wol": {
///   "macAddress": "AA:BB:CC:DD:EE:FF",
///   "broadcastAddress": "192.168.1.255",
///   "port": 9
/// }
/// </code>
/// </para>
/// </remarks>
public record WolOptions
{
    /// <summary>
    /// Gets or sets the MAC address of the target PC to wake.
    /// </summary>
    /// <remarks>
    /// Accepts formats: "AA:BB:CC:DD:EE:FF" or "AA-BB-CC-DD-EE-FF" (case-insensitive).
    /// Must not be empty or all zeros ("00:00:00:00:00:00").
    /// </remarks>
    /// <example>"00:11:22:33:44:55"</example>
    public string MacAddress { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the broadcast address to send the WoL packet to.
    /// </summary>
    /// <remarks>
    /// Defaults to "255.255.255.255" (global broadcast). For better reliability,
    /// use the subnet broadcast address (e.g., "192.168.1.255" for a /24 network).
    /// </remarks>
    public string BroadcastAddress { get; init; } = "255.255.255.255";

    /// <summary>
    /// Gets or sets the UDP port for the Wake-on-LAN packet.
    /// </summary>
    /// <remarks>
    /// Standard WoL uses port 9. Port 7 (echo) is also commonly used.
    /// Valid range: 1-65535.
    /// </remarks>
    public int Port { get; init; } = 9;
}
