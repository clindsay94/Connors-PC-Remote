namespace CPCRemote.UI.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CPCRemote.Core.IPC;
using CPCRemote.UI.Services;
using CPCRemote.UI.Strings;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

/// <summary>
/// ViewModel for the Dashboard page displaying live hardware stats and service status.
/// </summary>
public sealed partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly IPipeClient _pipeClient;
    private readonly ILogger<DashboardViewModel> _logger;
    private readonly SettingsService _settingsService;
    private readonly DispatcherQueue _dispatcherQueue;
    private CancellationTokenSource? _pollingCts;
    private bool _isDisposed;

    #region CPU Stats

    /// <summary>
    /// Gets or sets the total CPU utilization percentage.
    /// </summary>
    [ObservableProperty]
    public partial float? CpuUtility { get; set; }

    /// <summary>
    /// Gets or sets the CPU temperature (Tctl/Tdie).
    /// </summary>
    [ObservableProperty]
    public partial float? CpuTemp { get; set; }

    /// <summary>
    /// Gets or sets the CPU die average temperature.
    /// </summary>
    [ObservableProperty]
    public partial float? CpuDieAvgTemp { get; set; }

    /// <summary>
    /// Gets or sets the CPU IOD hotspot temperature.
    /// </summary>
    [ObservableProperty]
    public partial float? CpuIodHotspot { get; set; }

    /// <summary>
    /// Gets or sets the CPU package power in watts.
    /// </summary>
    [ObservableProperty]
    public partial float? CpuPackagePower { get; set; }

    /// <summary>
    /// Gets or sets the CPU PPT (Package Power Tracking) in watts.
    /// </summary>
    [ObservableProperty]
    public partial float? CpuPpt { get; set; }

    /// <summary>
    /// Gets or sets the average core clock in MHz.
    /// </summary>
    [ObservableProperty]
    public partial float? CpuCoreClock { get; set; }

    /// <summary>
    /// Gets or sets the average effective clock in MHz.
    /// </summary>
    [ObservableProperty]
    public partial float? CpuEffectiveClock { get; set; }

    /// <summary>
    /// Gets or sets the per-core effective clocks.
    /// </summary>
    [ObservableProperty]
    public partial float[]? CpuCoreEffectiveClocks { get; set; }

    #endregion

    #region Memory Stats

    /// <summary>
    /// Gets or sets the physical memory load percentage.
    /// </summary>
    [ObservableProperty]
    public partial float? MemoryLoad { get; set; }

    /// <summary>
    /// Gets or sets the DIMM temperatures.
    /// </summary>
    [ObservableProperty]
    public partial DimmTemp[]? DimmTemps { get; set; }

    #endregion

    #region GPU Stats

    /// <summary>
    /// Gets or sets the GPU temperature.
    /// </summary>
    [ObservableProperty]
    public partial float? GpuTemp { get; set; }

    /// <summary>
    /// Gets or sets the GPU memory junction temperature.
    /// </summary>
    [ObservableProperty]
    public partial float? GpuMemJunctionTemp { get; set; }

    /// <summary>
    /// Gets or sets the GPU power in watts.
    /// </summary>
    [ObservableProperty]
    public partial float? GpuPower { get; set; }

    /// <summary>
    /// Gets or sets the GPU clock speed in MHz.
    /// </summary>
    [ObservableProperty]
    public partial float? GpuClock { get; set; }

    /// <summary>
    /// Gets or sets the GPU effective clock in MHz.
    /// </summary>
    [ObservableProperty]
    public partial float? GpuEffectiveClock { get; set; }

    /// <summary>
    /// Gets or sets the GPU memory usage percentage.
    /// </summary>
    [ObservableProperty]
    public partial float? GpuMemoryUsage { get; set; }

    /// <summary>
    /// Gets or sets the GPU core load percentage.
    /// </summary>
    [ObservableProperty]
    public partial float? GpuCoreLoad { get; set; }

    #endregion

    #region Motherboard Stats

    /// <summary>
    /// Gets or sets the CPU core voltage (Vcore).
    /// </summary>
    [ObservableProperty]
    public partial float? Vcore { get; set; }

    /// <summary>
    /// Gets or sets the SOC voltage (VDDCR_SOC).
    /// </summary>
    [ObservableProperty]
    public partial float? Vsoc { get; set; }

    #endregion

    #region Service Status

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

    #endregion

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
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        // Load saved polling interval
        PollingIntervalSeconds = _settingsService.Get(nameof(PollingIntervalSeconds), 5);
    }

    /// <summary>
    /// Gets the available polling interval options.
    /// </summary>
    public ObservableCollection<PollingIntervalOption> PollingIntervalOptions { get; } =
    [
        new("1s", 1),
        new("2s", 2),
        new("5s", 5),
        new("10s", 10),
        new("30s", 30)
    ];

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

        RunOnUIThread(() =>
        {
            IsLoading = true;
            StatusMessage = Resources.Dashboard_ConnectingToService;
        });

        try
        {
            // Ensure connected
            if (!_pipeClient.IsConnected)
            {
                bool connected = await _pipeClient.ConnectAsync(IpcConstants.DefaultConnectTimeout, cancellationToken).ConfigureAwait(false);
                if (!connected)
                {
                    RunOnUIThread(() =>
                    {
                        IsServiceConnected = false;
                        StatusMessage = Resources.Dashboard_CannotConnect;
                        ClearStats();
                    });

                    return;
                }
            }

            // Fetch stats
            var statsResponse = await _pipeClient.SendRequestAsync<GetStatsResponse>(
                new GetStatsRequest(),
                IpcConstants.DefaultTimeout,
                cancellationToken).ConfigureAwait(false);

            // Fetch service status
            var statusResponse = await _pipeClient.SendRequestAsync<ServiceStatusResponse>(
                new ServiceStatusRequest(),
                IpcConstants.DefaultTimeout,
                cancellationToken).ConfigureAwait(false);

            // Marshal all UI updates to the UI thread
            RunOnUIThread(() =>
            {
                IsServiceConnected = true;

                if (statsResponse.Success)
                {
                    // CPU Stats
                    if (statsResponse.Cpu is not null)
                    {
                        CpuUtility = statsResponse.Cpu.Utility;
                        CpuTemp = statsResponse.Cpu.Temperature;
                        CpuDieAvgTemp = statsResponse.Cpu.DieAvgTemp;
                        CpuIodHotspot = statsResponse.Cpu.IodHotspot;
                        CpuPackagePower = statsResponse.Cpu.PackagePower;
                        CpuPpt = statsResponse.Cpu.Ppt;
                        CpuCoreClock = statsResponse.Cpu.CoreClock;
                        CpuEffectiveClock = statsResponse.Cpu.EffectiveClock;
                        CpuCoreEffectiveClocks = statsResponse.Cpu.CoreEffectiveClocks;
                    }

                    // Memory Stats
                    if (statsResponse.Memory is not null)
                    {
                        MemoryLoad = statsResponse.Memory.Load;
                        DimmTemps = statsResponse.Memory.DimmTemps;
                    }

                    // GPU Stats
                    if (statsResponse.Gpu is not null)
                    {
                        GpuTemp = statsResponse.Gpu.Temperature;
                        GpuMemJunctionTemp = statsResponse.Gpu.MemJunctionTemp;
                        GpuPower = statsResponse.Gpu.Power;
                        GpuClock = statsResponse.Gpu.Clock;
                        GpuEffectiveClock = statsResponse.Gpu.EffectiveClock;
                        GpuMemoryUsage = statsResponse.Gpu.MemoryUsage;
                        GpuCoreLoad = statsResponse.Gpu.CoreLoad;
                    }

                    // Motherboard Stats
                    if (statsResponse.Motherboard is not null)
                    {
                        Vcore = statsResponse.Motherboard.Vcore;
                        Vsoc = statsResponse.Motherboard.Vsoc;
                    }
                }

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

                StatusMessage = string.Format(Resources.Dashboard_LastUpdated, DateTime.Now.ToString("HH:mm:ss"));
            });
        }
        catch (OperationCanceledException)
        {
            // Expected when navigating away or stopping polling - silently ignore (includes TaskCanceledException)
        }
        catch (InvalidOperationException ex)
        {
            RunOnUIThread(() =>
            {
                IsServiceConnected = false;
                StatusMessage = ex.Message;
            });
            _logger.LogWarning(ex, "IPC connection error while fetching stats.");
        }
        catch (IpcException ex)
        {
            RunOnUIThread(() => StatusMessage = string.Format(Resources.Dashboard_ServiceError, ex.Message));
            _logger.LogError(ex, "IPC error while fetching stats.");
        }
        catch (Exception ex)
        {
            RunOnUIThread(() => StatusMessage = $"{Resources.Error}: {ex.Message}");
            _logger.LogError(ex, "Unexpected error while fetching stats.");
        }
        finally
        {
            RunOnUIThread(() => IsLoading = false);
        }
    }

    private void ClearStats()
    {
        // CPU
        CpuUtility = null;
        CpuTemp = null;
        CpuDieAvgTemp = null;
        CpuIodHotspot = null;
        CpuPackagePower = null;
        CpuPpt = null;
        CpuCoreClock = null;
        CpuEffectiveClock = null;
        CpuCoreEffectiveClocks = null;

        // Memory
        MemoryLoad = null;
        DimmTemps = null;

        // GPU
        GpuTemp = null;
        GpuMemJunctionTemp = null;
        GpuPower = null;
        GpuClock = null;
        GpuEffectiveClock = null;
        GpuMemoryUsage = null;
        GpuCoreLoad = null;

        // Motherboard
        Vcore = null;
        Vsoc = null;

        // Service
        ServiceVersion = null;
        ServiceUptime = null;
        HttpListenerAddress = null;
        IsHttpListening = false;
    }

    /// <summary>
    /// Marshals an action to the UI thread if not already on it.
    /// </summary>
    private void RunOnUIThread(DispatcherQueueHandler action)
    {
        if (_dispatcherQueue.HasThreadAccess)
        {
            action();
        }
        else
        {
            _dispatcherQueue.TryEnqueue(action);
        }
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
