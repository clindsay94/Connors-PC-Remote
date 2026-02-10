namespace CPCRemote.Service.Services;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

/// <summary>
/// Reads sensors selected for HWInfo's gadget from the Windows Registry.
/// HWInfo writes gadget sensor data to: HKCU\SOFTWARE\HWiNFO64\VSB
/// Format: Sensor0, Label0, Value0, ValueRaw0, Color0, Sensor1, Label1, etc.
/// </summary>
/// <remarks>
/// When running as a Windows Service (Session 0/SYSTEM), this class must read
/// from the interactive user's registry hive (HKU\{SID}\...) rather than HKCU.
/// </remarks>
public sealed class HWInfoRegistryReader
{
    private const string VsbRegistrySubPath = @"SOFTWARE\HWiNFO64\VSB";
    private readonly ILogger<HWInfoRegistryReader> _logger;

    public HWInfoRegistryReader(ILogger<HWInfoRegistryReader> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Reads all gadget-selected sensors from HWInfo's VSB registry.
    /// </summary>
    /// <returns>List of sensors configured in the HWInfo gadget, or empty if unavailable.</returns>
    public List<GadgetSensor> ReadGadgetSensors()
    {
        var sensors = new List<GadgetSensor>();

        try
        {
            using var key = OpenUserRegistryKey();
            if (key == null)
            {
                _logger.LogWarning("HWInfo VSB registry key not found. Ensure HWInfo is running with 'Show Sensors in Gadget' enabled.");
                return sensors;
            }

            // Read indexed entries: Sensor0, Label0, Value0, ValueRaw0, Color0, etc.
            for (int i = 0; i < 256; i++) // Support up to 256 sensors
            {
                var sensorName = key.GetValue($"Sensor{i}") as string;
                var label = key.GetValue($"Label{i}") as string;

                if (string.IsNullOrEmpty(sensorName) || string.IsNullOrEmpty(label))
                {
                    // No more sensors at this index
                    break;
                }

                var valueStr = key.GetValue($"Value{i}") as string;
                var valueRawStr = key.GetValue($"ValueRaw{i}") as string;
                var color = key.GetValue($"Color{i}") as string;

                float valueRaw = 0f;
                if (!string.IsNullOrEmpty(valueRawStr) &&
                    float.TryParse(valueRawStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                {
                    valueRaw = parsed;
                }

                // Extract unit from formatted value (e.g., "36.6 °C" → "°C")
                string unit = ExtractUnit(valueStr);

                // Auto-detect category from sensor name
                string category = DetectCategory(sensorName, label);

                sensors.Add(new GadgetSensor
                {
                    Index = i,
                    SensorName = sensorName,
                    Label = label,
                    Value = valueStr ?? string.Empty,
                    ValueRaw = valueRaw,
                    Unit = unit,
                    Color = color ?? string.Empty,
                    Category = category
                });
            }

            if (sensors.Count > 0)
            {
                _logger.LogInformation("Read {Count} sensors from HWInfo VSB registry.", sensors.Count);
            }
            else
            {
                _logger.LogWarning("No sensors found in HWInfo VSB registry. Add sensors to the HWInfo gadget.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading HWInfo VSB registry.");
        }

        return sensors;
    }

    /// <summary>
    /// Opens the VSB registry key, handling Session 0 isolation.
    /// When running as SYSTEM, reads from the interactive user's registry hive.
    /// </summary>
    private RegistryKey? OpenUserRegistryKey()
    {
        _logger.LogInformation("OpenUserRegistryKey: Starting registry key search...");
        
        // First, try the current user (works when running interactively)
        try
        {
            var key = Registry.CurrentUser.OpenSubKey(VsbRegistrySubPath, writable: false);
            if (key != null)
            {
                _logger.LogInformation("Opened VSB key from Registry.CurrentUser successfully!");
                return key;
            }
            _logger.LogInformation("Registry.CurrentUser path '{Path}' returned null (key doesn't exist)", VsbRegistrySubPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to open Registry.CurrentUser path");
        }

        // If that fails, try to find an interactive user's registry hive
        // by enumerating HKU (HKEY_USERS) for user SIDs
        _logger.LogInformation("Searching HKU (HKEY_USERS) for interactive user registry hives...");
        
        try
        {
            var subKeyNames = Registry.Users.GetSubKeyNames();
            _logger.LogInformation("Found {Count} subkeys in HKU: {Keys}", subKeyNames.Length, string.Join(", ", subKeyNames));
            
            foreach (var sidString in subKeyNames)
            {
                // Skip well-known system SIDs and _Classes suffixes
                if (sidString.StartsWith("S-1-5-18") ||  // SYSTEM
                    sidString.StartsWith("S-1-5-19") ||  // LOCAL SERVICE
                    sidString.StartsWith("S-1-5-20") ||  // NETWORK SERVICE
                    sidString.EndsWith("_Classes") ||
                    sidString == ".DEFAULT")
                {
                    _logger.LogDebug("Skipping system/special SID: {Sid}", sidString);
                    continue;
                }

                // Try to open the VSB key under this user's hive
                string userVsbPath = $@"{sidString}\{VsbRegistrySubPath}";
                _logger.LogInformation("Trying to open HKU path: {Path}", userVsbPath);
                
                try
                {
                    var key = Registry.Users.OpenSubKey(userVsbPath, writable: false);
                    
                    if (key != null)
                    {
                        _logger.LogInformation("SUCCESS! Found VSB key in HKU under SID: {Sid}", sidString);
                        return key;
                    }
                    else
                    {
                        _logger.LogInformation("Path exists but VSB key not found for SID: {Sid}", sidString);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to open registry path for SID {Sid}", sidString);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enumerating HKU subkeys");
        }

        _logger.LogWarning("Could not find HWInfo VSB registry key in any user hive");
        return null;
    }

    /// <summary>
    /// Extracts the unit from a formatted value string.
    /// </summary>
    private static string ExtractUnit(string? formattedValue)
    {
        if (string.IsNullOrEmpty(formattedValue))
            return string.Empty;

        // Find the last space and take everything after it
        int lastSpace = formattedValue.LastIndexOf(' ');
        if (lastSpace >= 0 && lastSpace < formattedValue.Length - 1)
        {
            return formattedValue[(lastSpace + 1)..];
        }

        return string.Empty;
    }

    /// <summary>
    /// Auto-detects the sensor category from its name and label.
    /// </summary>
    private static string DetectCategory(string sensorName, string label)
    {
        string combined = $"{sensorName} {label}".ToUpperInvariant();

        if (combined.Contains("CPU") || combined.Contains("PROCESSOR"))
            return "CPU";
        if (combined.Contains("GPU") || combined.Contains("GRAPHICS") || combined.Contains("NVIDIA") || combined.Contains("RADEON"))
            return "GPU";
        if (combined.Contains("MEMORY") || combined.Contains("RAM") || combined.Contains("DIMM"))
            return "Memory";
        if (combined.Contains("MOTHERBOARD") || combined.Contains("MAINBOARD") || combined.Contains("VCORE") || combined.Contains("VSOC"))
            return "Motherboard";
        if (combined.Contains("DISK") || combined.Contains("SSD") || combined.Contains("HDD") || combined.Contains("NVME") || combined.Contains("DRIVE"))
            return "Storage";
        if (combined.Contains("FAN"))
            return "Cooling";
        if (combined.Contains("NETWORK") || combined.Contains("ETHERNET") || combined.Contains("WIFI"))
            return "Network";

        return "Other";
    }
}

/// <summary>
/// Represents a sensor from HWInfo's gadget registry.
/// </summary>
public sealed record GadgetSensor
{
    /// <summary>
    /// Position in the gadget (0-indexed).
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// Full sensor name from HWInfo (e.g., "CPU [#0]: AMD Ryzen 5 5600G").
    /// </summary>
    public required string SensorName { get; init; }

    /// <summary>
    /// Sensor label (e.g., "CPU (Tctl/Tdie)").
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Formatted value with unit (e.g., "36.6 °C").
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Raw numeric value.
    /// </summary>
    public required float ValueRaw { get; init; }

    /// <summary>
    /// Extracted unit (e.g., "°C", "%", "MHz").
    /// </summary>
    public required string Unit { get; init; }

    /// <summary>
    /// HWInfo color code for the sensor.
    /// </summary>
    public required string Color { get; init; }

    /// <summary>
    /// Auto-detected category (CPU, GPU, Memory, Motherboard, Storage, Cooling, Network, Other).
    /// </summary>
    public required string Category { get; init; }
}
