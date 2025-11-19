using System.ComponentModel.DataAnnotations;

namespace CPCRemote.Service.Options
{
    /// <summary>
    /// Legacy placeholder attribute maintained for serialization compatibility. It performs no validation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class CertificatePathRequiredAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            return ValidationResult.Success;
        }
    }
}
