namespace CPCRemote.Service.Options;

using Microsoft.Extensions.Options;

/// <summary>
/// Validates <see cref="SensorOptions"/> configuration on application startup.
/// Ensures that sensor patterns are properly configured before the service starts
/// monitoring hardware.
/// </summary>
public sealed class SensorOptionsValidator : IValidateOptions<SensorOptions>
{
    /// <summary>
    /// Validates the <see cref="SensorOptions"/> configuration.
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="options">The options instance to validate.</param>
    /// <returns>A <see cref="ValidateOptionsResult"/> indicating success or failure.</returns>
    public ValidateOptionsResult Validate(string? name, SensorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var errors = new List<string>();

        // Validate required sensor mappings have at least one pattern
        if (options.CpuLoad.Patterns.Length == 0)
        {
            errors.Add("CpuLoad.Patterns must contain at least one pattern.");
        }

        if (options.MemoryLoad.Patterns.Length == 0)
        {
            errors.Add("MemoryLoad.Patterns must contain at least one pattern.");
        }

        if (options.CpuTemp.Patterns.Length == 0)
        {
            errors.Add("CpuTemp.Patterns must contain at least one pattern.");
        }

        if (options.GpuTemp.Patterns.Length == 0)
        {
            errors.Add("GpuTemp.Patterns must contain at least one pattern.");
        }

        // Validate custom sensors have required properties
        for (int i = 0; i < options.CustomSensors.Count; i++)
        {
            var sensor = options.CustomSensors[i];

            if (string.IsNullOrWhiteSpace(sensor.Name))
            {
                errors.Add($"CustomSensors[{i}].Name is required.");
            }

            if (string.IsNullOrWhiteSpace(sensor.Label))
            {
                errors.Add($"CustomSensors[{i}].Label is required.");
            }
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
