using CPCRemote.UI.Helpers;
using CPCRemote.UI.Services;
using CPCRemote.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using System;
using System.Diagnostics;

namespace CPCRemote.UI
{
    public partial class App : Application
    {
        public static MainWindow? CurrentMainWindow { get; private set; }
        public static ILogger? Logger { get; private set; }
        private static string? _bootstrapErrorMessage;
        private static ILoggerFactory? _loggerFactory;

        public static IServiceProvider? Services { get; private set; }

        public App()
        {
            // Initialize logging FIRST (before anything else)
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddDebug()
                    .SetMinimumLevel(LogLevel.Debug);
            });
            Logger = _loggerFactory.CreateLogger<App>();

            Logger?.LogInformation("Application starting...");

            // Initialize bootstrap BEFORE InitializeComponent()
            // This is critical because XAML resources need the Windows App SDK to be initialized
            bool bootstrapInitialized = BootstrapHelper.Initialize(message =>
            {
                Debug.WriteLine(message);
                Logger?.LogDebug("Bootstrap: {Message}", message);
            });

            if (!bootstrapInitialized)
            {
                _bootstrapErrorMessage = "Windows App SDK bootstrap initialization failed. The application may not function correctly.";
                Debug.WriteLine(_bootstrapErrorMessage);
                Logger?.LogError(_bootstrapErrorMessage);
            }

            Services = ConfigureServices();

            // Initialize XAML components AFTER bootstrap
            this.InitializeComponent();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<SettingsService>();
            services.AddSingleton<ServiceManagementViewModel>();

            return services.BuildServiceProvider();
        }

        public static T GetService<T>() where T : notnull
        {
            if (Services is null)
            {
                throw new InvalidOperationException("Service provider is not initialized.");
            }
            return Services.GetRequiredService<T>();
        }

        private Window? m_window; // Use a private field for the main window

        [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.22621.0")]
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                m_window = new MainWindow();
                CurrentMainWindow = m_window as MainWindow;

                // Tweak 2: Subscribe to the Closed event to call Shutdown
                m_window.Closed += (sender, e) =>
                {
                    Logger?.LogInformation("Application shutting down...");
                    Bootstrap.Shutdown();
                    _loggerFactory?.Dispose();
                };

                m_window.Activate();

                // Show bootstrap error dialog if initialization failed
                if (!string.IsNullOrEmpty(_bootstrapErrorMessage))
                {
                    _ = ShowBootstrapErrorAsync();
                }
            }
            catch (Exception e)
            {
                // Log the exception details to the debug output.
                Debug.WriteLine("Unhandled exception in OnLaunched:");
                Debug.WriteLine(e);

                // It's also a good idea to check the inner exception.
                if (e.InnerException != null)
                {
                    Debug.WriteLine("\nInner Exception:");
                    Debug.WriteLine(e.InnerException);
                }

                // The application is likely to crash here, but this will give you the error details first.
                throw;
            }
        }

        private async System.Threading.Tasks.Task ShowBootstrapErrorAsync()
        {
            await System.Threading.Tasks.Task.Delay(500); // Allow window to fully activate

            if (CurrentMainWindow?.Content is Microsoft.UI.Xaml.FrameworkElement element)
            {
                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    Title = "Initialization Warning",
                    Content = _bootstrapErrorMessage + "\n\nThe application will continue to run, but some features may not work properly. Please ensure Windows App SDK 1.8 runtime is installed.",
                    CloseButtonText = "OK",
                    XamlRoot = element.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
    }
}