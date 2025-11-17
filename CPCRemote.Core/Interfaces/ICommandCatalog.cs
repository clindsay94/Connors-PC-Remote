namespace CPCRemote.Core.Interfaces;

using System.Collections.Generic;

using CPCRemote.Core.Enums;
using CPCRemote.Core.Models;

/// <summary>
/// Provides read-only access to the catalog of tray commands exposed by the domain layer.
/// </summary>
public interface ICommandCatalog
{
    /// <summary>
    /// Gets an immutable view of the available commands in the catalog.
    /// </summary>
    IReadOnlyList<TrayCommand> Commands { get; }

    /// <summary>
    /// Retrieves the human-friendly label for the provided <see cref="TrayCommandType"/>.
    /// </summary>
    /// <param name="commandType">The command identifier being requested.</param>
    /// <returns>The display text if known; otherwise, <c>null</c>.</returns>
    string? GetText(TrayCommandType commandType);

    /// <summary>
    /// Resolves a command type from the supplied command name.
    /// </summary>
    /// <param name="commandName">The textual representation supplied by a client.</param>
    /// <returns>The matching <see cref="TrayCommandType"/> or <c>null</c> when unknown.</returns>
    TrayCommandType? GetCommandType(string commandName);

    /// <summary>
    /// Attempts to resolve a full <see cref="TrayCommand"/> record from the supplied command name.
    /// </summary>
    /// <param name="commandName">The textual representation supplied by a client.</param>
    /// <param name="command">The resolved command metadata, when available.</param>
    /// <returns><c>true</c> when the name maps to a known command; otherwise, <c>false</c>.</returns>
    bool TryGetCommandByName(string commandName, out TrayCommand? command);
}