using System.Runtime.Versioning;
using CPCRemote.Service.Options;
using NUnit.Framework;

namespace CPCRemote.Tests;

/// <summary>
/// Unit tests for <see cref="SensorOptions"/> and its related configuration classes.
/// Verifies that default values are correctly initialized.
/// </summary>
[TestFixture]
[SupportedOSPlatform("windows10.0.22621.0")]
public class SensorOptionsTests
{
    [Test]
    public void SensorOptions_Defaults_AreCorrect()
    {
        // Arrange & Act
        var options = new SensorOptions();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(options.CpuLoad, Is.Not.Null);
            Assert.That(options.CpuLoad.Patterns, Is.EquivalentTo(new[] { "total cpu usage", "cpu utilization", "cpu usage" }));
            Assert.That(options.CpuLoad.Unit, Is.EqualTo("%"));

            Assert.That(options.MemoryLoad, Is.Not.Null);
            Assert.That(options.MemoryLoad.Patterns, Is.EquivalentTo(new[] { "physical memory load", "memory usage", "memory load", "ram usage" }));
            Assert.That(options.MemoryLoad.Unit, Is.EqualTo("%"));

            Assert.That(options.CpuTemp, Is.Not.Null);
            Assert.That(options.CpuTemp.Patterns, Is.EquivalentTo(new[] { "cpu package", "cpu (tctl", "cpu (tdie", "cpu die", "core max", "tdie", "tctl" }));
            Assert.That(options.CpuTemp.Unit, Is.EqualTo("°c"));
            Assert.That(options.CpuTemp.RequirePositive, Is.True);

            Assert.That(options.GpuTemp, Is.Not.Null);
            Assert.That(options.GpuTemp.Patterns, Is.EquivalentTo(new[] { "gpu hot spot", "gpu temperature", "gpu core", "gpu edge", "gpu junction" }));
            Assert.That(options.GpuTemp.Unit, Is.EqualTo("°c"));
            Assert.That(options.GpuTemp.RequirePositive, Is.True);

            Assert.That(options.CustomSensors, Is.Not.Null);
            Assert.That(options.CustomSensors, Is.Empty);
        });
    }

    [Test]
    public void SensorMappingOptions_Defaults_AreCorrect()
    {
        // Arrange & Act
        var options = new SensorMappingOptions();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(options.Patterns, Is.Empty);
            Assert.That(options.Unit, Is.EqualTo(string.Empty));
            Assert.That(options.RequirePositive, Is.False);
        });
    }

    [Test]
    public void CustomSensorOptions_Defaults_AreCorrect()
    {
        // Arrange & Act
        var options = new CustomSensorOptions();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(options.Name, Is.EqualTo(string.Empty));
            Assert.That(options.Label, Is.EqualTo(string.Empty));
            Assert.That(options.Unit, Is.EqualTo(string.Empty));
            Assert.That(options.RequirePositive, Is.False);
        });
    }
}
