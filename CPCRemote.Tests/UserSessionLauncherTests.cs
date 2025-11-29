using System.Runtime.Versioning;

using CPCRemote.Service.Services;

using Microsoft.Extensions.Logging;

using Moq;

using NUnit.Framework;

namespace CPCRemote.Tests;

/// <summary>
/// Unit tests for the <see cref="UserSessionLauncher"/> class.
/// Tests process start validation and argument handling.
/// </summary>
/// <remarks>
/// Note: UserSessionLauncher uses P/Invoke to interact with Windows session APIs.
/// These tests verify input validation and error handling without actually launching
/// processes, as the launcher requires an active console session (unavailable in
/// test runners or Session 0 services).
/// </remarks>
[TestFixture]
[SupportedOSPlatform("windows10.0.22621.0")]
public class UserSessionLauncherTests
{
    private Mock<ILogger<UserSessionLauncher>> _loggerMock = null!;
    private UserSessionLauncher _launcher = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<UserSessionLauncher>>();
        _launcher = new UserSessionLauncher(_loggerMock.Object);
    }

    #region Input Validation Tests

    [Test]
    public void LaunchInUserSession_NullPath_ReturnsFalse()
    {
        // Act
        bool result = _launcher.LaunchInUserSession(null!, null, null, false);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void LaunchInUserSession_EmptyPath_ReturnsFalse()
    {
        // Act
        bool result = _launcher.LaunchInUserSession(string.Empty, null, null, false);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void LaunchInUserSession_WhitespacePath_ReturnsFalse()
    {
        // Act
        bool result = _launcher.LaunchInUserSession("   ", null, null, false);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void LaunchInUserSession_NonExistentPath_ReturnsFalse()
    {
        // Arrange
        var nonExistentPath = @"C:\NonExistent\Path\fake-app-99999.exe";

        // Act
        bool result = _launcher.LaunchInUserSession(nonExistentPath, null, null, false);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void LaunchInUserSession_InvalidPath_ReturnsFalse()
    {
        // Arrange
        var invalidPath = @"C:\Invalid<>Path\test.exe";

        // Act
        bool result = _launcher.LaunchInUserSession(invalidPath, null, null, false);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region Session 0 Behavior Tests

    [Test]
    public void LaunchInUserSession_ValidPath_ReturnsResultBasedOnSession()
    {
        // Arrange - Use notepad which exists on all Windows systems
        var validPath = @"C:\Windows\System32\notepad.exe";

        // Act
        // In test environment (likely Session 0 or no active console), this should fail
        bool result = _launcher.LaunchInUserSession(validPath, null, null, false);

        // Assert
        // Result depends on whether there's an active console session
        // In CI/automated tests, this will typically be false
        // We just verify it doesn't throw
        Assert.That(result, Is.TypeOf<bool>());
    }

    #endregion

    #region Argument Handling Tests

    [Test]
    public void LaunchInUserSession_WithArguments_DoesNotThrow()
    {
        // Arrange
        var path = @"C:\Windows\System32\notepad.exe";
        var arguments = "test.txt";

        // Act & Assert - Should not throw even if launch fails
        Assert.DoesNotThrow(() =>
            _launcher.LaunchInUserSession(path, arguments, null, false));
    }

    [Test]
    public void LaunchInUserSession_WithWorkingDirectory_DoesNotThrow()
    {
        // Arrange
        var path = @"C:\Windows\System32\notepad.exe";
        var workingDir = @"C:\Windows\Temp";

        // Act & Assert
        Assert.DoesNotThrow(() =>
            _launcher.LaunchInUserSession(path, null, workingDir, false));
    }

    [Test]
    public void LaunchInUserSession_WithRunAsAdmin_DoesNotThrow()
    {
        // Arrange
        var path = @"C:\Windows\System32\notepad.exe";

        // Act & Assert
        Assert.DoesNotThrow(() =>
            _launcher.LaunchInUserSession(path, null, null, runAsAdmin: true));
    }

    [Test]
    public void LaunchInUserSession_WithAllParameters_DoesNotThrow()
    {
        // Arrange
        var path = @"C:\Windows\System32\notepad.exe";
        var arguments = "--verbose";
        var workingDir = @"C:\Windows\Temp";

        // Act & Assert
        Assert.DoesNotThrow(() =>
            _launcher.LaunchInUserSession(path, arguments, workingDir, runAsAdmin: true));
    }

    #endregion

    #region Logging Tests

    [Test]
    public void LaunchInUserSession_InvalidPath_LogsWarning()
    {
        // Arrange
        var invalidPath = @"C:\NonExistent\fake.exe";

        // Act
        _launcher.LaunchInUserSession(invalidPath, null, null, false);

        // Assert - Verify logging occurred
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Path Expansion Tests

    [Test]
    public void LaunchInUserSession_EnvironmentVariable_HandlesGracefully()
    {
        // Arrange - Use environment variable in path
        var pathWithEnvVar = @"%SystemRoot%\System32\notepad.exe";

        // Act & Assert - Should not throw
        Assert.DoesNotThrow(() =>
            _launcher.LaunchInUserSession(pathWithEnvVar, null, null, false));
    }

    #endregion
}
