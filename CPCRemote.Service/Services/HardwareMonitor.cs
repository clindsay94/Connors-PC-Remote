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
                    _logger.LogInformation("HWiNFO shared memory connected. Version: {Version}, Sensors: {Count}",
                        header.dwVersion, header.dwNumSensorElements);
                }

                float? cpuLoad = null;
                float? memoryLoad = null;
                float? cpuTemp = null;
                float? gpuTemp = null;

                // Iterate through all sensor readings
                long readingOffset = header.dwOffsetOfReadingSection;
                int readingSize = (int)header.dwSizeOfReadingElement;

                for (uint i = 0; i < header.dwNumReadingElements; i++)
                {
                    var reading = ReadReading(accessor, readingOffset + (i * readingSize));

                    string label = reading.szLabelOrig.ToLowerInvariant();
                    string unit = reading.szUnit.ToLowerInvariant();

                    // CPU Total Load
                    if (cpuLoad is null && label.Contains("total") && label.Contains("cpu") && unit == "%")
                    {
                        cpuLoad = (float)reading.Value;
                    }
                    // CPU Package/Die Temperature
                    else if (cpuTemp is null && 
                             (label.Contains("cpu package") || label.Contains("cpu (tctl") || label.Contains("cpu die")) && 
                             unit == "°c")
                    {
                        cpuTemp = (float)reading.Value;
                    }
                    // GPU Temperature
                    else if (gpuTemp is null && label.Contains("gpu") && label.Contains("temp") && unit == "°c")
                    {
                        gpuTemp = (float)reading.Value;
                    }
                    // Physical Memory Load
                    else if (memoryLoad is null && label.Contains("physical memory load") && unit == "%")
                    {
                        memoryLoad = (float)reading.Value;
                    }
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