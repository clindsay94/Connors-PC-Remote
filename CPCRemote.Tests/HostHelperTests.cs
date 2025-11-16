using System.Runtime.Versioning;

using CPCRemote.Core.Interfaces;

using Moq;

using NUnit.Framework;

namespace CPCRemote.Tests; 
/// <summary>
/// Tests for HostHelper functionality
/// </summary>
[TestFixture]
[SupportedOSPlatform("windows10.0.22621.0")]
public class HostHelperTests {
    private Mock<ITrayCommandHelper> _mockTrayCommandHelper = null!;
    private Core.Helpers.HostHelper _hostHelper = null!;

    /// <summary>
    /// Initialize test setup
    /// </summary>
    [SetUp]
    public void Init() {
        _mockTrayCommandHelper = new Mock<ITrayCommandHelper>();
        _hostHelper = new Core.Helpers.HostHelper(_mockTrayCommandHelper.Object)
        {
            DefaultCommand = Core.Enums.TrayCommandType.Shutdown
        };
    }

    /// <summary>
    /// Tests that commands are executed correctly without secret
    /// </summary>
    [Test]
    [TestCase("shutdown", 3)]
    [TestCase("restart", 1)]
    [TestCase("lock", 5)]
    [TestCase("uefi-reboot", 6)]
    [TestCase("turn-screen-off", 2)]
    [TestCase("force-shutdown", 4)]
    public async Task ProcessRequestAsync_NoSecret_RunsCorrectCommand(
        string commandText,
        int expectedCommandValue) {
        var expectedCommand = (Core.Enums.TrayCommandType)expectedCommandValue;

        _mockTrayCommandHelper
            .Setup(x => x.GetCommandType(commandText))
            .Returns((Core.Enums.TrayCommandType?)expectedCommand);

        await _hostHelper.ProcessRequestAsync($"/{commandText}")
            .ConfigureAwait(false);

        _mockTrayCommandHelper.Verify(x => x.RunCommand(expectedCommand), Times.Once);
    }

    /// <summary>
    /// Tests that commands work with valid secret
    /// </summary>
    [Test]
    [TestCase("shutdown", (int)Core.Enums.TrayCommandType.Shutdown)]
    [TestCase("restart", (int)Core.Enums.TrayCommandType.Restart)]
    public async Task ProcessRequestAsync_WithSecret_RunsCorrectCommand_IfSecretMatches(
        string commandText,
        int expectedCommandValue) {
        _hostHelper.SecretCode = "abc";
        var expectedCommand = (Core.Enums.TrayCommandType)expectedCommandValue;
        _mockTrayCommandHelper
            .Setup(x => x.GetCommandType(commandText))
            .Returns((Core.Enums.TrayCommandType?)expectedCommand);

        await _hostHelper.ProcessRequestAsync($"/abc/{commandText}")
            .ConfigureAwait(false);

        _mockTrayCommandHelper.Verify(x => x.RunCommand(expectedCommand), Times.Once);
    }

    /// <summary>
    /// Tests that wrong secret is ignored
    /// </summary>
    [Test]
    public async Task ProcessRequestAsync_WithSecret_IgnoresIfSecretMismatch() {
        _hostHelper.SecretCode = "abc";
        await _hostHelper.ProcessRequestAsync("/wrong/shutdown")
            .ConfigureAwait(false);
        _mockTrayCommandHelper.Verify(x => x.RunCommand(It.IsAny<Core.Enums.TrayCommandType>()),
                       Times.Never);
    }

    /// <summary>
    /// Tests that empty requests are ignored
    /// </summary>
    [Test]
    public async Task ProcessRequestAsync_Empty_Ignored() {
        await _hostHelper.ProcessRequestAsync("").ConfigureAwait(false);
        _mockTrayCommandHelper.Verify(x => x.RunCommand(It.IsAny<Core.Enums.TrayCommandType>()),
                       Times.Never);
    }
}
// namespace CPCRemote.Tests
