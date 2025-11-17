namespace CPCRemote.Core.Helpers;

using System;
using System.Threading;
using System.Threading.Tasks;

using CPCRemote.Core.Enums;
using CPCRemote.Core.Interfaces;

/// <summary>
/// Validates inbound remote requests and dispatches commands through the catalog/executor abstractions.
/// </summary>
public sealed class HostHelper
{
    private readonly ICommandCatalog _commandCatalog;
    private readonly ICommandExecutor _commandExecutor;

    /// <summary>
    /// Initializes a new instance of the <see cref="HostHelper"/> class.
    /// </summary>
    /// <param name="commandCatalog">Catalog used to resolve command metadata.</param>
    /// <param name="commandExecutor">Executor responsible for issuing OS power commands.</param>
    public HostHelper(ICommandCatalog commandCatalog, ICommandExecutor commandExecutor)
    {
        _commandCatalog = commandCatalog ?? throw new ArgumentNullException(nameof(commandCatalog));
        _commandExecutor = commandExecutor ?? throw new ArgumentNullException(nameof(commandExecutor));
    }

    /// <summary>
    /// Gets or sets the fallback command executed when no explicit match is found.
    /// </summary>
    public TrayCommandType DefaultCommand { get; set; }

    /// <summary>
    /// Gets or sets the optional shared secret required before processing commands.
    /// </summary>
    public string? SecretCode { get; set; }

    /// <summary>
    /// Processes the remote request asynchronously without a cancellation token.
    /// </summary>
    /// <param name="request">The inbound request path (e.g., "/shutdown" or "/secret/shutdown").</param>
    /// <returns>A task that completes when processing finishes.</returns>
    public Task ProcessRequestAsync(string request)
    {
        return ProcessRequestAsync(request, CancellationToken.None);
    }

    /// <summary>
    /// Processes the remote request asynchronously, honoring cooperative cancellation.
    /// </summary>
    /// <param name="request">The inbound request path.</param>
    /// <param name="cancellationToken">Token propagated from the host to stop execution.</param>
    /// <returns>A task that completes when processing finishes.</returns>
    public async Task ProcessRequestAsync(string request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request) || !request.StartsWith('/'))
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        string[] segments = request[1..].Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return;
        }

        string? providedSecret = segments.Length > 1 ? segments[0] : null;
        string commandText = segments.Length > 1 ? segments[1] : segments[0];

        if (!string.IsNullOrEmpty(SecretCode) &&
            !string.Equals(providedSecret, SecretCode, StringComparison.Ordinal))
        {
            return;
        }

        TrayCommandType? resolvedCommand = _commandCatalog.GetCommandType(commandText);
        if (resolvedCommand.HasValue)
        {
            await _commandExecutor.RunCommandAsync(resolvedCommand.Value, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (DefaultCommand != TrayCommandType.None)
        {
            await _commandExecutor.RunCommandAsync(DefaultCommand, cancellationToken).ConfigureAwait(false);
        }
    }
}
