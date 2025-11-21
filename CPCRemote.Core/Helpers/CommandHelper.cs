namespace CPCRemote.Core.Helpers;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
public sealed partial class CommandHelper(WolOptions wolOptions) : ICommandCatalog, ICommandExecutor
{
    private readonly WolOptions _wolOptions = wolOptions;

    private static readonly TrayCommand[] SeedCommands =
    [
        new(TrayCommandType.Restart, "Restart"),
        new(TrayCommandType.TurnScreenOff, "Turn screen off"),
        new(TrayCommandType.Shutdown, "Shutdown"),
        new(TrayCommandType.ForceShutdown, "Force Shutdown"),
        new(TrayCommandType.Lock, "Lock"),
        new(TrayCommandType.UEFIReboot, "UEFI Reboot"),
        new(TrayCommandType.WakeOnLan, "Wake on LAN")
    ];

    private static readonly IReadOnlyList<TrayCommand> Catalog = new ReadOnlyCollection<TrayCommand>(SeedCommands);
    private static readonly IReadOnlyDictionary<string, TrayCommandType> CommandTypesByName =
        SeedCommands.ToDictionary(static c => c.Name, static c => c.CommandType, StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlyDictionary<TrayCommandType, string> CommandTextByType =
        SeedCommands.ToDictionary(static c => c.CommandType, static c => c.Name);

    [LibraryImport("user32.dll", EntryPoint = "SendMessageTimeoutW")]
    private static partial IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint Msg,
        IntPtr wParam,
        IntPtr lParam,
        uint fuFlags,
        uint uTimeout,
        out IntPtr lpdwResult);

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

        if (Enum.TryParse(commandName, true, out TrayCommandType enumResult) && Enum.IsDefined(enumResult))
        {
            return enumResult;
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
    public async Task RunCommandAsync(TrayCommandType commandType, CancellationToken cancellationToken)
    {
        await ExecuteCommandInternalAsync(commandType, cancellationToken).ConfigureAwait(false);
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

    private async Task ExecuteCommandInternalAsync(TrayCommandType commandType, CancellationToken cancellationToken)
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
                case TrayCommandType.WakeOnLan:
                    await SendWakeOnLanAsync(commandType, cancellationToken).ConfigureAwait(false);
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
            // Use SendMessageTimeout to prevent blocking indefinitely and avoid EEMessageException in Session 0
            // HWND_BROADCAST = 0xffff, WM_SYSCOMMAND = 0x0112, SC_MONITORPOWER = 0xf170, PowerOff = 2
            // SMTO_ABORTIFHUNG = 0x0002
            const uint SMTO_ABORTIFHUNG = 0x0002;
            SendMessageTimeout((IntPtr)0xffff, 0x0112, (IntPtr)0xf170, (IntPtr)0x0002, SMTO_ABORTIFHUNG, 1000, out _);
        }
        catch (Exception ex)
        {
            // Swallow exceptions here to prevent service crashes for non-critical UI operations
            Debug.WriteLine($"Failed to turn screen off for command '{commandType}': {ex}");
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

    private async Task SendWakeOnLanAsync(TrayCommandType commandType, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_wolOptions.MacAddress))
        {
             throw new InvalidOperationException("MAC address is not configured for Wake-on-LAN.");
        }

        // Parse MAC address
        string mac = _wolOptions.MacAddress.Replace(":", "").Replace("-", "");
        if (mac.Length != 12)
        {
             throw new InvalidOperationException("Invalid MAC address format.");
        }

        byte[] macBytes = new byte[6];
        for (int i = 0; i < 6; i++)
        {
            macBytes[i] = Convert.ToByte(mac.Substring(i * 2, 2), 16);
        }

        // Construct magic packet: 6x 0xFF followed by 16x MAC address
        byte[] packet = new byte[6 + 16 * 6];
        for (int i = 0; i < 6; i++)
        {
            packet[i] = 0xFF;
        }

        for (int i = 0; i < 16; i++)
        {
            Array.Copy(macBytes, 0, packet, 6 + i * 6, 6);
        }

        using UdpClient client = new();
        client.EnableBroadcast = true;
        
        IPAddress broadcastIp = IPAddress.Parse(_wolOptions.BroadcastAddress);
        IPEndPoint endPoint = new(broadcastIp, _wolOptions.Port);

        await client.SendAsync(packet, packet.Length, endPoint).ConfigureAwait(false);
    }
}