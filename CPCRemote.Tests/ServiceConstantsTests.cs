using System.Runtime.Versioning;

using CPCRemote.Core.Constants;

using NUnit.Framework;

namespace CPCRemote.Tests;

/// <summary>
/// Tests for ServiceConstants shared across the solution.
/// Item 1: Unified Windows service name.
/// Item 15: Additional automated test coverage.
/// </summary>
[TestFixture]
[SupportedOSPlatform("windows10.0.22621.0")]
public class ServiceConstantsTests
{
    [Test]
    public void RemoteShutdownServiceName_IsValid_SCMFormat()
    {
        // Arrange & Act
        string serviceName = ServiceConstants.RemoteShutdownServiceName;

        // Assert - Item 1: Service name should be valid for SCM
        Assert.That(serviceName, Is.EqualTo("CPCRemote.Service"));
        Assert.That(serviceName, Is.Not.Empty);
        Assert.That(serviceName, Does.Not.Contain(" ")); // SCM doesn't like spaces in service names
    }

    [Test]
    public void RemoteShutdownServiceName_MaxLength_Within256Chars()
    {
        // SCM service names have a max length of 256 characters
        string serviceName = ServiceConstants.RemoteShutdownServiceName;

        Assert.That(serviceName.Length, Is.LessThanOrEqualTo(256));
    }

    [Test]
    public void RemoteShutdownServiceName_ContainsOnlyValidChars()
    {
        // Service names can only contain alphanumeric characters, periods, and dashes
        string serviceName = ServiceConstants.RemoteShutdownServiceName;

        Assert.That(serviceName, Does.Match(@"^[a-zA-Z0-9.\-]+$"));
    }
}
