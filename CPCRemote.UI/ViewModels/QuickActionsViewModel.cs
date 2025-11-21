using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CPCRemote.UI.Services;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace CPCRemote.UI.ViewModels
{
    public partial class QuickActionsViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;
        private static readonly HttpClient _httpClient = new();

        [ObservableProperty]
        public partial string ResponseLog { get; set; } = string.Empty;

        public QuickActionsViewModel(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [RelayCommand]
        private async Task Shutdown() => await SendCommandAsync("Shutdown");

        [RelayCommand]
        private async Task Restart() => await SendCommandAsync("Restart");

        [RelayCommand]
        private async Task Lock() => await SendCommandAsync("Lock");

        [RelayCommand]
        private async Task TurnScreenOff() => await SendCommandAsync("TurnScreenOff");

        [RelayCommand]
        private async Task WakeOnLan() => await SendCommandAsync("WakeOnLan");

        private async Task SendCommandAsync(string command)
        {
            try
            {
                var config = await _settingsService.LoadServiceConfigurationAsync();
                string ip = config?.Rsm?.IpAddress ?? "localhost";
                int port = config?.Rsm?.Port ?? 5005;
                string secret = config?.Rsm?.Secret ?? string.Empty;
                string baseUrl = $"http://{ip}:{port}";

                Log($"Sending '{command}' to {baseUrl}...");

                using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/{command}");
                if (!string.IsNullOrEmpty(secret))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
                }

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _httpClient.SendAsync(request, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    Log($"Success: {response.StatusCode}");
                }
                else
                {
                    Log($"Failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            ResponseLog += $"{DateTime.Now:HH:mm:ss}: {message}\n";
        }
    }
}