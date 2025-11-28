namespace CPCRemote.Core.Models;

public record WolOptions
{
    public string MacAddress { get; init; } = string.Empty;
    public string BroadcastAddress { get; init; } = "255.255.255.255";
    /// <summary>
    /// Default Wake-on-LAN port. Standard WoL uses port 9.
    /// </summary>
    public int Port { get; init; } = 9;
}
