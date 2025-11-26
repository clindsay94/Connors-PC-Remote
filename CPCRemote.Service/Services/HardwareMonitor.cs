namespace CPCRemote.Service.Services;

using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

/// <summary>
/// Hardware monitoring service that reads from HWiNFO's shared memory interface.
/// Requires HWiNFO64 to be running with "Shared Memory Support" enabled in settings.
/// This approach works reliably from Session 0 services since it only reads shared memory.
/// </summary>
public sealed class HardwareMonitor : IDisposable
{
    private const string HWiNFO_SHARED_MEM_FILE_NAME = "Global\\HWiNFO_SENS_SM2";
    private const int HWiNFO_SENSORS_STRING_LEN = 128;
    private const int HWiNFO_UNIT_STRING_LEN = 16;
    private const uint HWiNFO_HEADER_MAGIC = 0x53695748; // "HWiS" - HWiNFO Sensors signature

    private readonly ILogger<HardwareMonitor> _logger;
    private readonly Lock _lock = new();
    private bool _hwInfoAvailable;
    private bool _availabilityChecked;

    public HardwareMonitor(ILogger<HardwareMonitor> logger)
    {
        _logger = logger;
        _logger.LogInformation("HardwareMonitor initialized. Will read from HWiNFO shared memory.");
    }

    public record PcStats(
        [property: JsonPropertyName("cpu")] float? Cpu,
        [property: JsonPropertyName("memory")] float? Memory,
        [property: JsonPropertyName("cpuTemp")] float? CpuTemp,
        [property: JsonPropertyName("gpuTemp")] float? GpuTemp
    );

