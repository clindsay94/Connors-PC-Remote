namespace CPCRemote.UI.Pages
{
    using System.Runtime.Versioning;
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using CPCRemote.Core.Enums;
    using CPCRemote.Core.Interfaces;
    using CPCRemote.UI.Services;

    /// <summary>
    /// Page for quick power management actions
    /// </summary>
    [SupportedOSPlatform("windows10.0.22621.0")]
    public sealed partial class QuickActionsPage : Page
    {
        private readonly ICommandCatalog _commandCatalog;
        private readonly ICommandExecutor _commandExecutor;
        private readonly SettingsService _settingsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickActionsPage"/> class.
        /// </summary>
        public QuickActionsPage()
        {
            this.InitializeComponent();
            _commandCatalog = App.GetService<ICommandCatalog>();
            _commandExecutor = App.GetService<ICommandExecutor>();
            _settingsService = App.GetService<SettingsService>();
            
            // Wire up event handlers
            ShutdownButton.Click += async (s, e) => await ExecuteCommandSafelyAsync(TrayCommandType.Shutdown);
            RestartButton.Click += async (s, e) => await ExecuteCommandSafelyAsync(TrayCommandType.Restart);
            ForceShutdownButton.Click += async (s, e) => await ExecuteCommandSafelyAsync(TrayCommandType.ForceShutdown);
            UEFIRebootButton.Click += async (s, e) => await ExecuteCommandSafelyAsync(TrayCommandType.UEFIReboot);
            LockButton.Click += async (s, e) => await ExecuteCommandSafelyAsync(TrayCommandType.Lock);
            TurnScreenOffButton.Click += async (s, e) => await ExecuteCommandSafelyAsync(TrayCommandType.TurnScreenOff);
        }

        /// <summary>
        /// Safely executes a command with optional confirmation dialog
        /// </summary>
        /// <param name="commandType">The command to execute</param>
        private async Task ExecuteCommandSafelyAsync(TrayCommandType commandType)
        {
            try
            {
                // Check if confirmations are enabled using SettingsService
                bool showConfirmations = _settingsService.Get("ShowConfirmations", true);

                if (showConfirmations)
                {
                    string commandName = _commandCatalog.GetText(commandType) ?? commandType.ToString();
                    ContentDialog confirmDialog = new()
                    {
                        Title = "Confirm Action",
                        Content = $"Are you sure you want to {commandName.ToLower()}?",
                        PrimaryButtonText = "Yes",
                        SecondaryButtonText = "Cancel",
                        DefaultButton = ContentDialogButton.Secondary,
                        XamlRoot = this.XamlRoot
                    };

                    ContentDialogResult result = await confirmDialog.ShowAsync();
                    if (result != ContentDialogResult.Primary)
                    {
                        return; // User cancelled
                    }
                }

                // Execute the command
                await Task.Run(() => _commandExecutor.RunCommand(commandType));
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
            catch (Exception ex)
            {
                // Show error dialog
                ContentDialog errorDialog = new()
                {
                    Title = "Error",
                    Content = $"Failed to execute command: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };

                await errorDialog.ShowAsync();
            }
        }
    }
}
