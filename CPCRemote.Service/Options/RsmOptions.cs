namespace CPCRemote.Service.Options
{
    /// <summary>
    /// Represents configuration options for the Remote Shutdown Manager (RSM) service.
    /// </summary>
    public sealed class RsmOptions
    {
        /// <summary>
        /// Gets or sets the IP address to bind the RSM service to.
        /// Must be configured in appsettings.json. Valid values: IP address, "localhost", "+", or "*"
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the port number for the RSM service.
        /// Must be between 1 and 65535.
        /// </summary>
        public int Port { get; set; } = 5005;

        /// <summary>
        /// Gets or sets the HTTPS port number to listen on when HTTPS is enabled.
        /// Must be between 1 and 65535.
        /// </summary>
        public int HttpsPort { get; set; } = 5006;

        /// <summary>
        /// Gets or sets whether to use HTTPS instead of HTTP.
        /// </summary>
        public bool UseHttps { get; set; } = false;

        /// <summary>
        /// Gets or sets the path to the SSL certificate file (.pfx).
        /// Required when UseHttps is true.
        /// </summary>
        public string? CertificatePath { get; set; }

        /// <summary>
        /// Gets or sets the password for the SSL certificate.
        /// Required when UseHttps is true and certificate is password-protected.
        /// </summary>
        public string? CertificatePassword { get; set; }

        /// <summary>
        /// Gets or sets the secret used for authenticating RSM requests.
        /// If empty, no authentication is required (NOT RECOMMENDED for production).
        /// Minimum length: 8 characters.
        /// </summary>
        public string? Secret { get; set; }
    }
}