namespace CPCRemote.Core.Helpers
{
    using System.Linq;

    using CPCRemote.Core.Enums;
    using CPCRemote.Core.Models;

    /// <summary>
    /// Defines the <see cref="TrayCommandHelper" />
    /// </summary>
    public class TrayCommandHelper
    {
        /// <summary>
        /// Defines the _commands
        /// </summary>
        private readonly TrayCommand[] _commands =
        [
            new() { CommandType = TrayCommandType.Shutdown, Name = "Shutdown" },
            new() { CommandType = TrayCommandType.Restart, Name = "Restart" },
            new() { CommandType = TrayCommandType.TurnScreenOff, Name = "Turn screen off" },
            new() { CommandType = TrayCommandType.ForceShutdown, Name = "Force Shutdown" },
            new() { CommandType = TrayCommandType.Lock, Name = "Lock" },
            new() { CommandType = TrayCommandType.UEFIReboot, Name = "UEFI Reboot" }
        ];

        /// <summary>
        /// The GetText
        /// </summary>
        /// <param name="commandType">The commandType<see cref="TrayCommandType"/></param>
        /// <returns>The <see cref="string"/></returns>
        public string GetText(TrayCommandType commandType)
        {
            var command = _commands.FirstOrDefault(c => c.CommandType == commandType);
            if (command == null)
            {
                return string.Empty;
            }
            return command.Name ?? string.Empty;
        }

        /// <summary>
        /// The GetCommandType
        /// </summary>
        /// <param name="commandName">The commandName<see cref="string"/></param>
        /// <returns>The <see cref="TrayCommandType?"/></returns>
        public TrayCommandType? GetCommandType(string commandName)
        {
            return _commands.FirstOrDefault(c => c.Name != null && c.Name.Equals(commandName, System.StringComparison.OrdinalIgnoreCase))?.CommandType;
        }

        /// <summary>
        /// Gets the Commands
        /// </summary>
        public TrayCommand[] Commands => _commands;
    }
}
