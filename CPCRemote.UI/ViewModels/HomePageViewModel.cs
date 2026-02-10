namespace CPCRemote.UI.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CPCRemote.Core.IPC;
using CPCRemote.UI.Models;
using CPCRemote.UI.Services;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

/// <summary>
/// ViewModel for the Home Page — the dynamic, widget-based dashboard.
/// Manages live sensor widgets, quick actions, and layout persistence.
/// </summary>
public sealed partial class HomePageViewModel : ObservableObject, IDisposable
{
    private readonly IPipeClient _pipeClient;
    private readonly ILogger<HomePageViewModel> _logger;
    private readonly SettingsService _settingsService;
    private readonly DispatcherQueue _dispatcherQueue;
    private CancellationTokenSource? _pollingCts;
    private bool _isDisposed;
    private bool _initialLoadDone;
    private bool _isUpdating; // Flag to prevent auto-save during updates

    private static readonly string LayoutFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CPCRemote", "dashboard_layout.json");

    private static readonly HttpClient _httpClient = new();

    // ── Observable Collections ──

    /// <summary>Dashboard widgets for sensor data.</summary>
    public ObservableCollection<DashboardWidgetViewModel> Widgets { get; } = [];

    // ── Properties ──

    /// <summary>Welcome message header.</summary>
    [ObservableProperty]
    public partial string WelcomeMessage { get; set; } = "Welcome back";

    /// <summary>System status subtitle.</summary>
    [ObservableProperty]
    public partial string SystemStatus { get; set; } = "Connecting...";

    /// <summary>Whether the service is connected.</summary>
    [ObservableProperty]
    public partial bool IsServiceConnected { get; set; }

    /// <summary>Whether data is loading.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    public partial bool IsLoading { get; set; }

    /// <summary>Whether any widgets are present.</summary>
    public bool HasWidgets => Widgets.Count > 0;

    /// <summary>Whether to show the empty state message.</summary>
    public bool ShowEmptyState => !IsLoading && !HasWidgets;

    /// <summary>Whether edit mode is active (enables drag/resize).</summary>
    [ObservableProperty]
    public partial bool IsEditMode { get; set; }

    /// <summary>Polling interval in seconds.</summary>
    [ObservableProperty]
    public partial int PollingIntervalSeconds { get; set; } = 5;

    /// <summary>Whether polling is active.</summary>
    [ObservableProperty]
    public partial bool IsPollingEnabled { get; set; } = true;

    /// <summary>Quick actions response log.</summary>
    [ObservableProperty]
    public partial string QuickActionLog { get; set; } = string.Empty;

    /// <summary>Service uptime display.</summary>
    [ObservableProperty]
    public partial string? ServiceUptime { get; set; }

    /// <summary>HTTP listener address.</summary>
    [ObservableProperty]
    public partial string? HttpListenerAddress { get; set; }

    /// <summary>Available polling interval options.</summary>
    public ObservableCollection<PollingIntervalOption> PollingIntervalOptions { get; } =
    [
        new("1s", 1),
        new("2s", 2),
        new("5s", 5),
        new("10s", 10),
        new("30s", 30)
    ];

    // ── Constructor ──

