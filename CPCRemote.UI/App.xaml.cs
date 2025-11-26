using System;
using System.Diagnostics;

using CPCRemote.Core.IPC;
using CPCRemote.UI;
using CPCRemote.UI.Services;
using CPCRemote.UI.ViewModels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

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
                // 3. Create the Window (without navigation)
                m_window = new MainWindow();
                CurrentMainWindow = m_window as MainWindow;

                // 4. Activate the window FIRST
                m_window.Activate();

                // 5. Configure Services AFTER window activation (ApplicationData.Current now available)
                Services = ConfigureServices();

                // 6. Apply saved settings
                ApplySavedSettings();

                // 7. Perform initial navigation after services are ready
                CurrentMainWindow?.PerformInitialNavigation();

                // Cleanup on exit
                m_window.Closed += (s, e) =>
                {
                    Logger?.LogInformation("Application shutting down...");
                    
                    // Dispose the pipe client
                    if (Services?.GetService<IPipeClient>() is IAsyncDisposable disposable)
                    {
                        _ = disposable.DisposeAsync();
                    }

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

            // Logging
            services.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Named Pipe IPC Client
            services.AddSingleton<NamedPipeClient>();
            services.AddSingleton<IPipeClient>(sp => sp.GetRequiredService<NamedPipeClient>());

            // ViewModels
            services.AddSingleton<SettingsService>();
            services.AddSingleton<ServiceManagementViewModel>();
            services.AddTransient<QuickActionsViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<AppCatalogViewModel>();

            // Core Services - WolOptions is required by CommandHelper (not used in UI but needed for DI)
            services.AddSingleton(new CPCRemote.Core.Models.WolOptions());
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

        /// <summary>
        /// Applies saved settings (theme, font, etc.) on startup.
        /// </summary>
        private static void ApplySavedSettings()
        {
            try
            {
                var settings = GetService<SettingsService>();

                // Apply theme
                string theme = settings.Get("AppTheme", "System");
                ApplyTheme(theme);

                // Apply font family
                string fontFamily = settings.Get("FontFamily", "Segoe UI Variable");
                ApplyFontFamily(fontFamily);

                // Apply font scale
                int fontScale = settings.Get("FontSizeScale", 100);
                ApplyFontScale(fontScale);

                Logger?.LogInformation("Applied saved settings: Theme={Theme}, Font={Font}, Scale={Scale}%", theme, fontFamily, fontScale);
            }
            catch (Exception ex)
            {
                Logger?.LogWarning(ex, "Failed to apply saved settings");
            }
        }

        /// <summary>
        /// Applies the specified theme to the application.
        /// </summary>
        public static void ApplyTheme(string theme)
        {
            if (CurrentMainWindow?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme switch
                {
                    "Light" => ElementTheme.Light,
                    "Dark" => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
            }
        }

        /// <summary>
        /// Applies the specified font family globally to the application.
        /// </summary>
        public static void ApplyFontFamily(string fontFamilyName)
        {
            if (CurrentMainWindow?.Content is Control rootControl)
            {
                rootControl.FontFamily = new FontFamily(fontFamilyName);
            }
        }

        /// <summary>
        /// Applies the specified font scale (percentage) to the application.
        /// </summary>
        public static void ApplyFontScale(int scalePercent)
        {
            if (CurrentMainWindow?.Content is Control rootControl)
            {
                // Scale is applied by setting the root control's font size
                // Child controls that don't explicitly set FontSize will inherit this
                double baseFontSize = 14.0; // Default WinUI font size
                double scaledFontSize = baseFontSize * (scalePercent / 100.0);
                rootControl.FontSize = scaledFontSize;
            }
        }
    }
}