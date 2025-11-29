using System.Runtime.Versioning;
using System.Text.Json;

using CPCRemote.Core.IPC;

using NUnit.Framework;

namespace CPCRemote.Tests;

/// <summary>
/// Unit tests for the Named Pipe Server message handling.
/// Tests IPC message serialization/deserialization and request/response contracts.
/// </summary>
/// <remarks>
/// These tests verify the IPC message contract without requiring actual pipe connections.
/// The NamedPipeServer class itself requires many dependencies and running services,
/// so we focus on testing the message types and serialization.
/// </remarks>
[TestFixture]
[SupportedOSPlatform("windows10.0.22621.0")]
public class NamedPipeServerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    #region IPC Message Serialization Tests

    [Test]
    public void GetStatsRequest_Serialization_RoundTrips()
    {
        // Arrange
        var request = new GetStatsRequest();

        // Act
        var json = JsonSerializer.Serialize<IpcMessage>(request, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<IpcMessage>(json, JsonOptions);

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized, Is.InstanceOf<GetStatsRequest>());
    }

    [Test]
    public void GetStatsResponse_Serialization_PreservesData()
    {
        // Arrange
        var response = new GetStatsResponse
        {
            Success = true,
            Cpu = new CpuStats
            {
                Utility = 50.5f,
                Temperature = 65.0f
            },
            Memory = new MemoryStats
            {
                Load = 75.0f
            }
        };

        // Act
        var json = JsonSerializer.Serialize<IpcMessage>(response, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<IpcMessage>(json, JsonOptions) as GetStatsResponse;

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Success, Is.True);
        Assert.That(deserialized.Cpu?.Utility, Is.EqualTo(50.5f).Within(0.1f));
        Assert.That(deserialized.Cpu?.Temperature, Is.EqualTo(65.0f).Within(0.1f));
        Assert.That(deserialized.Memory?.Load, Is.EqualTo(75.0f).Within(0.1f));
    }

    [Test]
    public void GetAppsRequest_Serialization_RoundTrips()
    {
        // Arrange
        var request = new GetAppsRequest();

        // Act
        var json = JsonSerializer.Serialize<IpcMessage>(request, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<IpcMessage>(json, JsonOptions);

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized, Is.InstanceOf<GetAppsRequest>());
    }

    [Test]
    public void ServiceStatusRequest_Serialization_RoundTrips()
    {
        // Arrange
        var request = new ServiceStatusRequest();

        // Act
        var json = JsonSerializer.Serialize<IpcMessage>(request, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<IpcMessage>(json, JsonOptions);

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized, Is.InstanceOf<ServiceStatusRequest>());
    }

    [Test]
    public void ServiceStatusResponse_Serialization_PreservesData()
    {
        // Arrange
        var response = new ServiceStatusResponse
        {
            Success = true,
            Version = "1.0.0",
            UptimeSeconds = 3600.5,
            HttpListenerAddress = "http://localhost:5005/",
            IsListening = true,
            IsHardwareMonitoringAvailable = true,
            StartTimeUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var json = JsonSerializer.Serialize<IpcMessage>(response, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<IpcMessage>(json, JsonOptions) as ServiceStatusResponse;

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Version, Is.EqualTo("1.0.0"));
        Assert.That(deserialized.UptimeSeconds, Is.EqualTo(3600.5).Within(0.1));
        Assert.That(deserialized.HttpListenerAddress, Is.EqualTo("http://localhost:5005/"));
        Assert.That(deserialized.IsListening, Is.True);
    }

    [Test]
    public void LaunchAppRequest_Serialization_PreservesSlot()
    {
        // Arrange
        var request = new LaunchAppRequest { Slot = "App1" };

        // Act
        var json = JsonSerializer.Serialize<IpcMessage>(request, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<IpcMessage>(json, JsonOptions) as LaunchAppRequest;

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Slot, Is.EqualTo("App1"));
    }

    [Test]
    public void ErrorResponse_Serialization_PreservesErrorDetails()
    {
        // Arrange
        var response = new ErrorResponse
        {
            Success = false,
            ErrorMessage = "Test error message",
            ExceptionType = "InvalidOperationException"
        };

        // Act
        var json = JsonSerializer.Serialize<IpcMessage>(response, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<IpcMessage>(json, JsonOptions) as ErrorResponse;

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Success, Is.False);
        Assert.That(deserialized.ErrorMessage, Is.EqualTo("Test error message"));
        Assert.That(deserialized.ExceptionType, Is.EqualTo("InvalidOperationException"));
    }

    #endregion

    #region IPC Constants Tests

    [Test]
    public void IpcConstants_PipeName_IsNotEmpty()
    {
        // Assert
        Assert.That(IpcConstants.PipeName, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void IpcConstants_BufferSize_IsPositive()
    {
        // Assert
        Assert.That(IpcConstants.BufferSize, Is.GreaterThan(0));
    }

    [Test]
    public void IpcConstants_MaxMessageSize_IsPositive()
    {
        // Assert
        Assert.That(IpcConstants.MaxMessageSize, Is.GreaterThan(0));
    }

    [Test]
    public void IpcConstants_DefaultTimeout_IsPositive()
    {
        // Assert
        Assert.That(IpcConstants.DefaultTimeout.TotalSeconds, Is.GreaterThan(0));
    }

    [Test]
    public void IpcConstants_DefaultConnectTimeout_IsPositive()
    {
        // Assert
        Assert.That(IpcConstants.DefaultConnectTimeout.TotalSeconds, Is.GreaterThan(0));
    }

    #endregion

    #region Request Type Polymorphism Tests

    [Test]
    public void IpcRequest_Subtypes_DeserializeCorrectly()
    {
        // Test that different request types maintain their type through serialization
        var requests = new IpcRequest[]
        {
            new GetStatsRequest(),
            new GetAppsRequest(),
            new ServiceStatusRequest(),
            new LaunchAppRequest { Slot = "App1" }
        };

        foreach (var request in requests)
        {
            // Act
            var json = JsonSerializer.Serialize<IpcMessage>(request, JsonOptions);
            var deserialized = JsonSerializer.Deserialize<IpcMessage>(json, JsonOptions);

            // Assert
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized!.GetType(), Is.EqualTo(request.GetType()),
                $"Request type {request.GetType().Name} did not round-trip correctly");
        }
    }

    [Test]
    public void IpcMessage_CorrelationId_IsPreserved()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var request = new GetStatsRequest { CorrelationId = correlationId };

        // Act
        var json = JsonSerializer.Serialize<IpcMessage>(request, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<IpcMessage>(json, JsonOptions);

        // Assert
        Assert.That(deserialized?.CorrelationId, Is.EqualTo(correlationId));
    }

    #endregion

    #region Message Type Validation Tests

    [Test]
    public void CpuStats_NullableProperties_AllowNull()
    {
        // Arrange
        var stats = new CpuStats();

        // Assert - all properties should be null by default
        Assert.Multiple(() =>
        {
            Assert.That(stats.Utility, Is.Null);
            Assert.That(stats.Temperature, Is.Null);
            Assert.That(stats.CoreClock, Is.Null);
            Assert.That(stats.CoreEffectiveClocks, Is.Null);
        });
    }

    [Test]
    public void DimmTemp_Serialization_PreservesValues()
    {
        // Arrange
        var dimmTemp = new DimmTemp { Slot = 1, Temp = 45.5f };

        // Act
        var json = JsonSerializer.Serialize(dimmTemp, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<DimmTemp>(json, JsonOptions);

        // Assert
        Assert.That(deserialized?.Slot, Is.EqualTo(1));
        Assert.That(deserialized?.Temp, Is.EqualTo(45.5f).Within(0.1f));
    }

    [Test]
    public void GpuStats_Serialization_OmitsNullValues()
    {
        // Arrange
        var stats = new GpuStats
        {
            Temperature = 75.0f
            // All other properties are null
        };

        // Act
        var json = JsonSerializer.Serialize(stats, JsonOptions);

        // Assert - JSON should not contain null properties due to JsonIgnore(WhenWritingNull)
        Assert.That(json, Does.Contain("temperature"));
        Assert.That(json, Does.Not.Contain("memJunctionTemp"));
        Assert.That(json, Does.Not.Contain("power"));
    }

    #endregion
}
