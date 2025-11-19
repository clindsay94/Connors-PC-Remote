namespace CPCRemote.Service.Options
{
    using System.Net;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Validator for Remote Shutdown Manager (RSM) configuration options
    /// </summary>
    public sealed class RsmOptionsValidator : IValidateOptions<RsmOptions>
    {
        /// <summary>
        /// Validates the RSM configuration options
        /// </summary>
        /// <param name="name">The name of the options instance being validated</param>
        /// <param name="options">The options instance to validate</param>
        /// <returns>Validation result indicating success or failure with error message</returns>
        public ValidateOptionsResult Validate(string? name, RsmOptions options)
        {
            if (options == null)
            {
                return ValidateOptionsResult.Fail("RsmOptions cannot be null.");
            }

            // Validate IP address format if provided
            if (!string.IsNullOrWhiteSpace(options.IpAddress))
            {
                // Try parsing as IP address
                if (!IPAddress.TryParse(options.IpAddress, out _))
                {
                    // If not a valid IP, check if it's "localhost" or a valid hostname pattern
                    if (!string.Equals(options.IpAddress, "localhost", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(options.IpAddress, "+", StringComparison.Ordinal) &&
                        !string.Equals(options.IpAddress, "*", StringComparison.Ordinal))
                    {
                        // Basic hostname validation (simplified)
                        if (!IsValidHostname(options.IpAddress))
                        {
                            return ValidateOptionsResult.Fail($"Invalid IP address or hostname format: {options.IpAddress}");
                        }
                    }
                }
            }

            return ValidateOptionsResult.Success;
        }

        /// <summary>
        /// Validates if a string is a valid hostname
        /// </summary>
        private static bool IsValidHostname(string hostname)
        {
            if (string.IsNullOrWhiteSpace(hostname))
                return false;

            if (hostname.Length > 253)
                return false;

            // Simple validation: alphanumeric, hyphens, dots
            // Does not allow consecutive dots or leading/trailing dots
            string[] labels = hostname.Split('.');
            foreach (string label in labels)
            {
                if (string.IsNullOrWhiteSpace(label) || label.Length > 63)
                    return false;

                if (label.StartsWith('-') || label.EndsWith('-'))
                    return false;

                if (!label.All(c => char.IsLetterOrDigit(c) || c == '-'))
                    return false;
            }

            return true;
        }
    }
}
