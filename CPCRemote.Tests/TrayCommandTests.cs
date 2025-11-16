using CPCRemote.Core.Enums;
using CPCRemote.Core.Helpers;
using CPCRemote.Core.Models;

using NUnit.Framework;

using System.Runtime.Versioning;

namespace CPCRemote.Tests;

/// <summary>
/// Defines the <see cref="TrayCommandTests" />
/// </summary>
[TestFixture]
[SupportedOSPlatform("windows10.0.22621.0")] // Add this attribute to the class to ensure all usages of Windows-only APIs are guarded
public class TrayCommandTests
{
    /// <summary>
    /// Defines the _trayCommandHelper
    /// </summary>
    private TrayCommandHelper _trayCommandHelper = null!;

    /// <summary>
    /// The Setup
    /// </summary>
    [SetUp] public void Setup() { _trayCommandHelper = new TrayCommandHelper(); }

    /// <summary>
    /// The GetText_Returns_Correct_String_For_Known_CommandType
    /// </summary>
    /// <param name="commandType">The commandType<see
    /// cref="TrayCommandType"/></param> <param name="expectedText">The
    /// expectedText<see cref="string"/></param>
    [Test]
    [TestCase(TrayCommandType.Shutdown, "Shutdown")]
    [TestCase(TrayCommandType.Restart, "Restart")]
    [TestCase(TrayCommandType.TurnScreenOff, "Turn screen off")]
    [TestCase(TrayCommandType.ForceShutdown, "Force Shutdown")]
    [TestCase(TrayCommandType.Lock, "Lock")]
    [TestCase(TrayCommandType.UEFIReboot, "UEFI Reboot")]
    public void
          GetText_Returns_Correct_String_For_Known_CommandType(
              TrayCommandType commandType,
              string expectedText)
    {
        string actualText = _trayCommandHelper.GetText(commandType);
        Assert.That(actualText, Is.EqualTo(expectedText));
    }

    /// <summary>
    /// The GetText_Returns_Empty_String_For_Unknown_CommandType
    /// </summary>
    [Test]
    public void GetText_Returns_Empty_String_For_Unknown_CommandType()
    {
        TrayCommandType unknownCommandType = (TrayCommandType)999;
        string actualText = _trayCommandHelper.GetText(unknownCommandType);
        Assert.That(actualText, Is.Empty);
    }

    /// <summary>
    /// The GetCommandType_Returns_Correct_CommandType_For_Known_Name
    /// </summary>
    /// <param name="commandName">The commandName<see cref="string"/></param>
    /// <param name="expectedCommandType">The expectedCommandType<see
    /// cref="TrayCommandType"/></param>
    [Test]
    [TestCase("Shutdown", TrayCommandType.Shutdown)]
    [TestCase("restart", TrayCommandType.Restart)]  // Test case insensitivity
    [TestCase("TURN SCREEN OFF",
                  TrayCommandType.TurnScreenOff)]  // Test case insensitivity
    [TestCase("Force Shutdown", TrayCommandType.ForceShutdown)]
    [TestCase("Lock", TrayCommandType.Lock)]
    [TestCase("UEFI Reboot", TrayCommandType.UEFIReboot)]
    public void
        GetCommandType_Returns_Correct_CommandType_For_Known_Name(
            string commandName,
            TrayCommandType expectedCommandType)
    {
        TrayCommandType? actualCommandType =
                              _trayCommandHelper.GetCommandType(commandName);
        Assert.That(actualCommandType, Is.EqualTo(expectedCommandType));
    }

    /// <summary>
    /// The GetCommandType_Returns_Null_For_Unknown_Name
    /// </summary>
    /// <param name="commandName">The commandName<see cref="string"/></param>
    [Test]
    [TestCase("NonExistentCommand")]
    [TestCase("ShutDownNow")]
    [TestCase("")]
    public void
          GetCommandType_Returns_Null_For_Unknown_Name(string commandName)
    {
        TrayCommandType? actualCommandType =
                              _trayCommandHelper.GetCommandType(commandName);
        Assert.That(actualCommandType, Is.Null);
    }

    /// <summary>
    /// The Commands_Property_Returns_Expected_Commands
    /// </summary>
    [Test]
    [SupportedOSPlatform("windows10.0.22621.0")] // Add this attribute to restrict test to supported platform
    public void Commands_Property_Returns_Expected_Commands()
    {
        TrayCommand[] commands = _trayCommandHelper.Commands;

        Assert.That(commands, Is.Not.Null);
        Assert.That(commands,
                    Has.Length.EqualTo(6));  // Based on the current implementation

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                commands.Any(c => c.CommandType == TrayCommandType.Shutdown &&
                                 c.Name == "Shutdown"),
                Is.True);
            Assert.That(commands.Any(c => c.CommandType == TrayCommandType.Restart &&
                                         c.Name == "Restart"),
                        Is.True);
            Assert.That(
                commands.Any(c => c.CommandType == TrayCommandType.TurnScreenOff &&
                                 c.Name == "Turn screen off"),
                Is.True);
            Assert.That(
                commands.Any(c => c.CommandType == TrayCommandType.ForceShutdown &&
                                 c.Name == "Force Shutdown"),
                Is.True);
            Assert.That(commands.Any(c => c.CommandType == TrayCommandType.Lock &&
                                         c.Name == "Lock"),
                        Is.True);
            Assert.That(
                commands.Any(c => c.CommandType == TrayCommandType.UEFIReboot &&
                                 c.Name == "UEFI Reboot"),
                Is.True);
        }
    }
}
// namespace CPCRemote.Tests
