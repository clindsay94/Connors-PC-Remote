namespace CPCRemote.UI.Services;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using H.NotifyIcon;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.Input;

/// <summary>
/// Manages the system tray icon for background operation.
/// Uses H.NotifyIcon.WinUI for tray integration.
/// </summary>
public sealed class TrayService : IDisposable
{
    private TaskbarIcon? _trayIcon;
    private bool _disposed;
    private readonly DispatcherQueue _dispatcherQueue;
    private static readonly HttpClient _httpClient = new();

    public TrayService(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
    }

    /// <summary>
    /// Initializes and shows the system tray icon.
    /// </summary>
    public void Initialize()
    {
        if (_trayIcon is not null) return;

        _trayIcon = new TaskbarIcon();

        // Set icon from PNG asset (no .ico available)
        try
        {
            string pngPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Square44x44Logo.targetsize-256.png");
            if (File.Exists(pngPath))
            {
                using var bitmap = new Bitmap(pngPath);
                _trayIcon.Icon = System.Drawing.Icon.FromHandle(bitmap.GetHicon());
            }
        }
        catch
        {
            // Fallback - no icon
        }

        _trayIcon.ToolTipText = "CPC Remote";

        // Create WinUI context flyout menu with quick actions
        var menuFlyout = new MenuFlyout();
        
        var showItem = new MenuFlyoutItem 
        { 
            Text = "Show CPC Remote", 
            Icon = new FontIcon { Glyph = "\uE737" },
            Command = new RelayCommand(ShowWindow)
        };
        menuFlyout.Items.Add(showItem);

        menuFlyout.Items.Add(new MenuFlyoutSeparator());

        // Quick Actions
        var lockItem = new MenuFlyoutItem 
        { 
            Text = "Lock PC", 
            Icon = new FontIcon { Glyph = "\uE72E" },
            Command = new RelayCommand(() => Helpers.PowerHelper.Lock())
        };
        menuFlyout.Items.Add(lockItem);

        menuFlyout.Items.Add(new MenuFlyoutSeparator());

        var restartItem = new MenuFlyoutItem 
        { 
            Text = "Restart", 
            Icon = new FontIcon { Glyph = "\uE777" },
            Command = new RelayCommand(() => SendQuickActionAsync("Restart"))
        };
        menuFlyout.Items.Add(restartItem);

        var uefiItem = new MenuFlyoutItem 
        { 
            Text = "Restart to UEFI", 
            Icon = new FontIcon { Glyph = "\uE770" },
            Command = new RelayCommand(() => SendQuickActionAsync("UEFIReboot"))
        };
        menuFlyout.Items.Add(uefiItem);

        var shutdownItem = new MenuFlyoutItem 
        { 
            Text = "Shutdown", 
            Icon = new FontIcon { Glyph = "\uE7E8" },
            Command = new RelayCommand(() => SendQuickActionAsync("Shutdown"))
        };
        menuFlyout.Items.Add(shutdownItem);

        menuFlyout.Items.Add(new MenuFlyoutSeparator());

        var exitItem = new MenuFlyoutItem 
        { 
            Text = "Exit CPC Remote", 
            Icon = new FontIcon { Glyph = "\uE711" },
            Command = new RelayCommand(ExitApplication)
        };
        menuFlyout.Items.Add(exitItem);

        _trayIcon.ContextFlyout = menuFlyout;

        // Double-click to show window
        _trayIcon.DoubleClickCommand = new RelayCommand(ShowWindow);

        _trayIcon.ForceCreate();
    }

    /// <summary>
    /// Shows the main window and brings it to the foreground.
    /// </summary>
    public void ShowWindow()
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (App.CurrentMainWindow is { } window)
            {
                window.Show();    // Win32 SW_SHOW + SetForegroundWindow
                window.Activate(); // WinUI activation
            }
        });
    }

    /// <summary>
    /// Sends a quick action command via HTTP to the service.
    /// </summary>
    private async void SendQuickActionAsync(string command)
    {
        try
        {
            var settingsService = App.GetService<SettingsService>();
            var config = await settingsService.LoadServiceConfigurationAsync();
            string ip = config?.Rsm?.IpAddress ?? "localhost";
            int port = config?.Rsm?.Port ?? 5005;
            string secret = config?.Rsm?.Secret ?? string.Empty;
            string baseUrl = $"http://{ip}:{port}";

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/{command}");
            if (!string.IsNullOrEmpty(secret))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
            }

            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2));
            await _httpClient.SendAsync(request, cts.Token);
        }
        catch
        {
            // Tray commands are fire-and-forget
        }
    }

    /// <summary>
    /// Exits the application completely.
    /// </summary>
    public void ExitApplication()
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            App.RequestExit = true;
            Dispose();
            Application.Current.Exit();
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _trayIcon?.Dispose();
        _trayIcon = null;
    }
}
