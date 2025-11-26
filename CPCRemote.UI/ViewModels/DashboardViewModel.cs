namespace CPCRemote.UI.ViewModels;

using System;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CPCRemote.Core.IPC;
using CPCRemote.UI.Services;

using Microsoft.Extensions.Logging;

/// <summary>
/// ViewModel for the Dashboard page displaying live hardware stats and service status.
/// </summary>
public sealed partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly IPipeClient _pipeClient;
    private readonly ILogger<DashboardViewModel> _logger;
    private readonly SettingsService _settingsService;
    private CancellationTokenSource? _pollingCts;
    private bool _isDisposed;

    /// <summary>
    /// Gets or sets the CPU load percentage.
    /// </summary>
    [ObservableProperty]
    public partial float? CpuLoad { get; set; }

    /// <summary>
    /// Gets or sets the memory usage percentage.
    /// </summary>
    [ObservableProperty]
    public partial float? MemoryLoad { get; set; }

    /// <summary>
    /// Gets or sets the CPU temperature in Celsius.
    /// </summary>
    [ObservableProperty]
    public partial float? CpuTemp { get; set; }

    /// <summary>
    /// Gets or sets the GPU temperature in Celsius.
    /// </summary>
    [ObservableProperty]
    public partial float? GpuTemp { get; set; }

    /// <summary>
    /// Gets or sets the service version string.
    /// </summary>
    [ObservableProperty]
    public partial string? ServiceVersion { get; set; }

    /// <summary>
    /// Gets or sets the service uptime display string.
    /// </summary>
    [ObservableProperty]
    public partial string? ServiceUptime { get; set; }

    /// <summary>
    /// Gets or sets the HTTP listener address.
    /// </summary>
    [ObservableProperty]
    public partial string? HttpListenerAddress { get; set; }

    /// <summary>
    /// Gets or sets whether the service is connected via IPC.
    /// </summary>
    [ObservableProperty]
    public partial bool IsServiceConnected { get; set; }

    /// <summary>
    /// Gets or sets whether the HTTP listener is active.
    /// </summary>
    [ObservableProperty]
    public partial bool IsHttpListening { get; set; }

    /// <summary>
    /// Gets or sets whether data is currently being loaded.
    /// </summary>
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>
    /// Gets or sets the status message to display.
    /// </summary>
    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    /// <summary>
    /// Gets or sets the polling interval in seconds.
    /// </summary>
    [ObservableProperty]
    public partial int PollingIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets whether polling is currently active.
    /// </summary>
    [ObservableProperty]
    public partial bool IsPollingEnabled { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardViewModel"/> class.
    /// </summary>
    /// <param name="pipeClient">The IPC pipe client.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="settingsService">The settings service.</param>
    public DashboardViewModel(IPipeClient pipeClient, ILogger<DashboardViewModel> logger, SettingsService settingsService)
    {
        _pipeClient = pipeClient;
        _logger = logger;
        _settingsService = settingsService;

        // Load saved polling interval
        PollingIntervalSeconds = _settingsService.Get(nameof(PollingIntervalSeconds), 5);
    }

    /// <summary>
    /// Starts polling for stats updates.
    /// </summary>
    [RelayCommand]
    public void StartPolling()
    {
        if (_pollingCts is not null)
        {
            return;
        }

        _pollingCts = new CancellationTokenSource();
        _ = PollStatsAsync(_pollingCts.Token);
        _logger.LogInformation("Started stats polling with interval {Interval}s", PollingIntervalSeconds);
    }

    /// <summary>
    /// Stops polling for stats updates.
    /// </summary>
    [RelayCommand]
    public void StopPolling()
    {
        _pollingCts?.Cancel();
        _pollingCts?.Dispose();
        _pollingCts = null;
        _logger.LogInformation("Stopped stats polling.");
    }

    /// <summary>
    /// Manually refreshes the stats once.
    /// </summary>
    [RelayCommand]
    public async Task RefreshStatsAsync()
    {
        await FetchStatsAsync(CancellationToken.None);
    }

    /// <summary>
    /// Saves the polling interval setting.
    /// </summary>
    partial void OnPollingIntervalSecondsChanged(int value)
    {
        _settingsService.Set(nameof(PollingIntervalSeconds), value);

        // Restart polling with new interval
        if (_pollingCts is not null)
        {
            StopPolling();
            StartPolling();
        }
    }

    private async Task PollStatsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await FetchStatsAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task FetchStatsAsync(CancellationToken cancellationToken)
    {
        // Don't attempt to fetch if already cancelled
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        IsLoading = true;
        StatusMessage = "Connecting to service...";

        try
        {
            // Ensure connected
            if (!_pipeClient.IsConnected)
            {
                bool connected = await _pipeClient.ConnectAsync(IpcConstants.DefaultConnectTimeout, cancellationToken).ConfigureAwait(false);
                if (!connected)
                {
                    IsServiceConnected = false;
                    StatusMessage = "Cannot connect to service. Is it running?";
                    ClearStats();

                    return;
                }
            }

            IsServiceConnected = true;

            // Fetch stats
            var statsResponse = await _pipeClient.SendRequestAsync<GetStatsResponse>(
                new GetStatsRequest(),
                IpcConstants.DefaultTimeout,
                cancellationToken).ConfigureAwait(false);

            if (statsResponse.Success)
            {
                CpuLoad = statsResponse.Cpu;
                MemoryLoad = statsResponse.Memory;
                CpuTemp = statsResponse.CpuTemp;
                GpuTemp = statsResponse.GpuTemp;
            }

            // Fetch service status
            var statusResponse = await _pipeClient.SendRequestAsync<ServiceStatusResponse>(
                new ServiceStatusRequest(),
                IpcConstants.DefaultTimeout,
                cancellationToken).ConfigureAwait(false);

            if (statusResponse.Success)
            {
                ServiceVersion = statusResponse.Version;
                HttpListenerAddress = statusResponse.HttpListenerAddress;
                IsHttpListening = statusResponse.IsListening;

                TimeSpan uptime = TimeSpan.FromSeconds(statusResponse.UptimeSeconds);
                ServiceUptime = uptime.TotalHours >= 1
                    ? $"{(int)uptime.TotalHours}h {uptime.Minutes}m {uptime.Seconds}s"
                    : uptime.TotalMinutes >= 1
                        ? $"{uptime.Minutes}m {uptime.Seconds}s"
                        : $"{uptime.Seconds}s";
            }

            StatusMessage = $"Last updated: {DateTime.Now:HH:mm:ss}";
        }
        catch (OperationCanceledException)
        {
            // Expected when navigating away or stopping polling - silently ignore (includes TaskCanceledException)
        }
        catch (InvalidOperationException ex)
        {
            IsServiceConnected = false;
            StatusMessage = ex.Message;
            _logger.LogWarning(ex, "IPC connection error while fetching stats.");
        }
        catch (IpcException ex)
        {
            StatusMessage = $"Service error: {ex.Message}";
            _logger.LogError(ex, "IPC error while fetching stats.");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            _logger.LogError(ex, "Unexpected error while fetching stats.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ClearStats()
    {
        CpuLoad = null;
        MemoryLoad = null;
        CpuTemp = null;
        GpuTemp = null;
        ServiceVersion = null;
        ServiceUptime = null;
        HttpListenerAddress = null;
        IsHttpListening = false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        StopPolling();
    }
}
