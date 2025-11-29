using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CPCRemote.UI.Services;
using CPCRemote.UI.Strings;

namespace CPCRemote.UI.ViewModels;

/// <summary>
/// ViewModel for the Quick Actions page providing one-click PC control commands.
/// </summary>
/// <remarks>
/// <para>
/// This ViewModel sends HTTP commands directly to the CPCRemote.Service via the REST API.
/// Commands are sent using Bearer token authentication configured in settings.
/// </para>
/// <para>
/// Available commands match the <see cref="CPCRemote.Core.Enums.TrayCommandType"/> enum
/// and correspond to the same endpoints used by the SmartThings Edge driver.
/// </para>
/// </remarks>
public partial class QuickActionsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private static readonly HttpClient _httpClient = new();

    /// <summary>
    /// Gets or sets the response log for displaying command results.
    /// </summary>
    [ObservableProperty]
    public partial string ResponseLog { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuickActionsViewModel"/> class.
    /// </summary>
    /// <param name="settingsService">The settings service for loading configuration.</param>
    public QuickActionsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    /// Sends a graceful shutdown command to the PC.
    /// </summary>
    [RelayCommand]
    private async Task Shutdown() => await SendCommandAsync("Shutdown");

    /// <summary>
    /// Sends a restart command to the PC.
    /// </summary>
    [RelayCommand]
    private async Task Restart() => await SendCommandAsync("Restart");

    /// <summary>
    /// Sends a lock workstation command to the PC.
    /// </summary>
    [RelayCommand]
    private async Task Lock() => await SendCommandAsync("Lock");

    /// <summary>
    /// Sends a turn screen off command to the PC.
    /// </summary>
    [RelayCommand]
    private async Task TurnScreenOff() => await SendCommandAsync("TurnScreenOff");

    /// <summary>
    /// Sends a Wake-on-LAN magic packet to wake the PC.
    /// </summary>
    [RelayCommand]
    private async Task WakeOnLan() => await SendCommandAsync("WakeOnLan");

    /// <summary>
    /// Sends a command to the service via HTTP GET request.
    /// </summary>
    /// <param name="command">The command name to send.</param>
    /// <remarks>
    /// Uses Bearer token authentication and a 5-second timeout.
    /// Results are appended to <see cref="ResponseLog"/>.
    /// </remarks>
    private async Task SendCommandAsync(string command)
    {
        try
        {
            var config = await _settingsService.LoadServiceConfigurationAsync();
            string ip = config?.Rsm?.IpAddress ?? "localhost";
            int port = config?.Rsm?.Port ?? 5005;
            string secret = config?.Rsm?.Secret ?? string.Empty;
            string baseUrl = $"http://{ip}:{port}";

            Log(string.Format(Resources.QuickActions_Sending, command, baseUrl));

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/{command}");
            if (!string.IsNullOrEmpty(secret))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.SendAsync(request, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                Log(string.Format(Resources.QuickActions_Success, response.StatusCode));
            }
            else
            {
                Log(string.Format(Resources.QuickActions_Failed, response.StatusCode));
            }
        }
        catch (OperationCanceledException)
        {
            // Silently ignore cancellation
        }
        catch (Exception ex)
        {
            Log($"{Resources.Error}: {ex.Message}");
        }
    }

    /// <summary>
    /// Appends a timestamped message to the response log.
    /// </summary>
    /// <param name="message">The message to log.</param>
    private void Log(string message)
    {
        ResponseLog += $"{DateTime.Now:HH:mm:ss}: {message}\n";
    }
}