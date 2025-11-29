using System;
using System.IO;
using System.Runtime.Versioning;

using CPCRemote.Core.Helpers;

using NUnit.Framework;

namespace CPCRemote.Tests;

/// <summary>
/// Unit tests for the <see cref="ConfigurationPaths"/> class.
/// Tests path generation and directory creation for configuration files.
/// </summary>
[TestFixture]
[SupportedOSPlatform("windows10.0.22621.0")]
public class ConfigurationPathsTests
{
    #region ServiceDataPath Tests

    [Test]
    public void ServiceDataPath_ReturnsPathInProgramData()
    {
        // Arrange
        string expectedBase = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        // Act
        string path = ConfigurationPaths.ServiceDataPath;

        // Assert
        Assert.That(path, Does.StartWith(expectedBase));
        Assert.That(path, Does.Contain(ConfigurationPaths.AppName));
    }

    [Test]
    public void ServiceDataPath_CreatesDirectoryIfNotExists()
    {
        // Act
        string path = ConfigurationPaths.ServiceDataPath;

        // Assert
        Assert.That(Directory.Exists(path), Is.True,
            $"Directory should be created: {path}");
    }

    [Test]
    public void ServiceDataPath_ReturnsConsistentPath()
    {
        // Act
        string path1 = ConfigurationPaths.ServiceDataPath;
        string path2 = ConfigurationPaths.ServiceDataPath;

        // Assert
        Assert.That(path1, Is.EqualTo(path2));
    }

    #endregion

    #region UserDataPath Tests

    [Test]
    public void UserDataPath_ReturnsPathInLocalAppData()
    {
        // Arrange
        string expectedBase = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Act
        string path = ConfigurationPaths.UserDataPath;

        // Assert
        Assert.That(path, Does.StartWith(expectedBase));
        Assert.That(path, Does.Contain(ConfigurationPaths.AppName));
    }

    [Test]
    public void UserDataPath_CreatesDirectoryIfNotExists()
    {
        // Act
        string path = ConfigurationPaths.UserDataPath;

        // Assert
        Assert.That(Directory.Exists(path), Is.True,
            $"Directory should be created: {path}");
    }

    [Test]
    public void UserDataPath_ReturnsConsistentPath()
    {
        // Act
        string path1 = ConfigurationPaths.UserDataPath;
        string path2 = ConfigurationPaths.UserDataPath;

        // Assert
        Assert.That(path1, Is.EqualTo(path2));
    }

    #endregion

    #region GetServiceConfigPath Tests

    [Test]
    public void GetServiceConfigPath_ReturnsPathInServiceDataDirectory()
    {
        // Arrange
        const string fileName = "test-config.json";

        // Act
        string path = ConfigurationPaths.GetServiceConfigPath(fileName);

        // Assert
        Assert.That(path, Does.StartWith(ConfigurationPaths.ServiceDataPath));
        Assert.That(path, Does.EndWith(fileName));
    }

    [Test]
    public void GetServiceConfigPath_ThrowsOnNullFileName()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ConfigurationPaths.GetServiceConfigPath(null!));
    }

    [Test]
    public void GetServiceConfigPath_ThrowsOnEmptyFileName()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            ConfigurationPaths.GetServiceConfigPath(string.Empty));
    }

    [Test]
    public void GetServiceConfigPath_ThrowsOnWhitespaceFileName()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            ConfigurationPaths.GetServiceConfigPath("   "));
    }

    #endregion

    #region GetUserConfigPath Tests

    [Test]
    public void GetUserConfigPath_ReturnsPathInUserDataDirectory()
    {
        // Arrange
        const string fileName = "user-settings.json";

        // Act
        string path = ConfigurationPaths.GetUserConfigPath(fileName);

        // Assert
        Assert.That(path, Does.StartWith(ConfigurationPaths.UserDataPath));
        Assert.That(path, Does.EndWith(fileName));
    }

    [Test]
    public void GetUserConfigPath_ThrowsOnNullFileName()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ConfigurationPaths.GetUserConfigPath(null!));
    }

    #endregion

    #region ApplicationDirectory Tests

    [Test]
    public void ApplicationDirectory_ReturnsNonNullPath()
    {
        // Act
        string path = ConfigurationPaths.ApplicationDirectory;

        // Assert
        Assert.That(path, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void ApplicationDirectory_ReturnsExistingDirectory()
    {
        // Act
        string path = ConfigurationPaths.ApplicationDirectory;

        // Assert
        Assert.That(Directory.Exists(path), Is.True);
    }

    #endregion

    #region IsPackagedApp Tests

    [Test]
    public void IsPackagedApp_ReturnsBoolean()
    {
        // Act
        bool result = ConfigurationPaths.IsPackagedApp();

        // Assert - Just verify it returns without throwing
        // The actual value depends on how tests are run
        Assert.That(result, Is.TypeOf<bool>());
    }

    #endregion

    #region EnsureServiceConfigExists Tests

    [Test]
    public void EnsureServiceConfigExists_ReturnsPathInServiceDataDirectory()
    {
        // Arrange
        const string fileName = "test-ensure.json";

        // Act
        string path = ConfigurationPaths.EnsureServiceConfigExists(fileName);

        // Assert
        Assert.That(path, Does.StartWith(ConfigurationPaths.ServiceDataPath));
        Assert.That(path, Does.EndWith(fileName));
    }

    [Test]
    public void EnsureServiceConfigExists_ThrowsOnNullFileName()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ConfigurationPaths.EnsureServiceConfigExists(null!));
    }

    #endregion

    #region AppName Tests

    [Test]
    public void AppName_IsExpectedValue()
    {
        // Assert
        Assert.That(ConfigurationPaths.AppName, Is.EqualTo("CPCRemote"));
    }

    #endregion
}
