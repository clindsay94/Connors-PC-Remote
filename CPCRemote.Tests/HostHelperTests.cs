using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using CPCRemote.Core.Enums;
using CPCRemote.Core.Interfaces;

using Moq;

using NUnit.Framework;

namespace CPCRemote.Tests;

/// <summary>
/// Tests for <see cref="CPCRemote.Core.Helpers.HostHelper"/> functionality.
/// </summary>
[TestFixture]
[SupportedOSPlatform("windows10.0.22621.0")]
public class HostHelperTests
{
    private Mock<ICommandCatalog> _mockCommandCatalog = null!;
    private Mock<ICommandExecutor> _mockCommandExecutor = null!;
    private Core.Helpers.HostHelper _hostHelper = null!;

    /// <summary>
    /// Initializes per-test dependencies.
    /// </summary>
    [SetUp]
    public void Init()
    {
        _mockCommandCatalog = new Mock<ICommandCatalog>();
        _mockCommandExecutor = new Mock<ICommandExecutor>();
        _hostHelper = new Core.Helpers.HostHelper(_mockCommandCatalog.Object, _mockCommandExecutor.Object)
        {
            DefaultCommand = TrayCommandType.Shutdown
        };
    }

    /// <summary>
    /// Tests that commands are executed correctly without a secret requirement.
    /// </summary>
    [Test]
    [TestCase("shutdown", 3)]
    [TestCase("restart", 1)]
    [TestCase("lock", 5)]
    [TestCase("uefi-reboot", 6)]
    [TestCase("turn-screen-off", 2)]
    [TestCase("force-shutdown", 4)]
    public async Task ProcessRequestAsync_NoSecret_RunsCorrectCommand(string commandText, int expectedCommandValue)
    {
        TrayCommandType expectedCommand = (TrayCommandType)expectedCommandValue;

        _mockCommandCatalog
            .Setup(x => x.GetCommandType(commandText))
            .Returns((TrayCommandType?)expectedCommand);

        await _hostHelper.ProcessRequestAsync($"/{commandText}").ConfigureAwait(false);

        _mockCommandExecutor.Verify(x => x.RunCommandAsync(expectedCommand, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that commands run when the provided secret matches configuration.
    /// </summary>
    [Test]
    [TestCase("shutdown", (int)TrayCommandType.Shutdown)]
    [TestCase("restart", (int)TrayCommandType.Restart)]
    public async Task ProcessRequestAsync_WithSecret_RunsCorrectCommand_IfSecretMatches(string commandText, int expectedCommandValue)
    {
        _hostHelper.SecretCode = "abc";
        TrayCommandType expectedCommand = (TrayCommandType)expectedCommandValue;

        _mockCommandCatalog
            .Setup(x => x.GetCommandType(commandText))
            .Returns((TrayCommandType?)expectedCommand);

        await _hostHelper.ProcessRequestAsync($"/abc/{commandText}").ConfigureAwait(false);

        _mockCommandExecutor.Verify(x => x.RunCommandAsync(expectedCommand, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that mismatched secrets short-circuit execution.
    /// </summary>
    [Test]
    public async Task ProcessRequestAsync_WithSecret_IgnoresIfSecretMismatch()
    {
        _hostHelper.SecretCode = "abc";

        await _hostHelper.ProcessRequestAsync("/wrong/shutdown").ConfigureAwait(false);

        _mockCommandExecutor.Verify(x => x.RunCommandAsync(It.IsAny<TrayCommandType>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Tests that empty requests are ignored.
    /// </summary>
    [Test]
    public async Task ProcessRequestAsync_Empty_Ignored()
    {
        await _hostHelper.ProcessRequestAsync(string.Empty).ConfigureAwait(false);

        _mockCommandExecutor.Verify(x => x.RunCommandAsync(It.IsAny<TrayCommandType>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