    public HomePageViewModel(IPipeClient pipeClient, ILogger<HomePageViewModel> logger, SettingsService settingsService)
    {
        _pipeClient = pipeClient;
        _logger = logger;
        _settingsService = settingsService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        PollingIntervalSeconds = _settingsService.Get(nameof(PollingIntervalSeconds), 5);

        // Set welcome time-based greeting
        var hour = DateTime.Now.Hour;
        WelcomeMessage = hour switch
        {
            < 12 => "Good morning",
            < 17 => "Good afternoon",
            _ => "Good evening"
        };

        // Save layout when widgets are reordered via drag-and-drop
        Widgets.CollectionChanged += (s, e) =>
        {
            if (_isUpdating) return;

            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Move ||
                e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove ||
                e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                _ = SaveLayoutAsync();
                _ = SaveSensorOrderAsync();
            }
        };
    }

    // ── Polling ──

    /// <summary>Starts live data polling.</summary>
    [RelayCommand]
    public void StartPolling()
    {
        if (_pollingCts is not null) return;

        _pollingCts = new CancellationTokenSource();
        _ = PollAsync(_pollingCts.Token);
        _logger.LogInformation("Home page polling started ({Interval}s)", PollingIntervalSeconds);
    }

    /// <summary>Stops live data polling.</summary>
    [RelayCommand]
    public void StopPolling()
    {
        _pollingCts?.Cancel();
        _pollingCts?.Dispose();
        _pollingCts = null;
        _logger.LogInformation("Home page polling stopped");
    }

    /// <summary>Manual refresh.</summary>
    [RelayCommand]
    public async Task RefreshAsync()
    {
        await FetchAllDataAsync(CancellationToken.None);
    }

    partial void OnPollingIntervalSecondsChanged(int value)
    {
        _settingsService.Set(nameof(PollingIntervalSeconds), value);
        if (_pollingCts is not null)
        {
            StopPolling();
            StartPolling();
        }
    }

    partial void OnIsEditModeChanged(bool value)
    {
        foreach (var widget in Widgets)
        {
            widget.IsInEditMode = value;
        }

        // Save layout when exiting edit mode
        if (!value && Widgets.Count > 0)
        {
            _ = SaveLayoutAsync();
        }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await FetchAllDataAsync(ct);
            }
            catch (OperationCanceledException) { break; }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), ct);
            }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task FetchAllDataAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;

        if (!_initialLoadDone)
        {
            RunOnUIThread(() => IsLoading = true);
        }

        RunOnUIThread(() => SystemStatus = "Updating...");

        try
        {
            if (!_pipeClient.IsConnected)
            {
                bool connected = await _pipeClient.ConnectAsync(IpcConstants.DefaultConnectTimeout, ct).ConfigureAwait(false);
                if (!connected)
                {
                    RunOnUIThread(() =>
                    {
                        IsServiceConnected = false;
                        SystemStatus = "Cannot connect to service";
                    });
                    return;
                }
            }

            // Fetch service status
            var statusResponse = await _pipeClient.SendRequestAsync<ServiceStatusResponse>(
                new ServiceStatusRequest(),
                IpcConstants.DefaultTimeout,
                ct).ConfigureAwait(false);

            // Fetch gadget sensors (these are the widget data sources)
            var sensorsResponse = await _pipeClient.SendRequestAsync<GetGadgetSensorsResponse>(
                new GetGadgetSensorsRequest(),
                IpcConstants.DefaultTimeout,
                ct).ConfigureAwait(false);

            RunOnUIThread(() =>
            {
                IsServiceConnected = true;

                // Update service status
                if (statusResponse.Success)
                {
                    HttpListenerAddress = statusResponse.HttpListenerAddress;
                    TimeSpan uptime = TimeSpan.FromSeconds(statusResponse.UptimeSeconds);
                    ServiceUptime = uptime.TotalHours >= 1
                        ? $"{(int)uptime.TotalHours}h {uptime.Minutes}m"
                        : uptime.TotalMinutes >= 1
                            ? $"{uptime.Minutes}m {uptime.Seconds}s"
                            : $"{uptime.Seconds}s";
                }

                // Update widgets with sensor data
                if (sensorsResponse.Success && sensorsResponse.Sensors.Length > 0)
                {
                    UpdateWidgets(sensorsResponse.Sensors);
                }

                var sensorCount = Widgets.Count;
                SystemStatus = $"All systems nominal · {sensorCount} sensors · Updated {DateTime.Now:HH:mm:ss}";
            });
        }
        catch (OperationCanceledException) { /* expected */ }
        catch (InvalidOperationException ex)
        {
            RunOnUIThread(() =>
            {
                IsServiceConnected = false;
                SystemStatus = ex.Message;
            });
            _logger.LogWarning(ex, "IPC error on Home Page");
        }
        catch (IpcException ex)
        {
            RunOnUIThread(() => SystemStatus = $"Service error: {ex.Message}");
            _logger.LogError(ex, "IPC error on Home Page");
        }
        catch (Exception ex)
        {
            RunOnUIThread(() => SystemStatus = $"Error: {ex.Message}");
            _logger.LogError(ex, "Unexpected error on Home Page");
        }
        finally
        {
            RunOnUIThread(() => IsLoading = false);
        }
    }

    private void UpdateWidgets(GadgetSensorDto[] sensors)
    {
        _isUpdating = true;
        try
        {
            // Load saved layout on first load
            DashboardLayout? savedLayout = null;
            if (!_initialLoadDone)
            {
                savedLayout = LoadLayout();
            }

            // Only show visible sensors
            var visibleSensors = sensors.Where(s => s.IsVisible).ToArray();

            foreach (var dto in visibleSensors)
            {
                var existing = Widgets.FirstOrDefault(w => w.Label == dto.Label);
                if (existing is not null)
                {
                    // Update live value only
                    existing.Value = dto.Value;
                }
                else
                {
                    // Determine layout from saved data or defaults
                    var savedEntry = savedLayout?.Widgets.FirstOrDefault(w => w.Label == dto.Label);

                    var widget = new DashboardWidgetViewModel
                    {
                        Label = dto.Label,
                        SensorName = dto.SensorName,
                        Category = dto.Category,
                        Unit = dto.Unit,
                        Value = dto.Value,
                        DisplayOrder = savedEntry?.Order ?? dto.DisplayOrder,
                        WidgetSize = savedEntry?.Size ?? WidgetSize.Square,
                        ColumnSpan = savedEntry?.ColumnSpan ?? 1,
                        RowSpan = savedEntry?.RowSpan ?? 1,
                        IsInEditMode = IsEditMode
                    };

                    Widgets.Add(widget);
                }
            }

            // Remove widgets for sensors that are no longer visible
            var currentLabels = visibleSensors.Select(s => s.Label).ToHashSet();
            var toRemove = Widgets.Where(w => !currentLabels.Contains(w.Label)).ToList();
            foreach (var widget in toRemove)
            {
                Widgets.Remove(widget);
            }

            // Sort on first load only
            if (!_initialLoadDone)
            {
                var sorted = Widgets.OrderBy(w => w.DisplayOrder).ToList();
                Widgets.Clear();
                foreach (var w in sorted)
                {
                    Widgets.Add(w);
                }
                _initialLoadDone = true;
            }

            OnPropertyChanged(nameof(HasWidgets));
            OnPropertyChanged(nameof(ShowEmptyState));
        }
        finally
        {
            _isUpdating = false;
        }
    }

    // ── Quick Actions ──

    [RelayCommand]
    private async Task SendQuickAction(string command)
    {
        try
        {
            var config = await _settingsService.LoadServiceConfigurationAsync();
            string ip = config?.Rsm?.IpAddress ?? "localhost";
            int port = config?.Rsm?.Port ?? 5005;
            string secret = config?.Rsm?.Secret ?? string.Empty;
            string baseUrl = $"http://{ip}:{port}";

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/{command}");
            if (!string.IsNullOrEmpty(secret))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var response = await _httpClient.SendAsync(request, cts.Token);

            RunOnUIThread(() =>
            {
                QuickActionLog = response.IsSuccessStatusCode
                    ? $"✓ {command} sent successfully"
                    : $"✗ {command} failed: {response.StatusCode}";
            });
        }
        catch (Exception ex)
        {
            RunOnUIThread(() => QuickActionLog = $"✗ {command} error: {ex.Message}");
        }
    }

    // ── Layout Persistence ──

    /// <summary>Saves the current widget layout to a JSON file.</summary>
    [RelayCommand]
    public async Task SaveLayoutAsync()
    {
        try
        {
            var layout = new DashboardLayout
            {
                Widgets = Widgets.Select((w, i) => new WidgetLayoutEntry
                {
                    Label = w.Label,
                    Order = i,
                    ColumnSpan = w.ColumnSpan,
                    RowSpan = w.RowSpan,
                    Size = w.WidgetSize
                }).ToList()
            };

            // Update display order on view models
            for (int i = 0; i < Widgets.Count; i++)
            {
                Widgets[i].DisplayOrder = i;
            }

            string dir = Path.GetDirectoryName(LayoutFilePath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(layout, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(LayoutFilePath, json);

            _logger.LogInformation("Dashboard layout saved ({Count} widgets)", layout.Widgets.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save dashboard layout");
        }
    }

    /// <summary>Loads the saved widget layout from disk.</summary>
    private DashboardLayout? LoadLayout()
    {
        try
        {
            if (!File.Exists(LayoutFilePath)) return null;

            string json = File.ReadAllText(LayoutFilePath);
            return JsonSerializer.Deserialize<DashboardLayout>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load dashboard layout");
            return null;
        }
    }

    /// <summary>Saves sensor preferences back to the service (reorder/visibility).</summary>
    [RelayCommand]
    public async Task SaveSensorOrderAsync()
    {
        try
        {
            if (!_pipeClient.IsConnected) return;

            var preferences = Widgets.Select((w, index) => new SensorPreferenceDto
            {
                Label = w.Label,
                DisplayOrder = index,
                IsVisible = true, // All widgets on Home Page are visible
                CustomColor = null
            }).ToArray();

            var response = await _pipeClient.SendRequestAsync<SaveSensorPreferencesResponse>(
                new SaveSensorPreferencesRequest { Preferences = preferences },
                IpcConstants.DefaultTimeout,
                CancellationToken.None).ConfigureAwait(false);

            if (response.Success)
            {
                _logger.LogInformation("Saved sensor order for {Count} sensors", preferences.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error saving sensor order");
        }
    }

    // ── Cycle Widget Size ──

    /// <summary>Cycles a widget through size options: Square → Wide → Tall → Square.</summary>
    [RelayCommand]
    private void CycleWidgetSize(DashboardWidgetViewModel? widget)
    {
        if (widget is null) return;

        widget.WidgetSize = widget.WidgetSize switch
        {
            WidgetSize.Square => WidgetSize.Wide,
            WidgetSize.Wide => WidgetSize.Tall,
            WidgetSize.Tall => WidgetSize.Square,
            _ => WidgetSize.Square
        };

        // Update spans based on size
        switch (widget.WidgetSize)
        {
            case WidgetSize.Wide:
                widget.ColumnSpan = 2;
                widget.RowSpan = 1;
                break;
            case WidgetSize.Tall:
                widget.ColumnSpan = 1;
                widget.RowSpan = 2;
                break;
            default:
                widget.ColumnSpan = 1;
                widget.RowSpan = 1;
                break;
        }
    }

    // ── Manual Sorting ──

    [RelayCommand]
    private void MoveWidgetLeft(DashboardWidgetViewModel widget)
    {
        var index = Widgets.IndexOf(widget);
        if (index > 0)
        {
            Widgets.Move(index, index - 1);
        }
    }

    [RelayCommand]
    private void MoveWidgetRight(DashboardWidgetViewModel widget)
    {
        var index = Widgets.IndexOf(widget);
        if (index < Widgets.Count - 1)
        {
            Widgets.Move(index, index + 1);
        }
    }

    // ── UI Thread Helper ──

    private void RunOnUIThread(DispatcherQueueHandler action)
    {
        if (_dispatcherQueue.HasThreadAccess)
            action();
        else
            _dispatcherQueue.TryEnqueue(action);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        StopPolling();
    }
}
