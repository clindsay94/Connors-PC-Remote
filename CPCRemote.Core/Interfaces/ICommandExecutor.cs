namespace CPCRemote.Core.Interfaces;

using System.Threading;
using System.Threading.Tasks;

using CPCRemote.Core.Enums;

/// <summary>
/// Executes power-management commands on the local machine.
/// </summary>
public interface ICommandExecutor
{
    /// <summary>
    /// Executes the specified command type immediately on the local host.
    /// </summary>
    /// <param name="commandType">The command to run.</param>
    void RunCommand(TrayCommandType commandType);

    /// <summary>
    /// Resolves and executes a command based on either <see cref="TrayCommandType"/> or its textual name.
    /// </summary>
    /// <param name="value">A recognized command identifier (enum value or string).</param>
    void RunCommandByName(object value);

    /// <summary>
    /// Executes the specified command type asynchronously, honoring the supplied cancellation token.
    /// </summary>
    /// <param name="commandType">The command to run.</param>
    /// <param name="cancellationToken">Token used to observe cancellation.</param>
    /// <returns>A task that completes when the command has been issued.</returns>
    Task RunCommandAsync(TrayCommandType commandType, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves and executes a command asynchronously based on either <see cref="TrayCommandType"/> or its textual name.
    /// </summary>
    /// <param name="value">A recognized command identifier (enum value or string).</param>
    /// <param name="cancellationToken">Token used to observe cancellation.</param>
    /// <returns>A task that completes when the command has been issued.</returns>
    Task RunCommandByNameAsync(object value, CancellationToken cancellationToken);
}