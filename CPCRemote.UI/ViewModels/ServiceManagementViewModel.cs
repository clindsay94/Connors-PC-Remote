using CPCRemote.UI.Services;
using CPCRemote.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Net.Http;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;

namespace CPCRemote.UI.ViewModels
{
    public partial class ServiceManagementViewModel : ObservableObject
    {
        private const string ServiceName = "CPCRemote.Service";
        private const int DefaultHttpTimeout = 5;
        private const int ServiceOperationTimeout = 30;
        private const string ConfigFileName = "appsettings.json";

        private readonly ILogger<ServiceManagementViewModel> _logger;
        private readonly SettingsService _settingsService;
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(DefaultHttpTimeout)
        };

        [ObservableProperty]
        public partial string? ServiceStatusText { get; set; }

        [ObservableProperty]
        public partial string? ServiceInstalledText { get; set; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartServiceCommand))]
        [NotifyCanExecuteChangedFor(nameof(StopServiceCommand))]
        [NotifyCanExecuteChangedFor(nameof(RestartServiceCommand))]
        public partial bool IsServiceInstalled { get; set; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartServiceCommand))]
        [NotifyCanExecuteChangedFor(nameof(StopServiceCommand))]
        [NotifyCanExecuteChangedFor(nameof(RestartServiceCommand))]
        public partial bool IsServiceRunning { get; set; }

        [ObservableProperty]
        public partial bool IsBusy { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PreviewUrl))]
        public partial string? IpAddress { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PreviewUrl))]
        public partial double Port { get; set; }

        [ObservableProperty]
        public partial string? Secret { get; set; }

        [ObservableProperty]
        public partial bool IsInfoBarOpen { get; set; }

        [ObservableProperty]
        public partial string? InfoBarMessage { get; set; }

        [ObservableProperty]
        public partial InfoBarSeverity InfoBarSeverity { get; set; }

        [ObservableProperty]
        public partial bool IsSafetyLockEnabled { get; set; }

        [ObservableProperty]
        public partial string? ServiceExecutablePath { get; set; }

        private bool _isShutdownEnabled;
        public bool IsShutdownEnabled
        {
            get => _isShutdownEnabled;
            set
            {
                if (_isShutdownEnabled != value)
                {
                    _isShutdownEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public ServiceManagementViewModel(ILogger<ServiceManagementViewModel> logger, SettingsService settingsService)
        {
            _logger = logger;
            _settingsService = settingsService;
            ServiceStatusText = "Unknown";
            ServiceInstalledText = "Unknown";
            IsSafetyLockEnabled = _settingsService.Get<bool>(nameof(IsSafetyLockEnabled), true);
            ServiceExecutablePath = FindServiceExecutable();

            LoadConfigurationCommand.Execute(null);
            RefreshStatusCommand.Execute(null);
        }

        [RelayCommand]
        private async Task LoadConfiguration()
        {
            var config = await _settingsService.LoadServiceConfigurationAsync();
            if (config != null)
            {
                IpAddress = config.Rsm.IpAddress;
                Port = config.Rsm.Port;
                Secret = config.Rsm.Secret;
            }
            else
            {
                IpAddress = "localhost";
                Port = 5005;
                Secret = string.Empty;
            }
        }

        [RelayCommand]
        private async Task SaveConfiguration()
        {
            ShowInfoBar("Saving configuration...", InfoBarSeverity.Informational);

            try
            {
                // 1. Save to local UI settings (existing logic)
                var config = new ServiceConfiguration
                {
                    Rsm = new RsmOptions
                    {
                        IpAddress = this.IpAddress,
                        Port = (int)this.Port,
                        Secret = this.Secret,
                    }
                };

                await _settingsService.SaveServiceConfigurationAsync(config);
                _settingsService.Set(nameof(IsSafetyLockEnabled), IsSafetyLockEnabled);

                // 2. Save to Service's appsettings.json
                string? serviceConfigPath = FindServiceConfigPath();
                if (!string.IsNullOrEmpty(serviceConfigPath))
                {
                    try
                    {
                        string jsonContent = await System.IO.File.ReadAllTextAsync(serviceConfigPath);
                        var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(jsonContent);

                        if (jsonNode != null)
                        {
                            // Ensure 'rsm' section exists
                            if (jsonNode["rsm"] is not System.Text.Json.Nodes.JsonObject rsmNode)
                            {
                                rsmNode = new System.Text.Json.Nodes.JsonObject();
                                jsonNode["rsm"] = rsmNode;
                            }

                            // Helper to update or add property case-insensitively
                            void UpdateProperty(System.Text.Json.Nodes.JsonObject node, string propertyName, System.Text.Json.Nodes.JsonNode? value)
                            {
                                var existingKey = node.Select(x => x.Key).FirstOrDefault(k => k.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
                                if (existingKey != null)
                                {
                                    node[existingKey] = value;
                                }
                                else
                                {
                                    node[propertyName] = value;
                                }
                            }

                            UpdateProperty(rsmNode, "IpAddress", System.Text.Json.Nodes.JsonValue.Create(this.IpAddress));
                            UpdateProperty(rsmNode, "Port", System.Text.Json.Nodes.JsonValue.Create(this.Port));
                            UpdateProperty(rsmNode, "Secret", System.Text.Json.Nodes.JsonValue.Create(this.Secret));

                            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                            await System.IO.File.WriteAllTextAsync(serviceConfigPath, jsonNode.ToJsonString(options));
                            
                            _logger.LogInformation("Updated service configuration at {Path}", serviceConfigPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to write to service configuration file at {Path}", serviceConfigPath);
                        throw new Exception($"Saved to UI settings, but failed to update Service config file: {ex.Message}. Try running as Administrator.");
                    }
                }
                else
                {
                    _logger.LogWarning("Could not locate service configuration file to update.");
                    ShowInfoBar("Saved to UI, but could not find Service config file. Service may not be updated.", InfoBarSeverity.Warning);
                    return;
                }

                ShowInfoBar("Configuration saved successfully. Restart the service for these settings to be applied.", InfoBarSeverity.Success);
                _logger.LogInformation("Configuration saved successfully.");
            }
            catch (Exception ex)
            {
                ShowInfoBar($"Error saving configuration: {ex.Message}", InfoBarSeverity.Error);
                _logger.LogError(ex, "Failed to save configuration.");
            }
        }

        [RelayCommand]
        private async Task RefreshStatus()
        {
            IsBusy = true;

            try
            {
                var (isInstalled, status) = await Task.Run(GetServiceStatus);

                ServiceInstalledText = isInstalled ? "Installed" : "Not Installed";
                ServiceStatusText = status;

                IsServiceInstalled = isInstalled;
                IsServiceRunning = status == "Running";
            }
            catch (Exception ex)
            {
                ServiceInstalledText = "Error";
                ServiceStatusText = ex.Message;
                _logger.LogError(ex, "Failed to refresh service status.");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private (bool isInstalled, string status) GetServiceStatus()
        {
            try
            {
                using var service = GetServiceControllerSafe();
                if (service != null)
                {
                    return (true, service.Status.ToString());
                }
                
                return (false, "Not Installed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh service status.");
                return (false, "Error");
            }
        }

        [RelayCommand(CanExecute = nameof(CanStartService))]
        private async Task StartService()
        {
            _logger.LogInformation("UI triggered start command for the Windows service.");
            await ExecuteServiceCommandAsync("start", "Starting service...", "Service started successfully.", "Failed to start service");
        }

        private bool CanStartService() => IsServiceInstalled && !IsServiceRunning;

        [RelayCommand(CanExecute = nameof(CanStopOrRestartService))]
        private async Task StopService()
        {
            _logger.LogInformation("UI triggered stop command for the Windows service.");
            await ExecuteServiceCommandAsync("stop", "Stopping service...", "Service stopped successfully.", "Failed to stop service");
        }

        [RelayCommand(CanExecute = nameof(CanStopOrRestartService))]
        private async Task RestartService()
        {
            _logger.LogInformation("UI triggered restart command for the Windows service.");
            ShowInfoBar("Restarting service...", InfoBarSeverity.Informational);

            try
            {
                await Task.Run(() =>
                {
                    _logger.LogInformation("Service restarted successfully from the UI.");
                    using var service = GetServiceControllerSafe();
                    if (service != null)
                    {
                        if (service.Status == ServiceControllerStatus.Running)
                        {
                            service.Stop();
                            service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(ServiceOperationTimeout));
                        }
                        service.Start();
                        service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(ServiceOperationTimeout));
                    }
                    else
                    {
                        throw new InvalidOperationException("Service not found.");
                    }
                });

                ShowInfoBar("Service restarted successfully.", InfoBarSeverity.Success);
                await RefreshStatusCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                ShowInfoBar($"Failed to restart service: {ex.Message}", InfoBarSeverity.Error);
                _logger.LogError(ex, "Failed to restart service from the UI.");
            }
        }

        private bool CanStopOrRestartService() => IsServiceInstalled && IsServiceRunning;

        private async Task ExecuteServiceCommandAsync(string command, string infoMessage, string successMessage, string errorPrefix)
        {
            ShowInfoBar(infoMessage, InfoBarSeverity.Informational);

            try
            {
                await Task.Run(() =>
                {
                    using var service = GetServiceControllerSafe();
                    if (service == null) throw new InvalidOperationException("Service not found.");

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

                ShowInfoBar(successMessage, InfoBarSeverity.Success);
                await RefreshStatusCommand.ExecuteAsync(null);
                _logger.LogInformation("Service command '{Command}' executed successfully via UI.", command);
            }
            catch (Exception ex)
            {
                ShowInfoBar($"{errorPrefix}: {ex.Message}", InfoBarSeverity.Error);
                _logger.LogError(ex, "Service command '{Command}' failed via UI.", command);
            }
        }

        private CancellationTokenSource? _cancellationTokenSource;

        [ObservableProperty]
        public partial double Progress { get; set; }

        [RelayCommand]
        private async Task InstallService(string exePath)
        {
            if (string.IsNullOrEmpty(exePath) || !System.IO.File.Exists(exePath))
            {
                ShowInfoBar("Please provide a valid executable path.", InfoBarSeverity.Error);
                return;
            }

            ShowInfoBar("Installing service...", InfoBarSeverity.Informational);
            _logger.LogInformation("Installing service from executable path {Path}", exePath);

            _cancellationTokenSource = new CancellationTokenSource();
            var progress = new Progress<double>(value => Progress = value);

            try
            {
                var result = await RunScCommandAsync($"create {ServiceName} binPath=\"{exePath}\" start=auto", _cancellationTokenSource.Token, progress);

                if (result.success)
                {
                    ShowInfoBar("Service installed successfully. You can now start it.", InfoBarSeverity.Success);
                    await RefreshStatusCommand.ExecuteAsync(null);
                    _logger.LogInformation("Service installed successfully via UI (sc create result cached).");
                }
                else
                {
                    ShowInfoBar($"Failed to install service: {result.output}", InfoBarSeverity.Error);
                    _logger.LogWarning("Service installation failed with message: {Message}", result.output);
                }
            }
            catch (OperationCanceledException)
            {
                ShowInfoBar("Service installation cancelled.", InfoBarSeverity.Warning);
                _logger.LogInformation("Service installation was cancelled by the user.");
            }
            catch (Exception ex)
            {
                ShowInfoBar($"Error installing service: {ex.Message}", InfoBarSeverity.Error);
                _logger.LogError(ex, "Exception during service installation.");
            }
            finally
            {
                _cancellationTokenSource = null;
                Progress = 0;
            }
        }

        [RelayCommand]
        private async Task UninstallService()
        {
            ShowInfoBar("Uninstalling service...", InfoBarSeverity.Informational);
            _logger.LogInformation("User requested service uninstallation via UI.");

            _cancellationTokenSource = new CancellationTokenSource();
            var progress = new Progress<double>(value => Progress = value);

            try
            {
                // Try to stop the service first
                await Task.Run(() =>
                {
                    try
                    {
                        using var service = GetServiceControllerSafe();
                        if (service != null && service.Status == ServiceControllerStatus.Running)
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

                var result = await RunScCommandAsync($"delete {ServiceName}", _cancellationTokenSource.Token, progress);

                if (result.success)
                {
                    ShowInfoBar("Service uninstalled successfully.", InfoBarSeverity.Success);
                    await RefreshStatusCommand.ExecuteAsync(null);
                    _logger.LogInformation("Service uninstalled successfully via UI.");
                }
                else
                {
                    ShowInfoBar($"Failed to uninstall service: {result.output}", InfoBarSeverity.Error);
                    _logger.LogWarning("Service uninstall failed: {Output}", result.output);
                }
            }
            catch (OperationCanceledException)
            {
                ShowInfoBar("Service uninstallation cancelled.", InfoBarSeverity.Warning);
                _logger.LogInformation("Service uninstallation was cancelled by the user.");
            }
            catch (Exception ex)
            {
                ShowInfoBar($"Error uninstalling service: {ex.Message}", InfoBarSeverity.Error);
                _logger.LogError(ex, "Exception during service uninstallation.");
            }
            finally
            {
                _cancellationTokenSource = null;
                Progress = 0;
            }
        }

        [RelayCommand]
        private void CancelOperation()
        {
            _cancellationTokenSource?.Cancel();
        }

        private async Task<(bool success, string output)> RunScCommandAsync(string arguments, CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (!IsAdministrator())
            {
                // Request elevation via UAC
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = arguments,
                    UseShellExecute = true, // Required for Verb = "runas"
                    Verb = "runas",
                    CreateNoWindow = true
                };

                try
                {
                    progress.Report(25);
                    using var process = System.Diagnostics.Process.Start(startInfo);
                    if (process == null)
                    {
                        return (false, "Failed to start elevated process. User may have cancelled UAC prompt.");
                    }

                    await process.WaitForExitAsync(cancellationToken);
                    progress.Report(100);

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
                var startInfo = new System.Diagnostics.ProcessStartInfo
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
                    progress.Report(25);
                    using var process = System.Diagnostics.Process.Start(startInfo);
                    if (process == null)
                    {
                        return (false, "Failed to start process.");
                    }

                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync(cancellationToken);
                    progress.Report(100);

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

        public string PreviewUrl => $"http://{IpAddress}:{Port}/";

        [RelayCommand]
        private async Task Shutdown() => await SendCommandAsync("shutdown");

        [RelayCommand]
        private async Task Restart() => await SendCommandAsync("restart");

        [RelayCommand]
        private async Task Lock() => await SendCommandAsync("lock");

        private async Task SendCommandAsync(string command)
        {
            string url = $"http://{IpAddress}:{Port}/{command}";

            ShowInfoBar($"Sending {command} command to {url}...", InfoBarSeverity.Informational);
            _logger.LogInformation("Sending {Command} command to {Url}", command, url);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);

                if (!string.IsNullOrEmpty(Secret))
                {
                    request.Headers.Add("Authorization", $"Bearer {Secret}");
                }

                var response = await _httpClient.SendAsync(request);
                string content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ShowInfoBar($"Successfully sent {command} command.", InfoBarSeverity.Success);
                    _logger.LogInformation("{Command} command sent successfully to {Url}", command, url);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ShowInfoBar("Command failed with Unauthorized. Check your secret is correct.", InfoBarSeverity.Warning);
                    _logger.LogWarning("{Command} command returned unauthorized for {Url}", command, url);
                }
                else
                {
                    ShowInfoBar($"Command failed with status {response.StatusCode}: {content}", InfoBarSeverity.Warning);
                    _logger.LogWarning("{Command} command failed with status {Status} for {Url}", command, response.StatusCode, url);
                }
            }
            catch (OperationCanceledException)
            {
                // Silently ignore cancellation
            }
            catch (HttpRequestException ex)
            {
                ShowInfoBar($"Connection failed: {ex.Message}. Ensure the service is running.", InfoBarSeverity.Error);
                _logger.LogError(ex, "{Command} command HTTP error for {Url}", command, url);
            }
            catch (Exception ex)
            {
                ShowInfoBar($"Error: {ex.Message}", InfoBarSeverity.Error);
                _logger.LogError(ex, "{Command} command failed for {Url}", command, url);
            }
        }

        private string? FindServiceConfigPath()
        {
            try
            {
                string exePath = GetServiceExecutablePath();
                if (!string.IsNullOrEmpty(exePath))
                {
                    string? directory = System.IO.Path.GetDirectoryName(exePath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        string configPath = System.IO.Path.Combine(directory, ConfigFileName);
                        if (System.IO.File.Exists(configPath))
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
            string? baseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(baseDir))
            {
                return null;
            }

            string[] searchPaths = new[]
            {
                System.IO.Path.Combine(baseDir, "..", "..", "..", "..", "CPCRemote.Service", ConfigFileName),
                System.IO.Path.Combine(baseDir, "..", "CPCRemote.Service", ConfigFileName),
            };

            foreach (string path in searchPaths)
            {
                string fullPath = System.IO.Path.GetFullPath(path);
                if (System.IO.File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }

        private string? FindServiceExecutable()
        {
            string? baseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(baseDir)) return null;

            // Search paths for the service executable
            string[] searchPaths = new[]
            {
                // Copied to ServiceBinaries folder (via csproj)
                System.IO.Path.Combine(baseDir, "ServiceBinaries", "CPCRemote.Service.exe"),
                // Published side-by-side
                System.IO.Path.Combine(baseDir, "CPCRemote.Service.exe"),
                // Development structure (Debug/Release)
                System.IO.Path.Combine(baseDir, "..", "..", "..", "..", "CPCRemote.Service", "bin", "Debug", "net10.0", "CPCRemote.Service.exe"),
                System.IO.Path.Combine(baseDir, "..", "..", "..", "..", "CPCRemote.Service", "bin", "Release", "net10.0", "CPCRemote.Service.exe"),
                // If running from bin/x64/Debug
                System.IO.Path.Combine(baseDir, "..", "..", "..", "..", "..", "CPCRemote.Service", "bin", "Debug", "net10.0", "CPCRemote.Service.exe"),
            };

            foreach (var path in searchPaths)
            {
                string fullPath = System.IO.Path.GetFullPath(path);
                if (System.IO.File.Exists(fullPath)) return fullPath;
            }
            
            return null;
        }

        private string GetServiceExecutablePath()
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"qc {ServiceName}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
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

        private async void ShowInfoBar(string message, InfoBarSeverity severity)
        {
            InfoBarMessage = message;
            InfoBarSeverity = severity;
            IsInfoBarOpen = true;
            
            try
            {
                await Task.Delay(5000);
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
            
            IsInfoBarOpen = false;
        }

        [RelayCommand]
        private async Task TestPing()
        {
            string url = $"http://{IpAddress}:{Port}/ping";

            ShowInfoBar($"Testing connection to {url}...", InfoBarSeverity.Informational);
            _logger.LogInformation("Testing service ping at {Url}", url);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                if (!string.IsNullOrEmpty(Secret))
                {
                    request.Headers.Add("Authorization", $"Bearer {Secret}");
                }

                var response = await _httpClient.SendAsync(request);
                string content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ShowInfoBar("Success! Service is responding.", InfoBarSeverity.Success);
                    _logger.LogInformation("Ping test succeeded against {Url}", url);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ShowInfoBar("Service responded with Unauthorized. Check your secret is correct.", InfoBarSeverity.Warning);
                    _logger.LogWarning("Ping test returned unauthorized for {Url}", url);
                }
                else
                {
                    ShowInfoBar($"Service responded with status {response.StatusCode}: {content}", InfoBarSeverity.Warning);
                    _logger.LogWarning("Ping test failed with status {Status} for {Url}", response.StatusCode, url);
                }
            }
            catch (OperationCanceledException)
            {
                // Silently ignore cancellation
            }
            catch (HttpRequestException ex)
            {
                ShowInfoBar($"Connection failed: {ex.Message}. Ensure the service is running.", InfoBarSeverity.Error);
                _logger.LogError(ex, "Ping test HTTP error for {Url}", url);
            }
            catch (Exception ex)
            {
                ShowInfoBar($"Error: {ex.Message}", InfoBarSeverity.Error);
                _logger.LogError(ex, "Ping test failed for {Url}", url);
            }
        }

        private ServiceController? GetServiceControllerSafe()
        {
            try
            {
                var services = ServiceController.GetServices();
                var service = services.FirstOrDefault(s => s.ServiceName == ServiceName);

                foreach (var s in services)
                {
                    if (s != service) s.Dispose();
                }

                return service;
            }
            catch
            {
                return null;
            }
        }

    }
}
