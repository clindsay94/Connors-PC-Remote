using System.Runtime.Versioning;

using CPCRemote.Core.IPC;

using NUnit.Framework;

namespace CPCRemote.Tests;

/// <summary>
/// Unit tests for Named Pipe client interface contracts and IPC message types.
/// Tests interface definitions and message contracts.
/// </summary>
/// <remarks>
/// Note: The NamedPipeClient implementation is in the UI project which has complex
/// dependencies. These tests focus on the interface contracts defined in Core.
/// </remarks>
[TestFixture]
[SupportedOSPlatform("windows10.0.22621.0")]
public class NamedPipeClientTests
{
    #region IPipeClient Interface Tests

    [Test]
    public void IPipeClient_Interface_DefinesConnectAsync()
    {
        // Verify the interface has the expected method signature
        var method = typeof(IPipeClient).GetMethod(nameof(IPipeClient.ConnectAsync));
        
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<bool>)));
    }

    [Test]
    public void IPipeClient_Interface_DefinesDisconnectAsync()
    {
        // Verify the interface has the expected method signature
        var method = typeof(IPipeClient).GetMethod(nameof(IPipeClient.DisconnectAsync));
        
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task)));
    }

    [Test]
    public void IPipeClient_Interface_DefinesIsConnected()
    {
        // Verify the interface has the expected property
        var property = typeof(IPipeClient).GetProperty(nameof(IPipeClient.IsConnected));
        
        Assert.That(property, Is.Not.Null);
        Assert.That(property!.PropertyType, Is.EqualTo(typeof(bool)));
    }

    [Test]
    public void IPipeClient_Interface_DefinedSendRequestAsync()
    {
        // Verify the interface has the SendRequestAsync method
        var methods = typeof(IPipeClient).GetMethods()
            .Where(m => m.Name == nameof(IPipeClient.SendRequestAsync));
        
        Assert.That(methods, Is.Not.Empty);
    }

    #endregion

    #region IPipeServer Interface Tests

    [Test]
    public void IPipeServer_Interface_DefinesIsRunning()
    {
        // Verify the interface has the expected property
        var property = typeof(IPipeServer).GetProperty(nameof(IPipeServer.IsRunning));
        
        Assert.That(property, Is.Not.Null);
        Assert.That(property!.PropertyType, Is.EqualTo(typeof(bool)));
    }

    [Test]
    public void IPipeServer_Interface_DefinesStartAsync()
    {
        // Verify the interface has the expected method signature
        var method = typeof(IPipeServer).GetMethod(nameof(IPipeServer.StartAsync));
        
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task)));
    }

    [Test]
    public void IPipeServer_Interface_DefinesStopAsync()
    {
        // Verify the interface has the expected method signature
        var method = typeof(IPipeServer).GetMethod(nameof(IPipeServer.StopAsync));
        
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task)));
    }

    #endregion

    #region Request/Response Contract Tests

    [Test]
    public void IpcRequest_HasCorrelationId()
    {
        // Arrange
        var request = new GetStatsRequest();

        // Act
        var correlationId = Guid.NewGuid().ToString();
        request = request with { CorrelationId = correlationId };

        // Assert
        Assert.That(request.CorrelationId, Is.EqualTo(correlationId));
    }

    [Test]
    public void IpcResponse_HasSuccessProperty()
    {
        // Arrange
        var response = new GetStatsResponse { Success = true };

        // Assert
        Assert.That(response.Success, Is.True);
    }

    [Test]
    public void ErrorResponse_HasErrorDetails()
    {
        // Arrange
        var response = new ErrorResponse
        {
            Success = false,
            ErrorMessage = "Connection failed",
            ExceptionType = "TimeoutException"
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Success, Is.False);
            Assert.That(response.ErrorMessage, Is.EqualTo("Connection failed"));
            Assert.That(response.ExceptionType, Is.EqualTo("TimeoutException"));
        });
    }

    #endregion

    #region Message Type Hierarchy Tests

    [Test]
    public void GetStatsRequest_InheritsFromIpcRequest()
    {
        // Assert
        Assert.That(typeof(GetStatsRequest).BaseType, Is.EqualTo(typeof(IpcRequest)));
    }

    [Test]
    public void GetStatsResponse_InheritsFromIpcResponse()
    {
        // Assert
        Assert.That(typeof(GetStatsResponse).BaseType, Is.EqualTo(typeof(IpcResponse)));
    }

    [Test]
    public void ServiceStatusRequest_InheritsFromIpcRequest()
    {
        // Assert
        Assert.That(typeof(ServiceStatusRequest).BaseType, Is.EqualTo(typeof(IpcRequest)));
    }

    [Test]
    public void ServiceStatusResponse_InheritsFromIpcResponse()
    {
        // Assert
        Assert.That(typeof(ServiceStatusResponse).BaseType, Is.EqualTo(typeof(IpcResponse)));
    }

    [Test]
    public void LaunchAppRequest_InheritsFromIpcRequest()
    {
        // Assert
        Assert.That(typeof(LaunchAppRequest).BaseType, Is.EqualTo(typeof(IpcRequest)));
    }

    [Test]
    public void LaunchAppResponse_InheritsFromIpcResponse()
    {
        // Assert
        Assert.That(typeof(LaunchAppResponse).BaseType, Is.EqualTo(typeof(IpcResponse)));
    }

    #endregion

    #region Timeout and Connection Constants Tests

    [Test]
    public void IpcConstants_HasReasonableDefaults()
    {
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(IpcConstants.BufferSize, Is.GreaterThanOrEqualTo(1024), "Buffer size too small");
            Assert.That(IpcConstants.MaxMessageSize, Is.GreaterThanOrEqualTo(IpcConstants.BufferSize), 
                "Max message size should be >= buffer size");
            Assert.That(IpcConstants.DefaultTimeout.TotalSeconds, Is.InRange(1, 300), 
                "Default timeout should be reasonable");
            Assert.That(IpcConstants.DefaultConnectTimeout.TotalSeconds, Is.InRange(1, 60), 
                "Connect timeout should be reasonable");
        });
    }

    #endregion
}
