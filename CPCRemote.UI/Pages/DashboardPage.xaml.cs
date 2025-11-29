namespace CPCRemote.UI.Pages;

using System;
using System.Runtime.Versioning;

using CPCRemote.UI.ViewModels;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

/// <summary>
/// Dashboard page displaying comprehensive hardware monitoring from HWiNFO.
/// </summary>
[SupportedOSPlatform("windows10.0.22621.0")]
public sealed partial class DashboardPage : Page
{
    private readonly DashboardViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardPage"/> class.
    /// </summary>
    public DashboardPage()
    {
        this.InitializeComponent();
        _viewModel = App.GetService<DashboardViewModel>();

        // Wire up property changes
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Start polling when page loads
        Loaded += DashboardPage_Loaded;
        Unloaded += DashboardPage_Unloaded;
    }

    private void DashboardPage_Loaded(object sender, RoutedEventArgs e)
    {
        _viewModel.StartPolling();
    }

    private void DashboardPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _viewModel.StopPolling();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Update UI on property changes (run on UI thread)
        DispatcherQueue.TryEnqueue(() =>
        {
            switch (e.PropertyName)
            {
                // CPU Stats
                case nameof(DashboardViewModel.CpuUtility):
                    UpdateCpuUtility();
                    break;
                case nameof(DashboardViewModel.CpuTemp):
                    CpuTempText.Text = _viewModel.CpuTemp?.ToString("F1") ?? "--";
                    break;
                case nameof(DashboardViewModel.CpuDieAvgTemp):
                    CpuDieAvgText.Text = _viewModel.CpuDieAvgTemp?.ToString("F1") ?? "--";
                    break;
                case nameof(DashboardViewModel.CpuIodHotspot):
                    CpuIodHotspotText.Text = _viewModel.CpuIodHotspot?.ToString("F1") ?? "--";
                    break;
                case nameof(DashboardViewModel.CpuPackagePower):
                    CpuPackagePowerText.Text = _viewModel.CpuPackagePower?.ToString("F1") ?? "--";
                    break;
                case nameof(DashboardViewModel.CpuPpt):
                    CpuPptText.Text = _viewModel.CpuPpt?.ToString("F1") ?? "--";
                    break;
                case nameof(DashboardViewModel.CpuEffectiveClock):
                    CpuEffectiveClockText.Text = _viewModel.CpuEffectiveClock?.ToString("F0") ?? "--";
                    break;
                case nameof(DashboardViewModel.CpuCoreEffectiveClocks):
                    UpdatePerCoreClocks();
                    break;

                // Memory Stats
                case nameof(DashboardViewModel.MemoryLoad):
                    UpdateMemoryLoad();
                    break;
                case nameof(DashboardViewModel.DimmTemps):
                    UpdateDimmTemps();
                    break;

                // GPU Stats
                case nameof(DashboardViewModel.GpuTemp):
                    GpuTempText.Text = _viewModel.GpuTemp?.ToString("F1") ?? "--";
                    break;
                case nameof(DashboardViewModel.GpuMemJunctionTemp):
                    GpuMemTempText.Text = _viewModel.GpuMemJunctionTemp?.ToString("F1") ?? "--";
                    break;
                case nameof(DashboardViewModel.GpuPower):
                    GpuPowerText.Text = _viewModel.GpuPower?.ToString("F1") ?? "--";
                    break;
                case nameof(DashboardViewModel.GpuClock):
                    GpuClockText.Text = _viewModel.GpuClock?.ToString("F0") ?? "--";
                    break;
                case nameof(DashboardViewModel.GpuEffectiveClock):
                    GpuEffectiveClockText.Text = _viewModel.GpuEffectiveClock?.ToString("F0") ?? "--";
                    break;
                case nameof(DashboardViewModel.GpuMemoryUsage):
                    UpdateGpuMemoryUsage();
                    break;
                case nameof(DashboardViewModel.GpuCoreLoad):
                    UpdateGpuCoreLoad();
                    break;

                // Motherboard Stats
                case nameof(DashboardViewModel.Vcore):
                    VcoreText.Text = _viewModel.Vcore?.ToString("F3") ?? "--";
                    break;
                case nameof(DashboardViewModel.Vsoc):
                    VsocText.Text = _viewModel.Vsoc?.ToString("F3") ?? "--";
                    break;

                // Service Status
                case nameof(DashboardViewModel.IsServiceConnected):
                    UpdateConnectionStatus();
                    break;
                case nameof(DashboardViewModel.HttpListenerAddress):
                    HttpAddressText.Text = _viewModel.HttpListenerAddress ?? string.Empty;
                    break;
                case nameof(DashboardViewModel.ServiceUptime):
                    UptimeText.Text = _viewModel.ServiceUptime is not null ? $"Uptime: {_viewModel.ServiceUptime}" : string.Empty;
                    break;
                case nameof(DashboardViewModel.StatusMessage):
                    StatusText.Text = _viewModel.StatusMessage ?? string.Empty;
                    break;
                case nameof(DashboardViewModel.IsLoading):
                    LoadingRing.IsActive = _viewModel.IsLoading;
                    break;
            }
        });
    }

    private void UpdateCpuUtility()
    {
        if (_viewModel.CpuUtility.HasValue)
        {
            CpuUtilityText.Text = _viewModel.CpuUtility.Value.ToString("F1");
            CpuProgressBar.Value = _viewModel.CpuUtility.Value;
        }
        else
        {
            CpuUtilityText.Text = "--";
            CpuProgressBar.Value = 0;
        }
    }

    private void UpdateMemoryLoad()
    {
        if (_viewModel.MemoryLoad.HasValue)
        {
            MemoryLoadText.Text = _viewModel.MemoryLoad.Value.ToString("F1");
            MemoryProgressBar.Value = _viewModel.MemoryLoad.Value;
        }
        else
        {
            MemoryLoadText.Text = "--";
            MemoryProgressBar.Value = 0;
        }
    }

    private void UpdateDimmTemps()
    {
        Dimm1TempText.Text = "--";
        Dimm2TempText.Text = "--";

        if (_viewModel.DimmTemps is not null)
        {
            foreach (var dimm in _viewModel.DimmTemps)
            {
                switch (dimm.Slot)
                {
                    case 1:
                        Dimm1TempText.Text = dimm.Temp.ToString("F1");
                        break;
                    case 2:
                        Dimm2TempText.Text = dimm.Temp.ToString("F1");
                        break;
                }
            }
        }
    }

    private void UpdateGpuCoreLoad()
    {
        if (_viewModel.GpuCoreLoad.HasValue)
        {
            GpuCoreLoadText.Text = _viewModel.GpuCoreLoad.Value.ToString("F1");
            GpuLoadProgressBar.Value = _viewModel.GpuCoreLoad.Value;
        }
        else
        {
            GpuCoreLoadText.Text = "--";
            GpuLoadProgressBar.Value = 0;
        }
    }

    private void UpdateGpuMemoryUsage()
    {
        if (_viewModel.GpuMemoryUsage.HasValue)
        {
            GpuMemoryUsageText.Text = _viewModel.GpuMemoryUsage.Value.ToString("F1");
            GpuMemoryProgressBar.Value = _viewModel.GpuMemoryUsage.Value;
        }
        else
        {
            GpuMemoryUsageText.Text = "--";
            GpuMemoryProgressBar.Value = 0;
        }
    }

    private void UpdatePerCoreClocks()
    {
        var clocks = _viewModel.CpuCoreEffectiveClocks;
        Core0Text.Text = clocks is { Length: > 0 } ? clocks[0].ToString("F0") : "--";
        Core1Text.Text = clocks is { Length: > 1 } ? clocks[1].ToString("F0") : "--";
        Core2Text.Text = clocks is { Length: > 2 } ? clocks[2].ToString("F0") : "--";
        Core3Text.Text = clocks is { Length: > 3 } ? clocks[3].ToString("F0") : "--";
        Core4Text.Text = clocks is { Length: > 4 } ? clocks[4].ToString("F0") : "--";
        Core5Text.Text = clocks is { Length: > 5 } ? clocks[5].ToString("F0") : "--";
        Core6Text.Text = clocks is { Length: > 6 } ? clocks[6].ToString("F0") : "--";
        Core7Text.Text = clocks is { Length: > 7 } ? clocks[7].ToString("F0") : "--";
    }

    private void UpdateConnectionStatus()
    {
        if (_viewModel.IsServiceConnected)
        {
            ConnectionIndicator.Background = new SolidColorBrush(Colors.LimeGreen);
            ServiceStatusText.Text = "Service: Connected";
        }
        else
        {
            ConnectionIndicator.Background = new SolidColorBrush(Colors.Red);
            ServiceStatusText.Text = "Service: Disconnected";
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _ = _viewModel.RefreshStatsCommand.ExecuteAsync(null);
    }

    private void PollingToggle_Toggled(object sender, RoutedEventArgs e)
    {
        // Guard against null ViewModel during initialization
        if (_viewModel is null)
        {
            return;
        }

        if (PollingToggle.IsOn)
        {
            _viewModel.StartPolling();
        }
        else
        {
            _viewModel.StopPolling();
        }
    }

    private void IntervalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Guard against null ViewModel during initialization
        if (_viewModel is null)
        {
            return;
        }

        if (IntervalComboBox.SelectedItem is ComboBoxItem selectedItem &&
            selectedItem.Tag is string tagValue &&
            int.TryParse(tagValue, out int seconds))
        {
            _viewModel.PollingIntervalSeconds = seconds;
        }
    }
}
