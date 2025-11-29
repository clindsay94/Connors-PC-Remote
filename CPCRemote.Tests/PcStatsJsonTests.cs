namespace CPCRemote.Tests;

using System.Text.Json;
using System.Text.Json.Serialization;
using global::CPCRemote.Core.IPC;
using NUnit.Framework;

/// <summary>
/// Tests for GetStatsResponse JSON serialization to ensure null values are handled correctly.
/// Null values must be omitted from JSON to prevent SmartThings Edge driver errors.
/// </summary>
/// <remarks>
/// <para>
/// <b>Bridge Safety:</b> These tests verify the JSON contract between the C# service
/// and the SmartThings Lua driver. The Lua driver's <c>update_stats()</c> function
/// expects null values to be omitted, not serialized as <c>null</c>.
/// </para>
/// </remarks>
[TestFixture]
public class StatsJsonTests
{
    private static readonly JsonSerializerOptions StatsJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Test]
    public void Serialize_CpuStats_AllValuesPresent_IncludesAllProperties()
    {
        // Arrange
        var stats = new CpuStats
        {
            Utility = 45.5f,
            Temperature = 55.0f,
            DieAvgTemp = 52.0f,
            PackagePower = 65.0f,
            CoreClock = 4500f,
            EffectiveClock = 4200f
        };

        // Act
        string json = JsonSerializer.Serialize(stats, StatsJsonOptions);

        // Assert
        Assert.That(json, Does.Contain("\"utility\":45.5"));
        Assert.That(json, Does.Contain("\"temperature\":55"));
        Assert.That(json, Does.Contain("\"dieAvgTemp\":52"));
        Assert.That(json, Does.Contain("\"packagePower\":65"));
    }

    [Test]
    public void Serialize_CpuStats_NullTemperature_OmitsTempFromJson()
    {
        // Arrange
        var stats = new CpuStats
        {
            Utility = 45.5f,
            Temperature = null,
            PackagePower = 65.0f
        };

        // Act
        string json = JsonSerializer.Serialize(stats, StatsJsonOptions);

        // Assert
        Assert.That(json, Does.Not.Contain("temperature"));
        Assert.That(json, Does.Contain("\"utility\":45.5"));
        Assert.That(json, Does.Contain("\"packagePower\":65"));
    }

    [Test]
    public void Serialize_GpuStats_AllValuesPresent_IncludesAllProperties()
    {
        // Arrange
        var stats = new GpuStats
        {
            Temperature = 55.0f,
            MemJunctionTemp = 78.0f,
            Power = 250.0f,
            EffectiveClock = 2500f,
            MemoryUsage = 45.5f,
            CoreLoad = 95.0f
        };

        // Act
        string json = JsonSerializer.Serialize(stats, StatsJsonOptions);

        // Assert
        Assert.That(json, Does.Contain("\"temperature\":55"));
        Assert.That(json, Does.Contain("\"memJunctionTemp\":78"));
        Assert.That(json, Does.Contain("\"power\":250"));
        Assert.That(json, Does.Contain("\"coreLoad\":95"));
    }

    [Test]
    public void Serialize_GpuStats_NullValues_OmitsNullsFromJson()
    {
        // Arrange
        var stats = new GpuStats
        {
            Temperature = 55.0f,
            MemJunctionTemp = null,
            Power = null
        };

        // Act
        string json = JsonSerializer.Serialize(stats, StatsJsonOptions);

        // Assert
        Assert.That(json, Does.Not.Contain("memJunctionTemp"));
        Assert.That(json, Does.Not.Contain("power"));
        Assert.That(json, Does.Contain("\"temperature\":55"));
    }

    [Test]
    public void Serialize_MemoryStats_WithDimmTemps_IncludesDimmArray()
    {
        // Arrange
        var stats = new MemoryStats
        {
            Load = 62.3f,
            DimmTemps =
            [
                new DimmTemp { Slot = 1, Temp = 42.5f },
                new DimmTemp { Slot = 3, Temp = 43.0f }
            ]
        };

        // Act
        string json = JsonSerializer.Serialize(stats, StatsJsonOptions);

        // Assert
        Assert.That(json, Does.Contain("\"load\":62.3"));
        Assert.That(json, Does.Contain("\"dimmTemps\""));
        Assert.That(json, Does.Contain("\"slot\":1"));
        Assert.That(json, Does.Contain("\"temp\":42.5"));
    }

    [Test]
    public void Serialize_MotherboardStats_VoltageValues_FormatsCorrectly()
    {
        // Arrange
        var stats = new MotherboardStats
        {
            Vcore = 1.325f,
            Vsoc = 1.100f
        };

        // Act
        string json = JsonSerializer.Serialize(stats, StatsJsonOptions);

        // Assert
        Assert.That(json, Does.Contain("\"vcore\":1.325"));
        Assert.That(json, Does.Contain("\"vsoc\":1.1"));
    }

    [Test]
    public void Serialize_GetStatsResponse_AllNull_ReturnsMinimalJson()
    {
        // Arrange
        var response = new GetStatsResponse
        {
            Success = true,
            Cpu = null,
            Memory = null,
            Gpu = null,
            Motherboard = null
        };

        // Act
        string json = JsonSerializer.Serialize(response, StatsJsonOptions);

        // Assert
        Assert.That(json, Does.Not.Contain("cpu"));
        Assert.That(json, Does.Not.Contain("memory"));
        Assert.That(json, Does.Not.Contain("gpu"));
        Assert.That(json, Does.Not.Contain("motherboard"));
    }

    [Test]
    public void Serialize_CpuStats_WithCoreEffectiveClocks_SerializesArray()
    {
        // Arrange
        var stats = new CpuStats
        {
            Utility = 50f,
            CoreEffectiveClocks = [4500f, 4400f, 4300f, 4200f, 4100f, 4000f, 3900f, 3800f]
        };

        // Act
        string json = JsonSerializer.Serialize(stats, StatsJsonOptions);

        // Assert
        Assert.That(json, Does.Contain("\"coreEffectiveClocks\""));
        Assert.That(json, Does.Contain("4500"));
        Assert.That(json, Does.Contain("3800"));
    }
}
