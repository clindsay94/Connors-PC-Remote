namespace CPCRemote.Service.Services;

using System;
using System.Linq;
using LibreHardwareMonitor.Hardware;
using Microsoft.Extensions.Logging;

public class HardwareMonitor : IDisposable
{
    private readonly Computer _computer;
    private readonly ILogger<HardwareMonitor> _logger;
    private readonly object _lock = new();

    public HardwareMonitor(ILogger<HardwareMonitor> logger)
    {
        _logger = logger;
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = false,
            IsControllerEnabled = false,
            IsNetworkEnabled = false,
            IsStorageEnabled = false
        };

        try
        {
            _computer.Open();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open Hardware Monitor. Ensure the service is running as Administrator.");
        }
    }

    public record PcStats(float? cpu, float? memory, float? cpuTemp, float? gpuTemp);

    public PcStats GetStats()
    {
        lock (_lock)
        {
            try
            {
                _computer.Accept(new UpdateVisitor());

                float? cpuLoad = null;
                float? cpuTemp = null;
                float? gpuTemp = null;
                float? memoryLoad = null;

                foreach (var hardware in _computer.Hardware)
                {
                    if (hardware.HardwareType == HardwareType.Cpu)
                    {
                        cpuLoad = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Total"))?.Value ?? cpuLoad;
                        
                        // Try to find Package temp, otherwise take the first available temp (usually Core 1 or Max)
                        cpuTemp = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name.Contains("Package"))?.Value 
                                  ?? hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature)?.Value 
                                  ?? cpuTemp;
                    }
                    else if (hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAmd || hardware.HardwareType == HardwareType.GpuIntel)
                    {
                        gpuTemp = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature)?.Value ?? gpuTemp;
                    }
                    else if (hardware.HardwareType == HardwareType.Memory)
                    {
                        memoryLoad = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Memory"))?.Value ?? memoryLoad;
                    }
                }

                return new PcStats(
                    cpu: cpuLoad.HasValue ? (float)Math.Round(cpuLoad.Value, 1) : null,
                    memory: memoryLoad.HasValue ? (float)Math.Round(memoryLoad.Value, 1) : null,
                    cpuTemp: cpuTemp.HasValue ? (float)Math.Round(cpuTemp.Value, 1) : null,
                    gpuTemp: gpuTemp.HasValue ? (float)Math.Round(gpuTemp.Value, 1) : null
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading hardware sensors.");
                return new PcStats(null, null, null, null);
            }
        }
    }

    public void Dispose()
    {
        _computer.Close();
        GC.SuppressFinalize(this);
    }

    private class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var subHardware in hardware.SubHardware) subHardware.Accept(this);
        }

        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }
}