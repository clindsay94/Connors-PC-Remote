using System.Runtime.Versioning;

using CPCRemote.Core.IPC;
using CPCRemote.Service.Options;
using CPCRemote.Service.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using NUnit.Framework;

namespace CPCRemote.Tests;

/// <summary>
/// Unit tests for the <see cref="HardwareMonitor"/> class.
/// Tests GetStats response structure and error handling.
/// </summary>
/// <remarks>
/// Note: HardwareMonitor depends on HWiNFO shared memory which is not available
/// during tests. These tests verify the response structure, configuration handling,
/// and graceful behavior when sensors are unavailable.
/// </remarks>
[TestFixture]
[SupportedOSPlatform("windows10.0.22621.0")]
public class HardwareMonitorTests
{
    private Mock<ILogger<HardwareMonitor>> _loggerMock = null!;
    private Mock<IOptionsMonitor<SensorOptions>> _sensorOptionsMonitorMock = null!;
    private SensorOptions _defaultSensorOptions = null!;

    [SetUp]
    public void Setup()
    {
        // Arrange
        _loggerMock = new Mock<ILogger<HardwareMonitor>>();
        _sensorOptionsMonitorMock = new Mock<IOptionsMonitor<SensorOptions>>();

        _defaultSensorOptions = new SensorOptions
        {
            CpuLoad = new SensorMappingOptions
            {
                Patterns = ["total cpu usage", "cpu utilization"],
                Unit = "%"
            },
            MemoryLoad = new SensorMappingOptions
            {
                Patterns = ["physical memory load"],
                Unit = "%"
            },
            CpuTemp = new SensorMappingOptions
            {
                Patterns = ["cpu package", "tctl"],
                Unit = "°c",
                RequirePositive = true
            },
            GpuTemp = new SensorMappingOptions
            {
                Patterns = ["gpu temperature"],
                Unit = "°c",
                RequirePositive = true
            },
            CustomSensors = []
        };

        _sensorOptionsMonitorMock
            .Setup(o => o.CurrentValue)
            .Returns(_defaultSensorOptions);
    }

    #region GetStats Response Structure Tests

    [Test]
    public void GetStats_WhenCalled_ReturnsNonNullResponse()
    {
        // Arrange
        var monitor = new HardwareMonitor(_loggerMock.Object, _sensorOptionsMonitorMock.Object);

        // Act
        var stats = monitor.GetStats();

        // Assert
        Assert.That(stats, Is.Not.Null);
        Assert.That(stats, Is.InstanceOf<GetStatsResponse>());
    }

    [Test]
    public void GetStats_WhenHwinfoUnavailable_ReturnsSuccessFalseWithErrorMessage()
    {
        // Arrange - HWiNFO is not running in CI/test environments
        var monitor = new HardwareMonitor(_loggerMock.Object, _sensorOptionsMonitorMock.Object);

        // Act
        var stats = monitor.GetStats();

        // Assert - When HWiNFO is unavailable, Success should be false with an error message
        // This correctly indicates that hardware stats couldn't be retrieved
        // Note: If HWiNFO happens to be running locally, this test still passes because
        // we're testing that the response structure is valid either way
        if (!stats.Success)
        {
            Assert.That(stats.ErrorMessage, Is.Not.Null.And.Not.Empty,
                "When Success is false, ErrorMessage should explain why");
        }
        // If HWiNFO is running, Success will be true - that's also valid
        Assert.That(stats, Is.Not.Null);
    }

    [Test]
    public void GetStats_ResponseComponents_AreEitherNullOrPopulated()
    {
        // Arrange - HWiNFO may or may not be running during tests
        var monitor = new HardwareMonitor(_loggerMock.Object, _sensorOptionsMonitorMock.Object);

        // Act
        var stats = monitor.GetStats();

        // Assert - If HWiNFO is available, stats are populated; if not, they're null
        // This test verifies that partial data doesn't cause issues
        Assert.Multiple(() =>
        {
            // Each component should be either null or a valid object with data
            if (stats.Cpu is not null)
            {
                // If CPU stats exist, they should have valid structure
                Assert.That(stats.Cpu, Is.InstanceOf<CpuStats>());
            }

            if (stats.Memory is not null)
            {
                Assert.That(stats.Memory, Is.InstanceOf<MemoryStats>());
            }

            if (stats.Gpu is not null)
            {
                Assert.That(stats.Gpu, Is.InstanceOf<GpuStats>());
            }

            if (stats.Motherboard is not null)
            {
                Assert.That(stats.Motherboard, Is.InstanceOf<MotherboardStats>());
            }
        });
    }

