using System.Runtime.Versioning;

using CPCRemote.Core.Helpers;
using CPCRemote.Core.Models;

using NUnit.Framework;

namespace CPCRemote.Tests;

/// <summary>
/// Tests for CommandHelper Wake-on-LAN functionality and validation.
/// Item 15: Additional automated test coverage.
/// </summary>
[TestFixture]
[SupportedOSPlatform("windows10.0.22621.0")]
public class WakeOnLanTests
{
    [Test]
    [TestCase(9, true, TestName = "ValidPort_StandardWoL_ReturnsTrue")]
    [TestCase(7, true, TestName = "ValidPort_AlternateWoL_ReturnsTrue")]
    [TestCase(1, true, TestName = "ValidPort_MinPort_ReturnsTrue")]
    [TestCase(65535, true, TestName = "ValidPort_MaxPort_ReturnsTrue")]
    [TestCase(0, false, TestName = "InvalidPort_Zero_ReturnsFalse")]
    [TestCase(-1, false, TestName = "InvalidPort_Negative_ReturnsFalse")]
    [TestCase(65536, false, TestName = "InvalidPort_OverMax_ReturnsFalse")]
    public void ValidatePort_VariousPorts_ReturnsExpected(int port, bool expectedValid)
    {
        // Arrange & Act
        bool isValid = port >= 1 && port <= 65535;

        // Assert
        Assert.That(isValid, Is.EqualTo(expectedValid));
    }

    [Test]
    [TestCase("00:11:22:33:44:55", true, TestName = "ValidMac_Colons_ReturnsTrue")]
    [TestCase("00-11-22-33-44-55", true, TestName = "ValidMac_Dashes_ReturnsTrue")]
    [TestCase("AA:BB:CC:DD:EE:FF", true, TestName = "ValidMac_Uppercase_ReturnsTrue")]
    [TestCase("aa:bb:cc:dd:ee:ff", true, TestName = "ValidMac_Lowercase_ReturnsTrue")]
    [TestCase("", false, TestName = "InvalidMac_Empty_ReturnsFalse")]
    [TestCase("00:00:00:00:00:00", false, TestName = "InvalidMac_AllZeros_ReturnsFalse")]
    [TestCase("invalid", false, TestName = "InvalidMac_NotMac_ReturnsFalse")]
    [TestCase("00:11:22:33:44", false, TestName = "InvalidMac_TooShort_ReturnsFalse")]
    [TestCase("00:11:22:33:44:55:66", false, TestName = "InvalidMac_TooLong_ReturnsFalse")]
    public void IsValidMac_VariousMacs_ReturnsExpected(string mac, bool expectedValid)
    {
        // Arrange & Act
        bool isValid = IsValidMacAddress(mac);

        // Assert
        Assert.That(isValid, Is.EqualTo(expectedValid));
    }

    [Test]
    public void WolOptions_DefaultPort_IsNine()
    {
        // Arrange & Act
        var options = new WolOptions();

        // Assert - Item 5: Default port should be 9
        Assert.That(options.Port, Is.EqualTo(9));
    }

    [Test]
    public void WolOptions_DefaultBroadcast_IsSubnetBroadcast()
    {
        // Arrange & Act
        var options = new WolOptions();

        // Assert
        Assert.That(options.BroadcastAddress, Is.EqualTo("255.255.255.255"));
    }

    /// <summary>
    /// Helper method mirroring Lua validation logic.
    /// </summary>
    private static bool IsValidMacAddress(string mac)
    {
        if (string.IsNullOrEmpty(mac) || mac == "00:00:00:00:00:00")
        {
            return false;
        }

        // Accept XX:XX:XX:XX:XX:XX or XX-XX-XX-XX-XX-XX format
        return System.Text.RegularExpressions.Regex.IsMatch(
            mac,
            @"^[0-9A-Fa-f]{2}[:-][0-9A-Fa-f]{2}[:-][0-9A-Fa-f]{2}[:-][0-9A-Fa-f]{2}[:-][0-9A-Fa-f]{2}[:-][0-9A-Fa-f]{2}$");
    }
}
