using System.ComponentModel.DataAnnotations;

namespace CPCRemote.Service.Options
{
    /// <summary>
    /// Validation attribute to ensure a certificate path is provided when HTTPS is enabled.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class CertificatePathRequiredAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is RsmOptions options)
            {
                if (options.UseHttps && string.IsNullOrWhiteSpace(options.CertificatePath))
                {
                    return new ValidationResult("CertificatePath is required when UseHttps is true.", new[] { nameof(RsmOptions.CertificatePath) });
                }
            }

            return ValidationResult.Success;
        }
    }
}
