namespace CPCRemote.UI.Models
{
    public class ServiceConfiguration
    {
        public RsmOptions Rsm { get; set; } = new();
    }

    public class RsmOptions
    {
        public string? IpAddress { get; set; }
        public int Port { get; set; }
        public string? Secret { get; set; }
    }
}
