namespace CPCRemote.Core.Helpers;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using CPCRemote.Core.Enums;
using CPCRemote.Core.Interfaces;
using CPCRemote.Core.Models;

/// <summary>
/// Provides immutable command metadata as well as the concrete implementations that execute each power action.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed partial class CommandHelper : ICommandCatalog, ICommandExecutor
{
    private static readonly TrayCommand[] SeedCommands =
    [
        new(TrayCommandType.Restart, "Restart"),
        new(TrayCommandType.TurnScreenOff, "Turn screen off"),
        new(TrayCommandType.Shutdown, "Shutdown"),
        new(TrayCommandType.ForceShutdown, "Force Shutdown"),
        new(TrayCommandType.Lock, "Lock"),
        new(TrayCommandType.UEFIReboot, "UEFI Reboot")
    ];

    private static readonly IReadOnlyList<TrayCommand> Catalog = new ReadOnlyCollection<TrayCommand>(SeedCommands);
    private static readonly IReadOnlyDictionary<string, TrayCommandType> CommandTypesByName =
        SeedCommands.ToDictionary(static c => c.Name, static c => c.CommandType, StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlyDictionary<TrayCommandType, string> CommandTextByType =
        SeedCommands.ToDictionary(static c => c.CommandType, static c => c.Name);

    [LibraryImport("user32.dll")]
    private static partial IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool LockWorkStation();

    /// <inheritdoc />
    public IReadOnlyList<TrayCommand> Commands => Catalog;

    /// <inheritdoc />
    public string? GetText(TrayCommandType commandType)
    {
        return CommandTextByType.TryGetValue(commandType, out string? value) ? value : null;
    }

    /// <inheritdoc />
    public TrayCommandType? GetCommandType(string commandName)
    {
        if (string.IsNullOrWhiteSpace(commandName))
        {
            return null;
        }

        if (CommandTypesByName.TryGetValue(commandName, out TrayCommandType type))
        {
            return type;
        }

        return null;
    }

    /// <inheritdoc />
    public bool TryGetCommandByName(string commandName, out TrayCommand? command)
    {
        command = null;
        TrayCommandType? type = GetCommandType(commandName);
        if (!type.HasValue)
        {
            return false;
        }

        command = SeedCommands.First(c => c.CommandType == type.Value);
        return true;
    }

    /// <inheritdoc />
    public void RunCommand(TrayCommandType commandType)
    {
        RunCommandAsync(commandType, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public void RunCommandByName(object value)
    {
        RunCommandByNameAsync(value, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public Task RunCommandAsync(TrayCommandType commandType, CancellationToken cancellationToken)
    {
        ExecuteCommandInternal(commandType, cancellationToken);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RunCommandByNameAsync(object value, CancellationToken cancellationToken)
    {
        switch (value)
        {
            case TrayCommandType commandType:
                return RunCommandAsync(commandType, cancellationToken);
            case string commandName:
                {
                    TrayCommandType? resolvedType = GetCommandType(commandName);
                    return resolvedType.HasValue
                        ? RunCommandAsync(resolvedType.Value, cancellationToken)
                        : Task.CompletedTask;
                }
            default:
                return Task.CompletedTask;
        }
    }

    private static void ExecuteCommandInternal(TrayCommandType commandType, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (commandType == TrayCommandType.None)
        {
            return;
        }

        try
        {
            switch (commandType)
            {
                case TrayCommandType.Restart:
                    ExecuteShutdownCommand("/r /t 0", commandType, cancellationToken);
                    break;
                case TrayCommandType.Shutdown:
                    ExecuteShutdownCommand("/s /t 0", commandType, cancellationToken);
                    break;
                case TrayCommandType.TurnScreenOff:
                    TurnScreenOff(commandType, cancellationToken);
                    break;
                case TrayCommandType.ForceShutdown:
                    ExecuteShutdownCommand("/s /f /t 10", commandType, cancellationToken);
                    break;
                case TrayCommandType.Lock:
                    LockWorkstation(commandType, cancellationToken);
                    break;
                case TrayCommandType.UEFIReboot:
                    ExecuteShutdownCommand("/r /fw /t 0", commandType, cancellationToken);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown command type: {commandType}");
            }
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException($"Failed to execute command '{commandType}'. See inner exception for details.", ex);
        }
    }

    private static void ExecuteShutdownCommand(string arguments, TrayCommandType commandType, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            Process? process = Process.Start("shutdown", arguments);
            if (process == null)
            {
                throw new InvalidOperationException($"Failed to start shutdown process for command '{commandType}'.");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to execute shutdown command '{commandType}' with arguments '{arguments}'.", ex);
        }
    }

    private static void TurnScreenOff(TrayCommandType commandType, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _ = SendMessage((IntPtr)0xffff, 0x0112, (IntPtr)0xf170, (IntPtr)0x0002);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to turn screen off for command '{commandType}'.", ex);
        }
    }

    private static void LockWorkstation(TrayCommandType commandType, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (!LockWorkStation())
            {
                throw new InvalidOperationException($"LockWorkStation returned false for '{commandType}'.");
            }
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException($"Failed to lock workstation for command '{commandType}'.", ex);
        }
    }
}
