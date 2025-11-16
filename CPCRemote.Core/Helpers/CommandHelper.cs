namespace CPCRemote.Core.Helpers;

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using CPCRemote.Core.Enums;
using CPCRemote.Core.Interfaces;
using CPCRemote.Core.Models;

[SupportedOSPlatform("windows")]
public sealed partial class CommandHelper : ITrayCommandHelper
{
    private
     TrayCommand[]? _commands;

    [LibraryImport("user32.dll")]
    private static partial IntPtr
    SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool
        LockWorkStation();

    public
     TrayCommand[] Commands
    {
        get
        {
            _commands ??=
                [
                    new TrayCommand{CommandType = TrayCommandType.Restart, Name = "Restart"},
                    new TrayCommand{CommandType = TrayCommandType.TurnScreenOff, Name = "Turn screen off"},
                    new TrayCommand{CommandType = TrayCommandType.Shutdown, Name = "Shutdown"},
                    new TrayCommand{CommandType = TrayCommandType.ForceShutdown, Name = "Force Shutdown"},
                    new TrayCommand{CommandType = TrayCommandType.Lock, Name = "Lock"},
                    new TrayCommand{CommandType = TrayCommandType.UEFIReboot, Name = "UEFI Reboot"},
                ];
            return _commands;
        }
    }

    public string? GetText(TrayCommandType commandType) => Commands.SingleOrDefault(c => c.CommandType == commandType)
          ?.Name;

    public TrayCommandType? GetCommandType(string commandName) => Commands.SingleOrDefault(
                c => string.Equals(
                          c.Name,
                          commandName,
                          StringComparison.InvariantCultureIgnoreCase))
          ?.CommandType;

    // Change from static to instance method to implement the interface
    public void RunCommand(TrayCommandType commandType)
    {
        RunCommandInternal(commandType);
    }

    // Move the original static logic to a private static helper
    private static void RunCommandInternal(TrayCommandType commandType)
    {
        if (commandType == TrayCommandType.None)
        {
            return; // No-op for None command
        }

        try
        {
            switch (commandType)
            {
                case TrayCommandType.Restart:
                    ExecuteShutdownCommand("/r /t 0", commandType);
                    break;
                case TrayCommandType.Shutdown:
                    ExecuteShutdownCommand("/s /t 0", commandType);
                    break;
                case TrayCommandType.TurnScreenOff:
                    TurnScreenOff(commandType);
                    break;
                case TrayCommandType.ForceShutdown:
                    ExecuteShutdownCommand("/s /f /t 10", commandType);
                    break;
                case TrayCommandType.Lock:
                    LockWorkstation(commandType);
                    break;
                case TrayCommandType.UEFIReboot:
                    // UEFI reboot requires administrator privileges
                    ExecuteShutdownCommand("/r /fw /t 0", commandType);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown command type: {commandType}");
            }
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            // Wrap any non-InvalidOperationException with context about which command failed
            throw new InvalidOperationException($"Failed to execute command '{commandType}'. See inner exception for details.", ex);
        }
    }

    private static void ExecuteShutdownCommand(string arguments, TrayCommandType commandType)
    {
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

    private static void TurnScreenOff(TrayCommandType commandType)
    {
        try
        {
            IntPtr result = SendMessage((IntPtr)0xffff, 0x0112, (IntPtr)0xf170, (IntPtr)0x0002);
            // SendMessage returns zero if the message was processed, but we can't reliably detect failure
            // Log the result but don't throw unless we have a clear error
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to turn screen off for command '{commandType}'.", ex);
        }
    }

    private static void LockWorkstation(TrayCommandType commandType)
    {
        try
        {
            if (!LockWorkStation())
            {
                throw new InvalidOperationException($"LockWorkStation() returned false for command '{commandType}'.");
            }
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException($"Failed to lock workstation for command '{commandType}'.", ex);
        }
    }

    // Change the static method to an instance method to match the interface
    public void RunCommandByName(object value)
    {
        if (value is TrayCommandType commandType)
        {
            RunCommand(commandType);
        }
        else if (value is string commandName)
        {
            var type = GetCommandType(commandName);
            if (type.HasValue)
                RunCommand(type.Value);
        }
        // Optionally, handle other types or throw if unsupported
    }

    // Optionally, if you still want a static version, rename it:
    public static void RunCommandByNameStatic(object value)
    {
        if (value is TrayCommandType commandType)
        {
            // Create an instance to call the instance method
            var helper = new CommandHelper();
            helper.RunCommand(commandType);
        }
        else if (value is string commandName)
        {
            var helper = new CommandHelper();
            var type = helper.GetCommandType(commandName);
            if (type.HasValue)
                helper.RunCommand(type.Value);
        }
        // Optionally, handle other types or throw if unsupported
    }
}
