namespace CPCRemote.Core.Helpers
{
    using System.Threading.Tasks;

    using CPCRemote.Core.Enums;
    using CPCRemote.Core.Interfaces;

    /// <summary>
    /// Defines the <see cref="HostHelper" />
    /// </summary>
    public class HostHelper(ITrayCommandHelper trayCommandHelper)
    {
        /// <summary>
        /// Defines the _trayCommandHelper
        /// </summary>
        private readonly ITrayCommandHelper _trayCommandHelper = trayCommandHelper;

        /// <summary>
        /// Gets or sets the DefaultCommand
        /// </summary>
        public TrayCommandType DefaultCommand { get; set; }

        /// <summary>
        /// Gets or sets the SecretCode
        /// </summary>
        public string? SecretCode { get; set; }

        /// <summary>
        /// The ProcessRequestAsync
        /// </summary>
        /// <param name="request">The request<see cref="string"/></param>
        /// <returns>The <see cref="Task"/></returns>
        public async Task ProcessRequestAsync(string request)
        {
            // Simplified logic based on usage in tests
            if (string.IsNullOrEmpty(request) || !request.StartsWith('/'))
            {
                return;
            }

            string[] parts = request[1..].Split('/');

            string commandText = parts[0];
            string? secret = null;

            if (parts.Length > 1)
            {
                secret = parts[0];
                commandText = parts[1];
            }

            if (!string.IsNullOrEmpty(SecretCode) && secret != SecretCode)
            {
                return; // Secret mismatch
            }

            TrayCommandType? commandType = _trayCommandHelper.GetCommandType(commandText);

            if (commandType.HasValue)
            {
                await Task.Run(() => _trayCommandHelper.RunCommand(commandType.Value));
            }
            else if (DefaultCommand != 0) // Assuming 0 is a default/invalid value
            {
                await Task.Run(() => _trayCommandHelper.RunCommand(DefaultCommand));
            }
        }
    }
}
