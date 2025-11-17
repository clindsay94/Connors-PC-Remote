namespace CPCRemote.Core.Models;

using CPCRemote.Core.Enums;

/// <summary>
/// Immutable metadata describing a tray command, including its semantic type and display label.
/// </summary>
/// <param name="CommandType">The unique domain identifier for the command.</param>
/// <param name="Name">The human-friendly text surfaced in UI layers.</param>
public sealed record TrayCommand(TrayCommandType CommandType, string Name);
