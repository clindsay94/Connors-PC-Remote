namespace CPCRemote.UI.Models
{
    public class ServiceConfiguration
    {
        public RsmOptions Rsm { get; set; }
    }

    public class RsmOptions
    {
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public string Secret { get; set; }
        public bool UseHttps { get; set; }
        public string CertificatePath { get; set; }
        public string CertificatePassword { get; set; }
    }
}
