using System;
using System.Diagnostics;

using CPCRemote.Core.IPC;
using CPCRemote.UI;
using CPCRemote.UI.Services;
using CPCRemote.UI.ViewModels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using CommunityToolkit.WinUI;
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

            // 2. Register global unhandled exception handler
            this.UnhandledException += OnUnhandledException;

            // 3. Initialize Component (Parses App.xaml)
            this.InitializeComponent();

            // DO NOT configure services here. 
            // DO NOT bootstrap here (already done in Program.cs).
        }

        /// <summary>
        /// Global handler for unhandled exceptions in the WinUI application.
        /// Logs the exception and prevents silent failures.
        /// </summary>
        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // Log to file first
            Program.LogFailure(e.Exception, "Xaml.UnhandledException");
            
            Logger?.LogCritical(e.Exception, "Unhandled exception occurred: {Message}", e.Message);
            Debug.WriteLine($"UNHANDLED EXCEPTION: {e.Exception}");

            // Mark as handled to prevent app crash (allow graceful shutdown)
            // Set to false if you want the app to crash and show the default error dialog
            e.Handled = true;
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

            // Named Pipe IPC Client - Singleton: Single connection shared across all pages
            services.AddSingleton<NamedPipeClient>();
            services.AddSingleton<IPipeClient>(sp => sp.GetRequiredService<NamedPipeClient>());

            // ViewModels - Lifetime choices:
            // - Singleton: Shared state across navigation, survives page changes (e.g., service status)
            // - Transient: Fresh instance each time, no shared state (e.g., dashboard refreshes on each visit)
            services.AddSingleton<SettingsService>();           // Singleton: Caches settings, shared across pages
            
            // HttpClient for ServiceManagementViewModel - uses IHttpClientFactory pattern
            // This properly manages HttpClient lifecycle, avoiding socket exhaustion
            services.AddHttpClient<ServiceManagementViewModel>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(5); // Match previous DefaultHttpTimeout
            });
            
            services.AddTransient<QuickActionsViewModel>();      // Transient: Fresh state on each page visit
            services.AddTransient<DashboardViewModel>();         // Transient: Refreshes stats on each navigation
            services.AddTransient<AppCatalogViewModel>();        // Transient: Reloads catalog from service each visit
            services.AddTransient<SettingsPageViewModel>();      // Transient: Settings Page VM

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
        /// Applies saved settings (theme, font, backdrop, etc.) on startup.
        /// </summary>
        private static void ApplySavedSettings()
        {
            try
            {
                var settings = GetService<SettingsService>();

                // Apply theme
                string theme = settings.Get("AppTheme", "System");
                ApplyTheme(theme);

                // Apply backdrop (default to Acrylic as user preference)
                string backdrop = settings.Get("Backdrop", "Acrylic");
                ApplyBackdrop(backdrop);

                // Apply font family
                string fontFamily = settings.Get("FontFamily", "Segoe UI Variable");
                ApplyFontFamily(fontFamily);

                // Apply font scale
                int fontScale = settings.Get("FontSizeScale", 100);
                ApplyFontScale(fontScale);

                // Apply accent color (if custom)
                if (!settings.Get("UseSystemAccent", true))
                {
                    string accentColor = settings.Get("AccentColor", string.Empty);
                    ApplyAccentColor(accentColor);
                }

                Logger?.LogInformation("Applied saved settings: Theme={Theme}, Backdrop={Backdrop}, Font={Font}, Scale={Scale}%", 
                    theme, backdrop, fontFamily, fontScale);
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
        /// Applies the specified backdrop material to the main window.
        /// Supports Mica, MicaAlt, Acrylic, and None.
        /// </summary>
        public static void ApplyBackdrop(string backdrop)
        {
            if (CurrentMainWindow is null)
            {
                return;
            }

            try
            {
                CurrentMainWindow.SystemBackdrop = backdrop switch
                {
                    "Mica" => new Microsoft.UI.Xaml.Media.MicaBackdrop { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base },
                    "MicaAlt" => new Microsoft.UI.Xaml.Media.MicaBackdrop { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt },
                    "Acrylic" => new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop(),
                    _ => null
                };
                Logger?.LogDebug("Applied backdrop: {Backdrop}", backdrop);
            }
            catch (Exception ex)
            {
                Logger?.LogWarning(ex, "Failed to apply backdrop: {Backdrop}", backdrop);
            }
        }

        /// <summary>
        /// Applies the specified hex color as the application's accent color.
        /// Generates necessary light/dark variants for proper contrast.
        /// </summary>
        public static void ApplyAccentColor(string hexColor)
        {
            if (string.IsNullOrWhiteSpace(hexColor)) return;

            try
            {
                // Parse hex string to Color
                hexColor = hexColor.Replace("#", "");
                if (hexColor.Length == 6) hexColor = "FF" + hexColor; // Add Alpha if missing

                var color = Windows.UI.Color.FromArgb(
                    byte.Parse(hexColor.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                    byte.Parse(hexColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                    byte.Parse(hexColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber),
                    byte.Parse(hexColor.Substring(6, 2), System.Globalization.NumberStyles.HexNumber));

                // Update Application Resources
                Application.Current.Resources["SystemAccentColor"] = color;
                Application.Current.Resources["SystemAccentColorLight1"] = ChangeColorBrightness(color, 0.3f);
                Application.Current.Resources["SystemAccentColorLight2"] = ChangeColorBrightness(color, 0.5f);
                Application.Current.Resources["SystemAccentColorLight3"] = ChangeColorBrightness(color, 0.7f);
                Application.Current.Resources["SystemAccentColorDark1"] = ChangeColorBrightness(color, -0.3f);
                Application.Current.Resources["SystemAccentColorDark2"] = ChangeColorBrightness(color, -0.5f);
                Application.Current.Resources["SystemAccentColorDark3"] = ChangeColorBrightness(color, -0.7f);

                // Force update on some brushes if they don't automatically update
                // (WinUI ThemeResources usually bind to SystemAccentColor, but dynamic updates can be tricky)
                
                Logger?.LogDebug("Applied custom accent color: {Hex}", hexColor);
            }
            catch (Exception ex)
            {
                Logger?.LogWarning(ex, "Failed to apply accent color: {Hex}", hexColor);
            }
        }

        private static Windows.UI.Color ChangeColorBrightness(Windows.UI.Color color, float factor)
        {
            float r = (float)color.R;
            float g = (float)color.G;
            float b = (float)color.B;

            if (factor < 0)
            {
                factor = 1 + factor;
                r *= factor;
                g *= factor;
                b *= factor;
            }
            else
            {
                r = (255 - r) * factor + r;
                g = (255 - g) * factor + g;
                b = (255 - b) * factor + b;
            }

            return Windows.UI.Color.FromArgb(color.A, (byte)r, (byte)g, (byte)b);
        }

        /// <summary>
        /// Applies the specified font family globally to the application.
        /// </summary>
        public static void ApplyFontFamily(string fontFamilyName)
        {
            if (string.IsNullOrWhiteSpace(fontFamilyName)) return;

            try
            {
                var font = new FontFamily(fontFamilyName);

                // 1. Update Global Resource (affects new controls & dynamic lookups)
                // Note: This might not refresh existing controls immediately unless they use ThemeResource
                if (Application.Current.Resources.ContainsKey("ContentControlThemeFontFamily"))
                {
                    Application.Current.Resources["ContentControlThemeFontFamily"] = font;
                }
                else
                {
                    Application.Current.Resources.Add("ContentControlThemeFontFamily", font);
                }

                // 2. Apply to MainWindow Content (Cascades to children via inheritance)
                if (CurrentMainWindow?.Content is Control rootControl)
                {
                    rootControl.FontFamily = font;
                }
                else if (CurrentMainWindow?.Content is Panel rootPanel)
                {
                    // If root is a Grid/Panel, it doesn't have FontFamily, so set on children
                    foreach (var child in rootPanel.Children)
                    {
                        if (child is Control control)
                        {
                            control.FontFamily = font;
                        }
                        else if (child is NavigationView navView)
                        {
                            // NavigationView is a Control, but specifically calling it out just in case
                            navView.FontFamily = font;
                        }
                    }
                }

                Logger?.LogDebug("Applied font family globally: {Font}", fontFamilyName);
            }
            catch (Exception ex)
            {
                Logger?.LogWarning(ex, "Failed to apply font family: {Font}", fontFamilyName);
            }
        }

        /// <summary>
        /// Applies the specified font scale (percentage) to the application.
        /// </summary>
        public static void ApplyFontScale(int scalePercent)
        {
            if (CurrentMainWindow?.Content is Grid grid)
            {
                // Find the NavigationView inside the Grid
                foreach (var child in grid.Children)
                {
                    if (child is NavigationView navView)
                    {
                        double baseFontSize = 14.0;
                        double scaledFontSize = baseFontSize * (scalePercent / 100.0);
                        navView.FontSize = scaledFontSize;
                        Logger?.LogDebug("Applied font scale: {Scale}% ({Size}px)", scalePercent, scaledFontSize);
                        return;
                    }
                }
            }

            // Fallback: try to set on Content directly if it's a Control
            if (CurrentMainWindow?.Content is Control rootControl)
            {
                double baseFontSize = 14.0;
                double scaledFontSize = baseFontSize * (scalePercent / 100.0);
                rootControl.FontSize = scaledFontSize;
                Logger?.LogDebug("Applied font scale (fallback): {Scale}%", scalePercent);
            }
        }
    }
}