namespace CPCRemote.Core.Models;

public record WolOptions
{
    public string MacAddress { get; init; } = string.Empty;
    public string BroadcastAddress { get; init; } = "255.255.255.255";
    public int Port { get; init; } = 7;
}
