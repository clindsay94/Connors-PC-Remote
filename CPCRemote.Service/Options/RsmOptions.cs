using System.ComponentModel.DataAnnotations;

namespace CPCRemote.Service.Options
{
    /// <summary>
    /// Represents configuration options for the Remote Shutdown Manager (RSM) service.
    /// </summary>
    [CertificatePathRequired]
    public sealed class RsmOptions
    {
        /// <summary>
        /// Gets or sets the IP address to bind the RSM service to.
        /// Must be configured in appsettings.json. Valid values: IP address, "localhost", "+", or "*"
        /// </summary>
        [Required(AllowEmptyStrings = false, ErrorMessage = "IP Address cannot be null or empty. Use a valid IP address.")]
        public string? IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the port number for the RSM service.
        /// Must be between 1 and 65535.
        /// </summary>
        [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
        public int Port { get; set; } = 5005;

        /// <summary>
        /// Gets or sets the secret used for authenticating RSM requests.
        /// If empty, no authentication is required (NOT RECOMMENDED for production).
        /// Minimum length: 8 characters.
        /// </summary>
        [MinLength(8, ErrorMessage = "Secret must be at least 8 characters for security.")]
        public string? Secret { get; set; }
    }
}