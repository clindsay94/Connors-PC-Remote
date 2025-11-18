using CPCRemote.UI;
using CPCRemote.UI.Helpers;
using CPCRemote.UI.Services;
using CPCRemote.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using System;
using System.Diagnostics;

namespace CPCRemote 
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
            // Initialize logging FIRST
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddDebug()
                    .SetMinimumLevel(LogLevel.Debug);
            });
            Logger = _loggerFactory.CreateLogger<App>();

            Logger?.LogInformation("Application starting...");

            // Initialize bootstrap
            bool bootstrapInitialized = BootstrapHelper.Initialize(message =>
            {
                Debug.WriteLine(message);
                Logger?.LogDebug("Bootstrap: {Message}", message);
            });

            if (!bootstrapInitialized)
            {
                _bootstrapErrorMessage = "Windows App SDK bootstrap initialization failed.";
                Debug.WriteLine(_bootstrapErrorMessage);
                Logger?.LogError(_bootstrapErrorMessage);
            }

            Services = ConfigureServices();

            this.InitializeComponent();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // MAKE SURE THESE CLASSES EXIST IN YOUR PROJECT
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

        private Window? m_window;

        [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.22621.0")]
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                // This now works because of the "using CPCRemote.UI;" at the top
                m_window = new MainWindow();
                CurrentMainWindow = m_window as MainWindow;

                m_window.Closed += (sender, e) =>
                {
                    Logger?.LogInformation("Application shutting down...");
                    Bootstrap.Shutdown();
                    _loggerFactory?.Dispose();
                };

                m_window.Activate();

                if (!string.IsNullOrEmpty(_bootstrapErrorMessage))
                {
                    _ = ShowBootstrapErrorAsync();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Unhandled exception in OnLaunched:");
                Debug.WriteLine(e);
                if (e.InnerException != null)
                {
                    Debug.WriteLine("\nInner Exception:");
                    Debug.WriteLine(e.InnerException);
                }
                throw;
            }
        }

        private async System.Threading.Tasks.Task ShowBootstrapErrorAsync()
        {
            await System.Threading.Tasks.Task.Delay(500);

            if (CurrentMainWindow?.Content is Microsoft.UI.Xaml.FrameworkElement element)
            {
                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    Title = "Initialization Warning",
                    Content = _bootstrapErrorMessage + "\n\nThe application will continue to run, but some features may not work properly.",
                    CloseButtonText = "OK",
                    XamlRoot = element.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
    }
}