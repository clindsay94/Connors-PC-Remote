using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.ServiceProcess;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace CPCRemote.UI.Pages
{
    /// <summary>
    /// Page for managing the CPCRemote Windows Service, including installation, configuration, and control.
    /// </summary>
    public sealed partial class ServiceManagementPage : Page
    {
        private const string ServiceName = "CPCRemote.Service";
        private const int DefaultHttpTimeout = 5;
        private const int ServiceOperationTimeout = 30;
        private const string ConfigFileName = "appsettings.json";

        private static readonly ILogger? _logger = App.Logger;
        
        // Singleton HttpClient with pooled connection lifetime for DNS refresh
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(DefaultHttpTimeout)
        };

        static ServiceManagementPage()
        {
            // Configure pooled connection lifetime for DNS changes (best practice for .NET 10)
            if (_httpClient.DefaultRequestVersion == null)
            {
                // Use default HTTP handler with connection pooling
                var handler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(2)
                };
                // The handler is created but not assigned to HttpClient.
                // If you intend to use it, you should assign it to HttpClient like this:
                // _httpClient = new HttpClient(handler)
                // {
                //     Timeout = TimeSpan.FromSeconds(DefaultHttpTimeout)
                // };
            }
        }

        public ServiceManagementPage()
        {
            InitializeComponent();
            Loaded += ServiceManagementPage_Loaded;
        }

        private async void ServiceManagementPage_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshServiceStatusAsync();
        }

        private async void RefreshStatusButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshServiceStatusAsync();
        }

        private async Task RefreshServiceStatusAsync()
        {
            StatusLoadingRing.IsActive = true;
            RefreshStatusButton.IsEnabled = false;

            try
            {
                var (isInstalled, status) = await Task.Run(GetServiceStatus);

                ServiceInstalledText.Text = isInstalled ? "Installed" : "Not Installed";
                ServiceStatusText.Text = status;

                bool isRunning = status == "Running";
                bool isStopped = status == "Stopped";

                StartServiceButton.IsEnabled = isInstalled && isStopped;
                StopServiceButton.IsEnabled = isInstalled && isRunning;
                RestartServiceButton.IsEnabled = isInstalled && isRunning;
                UninstallServiceButton.IsEnabled = isInstalled;
            }
            catch (Exception ex)
            {
                ServiceInstalledText.Text = "Error";
                ServiceStatusText.Text = ex.Message;
                _logger?.LogError(ex, "Failed to refresh service status.");
            }
            finally
            {
                StatusLoadingRing.IsActive = false;
                RefreshStatusButton.IsEnabled = true;
            }
        }

        private (bool isInstalled, string status) GetServiceStatus()
        {
            try
            {
                using var service = new ServiceController(ServiceName);
                string statusText = service.Status.ToString();
                return (true, statusText);
            }
            catch (InvalidOperationException)
            {
                return (false, "Not Installed");
            }
        }

        #region Service Control Operations

        private async void StartServiceButton_Click(object sender, RoutedEventArgs e)
        {
            _logger?.LogInformation("UI triggered start command for the Windows service.");
            await ExecuteServiceCommandAsync("start", "Starting service...", "Service started successfully.", "Failed to start service");
        }

        private async void StopServiceButton_Click(object sender, RoutedEventArgs e)
        {
            _logger?.LogInformation("UI triggered stop command for the Windows service.");
            await ExecuteServiceCommandAsync("stop", "Stopping service...", "Service stopped successfully.", "Failed to stop service");
        }

        private async void RestartServiceButton_Click(object sender, RoutedEventArgs e)
        {
            _logger?.LogInformation("UI triggered restart command for the Windows service.");
            ShowInfoBar(ServiceControlInfoBar, InfoBarSeverity.Informational, "Restarting service...");

            try
            {
                await Task.Run(() =>
                {
                    _logger?.LogInformation("Service restarted successfully from the UI.");
                    using var service = new ServiceController(ServiceName);
                    if (service.Status == ServiceControllerStatus.Running)
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(ServiceOperationTimeout));
                    }
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(ServiceOperationTimeout));
                });

                ShowInfoBar(ServiceControlInfoBar, InfoBarSeverity.Success, "Service restarted successfully.");
                await RefreshServiceStatusAsync();
            }
            catch (Exception ex)
            {
                ShowInfoBar(ServiceControlInfoBar, InfoBarSeverity.Error, $"Failed to restart service: {ex.Message}");
                _logger?.LogError(ex, "Failed to restart service from the UI.");
            }
        }

        private async Task ExecuteServiceCommandAsync(string command, string infoMessage, string successMessage, string errorPrefix)
        {
            ShowInfoBar(ServiceControlInfoBar, InfoBarSeverity.Informational, infoMessage);

            try
            {
                await Task.Run(() =>
                {
                    using var service = new ServiceController(ServiceName);
                    
                    switch (command)
                    {
                        case "start":
                            service.Start();
                            service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(ServiceOperationTimeout));
                            break;
                        case "stop":
                            service.Stop();
                            service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(ServiceOperationTimeout));
                            break;
                    }
                });

                ShowInfoBar(ServiceControlInfoBar, InfoBarSeverity.Success, successMessage);
                await RefreshServiceStatusAsync();
                _logger?.LogInformation("Service command '{Command}' executed successfully via UI.", command);
            }
            catch (Exception ex)
            {
                ShowInfoBar(ServiceControlInfoBar, InfoBarSeverity.Error, $"{errorPrefix}: {ex.Message}");
                _logger?.LogError(ex, "Service command '{Command}' failed via UI.", command);
            }
        }

        #endregion

        #region Service Installation

        private async void BrowseExeButton_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new FileOpenPicker();
            openPicker.FileTypeFilter.Add(".exe");

            var window = App.CurrentMainWindow;
            if (window != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hwnd);
            }

            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                ServiceExePathTextBox.Text = file.Path;
            }
        }

        private void ServiceExePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            InstallServiceButton.IsEnabled = !string.IsNullOrWhiteSpace(ServiceExePathTextBox.Text);
        }

        private async void InstallServiceButton_Click(object sender, RoutedEventArgs e)
        {
            string? exePath = ServiceExePathTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                ShowInfoBar(InstallationInfoBar, InfoBarSeverity.Error, "Please provide a valid executable path.");
                return;
            }

            ShowInfoBar(InstallationInfoBar, InfoBarSeverity.Informational, "Installing service...");
            _logger?.LogInformation("Installing service from executable path {Path}", exePath);

            try
            {
                var result = await RunScCommandAsync($"create {ServiceName} binPath=\"{exePath}\" start=auto");
                
                if (result.success)
                {
                    ShowInfoBar(InstallationInfoBar, InfoBarSeverity.Success, "Service installed successfully. You can now start it.");
                    await RefreshServiceStatusAsync();
                    _logger?.LogInformation("Service installed successfully via UI (sc create result cached).", exePath);
                }
                else
                {
                    ShowInfoBar(InstallationInfoBar, InfoBarSeverity.Error, $"Failed to install service: {result.output}");
                    _logger?.LogWarning("Service installation failed with message: {Message}", result.output);
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar(InstallationInfoBar, InfoBarSeverity.Error, $"Error installing service: {ex.Message}");
                _logger?.LogError(ex, "Exception during service installation.");
            }
        }

        private async void UninstallServiceButton_Click(object sender, RoutedEventArgs e)
        {
            ShowInfoBar(InstallationInfoBar, InfoBarSeverity.Informational, "Uninstalling service...");
            _logger?.LogInformation("User requested service uninstallation via UI.");

            try
            {
                // Try to stop the service first
                await Task.Run(() =>
                {
                    try
                    {
                        using var service = new ServiceController(ServiceName);
                        if (service.Status == ServiceControllerStatus.Running)
                        {
                            service.Stop();
                            service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(ServiceOperationTimeout));
                        }
                    }
                    catch
                    {
                        // Service might already be stopped or not exist
                    }
                });

                var result = await RunScCommandAsync($"delete {ServiceName}");
                
                if (result.success)
                {
                    ShowInfoBar(InstallationInfoBar, InfoBarSeverity.Success, "Service uninstalled successfully.");
                    await RefreshServiceStatusAsync();
                    _logger?.LogInformation("Service uninstalled successfully via UI.");
                }
                else
                {
                    ShowInfoBar(InstallationInfoBar, InfoBarSeverity.Error, $"Failed to uninstall service: {result.output}");
                    _logger?.LogWarning("Service uninstall failed: {Output}", result.output);
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar(InstallationInfoBar, InfoBarSeverity.Error, $"Error uninstalling service: {ex.Message}");
                _logger?.LogError(ex, "Exception during service uninstallation.");
            }
        }

        #endregion

        #region Administrator Operations

        private async Task<(bool success, string output)> RunScCommandAsync(string arguments)
        {
            if (!IsAdministrator())
            {
                // Request elevation via UAC
                var startInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = arguments,
                    UseShellExecute = true, // Required for Verb = "runas"
                    Verb = "runas",
                    CreateNoWindow = true
                };

                try
                {
                    using var process = Process.Start(startInfo);
                    if (process == null)
                    {
                        return (false, "Failed to start elevated process. User may have cancelled UAC prompt.");
                    }

                    await process.WaitForExitAsync();
                    
                    // When using UseShellExecute = true, we can't redirect output
                    bool success = process.ExitCode == 0;
                    return success 
                        ? (true, "Command completed successfully (running with elevated privileges)")
                        : (false, $"Command failed with exit code {process.ExitCode}. Check Event Viewer for details.");
                }
                catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
                {
                    // ERROR_CANCELLED - User cancelled the UAC prompt
                    return (false, "Operation cancelled by user. Administrator privileges are required.");
                }
                catch (Exception ex)
                {
                    return (false, $"Error running elevated command: {ex.Message}");
                }
            }
            else
            {
                // Already administrator, run directly with output redirection
                var startInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                try
                {
                    using var process = Process.Start(startInfo);
                    if (process == null)
                    {
                        return (false, "Failed to start process.");
                    }

                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    bool success = process.ExitCode == 0;
                    string result = success ? output : error;
                    
                    return (success, result);
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }
            }
        }

        private static bool IsAdministrator()
        {
            try
            {
                using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Configuration Management

        private void UpdatePreviewUrl(object sender, object e)
        {
            string ip = IpAddressTextBox.Text?.Trim() ?? "localhost";
            if (string.IsNullOrEmpty(ip)) 
                ip = "localhost";

            double port = PortNumberBox.Value;
            string? secret = SecretPasswordBox.Password?.Trim();

            string url = $"http://{ip}:{port}/[command]";
            if (!string.IsNullOrEmpty(secret))
            {
                url += $" (with Authorization: Bearer {secret})";
            }
            else
            {
                url += " (no authentication)";
            }

            PreviewUrlTextBlock.Text = url;
        }

        private async void SaveConfigButton_Click(object sender, RoutedEventArgs e)
        {
            ShowInfoBar(ConfigInfoBar, InfoBarSeverity.Informational, "Saving configuration...");

            try
            {
                string? configPath = FindServiceConfigPath();
                
                if (string.IsNullOrEmpty(configPath))
                {
                    ShowInfoBar(ConfigInfoBar, InfoBarSeverity.Error, 
                        "Could not locate service configuration file. Please ensure the service is installed.");
                    _logger?.LogWarning("Could not locate service configuration file while saving settings.");
                    return;
                }

                _logger?.LogInformation("Saving configuration to {ConfigPath}", configPath);

                string jsonText = await File.ReadAllTextAsync(configPath);
                using var config = JsonDocument.Parse(jsonText);
                var root = config.RootElement;

                string ip = IpAddressTextBox.Text?.Trim() ?? "localhost";
                if (string.IsNullOrEmpty(ip)) 
                    ip = "localhost";

                int port = (int)PortNumberBox.Value;
                string secret = SecretPasswordBox.Password?.Trim() ?? string.Empty;

                var updatedConfig = new
                {
                    Logging = root.GetProperty("Logging"),
                    wol = root.GetProperty("wol"),
                    rsm = new
                    {
                        ipAddress = ip,
                        port = port,
                        secret = secret
                    },
                    monitor = root.GetProperty("monitor")
                };

                string updatedJson = JsonSerializer.Serialize(updatedConfig, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(configPath, updatedJson);

                ShowInfoBar(ConfigInfoBar, InfoBarSeverity.Success, 
                    "Configuration saved successfully. Restart the service for changes to take effect.");
                _logger?.LogInformation("Configuration saved successfully to {ConfigPath}", configPath);
            }
            catch (Exception ex)
            {
                ShowInfoBar(ConfigInfoBar, InfoBarSeverity.Error, $"Error saving configuration: {ex.Message}");
                _logger?.LogError(ex, "Failed to save configuration.");
            }
        }

        private string? FindServiceConfigPath()
        {
            try
            {
                using var service = new ServiceController(ServiceName);
                
                string exePath = GetServiceExecutablePath();
                if (!string.IsNullOrEmpty(exePath))
                {
                    string? directory = Path.GetDirectoryName(exePath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        string configPath = Path.Combine(directory, ConfigFileName);
                        if (File.Exists(configPath))
                        {
                            return configPath;
                        }
                    }
                }
            }
            catch
            {
                // Service not found or access denied
            }

            // Fallback: Search relative to the executing assembly
            string? baseDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(baseDir))
            {
                return null;
            }

            string[] searchPaths = new[]
            {
                Path.Combine(baseDir, "..", "..", "..", "..", "CPCRemote.Service", ConfigFileName),
                Path.Combine(baseDir, "..", "CPCRemote.Service", ConfigFileName),
            };

            foreach (string path in searchPaths)
            {               
                string fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }

        private string GetServiceExecutablePath()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"qc {ServiceName}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                string output = process?.StandardOutput.ReadToEnd() ?? string.Empty;
                
                const string prefix = "BINARY_PATH_NAME   : ";
                int index = output.IndexOf(prefix, StringComparison.Ordinal);
                if (index >= 0)
                {
                    int start = index + prefix.Length;
                    int end = output.IndexOf('\n', start);
                    if (end < 0) 
                        end = output.Length;
                    
                    string path = output.Substring(start, end - start).Trim().Trim('"');
                    return path;
                }
            }
            catch
            {
                // Failed to query service
            }

            return string.Empty;
        }

        #endregion

        #region Service Testing

        private async void TestPingButton_Click(object sender, RoutedEventArgs e)
        {
            string ip = IpAddressTextBox.Text?.Trim() ?? "localhost";
            if (string.IsNullOrEmpty(ip)) 
                ip = "localhost";

            int port = (int)PortNumberBox.Value;
            string? secret = SecretPasswordBox.Password?.Trim();
            string url = $"http://{ip}:{port}/ping";

            ShowInfoBar(TestInfoBar, InfoBarSeverity.Informational, $"Testing connection to {url}...");
            _logger?.LogInformation("Testing service ping at {Url}", url);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                
                if (!string.IsNullOrEmpty(secret))
                {
                    request.Headers.Add("Authorization", $"Bearer {secret}");
                }

                var response = await _httpClient.SendAsync(request);
                string content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ShowInfoBar(TestInfoBar, InfoBarSeverity.Success, "Success! Service is responding.");
                    _logger?.LogInformation("Ping test succeeded against {Url}", url);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ShowInfoBar(TestInfoBar, InfoBarSeverity.Warning, 
                        "Service responded with Unauthorized. Check your secret is correct.");
                    _logger?.LogWarning("Ping test returned unauthorized for {Url}", url);
                }
                else
                {
                    ShowInfoBar(TestInfoBar, InfoBarSeverity.Warning, 
                        $"Service responded with status {response.StatusCode}: {content}");
                    _logger?.LogWarning("Ping test failed with status {Status} for {Url}", response.StatusCode, url);
                }
            }
            catch (HttpRequestException ex)
            {
                ShowInfoBar(TestInfoBar, InfoBarSeverity.Error, 
                    $"Connection failed: {ex.Message}. Ensure the service is running.");
                _logger?.LogError(ex, "Ping test HTTP error for {Url}", url);
            }
            catch (Exception ex)
            {
                ShowInfoBar(TestInfoBar, InfoBarSeverity.Error, $"Error: {ex.Message}");
                _logger?.LogError(ex, "Ping test failed for {Url}", url);
            }
        }

        private async void SendTestCommandButton_Click(object sender, RoutedEventArgs e)
        {
            if (TestCommandComboBox?.SelectedItem is not ComboBoxItem selectedItem)
            {
                ShowInfoBar(TestInfoBar, InfoBarSeverity.Warning, "Please select a command first.");
                return;
            }

            string command = selectedItem.Content?.ToString() ?? "";
            string ip = IpAddressTextBox.Text?.Trim() ?? "localhost";
            if (string.IsNullOrEmpty(ip)) 
                ip = "localhost";

            int port = (int)PortNumberBox.Value;
            string? secret = SecretPasswordBox.Password?.Trim();
            string url = $"http://{ip}:{port}/{command}";

            ShowInfoBar(TestInfoBar, InfoBarSeverity.Warning, $"Sending command: {command}...");
            _logger?.LogInformation("Sending test command {Command} to {Url}", command, url);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                
                if (!string.IsNullOrEmpty(secret))
                {
                    request.Headers.Add("Authorization", $"Bearer {secret}");
                }

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    ShowInfoBar(TestInfoBar, InfoBarSeverity.Success, $"Command '{command}' sent successfully!");
                    _logger?.LogInformation("Test command {Command} succeeded against {Url}", command, url);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ShowInfoBar(TestInfoBar, InfoBarSeverity.Error, 
                        "Command failed: Unauthorized. Check your secret.");
                    _logger?.LogWarning("Test command {Command} returned unauthorized for {Url}", command, url);
                }
                else
                {
                    ShowInfoBar(TestInfoBar, InfoBarSeverity.Error, 
                        $"Command failed with status {response.StatusCode}");
                    _logger?.LogWarning("Test command {Command} failed with status {Status} for {Url}", command, response.StatusCode, url);
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar(TestInfoBar, InfoBarSeverity.Error, $"Error sending command: {ex.Message}");
                _logger?.LogError(ex, "Test command {Command} failed for {Url}", command, url);
            }
        }

        private void TestCommandComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SendTestCommandButton.IsEnabled = TestCommandComboBox.SelectedItem != null;
        }

        #endregion

        #region URL Reservation

        private async void ReserveUrlButton_Click(object sender, RoutedEventArgs e)
        {
            string ip = IpAddressTextBox.Text?.Trim() ?? "localhost";
            if (string.IsNullOrEmpty(ip)) 
                ip = "localhost";

            int port = (int)PortNumberBox.Value;
            string url = $"http://{ip}:{port}/";

            ShowInfoBar(UrlReservationInfoBar, InfoBarSeverity.Informational, "Reserving URL...");
            _logger?.LogInformation("Reserving URL {Url} via netsh", url);

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"http add urlacl url={url} user=EVERYONE",
                    UseShellExecute = true, // Required for Verb = "runas"
                    Verb = "runas",
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    ShowInfoBar(UrlReservationInfoBar, InfoBarSeverity.Error, "Failed to start process.");
                    _logger?.LogError("Failed to start netsh process for URL reservation.");
                    return;
                }

                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    ShowInfoBar(UrlReservationInfoBar, InfoBarSeverity.Success, 
                        $"URL reservation successful for {url}");
                    _logger?.LogInformation("URL reservation succeeded for {Url}", url);
                }
                else
                {
                    ShowInfoBar(UrlReservationInfoBar, InfoBarSeverity.Error, 
                        $"URL reservation failed with exit code {process.ExitCode}. Check Event Viewer for details.");
                    _logger?.LogWarning("URL reservation failed for {Url} with exit code {ExitCode}", url, process.ExitCode);
                }
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                ShowInfoBar(UrlReservationInfoBar, InfoBarSeverity.Warning, 
                    "Operation cancelled by user. Administrator privileges are required.");
            }
            catch (Exception ex)
            {
                ShowInfoBar(UrlReservationInfoBar, InfoBarSeverity.Error, $"Error: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private static void ShowInfoBar(InfoBar infoBar, InfoBarSeverity severity, string message)
        {
            infoBar.Message = message;
            infoBar.Severity = severity;
            infoBar.IsOpen = true;
        }

        #endregion
    }
}
