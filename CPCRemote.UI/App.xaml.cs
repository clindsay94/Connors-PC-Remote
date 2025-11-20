using System;
using System.Diagnostics;

using CPCRemote.UI;
using CPCRemote.UI.Services;
using CPCRemote.UI.ViewModels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace CPCRemote.UI
{
    public partial class App : Application
    {
        public static MainWindow? CurrentMainWindow { get; private set; }
        public static ILogger? Logger { get; private set; }
        public static IServiceProvider? Services { get; private set; }

        private ILoggerFactory? _loggerFactory;
        private Window? m_window;

        public App()
        {
            // 1. Initialize Logging (Safe to do early)
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddDebug().SetMinimumLevel(LogLevel.Debug);
            });
            Logger = _loggerFactory.CreateLogger<App>();
            Logger?.LogInformation("Application Constructor Reached.");

            // 2. Initialize Component (Parses App.xaml)
            this.InitializeComponent();

            // DO NOT configure services here. 
            // DO NOT bootstrap here (already done in Program.cs).
        }

        // This is the "Safe Zone" - The UI thread is fully ready.
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                // 3. Configure Services NOW
                Services = ConfigureServices();

                // 4. Create the Window
                m_window = new MainWindow();
                CurrentMainWindow = m_window as MainWindow;

                // 5. Activate
                m_window.Activate();

                // Cleanup on exit
                m_window.Closed += (s, e) =>
                {
                    Logger?.LogInformation("Application shutting down...");
                    _loggerFactory?.Dispose();
                };
            }
            catch (Exception e)
            {
                Logger?.LogCritical(e, "Crash in OnLaunched");
                Debug.WriteLine($"CRITICAL: {e}");
                throw; // Re-throw so the debugger catches it
            }
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // ViewModels
            services.AddSingleton<SettingsService>();
            services.AddSingleton<ServiceManagementViewModel>();

            // Core Services
            services.AddSingleton<CPCRemote.Core.Helpers.CommandHelper>();
            services.AddSingleton<CPCRemote.Core.Interfaces.ICommandCatalog>(sp => sp.GetRequiredService<CPCRemote.Core.Helpers.CommandHelper>());
            services.AddSingleton<CPCRemote.Core.Interfaces.ICommandExecutor>(sp => sp.GetRequiredService<CPCRemote.Core.Helpers.CommandHelper>());

            return services.BuildServiceProvider();
        }

        // Helper to access services safely
        public static T GetService<T>() where T : notnull
        {
            if (Services is null)
                throw new InvalidOperationException("Services accessed before OnLaunched!");

            return Services.GetRequiredService<T>();
        }
    }
}