using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;

using CPCRemote.Core.Enums;
using CPCRemote.Core.Helpers;
using CPCRemote.Core.Interfaces;
using CPCRemote.Core.Models;

using NUnit.Framework;

namespace CPCRemote.Tests;

/// <summary>
/// Verifies the consolidated command catalog implementation.
/// </summary>
[TestFixture]
[SupportedOSPlatform("windows10.0.22621.0")]
public class TrayCommandTests
{
    private ICommandCatalog _commandCatalog = null!;

    [SetUp]
    public void Setup()
    {
        CommandHelper helper = new();
        _commandCatalog = helper;
    }

    [Test]
    [TestCase(TrayCommandType.Shutdown, "Shutdown")]
    [TestCase(TrayCommandType.Restart, "Restart")]
    [TestCase(TrayCommandType.TurnScreenOff, "Turn screen off")]
    [TestCase(TrayCommandType.ForceShutdown, "Force Shutdown")]
    [TestCase(TrayCommandType.Lock, "Lock")]
    [TestCase(TrayCommandType.UEFIReboot, "UEFI Reboot")]
    public void GetText_KnownCommandType_ReturnsDisplayName(TrayCommandType commandType, string expectedText)
    {
        string? actual = _commandCatalog.GetText(commandType);
        Assert.That(actual, Is.EqualTo(expectedText));
    }

    [Test]
    public void GetText_UnknownCommandType_ReturnsNull()
    {
        TrayCommandType invalidType = (TrayCommandType)999;
        string? actual = _commandCatalog.GetText(invalidType);
        Assert.That(actual, Is.Null);
    }

    [Test]
    [TestCase("Shutdown", TrayCommandType.Shutdown)]
    [TestCase("restart", TrayCommandType.Restart)]
    [TestCase("TURN SCREEN OFF", TrayCommandType.TurnScreenOff)]
    [TestCase("Force Shutdown", TrayCommandType.ForceShutdown)]
    [TestCase("Lock", TrayCommandType.Lock)]
    [TestCase("UEFI Reboot", TrayCommandType.UEFIReboot)]
    public void GetCommandType_KnownName_ReturnsCommand(string commandName, TrayCommandType expected)
    {
        TrayCommandType? actual = _commandCatalog.GetCommandType(commandName);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    [TestCase("NonExistentCommand")]
    [TestCase("ShutDownNow")]
    [TestCase("")]
    public void GetCommandType_UnknownName_ReturnsNull(string commandName)
    {
        TrayCommandType? actual = _commandCatalog.GetCommandType(commandName);
        Assert.That(actual, Is.Null);
    }

    [Test]
    public void Commands_WhenRequested_ReturnsImmutableCatalog()
    {
        IReadOnlyList<TrayCommand> commands = _commandCatalog.Commands;

        Assert.That(commands, Is.Not.Null);
        Assert.That(commands.Count, Is.EqualTo(6));

        Assert.Multiple(() =>
        {
            Assert.That(commands.Any(c => c.CommandType == TrayCommandType.Shutdown && c.Name == "Shutdown"));
            Assert.That(commands.Any(c => c.CommandType == TrayCommandType.Restart && c.Name == "Restart"));
            Assert.That(commands.Any(c => c.CommandType == TrayCommandType.TurnScreenOff && c.Name == "Turn screen off"));
            Assert.That(commands.Any(c => c.CommandType == TrayCommandType.ForceShutdown && c.Name == "Force Shutdown"));
            Assert.That(commands.Any(c => c.CommandType == TrayCommandType.Lock && c.Name == "Lock"));
            Assert.That(commands.Any(c => c.CommandType == TrayCommandType.UEFIReboot && c.Name == "UEFI Reboot"));
        });
    }
}