    public PcStats GetStats()
    {
        lock (_lock)
        {
            try
            {
                using var mmf = MemoryMappedFile.OpenExisting(HWiNFO_SHARED_MEM_FILE_NAME, MemoryMappedFileRights.Read);
                using var accessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

                // Read the header
                accessor.Read(0, out HWiNFOSharedMemHeader header);

                if (header.dwSignature != HWiNFO_HEADER_MAGIC) // "HWiS" signature
                {
                    _logger.LogWarning("Invalid HWiNFO shared memory signature. Got 0x{Signature:X8}, expected 0x{Expected:X8}", 
                        header.dwSignature, HWiNFO_HEADER_MAGIC);
                    return new PcStats(null, null, null, null);
                }

                if (!_availabilityChecked)
                {
                    _hwInfoAvailable = true;
                    _availabilityChecked = true;
                    _logger.LogInformation("HWiNFO shared memory connected. Version: {Version}, Sensors: {Count}, Readings: {Readings}",
                        header.dwVersion, header.dwNumSensorElements, header.dwNumReadingElements);
                    
                    // Log available sensors for debugging (only on first connection)
                    LogAvailableSensors(accessor, header);
                }

                float? cpuLoad = null;
                float? memoryLoad = null;
                float? cpuTemp = null;
                float? gpuTemp = null;
                float? firstCpuTemp = null;  // Fallback: first CPU-related temp
                float? firstGpuTemp = null;  // Fallback: first GPU-related temp

                // Iterate through all sensor readings
                long readingOffset = header.dwOffsetOfReadingSection;
                int readingSize = (int)header.dwSizeOfReadingElement;

                for (uint i = 0; i < header.dwNumReadingElements; i++)
                {
                    var reading = ReadReading(accessor, readingOffset + (i * readingSize));

                    string label = reading.szLabelOrig.ToLowerInvariant();
                    string unit = reading.szUnit.ToLowerInvariant();

                    // CPU Total Load - look for total/overall CPU usage percentage
                    if (cpuLoad is null && unit == "%" &&
                        ((label.Contains("total") && label.Contains("cpu")) ||
                         label.Contains("cpu usage") ||
                         label.Contains("cpu utilization") ||
                         label == "total cpu usage"))
                    {
                        cpuLoad = (float)reading.Value;
                    }
                    // CPU Package/Die Temperature - various naming conventions
                    else if (cpuTemp is null && unit == "°c" &&
                             (label.Contains("cpu package") ||
                              label.Contains("cpu (tctl") ||
                              label.Contains("cpu (tdie") ||
                              label.Contains("cpu die") ||
                              label.Contains("core max") ||
                              label.Contains("tdie") ||
                              label.Contains("tctl") ||
                              (label.Contains("cpu") && label.Contains("temp") && !label.Contains("vrm"))))
                    {
                        cpuTemp = (float)reading.Value;
                    }
                    // GPU Temperature - various naming conventions
                    else if (gpuTemp is null && unit == "°c" &&
                             ((label.Contains("gpu") && (label.Contains("temp") || label.Contains("hot spot"))) ||
                              label.Contains("gpu temperature") ||
                              label.Contains("gpu core") ||
                              label.Contains("gpu edge") ||
                              label.Contains("gpu junction")))
                    {
                        gpuTemp = (float)reading.Value;
                    }
                    // Physical Memory Load - various naming conventions
                    else if (memoryLoad is null && unit == "%" &&
                             (label.Contains("physical memory load") ||
                              label.Contains("memory usage") ||
                              label.Contains("ram usage") ||
                              label == "memory load" ||
                              (label.Contains("memory") && label.Contains("used"))))
                    {
                        memoryLoad = (float)reading.Value;
                    }

                    // Fallback: capture first CPU/GPU temperature we see (any temp sensor related to cpu/gpu)
                    if (unit == "°c" && reading.Value > 0 && reading.Value < 150)
                    {
                        if (firstCpuTemp is null && (label.Contains("cpu") || label.Contains("core") || label.Contains("tctl") || label.Contains("tdie")))
                        {
                            firstCpuTemp = (float)reading.Value;
                        }
                        if (firstGpuTemp is null && label.Contains("gpu"))
                        {
                            firstGpuTemp = (float)reading.Value;
                        }
                    }
                }

                // Use fallbacks if primary matching didn't find temps
                cpuTemp ??= firstCpuTemp;
                gpuTemp ??= firstGpuTemp;

                // Log warning if temps still not found (helps debugging)
                if (cpuTemp is null || gpuTemp is null)
                {
                    LogMissingTemps(accessor, header, cpuTemp, gpuTemp);
                }

                return new PcStats(
                    Cpu: cpuLoad.HasValue ? (float)Math.Round(cpuLoad.Value, 1) : null,
                    Memory: memoryLoad.HasValue ? (float)Math.Round(memoryLoad.Value, 1) : null,
                    CpuTemp: cpuTemp.HasValue ? (float)Math.Round(cpuTemp.Value, 1) : null,
                    GpuTemp: gpuTemp.HasValue ? (float)Math.Round(gpuTemp.Value, 1) : null
                );
            }
            catch (System.IO.FileNotFoundException)
            {
                if (_hwInfoAvailable || !_availabilityChecked)
                {
                    _logger.LogWarning("HWiNFO shared memory not available. Ensure HWiNFO is running with 'Shared Memory Support' enabled in Settings > General.");
                    _hwInfoAvailable = false;
                    _availabilityChecked = true;
                }
                return new PcStats(null, null, null, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading HWiNFO shared memory.");
                return new PcStats(null, null, null, null);
            }
        }
    }

    private static HWiNFOReadingElement ReadReading(MemoryMappedViewAccessor accessor, long offset)
    {
        var reading = new HWiNFOReadingElement();
        
        reading.tReading = (HWiNFOSensorReadingType)accessor.ReadUInt32(offset);
        reading.dwSensorIndex = accessor.ReadUInt32(offset + 4);
        reading.dwReadingID = accessor.ReadUInt32(offset + 8);

        // Read label string
        byte[] labelBytes = new byte[HWiNFO_SENSORS_STRING_LEN];
        accessor.ReadArray(offset + 12, labelBytes, 0, HWiNFO_SENSORS_STRING_LEN);
        reading.szLabelOrig = Encoding.ASCII.GetString(labelBytes).TrimEnd('\0');

        // Read user label string  
        byte[] userLabelBytes = new byte[HWiNFO_SENSORS_STRING_LEN];
        accessor.ReadArray(offset + 12 + HWiNFO_SENSORS_STRING_LEN, userLabelBytes, 0, HWiNFO_SENSORS_STRING_LEN);
        reading.szLabelUser = Encoding.ASCII.GetString(userLabelBytes).TrimEnd('\0');

        // Read unit string
        byte[] unitBytes = new byte[HWiNFO_UNIT_STRING_LEN];
        accessor.ReadArray(offset + 12 + (2 * HWiNFO_SENSORS_STRING_LEN), unitBytes, 0, HWiNFO_UNIT_STRING_LEN);
        reading.szUnit = Encoding.ASCII.GetString(unitBytes).TrimEnd('\0');

        // Read values (after the strings)
        long valueOffset = offset + 12 + (2 * HWiNFO_SENSORS_STRING_LEN) + HWiNFO_UNIT_STRING_LEN;
        reading.Value = accessor.ReadDouble(valueOffset);
        reading.ValueMin = accessor.ReadDouble(valueOffset + 8);
        reading.ValueMax = accessor.ReadDouble(valueOffset + 16);
        reading.ValueAvg = accessor.ReadDouble(valueOffset + 24);

        return reading;
    }

    public void Dispose()
    {
        // Nothing to dispose - we open/close the memory mapped file on each read
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Logs available sensors that match our search criteria for debugging.
    /// </summary>
    private void LogAvailableSensors(MemoryMappedViewAccessor accessor, HWiNFOSharedMemHeader header)
    {
        try
        {
            long readingOffset = header.dwOffsetOfReadingSection;
            int readingSize = (int)header.dwSizeOfReadingElement;

            var cpuSensors = new List<string>();
            var tempSensors = new List<string>();
            var memorySensors = new List<string>();

            for (uint i = 0; i < header.dwNumReadingElements; i++)
            {
                var reading = ReadReading(accessor, readingOffset + (i * readingSize));
                string label = reading.szLabelOrig.ToLowerInvariant();
                string unit = reading.szUnit.ToLowerInvariant();

                // Collect CPU usage sensors
                if (unit == "%" && label.Contains("cpu"))
                {
                    cpuSensors.Add($"{reading.szLabelOrig} ({reading.Value:F1}{reading.szUnit})");
                }
                // Collect temperature sensors
                else if (unit == "°c" && (label.Contains("cpu") || label.Contains("gpu")))
                {
                    tempSensors.Add($"{reading.szLabelOrig} ({reading.Value:F1}{reading.szUnit})");
                }
                // Collect memory sensors
                else if (unit == "%" && label.Contains("memory"))
                {
                    memorySensors.Add($"{reading.szLabelOrig} ({reading.Value:F1}{reading.szUnit})");
                }
            }

            if (cpuSensors.Count > 0)
                _logger.LogDebug("Available CPU usage sensors: {Sensors}", string.Join(", ", cpuSensors.Take(5)));
            if (tempSensors.Count > 0)
                _logger.LogDebug("Available temp sensors: {Sensors}", string.Join(", ", tempSensors.Take(10)));
            if (memorySensors.Count > 0)
                _logger.LogDebug("Available memory sensors: {Sensors}", string.Join(", ", memorySensors.Take(3)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enumerate available sensors.");
        }
    }

    /// <summary>
    /// Logs available temperature sensors when temps couldn't be matched (throttled to once per minute).
    /// </summary>
    private DateTime _lastMissingTempLog = DateTime.MinValue;
    private void LogMissingTemps(MemoryMappedViewAccessor accessor, HWiNFOSharedMemHeader header, float? cpuTemp, float? gpuTemp)
    {
        // Throttle to once per minute to avoid log spam
        if ((DateTime.UtcNow - _lastMissingTempLog).TotalMinutes < 1)
            return;

        _lastMissingTempLog = DateTime.UtcNow;

        try
        {
            long readingOffset = header.dwOffsetOfReadingSection;
            int readingSize = (int)header.dwSizeOfReadingElement;
            var allTempSensors = new List<string>();

            for (uint i = 0; i < header.dwNumReadingElements; i++)
            {
                var reading = ReadReading(accessor, readingOffset + (i * readingSize));
                string unit = reading.szUnit.ToLowerInvariant();

                // Log ALL temperature sensors
                if (unit == "°c" && reading.Value > 0 && reading.Value < 150)
                {
                    allTempSensors.Add($"'{reading.szLabelOrig}'");
                }
            }

            if (cpuTemp is null)
                _logger.LogWarning("CPU temperature not found. Available temp sensors: {Sensors}", string.Join(", ", allTempSensors.Take(20)));
            if (gpuTemp is null)
                _logger.LogWarning("GPU temperature not found. Available temp sensors: {Sensors}", string.Join(", ", allTempSensors.Take(20)));
        }
        catch
        {
            // Ignore errors in diagnostic logging
        }
    }

    #region HWiNFO Shared Memory Structures

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct HWiNFOSharedMemHeader
    {
        public uint dwSignature;        // "HWiI" = 0x49574848
        public uint dwVersion;          // Version of shared memory
        public uint dwRevision;         // Revision
        public long poll_time;          // Polling time in ms
        public uint dwOffsetOfSensorSection;
        public uint dwSizeOfSensorElement;
        public uint dwNumSensorElements;
        public uint dwOffsetOfReadingSection;
        public uint dwSizeOfReadingElement;
        public uint dwNumReadingElements;
    }

    private enum HWiNFOSensorReadingType : uint
    {
        None = 0,
        Temp = 1,
        Volt = 2,
        Fan = 3,
        Current = 4,
        Power = 5,
        Clock = 6,
        Usage = 7,
        Other = 8
    }

    private struct HWiNFOReadingElement
    {
        public HWiNFOSensorReadingType tReading;
        public uint dwSensorIndex;
        public uint dwReadingID;
        public string szLabelOrig;
        public string szLabelUser;
        public string szUnit;
        public double Value;
        public double ValueMin;
        public double ValueMax;
        public double ValueAvg;
    }

    #endregion
}