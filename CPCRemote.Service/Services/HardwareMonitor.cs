namespace CPCRemote.Service.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using CPCRemote.Core.IPC;
using CPCRemote.Service.Options;
using Hwinfo.SharedMemory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Hardware monitoring service that reads from HWiNFO's shared memory interface.
/// Requires HWiNFO64 to be running with "Shared Memory Support" enabled in settings.
/// Uses the Hwinfo.SharedMemory.Net library for reliable shared memory access.
/// </summary>
public sealed class HardwareMonitor : IDisposable
{
    private readonly ILogger<HardwareMonitor> _logger;
    private readonly IOptionsMonitor<SensorOptions> _sensorOptionsMonitor;
    private SharedMemoryReader? _reader;
    private readonly Lock _lock = new();
    private bool _hwInfoAvailable;
    private bool _availabilityChecked;
    private bool _readerInitFailed;

    public HardwareMonitor(ILogger<HardwareMonitor> logger, IOptionsMonitor<SensorOptions> sensorOptionsMonitor)
    {
        _logger = logger;
        _sensorOptionsMonitor = sensorOptionsMonitor;
        _logger.LogInformation("HardwareMonitor initialized. Will read from HWiNFO shared memory via library.");
    }

    /// <summary>
    /// Gets or creates the SharedMemoryReader, handling initialization failures gracefully.
    /// </summary>
    private SharedMemoryReader? GetReader()
    {
        if (_reader is not null)
            return _reader;

        if (_readerInitFailed)
            return null;

        try
        {
            _reader = new SharedMemoryReader();
            return _reader;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize HWiNFO SharedMemoryReader. HWiNFO may not be running or shared memory support is disabled.");
            _readerInitFailed = true;
            return null;
        }
    }

    /// <summary>
    /// Resets the reader initialization state to allow retry.
    /// Called periodically to check if HWiNFO has become available.
    /// </summary>
    private void ResetReaderIfNeeded()
    {
        // Reset the failed flag periodically to allow retry
        if (_readerInitFailed && _reader is null)
        {
            _readerInitFailed = false;
        }
    }