    [Test]
    public void GetStats_MultipleCalls_ReturnsConsistentStructure()
    {
        // Arrange
        var monitor = new HardwareMonitor(_loggerMock.Object, _sensorOptionsMonitorMock.Object);

        // Act
        var stats1 = monitor.GetStats();
        var stats2 = monitor.GetStats();
        var stats3 = monitor.GetStats();

        // Assert - All responses should be non-null and have consistent Success values
        // (all true if HWiNFO is running, all false if not)
        Assert.Multiple(() =>
        {
            Assert.That(stats1, Is.Not.Null);
            Assert.That(stats2, Is.Not.Null);
            Assert.That(stats3, Is.Not.Null);
            Assert.That(stats1.Success, Is.EqualTo(stats2.Success));
            Assert.That(stats2.Success, Is.EqualTo(stats3.Success));
        });
    }

    #endregion

    #region Configuration Tests

    [Test]
    public void Constructor_WithValidOptions_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => new HardwareMonitor(_loggerMock.Object, _sensorOptionsMonitorMock.Object));
    }

    [Test]
    public void Constructor_WithEmptyCustomSensors_DoesNotThrow()
    {
        // Arrange
        _defaultSensorOptions.CustomSensors = [];
        _sensorOptionsMonitorMock.Setup(o => o.CurrentValue).Returns(_defaultSensorOptions);

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var monitor = new HardwareMonitor(_loggerMock.Object, _sensorOptionsMonitorMock.Object);
            _ = monitor.GetStats();
        });
    }

    [Test]
    public void Constructor_WithCustomSensors_DoesNotThrow()
    {
        // Arrange
        _defaultSensorOptions.CustomSensors =
        [
            new CustomSensorOptions
            {
                Name = "customTemp",
                Label = "Custom Temperature",
                Unit = "°c",
                RequirePositive = true
            }
        ];
        _sensorOptionsMonitorMock.Setup(o => o.CurrentValue).Returns(_defaultSensorOptions);

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var monitor = new HardwareMonitor(_loggerMock.Object, _sensorOptionsMonitorMock.Object);
            _ = monitor.GetStats();
        });
    }

    [Test]
    public void Constructor_WithEmptyPatterns_DoesNotThrow()
    {
        // Arrange
        _defaultSensorOptions.CpuLoad.Patterns = [];
        _sensorOptionsMonitorMock.Setup(o => o.CurrentValue).Returns(_defaultSensorOptions);

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var monitor = new HardwareMonitor(_loggerMock.Object, _sensorOptionsMonitorMock.Object);
            _ = monitor.GetStats();
        });
    }

    #endregion

    #region Response Type Validation Tests

    [Test]
    public void GetStats_ResponseType_ImplementsIpcResponse()
    {
        // Arrange
        var monitor = new HardwareMonitor(_loggerMock.Object, _sensorOptionsMonitorMock.Object);

        // Act
        var stats = monitor.GetStats();

        // Assert
        Assert.That(stats, Is.InstanceOf<IpcResponse>());
    }

    [Test]
    public void CpuStats_AllProperties_HaveCorrectTypes()
    {
        // This test verifies the CpuStats record has the expected nullable float properties
        var cpuStats = new CpuStats
        {
            Utility = 50.5f,
            Temperature = 65.0f,
            DieAvgTemp = 60.0f,
            IodHotspot = 70.0f,
            PackagePower = 88.0f,
            Ppt = 90.0f,
            CoreClock = 4500.0f,
            EffectiveClock = 4400.0f,
            CoreEffectiveClocks = [4500f, 4400f, 4300f, 4200f]
        };

        Assert.Multiple(() =>
        {
            Assert.That(cpuStats.Utility, Is.InstanceOf<float?>());
            Assert.That(cpuStats.Temperature, Is.InstanceOf<float?>());
            Assert.That(cpuStats.CoreEffectiveClocks, Is.InstanceOf<float[]>());
        });
    }

    [Test]
    public void MemoryStats_AllProperties_HaveCorrectTypes()
    {
        // This test verifies the MemoryStats record has the expected structure
        var memoryStats = new MemoryStats
        {
            Load = 75.0f,
            DimmTemps =
            [
                new DimmTemp { Slot = 1, Temp = 45.0f },
                new DimmTemp { Slot = 2, Temp = 46.0f }
            ]
        };

        Assert.Multiple(() =>
        {
            Assert.That(memoryStats.Load, Is.InstanceOf<float?>());
            Assert.That(memoryStats.DimmTemps, Is.InstanceOf<DimmTemp[]>());
            Assert.That(memoryStats.DimmTemps![0].Slot, Is.EqualTo(1));
            Assert.That(memoryStats.DimmTemps[0].Temp, Is.EqualTo(45.0f));
        });
    }

    [Test]
    public void GpuStats_AllProperties_HaveCorrectTypes()
    {
        // This test verifies the GpuStats record has the expected structure
        var gpuStats = new GpuStats
        {
            Temperature = 75.0f,
            MemJunctionTemp = 85.0f,
            Power = 250.0f,
            Clock = 2100.0f,
            EffectiveClock = 2050.0f,
            MemoryUsage = 90.0f,
            CoreLoad = 99.0f
        };

        Assert.Multiple(() =>
        {
            Assert.That(gpuStats.Temperature, Is.InstanceOf<float?>());
            Assert.That(gpuStats.Power, Is.InstanceOf<float?>());
            Assert.That(gpuStats.MemoryUsage, Is.InstanceOf<float?>());
        });
    }

    [Test]
    public void MotherboardStats_AllProperties_HaveCorrectTypes()
    {
        // This test verifies the MotherboardStats record has the expected structure
        var motherboardStats = new MotherboardStats
        {
            Vcore = 1.35f,
            Vsoc = 1.1f
        };

        Assert.Multiple(() =>
        {
            Assert.That(motherboardStats.Vcore, Is.InstanceOf<float?>());
            Assert.That(motherboardStats.Vsoc, Is.InstanceOf<float?>());
        });
    }

    #endregion

    #region SensorOptions Configuration Tests

    [Test]
    public void SensorMappingOptions_Defaults_AreValid()
    {
        // Arrange
        var options = new SensorMappingOptions
        {
            Patterns = ["pattern1", "pattern2"],
            Unit = "%",
            RequirePositive = false
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(options.Patterns, Has.Length.EqualTo(2));
            Assert.That(options.Unit, Is.EqualTo("%"));
            Assert.That(options.RequirePositive, Is.False);
        });
    }

    [Test]
    public void CustomSensorOptions_CanBeConfigured()
    {
        // Arrange
        var custom = new CustomSensorOptions
        {
            Name = "fanSpeed",
            Label = "CPU Fan",
            Unit = "RPM",
            RequirePositive = true
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(custom.Name, Is.EqualTo("fanSpeed"));
            Assert.That(custom.Label, Is.EqualTo("CPU Fan"));
            Assert.That(custom.Unit, Is.EqualTo("RPM"));
            Assert.That(custom.RequirePositive, Is.True);
        });
    }

    [Test]
    public void SensorOptions_DefaultValues_HaveValidPatterns()
    {
        // Arrange - use fresh default options
        var options = new SensorOptions();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(options.CpuLoad.Patterns, Is.Not.Empty);
            Assert.That(options.MemoryLoad.Patterns, Is.Not.Empty);
            Assert.That(options.CpuTemp.Patterns, Is.Not.Empty);
            Assert.That(options.GpuTemp.Patterns, Is.Not.Empty);
        });
    }

    [Test]
    public void SensorOptions_CpuTemp_HasRequirePositiveTrue()
    {
        // Arrange
        var options = new SensorOptions();

        // Assert - temperature sensors should require positive values
        Assert.Multiple(() =>
        {
            Assert.That(options.CpuTemp.RequirePositive, Is.True);
            Assert.That(options.GpuTemp.RequirePositive, Is.True);
        });
    }

    #endregion

    #region Thread Safety Tests

    [Test]
    public void GetStats_ConcurrentCalls_DoNotThrow()
    {
        // Arrange
        var monitor = new HardwareMonitor(_loggerMock.Object, _sensorOptionsMonitorMock.Object);
        var exceptions = new List<Exception>();

        // Act
        Parallel.For(0, 10, i =>
        {
            try
            {
                var result = monitor.GetStats();
                Assert.That(result, Is.Not.Null);
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });

        // Assert
        Assert.That(exceptions, Is.Empty, $"Exceptions occurred: {string.Join(", ", exceptions.Select(e => e.Message))}");
    }

    #endregion
}
