using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace CPCRemote.UI
{
    public sealed partial class QuickActionsPage : Page
    {
        private static readonly HttpClient Client = new();
        // Configured IP/Port from requirements
        private const string BaseUrl = "http://10.0.0.69:5005";
        private const string Secret = "Slate123";

        public QuickActionsPage()
        {
            this.InitializeComponent();
        }

        private async void ShutdownBtn_Click(object sender, RoutedEventArgs e) => await SendCommandAsync("Shutdown");
        private async void RestartBtn_Click(object sender, RoutedEventArgs e) => await SendCommandAsync("Restart");
        private async void LockBtn_Click(object sender, RoutedEventArgs e) => await SendCommandAsync("Lock");
        private async void ScreenOffBtn_Click(object sender, RoutedEventArgs e) => await SendCommandAsync("TurnScreenOff");
        private async void WolBtn_Click(object sender, RoutedEventArgs e) => await SendCommandAsync("WakeOnLan");

        private async Task SendCommandAsync(string command)
        {
            try
            {
                Log($"Sending '{command}' to {BaseUrl}...");
                
                // Create request with Auth header
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/{command}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Secret);

                // Set a short timeout for UI responsiveness
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                
                var response = await Client.SendAsync(request, cts.Token);
                
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
            ResponseLog.Text += $"{DateTime.Now:HH:mm:ss}: {message}\n";
            // Auto-scroll
            if (ResponseLog.Text.Length > 0)
            {
                ResponseLog.Select(ResponseLog.Text.Length, 0);
            }
        }
    }
}
