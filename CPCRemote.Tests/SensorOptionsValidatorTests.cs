using System.Runtime.Versioning;

using CPCRemote.Service.Options;

using NUnit.Framework;

namespace CPCRemote.Tests;

/// <summary>
/// Unit tests for the <see cref="SensorOptionsValidator"/> class.
/// Verifies configuration validation logic for sensor options.
/// </summary>
[TestFixture]
[SupportedOSPlatform("windows10.0.22621.0")]
public class SensorOptionsValidatorTests
{
    private SensorOptionsValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new SensorOptionsValidator();
    }

    #region Valid Configuration Tests

    [Test]
    public void Validate_DefaultOptions_ReturnsSuccess()
    {
        // Arrange
        var options = new SensorOptions();

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public void Validate_WithCustomSensors_ReturnsSuccess()
    {
        // Arrange
        var options = new SensorOptions
        {
            CustomSensors =
            [
                new CustomSensorOptions
                {
                    Name = "fanSpeed",
                    Label = "CPU Fan",
                    Unit = "RPM"
                }
            ]
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public void Validate_WithMultipleCustomSensors_ReturnsSuccess()
    {
        // Arrange
        var options = new SensorOptions
        {
            CustomSensors =
            [
                new CustomSensorOptions { Name = "sensor1", Label = "Label 1", Unit = "°c" },
                new CustomSensorOptions { Name = "sensor2", Label = "Label 2", Unit = "%" },
                new CustomSensorOptions { Name = "sensor3", Label = "Label 3", Unit = "RPM" }
            ]
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        Assert.That(result.Succeeded, Is.True);
    }

    #endregion

    #region Empty Patterns Tests

    [Test]
    public void Validate_EmptyCpuLoadPatterns_ReturnsFail()
    {
        // Arrange
        var options = new SensorOptions
        {
            CpuLoad = new SensorMappingOptions { Patterns = [], Unit = "%" }
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.FailureMessage, Does.Contain("CpuLoad.Patterns"));
        });
    }

    [Test]
    public void Validate_EmptyMemoryLoadPatterns_ReturnsFail()
    {
        // Arrange
        var options = new SensorOptions
        {
            MemoryLoad = new SensorMappingOptions { Patterns = [], Unit = "%" }
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.FailureMessage, Does.Contain("MemoryLoad.Patterns"));
        });
    }

    [Test]
    public void Validate_EmptyCpuTempPatterns_ReturnsFail()
    {
        // Arrange
        var options = new SensorOptions
        {
            CpuTemp = new SensorMappingOptions { Patterns = [], Unit = "°c" }
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.FailureMessage, Does.Contain("CpuTemp.Patterns"));
        });
    }

    [Test]
    public void Validate_EmptyGpuTempPatterns_ReturnsFail()
    {
        // Arrange
        var options = new SensorOptions
        {
            GpuTemp = new SensorMappingOptions { Patterns = [], Unit = "°c" }
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.FailureMessage, Does.Contain("GpuTemp.Patterns"));
        });
    }

    [Test]
    public void Validate_AllEmptyPatterns_ReturnsMultipleErrors()
    {
        // Arrange
        var options = new SensorOptions
        {
            CpuLoad = new SensorMappingOptions { Patterns = [] },
            MemoryLoad = new SensorMappingOptions { Patterns = [] },
            CpuTemp = new SensorMappingOptions { Patterns = [] },
            GpuTemp = new SensorMappingOptions { Patterns = [] }
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.FailureMessage, Does.Contain("CpuLoad"));
            Assert.That(result.FailureMessage, Does.Contain("MemoryLoad"));
            Assert.That(result.FailureMessage, Does.Contain("CpuTemp"));
            Assert.That(result.FailureMessage, Does.Contain("GpuTemp"));
        });
    }

    #endregion

    #region Custom Sensor Validation Tests

    [Test]
    public void Validate_CustomSensorMissingName_ReturnsFail()
    {
        // Arrange
        var options = new SensorOptions
        {
            CustomSensors =
            [
                new CustomSensorOptions { Name = "", Label = "CPU Fan", Unit = "RPM" }
            ]
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.FailureMessage, Does.Contain("CustomSensors[0].Name"));
        });
    }

    [Test]
    public void Validate_CustomSensorMissingLabel_ReturnsFail()
    {
        // Arrange
        var options = new SensorOptions
        {
            CustomSensors =
            [
                new CustomSensorOptions { Name = "fanSpeed", Label = "", Unit = "RPM" }
            ]
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.FailureMessage, Does.Contain("CustomSensors[0].Label"));
        });
    }

    [Test]
    public void Validate_CustomSensorWhitespaceName_ReturnsFail()
    {
        // Arrange
        var options = new SensorOptions
        {
            CustomSensors =
            [
                new CustomSensorOptions { Name = "   ", Label = "CPU Fan", Unit = "RPM" }
            ]
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        Assert.That(result.Failed, Is.True);
    }

    [Test]
    public void Validate_MultipleInvalidCustomSensors_ReportsAllErrors()
    {
        // Arrange
        var options = new SensorOptions
        {
            CustomSensors =
            [
                new CustomSensorOptions { Name = "", Label = "Label1" },      // Missing name
                new CustomSensorOptions { Name = "sensor2", Label = "" },     // Missing label
                new CustomSensorOptions { Name = "", Label = "" }              // Missing both
            ]
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.FailureMessage, Does.Contain("CustomSensors[0].Name"));
            Assert.That(result.FailureMessage, Does.Contain("CustomSensors[1].Label"));
            Assert.That(result.FailureMessage, Does.Contain("CustomSensors[2].Name"));
            Assert.That(result.FailureMessage, Does.Contain("CustomSensors[2].Label"));
        });
    }

    #endregion

    #region Null Handling Tests

    [Test]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => _validator.Validate(null, null!));
    }

    #endregion
}
