namespace CPCRemote.UI.Pages;

using System;
using System.Runtime.Versioning;

using CPCRemote.UI.ViewModels;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

/// <summary>
/// Dashboard page displaying live hardware stats and service status.
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
                case nameof(DashboardViewModel.CpuLoad):
                    UpdateCpuLoad();
                    break;
                case nameof(DashboardViewModel.MemoryLoad):
                    UpdateMemoryLoad();
                    break;
                case nameof(DashboardViewModel.CpuTemp):
                    CpuTempText.Text = _viewModel.CpuTemp?.ToString("F1") ?? "--";
                    break;
                case nameof(DashboardViewModel.GpuTemp):
                    GpuTempText.Text = _viewModel.GpuTemp?.ToString("F1") ?? "--";
                    break;
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

    private void UpdateCpuLoad()
    {
        if (_viewModel.CpuLoad.HasValue)
        {
            CpuLoadText.Text = _viewModel.CpuLoad.Value.ToString("F1");
            CpuProgressBar.Value = _viewModel.CpuLoad.Value;
        }
        else
        {
            CpuLoadText.Text = "--";
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
