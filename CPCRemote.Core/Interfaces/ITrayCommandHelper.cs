namespace CPCRemote.Core.Interfaces;

using CPCRemote.Core.Enums;
using CPCRemote.Core.Models;

public interface ITrayCommandHelper
{
    TrayCommand[] Commands { get; }
    string? GetText(TrayCommandType commandType);
    TrayCommandType? GetCommandType(string commandName);
    void RunCommand(TrayCommandType commandType);
    void RunCommandByName(object value);
}