    /// <summary>
    /// Gets comprehensive PC statistics organized by component.
    /// </summary>
    public GetStatsResponse GetStats()
    {
        lock (_lock)
        {
            try
            {
                // Reset reader state to allow retry if HWiNFO wasn't available before
                ResetReaderIfNeeded();

                var reader = GetReader();
                if (reader is null)
                {
                    if (!_availabilityChecked)
                    {
                        _logger.LogWarning("HWiNFO shared memory not available. Ensure HWiNFO is running with 'Shared Memory Support' enabled in Settings > General.");
                        _availabilityChecked = true;
                    }
                    return new GetStatsResponse { Success = false, ErrorMessage = "HWiNFO not running" };
                }

                var readings = reader.ReadLocal().ToList();

                if (!_hwInfoAvailable)
                {
                    _hwInfoAvailable = true;
                    _availabilityChecked = true;
                    _logger.LogInformation("HWiNFO shared memory connected. Found {Count} readings.", readings.Count);
                    LogAvailableSensors(readings);
                }

                // Build response from readings
                return BuildStatsResponse(readings);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not running") || ex.Message.Contains("shared memory"))
            {
                if (_hwInfoAvailable || !_availabilityChecked)
                {
                    _logger.LogWarning("HWiNFO shared memory not available. Ensure HWiNFO is running with 'Shared Memory Support' enabled in Settings > General.");
                    _hwInfoAvailable = false;
                    _availabilityChecked = true;
                }
                // Reset reader so we try again next time
                DisposeReader();
                return new GetStatsResponse { Success = false, ErrorMessage = "HWiNFO not running" };
            }
            catch (System.IO.FileNotFoundException)
            {
                if (_hwInfoAvailable || !_availabilityChecked)
                {
                    _logger.LogWarning("HWiNFO shared memory not found. Ensure HWiNFO is running with 'Shared Memory Support' enabled.");
                    _hwInfoAvailable = false;
                    _availabilityChecked = true;
                }
                DisposeReader();
                return new GetStatsResponse { Success = false, ErrorMessage = "HWiNFO not running" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading HWiNFO shared memory.");
                DisposeReader();
                return new GetStatsResponse { Success = false, ErrorMessage = ex.Message };
            }
        }
    }

    private void DisposeReader()
    {
        _reader?.Dispose();
        _reader = null;
        _readerInitFailed = false;
    }

    private GetStatsResponse BuildStatsResponse(List<SensorReading> readings)
    {
        var cpu = BuildCpuStats(readings);
        var memory = BuildMemoryStats(readings);
        var gpu = BuildGpuStats(readings);
        var motherboard = BuildMotherboardStats(readings);

        return new GetStatsResponse
        {
            Success = true,
            Cpu = cpu,
            Memory = memory,
            Gpu = gpu,
            Motherboard = motherboard
        };
    }

    private CpuStats BuildCpuStats(List<SensorReading> readings)
    {
        float? utility = null;
        float? temperature = null;
        float? dieAvgTemp = null;
        float? hotspot = null;
        float? packagePower = null;
        float? ppt = null;
        float? coreClock = null;
        float? effectiveClock = null;
        var coreEffectiveClocks = new float?[8]; // Core 0-7
        var coreBaseClocks = new List<float>(); // For calculating average Core Clocks

        foreach (var r in readings)
        {
            string label = r.LabelUser ?? r.LabelOrig ?? string.Empty;
            string labelLower = label.ToLowerInvariant();
            string origLabel = r.LabelOrig ?? string.Empty;
            string origLabelLower = origLabel.ToLowerInvariant();
            string unit = r.Unit ?? string.Empty;
            string unitLower = unit.ToLowerInvariant();
            double value = r.Value;
            bool isTemp = r.Type == SensorType.SensorTypeTemp;

            // Total CPU Utility (exact match from screenshot)
            if (utility is null && unitLower == "%" && 
                (labelLower == "total cpu utility" || labelLower.Contains("total cpu utility")))
            {
                utility = Round(value);
            }

            // CPU Control (exact match from screenshot - renamed sensor)
            // Also check original label "CPU (Tctl/Tdie)" which is what HWiNFO uses by default
            if (temperature is null && isTemp && value > 0 && value < 150)
            {
                if (label == "CPU Control" || labelLower == "cpu control")
                {
                    temperature = Round(value);
                }
                // Fallback: match by original label (Tctl/Tdie)
                else if (origLabelLower == "cpu (tctl/tdie)" || origLabelLower.Contains("tctl/tdie"))
                {
                    temperature ??= Round(value);
                }
            }

            // CPU Die (exact match from screenshot - renamed sensor)
            // Also check original label "CPU Die (average)"
            if (dieAvgTemp is null && isTemp && value > 0 && value < 150)
            {
                if (label == "CPU Die" || labelLower == "cpu die")
                {
                    dieAvgTemp = Round(value);
                }
                // Fallback: match by original label
                else if (origLabelLower == "cpu die (average)" || origLabelLower.Contains("die (average)"))
                {
                    dieAvgTemp ??= Round(value);
                }
            }

            // CPU Hotspot (exact match from screenshot - renamed sensor)
            // Also check original label "IOD Hotspot"
            if (hotspot is null && isTemp && value > 0 && value < 150)
            {
                if (label == "CPU Hotspot" || labelLower == "cpu hotspot")
                {
                    hotspot = Round(value);
                }
                // Fallback: match by original label (IOD Hotspot)
                else if (origLabelLower.Contains("iod hotspot") || origLabelLower.Contains("hotspot"))
                {
                    hotspot ??= Round(value);
                }
            }

            // CPU Package Power (exact match from screenshot)
            if (packagePower is null && unitLower == "w" &&
                (label == "CPU Package Power" || labelLower == "cpu package power"))
            {
                packagePower = Round(value);
            }

            // CPU PPT (exact match from screenshot)
            if (ppt is null && unitLower == "w" &&
                (label == "CPU PPT" || labelLower == "cpu ppt"))
            {
                ppt = Round(value);
            }

            // Core Clocks - HWiNFO doesn't have an aggregate sensor, so we look for:
            // 1. User-renamed "Core Clocks" sensor (if they created one)
            // 2. Individual "Core X Clock (perf #Y)" sensors to calculate average
            if (unitLower == "mhz")
            {
                // Direct match for user-renamed sensor
                if (coreClock is null && (label == "Core Clocks" || labelLower == "core clocks"))
                {
                    coreClock = Round(value);
                }
                // Collect individual core clocks: "Core 0 Clock (perf #3)", etc.
                else if (origLabelLower.StartsWith("core ") && origLabelLower.Contains(" clock (perf"))
                {
                    coreBaseClocks.Add((float)value);
                }
            }

            // Avg Effective Clock (exact match from screenshot)
            if (effectiveClock is null && unitLower == "mhz")
            {
                if (label == "Avg Effective Clock" || labelLower == "avg effective clock")
                {
                    effectiveClock = Round(value);
                }
                // Fallback to Average Effective Clock: if Avg Effective Clock not found
                else if (labelLower.StartsWith("average effective clock") || origLabelLower.StartsWith("average effective clock"))
                {
                    effectiveClock ??= Round(value);
                }
            }

            // Per-core effective clocks (Core 0-7 Effective Clock from screenshot)
            for (int core = 0; core < 8; core++)
            {
                if (coreEffectiveClocks[core] is null && unitLower == "mhz")
                {
                    // Match exact pattern from screenshot: "Core 0 Effective Clock", etc.
                    string exactPattern = $"core {core} effective clock";
                    string t0Pattern = $"core {core} t0 effective clock";
                    if (labelLower == exactPattern || labelLower.Contains(t0Pattern) ||
                        origLabelLower == exactPattern || origLabelLower.Contains(t0Pattern))
                    {
                        coreEffectiveClocks[core] = Round(value);
                    }
                }
            }
        }

        // Calculate average Core Clocks from individual core clocks if not found directly
        if (coreClock is null && coreBaseClocks.Count > 0)
        {
            coreClock = Round(coreBaseClocks.Average());
        }

        // Check if we have any per-core data
        var effectiveArray = coreEffectiveClocks.Any(c => c.HasValue) 
            ? coreEffectiveClocks.Select(c => c ?? 0f).ToArray() 
            : null;

        return new CpuStats
        {
            Utility = utility,
            Temperature = temperature,
            DieAvgTemp = dieAvgTemp,
            IodHotspot = hotspot,
            PackagePower = packagePower,
            Ppt = ppt,
            CoreClock = coreClock,
            EffectiveClock = effectiveClock,
            CoreEffectiveClocks = effectiveArray
        };
    }

    private MemoryStats BuildMemoryStats(List<SensorReading> readings)
    {
        float? load = null;
        var dimmTemps = new List<DimmTemp>();

        foreach (var r in readings)
        {
            string label = r.LabelUser ?? r.LabelOrig ?? string.Empty;
            string labelLower = label.ToLowerInvariant();
            string origLabel = r.LabelOrig ?? string.Empty;
            string origLabelLower = origLabel.ToLowerInvariant();
            string unitLower = (r.Unit ?? string.Empty).ToLowerInvariant();
            double value = r.Value;
            bool isTemp = r.Type == SensorType.SensorTypeTemp;

            // Memory Load
            if (load is null && unitLower == "%" &&
                (labelLower.Contains("physical memory load") || labelLower.Contains("memory usage")))
            {
                load = Round(value);
            }

            // DIMM Temperatures - Match exact names from screenshot: "DIMM 1 Temp" and "DIMM 2 Temp"
            // Also fallback to "SPD Hub Temperature" via original label if custom names not found
            if (isTemp && value > 0 && value < 100)
            {
                // Try exact match for user-renamed sensors first
                if (label == "DIMM 1 Temp" || labelLower == "dimm 1 temp")
                {
                    dimmTemps.Add(new DimmTemp { Slot = 1, Temp = Round(value) });
                }
                else if (label == "DIMM 2 Temp" || labelLower == "dimm 2 temp")
                {
                    dimmTemps.Add(new DimmTemp { Slot = 2, Temp = Round(value) });
                }
                // Fallback: check original label for SPD Hub Temperature
                else if ((origLabelLower == "spd hub temperature" || origLabelLower.Contains("spd hub")) && dimmTemps.Count < 2)
                {
                    // Assign slot numbers sequentially (1, 2) for dual channel
                    int nextSlot = dimmTemps.Count == 0 ? 1 : 2;
                    dimmTemps.Add(new DimmTemp { Slot = nextSlot, Temp = Round(value) });
                }
            }
        }

        return new MemoryStats
        {
            Load = load,
            DimmTemps = dimmTemps.Count > 0 ? dimmTemps.OrderBy(d => d.Slot).ToArray() : null
        };
    }

    private GpuStats BuildGpuStats(List<SensorReading> readings)
    {
        float? temperature = null;
        float? memTemp = null;
        float? power = null;
        float? clock = null;
        float? effectiveClock = null;
        float? memoryUsage = null;
        float? coreLoad = null;

        foreach (var r in readings)
        {
            string label = r.LabelUser ?? r.LabelOrig ?? string.Empty;
            string labelLower = label.ToLowerInvariant();
            string origLabel = r.LabelOrig ?? string.Empty;
            string origLabelLower = origLabel.ToLowerInvariant();
            string unitLower = (r.Unit ?? string.Empty).ToLowerInvariant();
            double value = r.Value;
            bool isTemp = r.Type == SensorType.SensorTypeTemp;

            // GPU Temp (exact match from screenshot - renamed sensor)
            // Also check original label "GPU Temperature"
            if (temperature is null && isTemp && value > 0 && value < 150)
            {
                if (label == "GPU Temp" || labelLower == "gpu temp")
                {
                    temperature = Round(value);
                }
                // Fallback: match by original label
                else if (origLabelLower == "gpu temperature" || origLabelLower.Contains("gpu temp"))
                {
                    temperature ??= Round(value);
                }
            }

            // GPU Mem Temp (exact match from screenshot - renamed sensor)
            // Also check original label "GPU Memory Junction Temperature"
            if (memTemp is null && isTemp && value > 0 && value < 150)
            {
                if (label == "GPU Mem Temp" || labelLower == "gpu mem temp")
                {
                    memTemp = Round(value);
                }
                // Fallback: match by original label
                else if (origLabelLower.Contains("memory junction") || origLabelLower.Contains("gpu memory junction"))
                {
                    memTemp ??= Round(value);
                }
            }

            // GPU Power (exact match from screenshot)
            if (power is null && unitLower == "w" &&
                (label == "GPU Power" || labelLower == "gpu power"))
            {
                power = Round(value);
            }

            // GPU Clock (exact match from screenshot - renamed sensor)
            if (clock is null && unitLower == "mhz")
            {
                if (label == "GPU Clock" || labelLower == "gpu clock")
                {
                    clock = Round(value);
                }
            }

            // GPU Effective Clock (exact match from screenshot - renamed sensor)
            if (effectiveClock is null && unitLower == "mhz")
            {
                if (label == "GPU Effective Clock" || labelLower == "gpu effective clock")
                {
                    effectiveClock = Round(value);
                }
            }

            // GPU Memory Usage (exact match from screenshot)
            if (memoryUsage is null && unitLower == "%" &&
                (label == "GPU Memory Usage" || labelLower == "gpu memory usage" || labelLower == "gpu memory allocated"))
            {
                memoryUsage = Round(value);
            }

            // GPU Core Load (exact match from screenshot)
            if (coreLoad is null && unitLower == "%" &&
                (label == "GPU Core Load" || labelLower == "gpu core load" || labelLower.Contains("gpu utilization")))
            {
                coreLoad = Round(value);
            }
        }

        return new GpuStats
        {
            Temperature = temperature,
            MemJunctionTemp = memTemp,
            Power = power,
            Clock = clock,
            EffectiveClock = effectiveClock,
            MemoryUsage = memoryUsage,
            CoreLoad = coreLoad
        };
    }

    private MotherboardStats BuildMotherboardStats(List<SensorReading> readings)
    {
        float? vcore = null;
        float? vsoc = null;

        foreach (var r in readings)
        {
            string label = r.LabelUser ?? r.LabelOrig ?? string.Empty;
            string labelLower = label.ToLowerInvariant();
            string origLabelLower = (r.LabelOrig ?? string.Empty).ToLowerInvariant();
            string unitLower = (r.Unit ?? string.Empty).ToLowerInvariant();
            double value = r.Value;

            // Vcore MB (exact match from screenshot - renamed sensor)
            if (vcore is null && unitLower == "v")
            {
                if (label == "Vcore MB" || labelLower == "vcore mb")
                {
                    vcore = (float)Math.Round(value, 3);
                }
                // Fallback to Vcore if Vcore MB not found
                else if (origLabelLower == "vcore" || origLabelLower.Contains("cpu core voltage"))
                {
                    vcore ??= (float)Math.Round(value, 3);
                }
            }

            // VDDCR_SOC MB (exact match from screenshot - renamed sensor)
            if (vsoc is null && unitLower == "v")
            {
                if (label == "VDDCR_SOC MB" || labelLower == "vddcr_soc mb")
                {
                    vsoc = (float)Math.Round(value, 3);
                }
                // Fallback to VDDCR_SOC if VDDCR_SOC MB not found
                else if (origLabelLower.Contains("vddcr_soc") || origLabelLower == "vsoc")
                {
                    vsoc ??= (float)Math.Round(value, 3);
                }
            }
        }

        return new MotherboardStats
        {
            Vcore = vcore,
            Vsoc = vsoc
        };
    }

    private static float Round(double value) => (float)Math.Round(value, 1);

    public void Dispose()
    {
        _reader?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Logs available sensors that match our search criteria for debugging.
    /// </summary>
    private void LogAvailableSensors(List<SensorReading> readings)
    {
        try
        {
            var tempSensors = new List<string>();
            var cpuSensors = new List<string>();
            var gpuSensors = new List<string>();
            var clockSensors = new List<string>();

            foreach (var r in readings)
            {
                string label = r.LabelUser ?? r.LabelOrig ?? string.Empty;
                string labelLower = label.ToLowerInvariant();
                string origLabel = r.LabelOrig ?? string.Empty;
                string origLabelLower = origLabel.ToLowerInvariant();
                string unit = r.Unit ?? string.Empty;
                string unitLower = unit.ToLowerInvariant();
                double value = r.Value;
                bool isTemp = r.Type == SensorType.SensorTypeTemp;

                // Show renamed sensors
                string displayLabel = !string.IsNullOrWhiteSpace(r.LabelUser) && r.LabelUser != r.LabelOrig
                    ? $"{r.LabelUser} (was: {r.LabelOrig})"
                    : label;

                if (labelLower.Contains("cpu") && (unitLower == "%" || unitLower == "mhz" || unitLower == "w"))
                {
                    cpuSensors.Add($"{displayLabel} ({value:F1}{unit})");
                }
                
                // Log ALL clock sensors (MHz) for debugging Core Clocks issue
                if (unitLower == "mhz" && (labelLower.Contains("clock") || origLabelLower.Contains("clock")))
                {
                    clockSensors.Add($"User='{r.LabelUser}' Orig='{r.LabelOrig}' = {value:F1} {unit}");
                }
                
                // Log ALL temperature type readings
                if (isTemp && value > 0 && value < 150)
                {
                    tempSensors.Add($"[Type=Temp] {displayLabel} = {value:F1} {unit}");
                }
                
                if (labelLower.Contains("gpu"))
                {
                    gpuSensors.Add($"{displayLabel} ({value:F1}{unit})");
                }
            }

            _logger.LogInformation("CPU sensors found: {Count}", cpuSensors.Count);
            _logger.LogInformation("Temperature sensors found: {Count}", tempSensors.Count);
            _logger.LogInformation("GPU sensors found: {Count}", gpuSensors.Count);
            _logger.LogInformation("Clock sensors (MHz) found: {Count}", clockSensors.Count);
            
            // Log clock sensors for debugging
            foreach (var clk in clockSensors.Take(15))
            {
                _logger.LogInformation("  Clock sensor: {Sensor}", clk);
            }
            
            // Log ALL temperature sensors for debugging
            foreach (var temp in tempSensors.Take(20))
            {
                _logger.LogInformation("  Temp sensor: {Sensor}", temp);
            }
            if (tempSensors.Count > 20)
            {
                _logger.LogInformation("  ... and {Count} more temperature sensors", tempSensors.Count - 20);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enumerate available sensors.");
        }
    }
}
