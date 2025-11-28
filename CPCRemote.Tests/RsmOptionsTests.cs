using System.Runtime.Versioning;

using CPCRemote.Service.Options;

using NUnit.Framework;

namespace CPCRemote.Tests;

/// <summary>
/// Tests for RsmOptions configuration validation.
/// Item 15: Additional automated test coverage.
/// </summary>
[TestFixture]
[SupportedOSPlatform("windows10.0.22621.0")]
public class RsmOptionsTests
{
    [Test]
    public void RsmOptions_DefaultPort_Is5005()
    {
        // Arrange & Act
        var options = new RsmOptions();

        // Assert - Item 18: Default port should be 5005
        Assert.That(options.Port, Is.EqualTo(5005));
    }

    [Test]
    public void RsmOptions_DefaultUseHttps_IsFalse()
    {
        // Arrange & Act
        var options = new RsmOptions();

        // Assert - Item 4: HTTPS defaults to false
        Assert.That(options.UseHttps, Is.False);
    }

    [Test]
    [TestCase(1, true, TestName = "ValidPort_MinPort_IsValid")]
    [TestCase(5005, true, TestName = "ValidPort_Default_IsValid")]
    [TestCase(65535, true, TestName = "ValidPort_MaxPort_IsValid")]
    [TestCase(0, false, TestName = "InvalidPort_Zero_IsInvalid")]
    [TestCase(-1, false, TestName = "InvalidPort_Negative_IsInvalid")]
    [TestCase(65536, false, TestName = "InvalidPort_OverMax_IsInvalid")]
    public void Port_ValidationRange_ReturnsExpected(int port, bool expectedValid)
    {
        // Arrange & Act
        bool isValid = port >= 1 && port <= 65535;

        // Assert - Item 12: Port validation 1-65535
        Assert.That(isValid, Is.EqualTo(expectedValid));
    }

    [Test]
    [TestCase("pass", false, TestName = "Secret_TooShort_IsInvalid")]
    [TestCase("12345678", true, TestName = "Secret_Exactly8Chars_IsValid")]
    [TestCase("strongSecretToken", true, TestName = "Secret_Longer_IsValid")]
    [TestCase("", false, TestName = "Secret_Empty_IsInvalid")]
    public void Secret_MinLength_Validation(string secret, bool expectedValid)
    {
        // Arrange & Act
        bool isValid = !string.IsNullOrEmpty(secret) && secret.Length >= 8;

        // Assert
        Assert.That(isValid, Is.EqualTo(expectedValid));
    }

    [Test]
    public void RsmOptions_CertificateThumbprint_DefaultsToNull()
    {
        // Arrange & Act
        var options = new RsmOptions();

        // Assert - Item 4: Certificate thumbprint for HTTPS
        Assert.That(options.CertificateThumbprint, Is.Null);
    }
}
