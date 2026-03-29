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
        ValidateMapping(errors, "CpuLoad", options.CpuLoad);
        ValidateMapping(errors, "MemoryLoad", options.MemoryLoad);
        ValidateMapping(errors, "CpuTemp", options.CpuTemp);
        ValidateMapping(errors, "GpuTemp", options.GpuTemp);

        // Validate custom sensors have required properties
        if (options.CustomSensors == null)
        {
            errors.Add("CustomSensors collection cannot be null.");
        }
        else
        {
            for (int i = 0; i < options.CustomSensors.Count; i++)
            {
                var sensor = options.CustomSensors[i];

                if (sensor == null)
                {
                    errors.Add($"CustomSensors[{i}] cannot be null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(sensor.Name))
                {
                    errors.Add($"CustomSensors[{i}].Name is required.");
                }

                if (string.IsNullOrWhiteSpace(sensor.Label))
                {
                    errors.Add($"CustomSensors[{i}].Label is required.");
                }
            }
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateMapping(List<string> errors, string propertyName, SensorMappingOptions? mapping)
    {
        if (mapping == null)
        {
            errors.Add($"{propertyName} configuration is required.");
            return;
        }

        if (mapping.Patterns == null)
        {
            errors.Add($"{propertyName}.Patterns cannot be null.");
            return;
        }

        if (mapping.Patterns.Length == 0)
        {
            errors.Add($"{propertyName}.Patterns must contain at least one pattern.");
        }
    }
}
