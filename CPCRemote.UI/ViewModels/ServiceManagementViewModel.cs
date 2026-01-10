using CPCRemote.UI.Models;
using CPCRemote.UI.Services;
using CPCRemote.UI.Strings;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;

using System;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace CPCRemote.UI.ViewModels
{
    public partial class ServiceManagementViewModel : ObservableObject
    {
        private const string ServiceName = CPCRemote.Core.Constants.ServiceConstants.RemoteShutdownServiceName;
        private const int ServiceOperationTimeout = 30;
        private const string ConfigFileName = "appsettings.json";

        private readonly ILogger<ServiceManagementViewModel> _logger;
        private readonly SettingsService _settingsService;
        private readonly HttpClient _httpClient;
        private readonly NamedPipeClient _pipeClient;

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
        [NotifyPropertyChangedFor(nameof(IsPortValid))]
        [NotifyPropertyChangedFor(nameof(PortValidationMessage))]
        public partial double Port { get; set; }

        /// <summary>
        /// Validates port is in valid range (1-65535).
        /// </summary>
        public bool IsPortValid => Port >= 1 && Port <= 65535;

        /// <summary>
        /// Gets the validation message for an invalid port, or empty string if valid.
        /// </summary>
        public string PortValidationMessage => IsPortValid ? string.Empty : Resources.ServiceManagement_PortValidation;

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

        public ServiceManagementViewModel(
            ILogger<ServiceManagementViewModel> logger,
            SettingsService settingsService,
            HttpClient httpClient,
            NamedPipeClient pipeClient)
        {
            _logger = logger;
            _settingsService = settingsService;
            _httpClient = httpClient;
            _pipeClient = pipeClient;
            ServiceStatusText = Resources.Unknown;
            ServiceInstalledText = Resources.Unknown;
            IsSafetyLockEnabled = _settingsService.Get<bool>(nameof(IsSafetyLockEnabled), true);
            ServiceExecutablePath = FindServiceExecutable();

            // Item 8: Defer async initialization to avoid blocking constructor
            // Commands will be loaded when view is ready via InitializeAsync()
        }

        /// <summary>
        /// Item 8: Async initialization method to be called after construction
        /// </summary>
        public async Task InitializeAsync()
        {
            await LoadConfigurationCommand.ExecuteAsync(null);
            await RefreshStatusCommand.ExecuteAsync(null);
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
            if (!IsPortValid)
            {
                ShowInfoBar(string.Format(Resources.ServiceManagement_InvalidPort, PortValidationMessage), InfoBarSeverity.Error);
                return;
            }

            ShowInfoBar(Resources.ServiceManagement_SavingConfig, InfoBarSeverity.Informational);

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

                // 2. Try to update Service via IPC
                bool ipcSuccess = false;
                if (IsServiceRunning)
                {
                    try
                    {
                        if (!_pipeClient.IsConnected)
                        {
                            await _pipeClient.ConnectAsync(TimeSpan.FromSeconds(2));
                        }

                        if (_pipeClient.IsConnected)
                        {
                            var request = new CPCRemote.Core.IPC.SaveRsmConfigRequest
                            {
                                Config = new CPCRemote.Core.IPC.RsmConfigDto
                                {
                                    IpAddress = this.IpAddress ?? "0.0.0.0",
                                    Port = (int)this.Port,
                                    Secret = this.Secret
                                }
                            };

                            var response = await _pipeClient.SendRequestAsync<CPCRemote.Core.IPC.SaveRsmConfigResponse>(request, TimeSpan.FromSeconds(5));
                            
                            if (response.Success)
                            {
                                ipcSuccess = true;
                                _logger.LogInformation("Service configuration updated via IPC.");
                            }
                            else
                            {
                                _logger.LogWarning("Service configuration IPC update failed: {Error}", response.ErrorMessage);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update service config via IPC.");
                    }
                }

                // 3. Fallback to direct file write if IPC failed or service not running
                if (!ipcSuccess)
                {
                    _logger.LogInformation("Falling back to direct file write for service config.");
                    
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
                            // Only throw if we had no success via IPC either (which we know we didn't)
                            // But maybe we don't want to crash the whole operation?
                            // Let's warn the user.
                            ShowInfoBar(string.Format(Resources.ServiceManagement_ConfigUpdateFailed, ex.Message), InfoBarSeverity.Warning);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Could not locate service configuration file to update.");
                        if (!ipcSuccess) // Redundant check but clear intent
                        {
                             ShowInfoBar(Resources.ServiceManagement_ConfigSavedWarning, InfoBarSeverity.Warning);
                             return;
                        }
                    }
                }

                ShowInfoBar(Resources.ServiceManagement_ConfigSavedSuccess, InfoBarSeverity.Success);
                _logger.LogInformation("Configuration saved successfully.");
            }
            catch (Exception ex)
            {
                ShowInfoBar(string.Format(Resources.ServiceManagement_ConfigSaveError, ex.Message), InfoBarSeverity.Error);
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
                ServiceInstalledText = Resources.Error;
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
                if (service is not null)
                {
                    return (true, service.Status.ToString());
                }
                
                return (false, "Not Installed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh service status.");
                return (false, Resources.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanStartService))]
        private async Task StartService()
        {
            _logger.LogInformation("UI triggered start command for the Windows service.");
            await ExecuteServiceCommandAsync(
                "start",
                Resources.ServiceManagement_StartingService,
                Resources.ServiceManagement_ServiceStarted,
                Resources.ServiceManagement_StartServiceFailed);
        }

        private bool CanStartService() => IsServiceInstalled && !IsServiceRunning;

        [RelayCommand(CanExecute = nameof(CanStopOrRestartService))]
        private async Task StopService()
        {
            _logger.LogInformation("UI triggered stop command for the Windows service.");
            await ExecuteServiceCommandAsync(
                "stop",
                Resources.ServiceManagement_StoppingService,
                Resources.ServiceManagement_ServiceStopped,
                Resources.ServiceManagement_StopServiceFailed);
        }

        [RelayCommand(CanExecute = nameof(CanStopOrRestartService))]
        private async Task RestartService()
        {
            _logger.LogInformation("UI triggered restart command for the Windows service.");
            ShowInfoBar(Resources.ServiceManagement_RestartingService, InfoBarSeverity.Informational);

            try
            {
                await Task.Run(() =>
                {
                    _logger.LogInformation("Service restarted successfully from the UI.");
                    using var service = GetServiceControllerSafe();
                    if (service is not null)
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

                ShowInfoBar(Resources.ServiceManagement_ServiceRestarted, InfoBarSeverity.Success);
                await RefreshStatusCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                ShowInfoBar(string.Format(Resources.ServiceManagement_RestartServiceFailed, ex.Message), InfoBarSeverity.Error);
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
                ShowInfoBar(Resources.ServiceManagement_InvalidExePath, InfoBarSeverity.Error);
                return;
            }

            ShowInfoBar(Resources.ServiceManagement_InstallingService, InfoBarSeverity.Informational);
            _logger.LogInformation("Installing service from executable path {Path}", exePath);

            _cancellationTokenSource = new CancellationTokenSource();
            var progress = new Progress<double>(value => Progress = value);

            try
            {
                var result = await RunScCommandAsync($"create {ServiceName} binPath=\"{exePath}\" start=auto", _cancellationTokenSource.Token, progress);

                if (result.success)
                {
                    ShowInfoBar(Resources.ServiceManagement_ServiceInstalled, InfoBarSeverity.Success);
                    await RefreshStatusCommand.ExecuteAsync(null);
                    _logger.LogInformation("Service installed successfully via UI (sc create result cached).");
                }
                else
                {
                    ShowInfoBar(string.Format(Resources.ServiceManagement_InstallServiceFailed, result.output), InfoBarSeverity.Error);
                    _logger.LogWarning("Service installation failed with message: {Message}", result.output);
                }
            }
            catch (OperationCanceledException)
            {
                ShowInfoBar(Resources.ServiceManagement_InstallCancelled, InfoBarSeverity.Warning);
                _logger.LogInformation("Service installation was cancelled by the user.");
            }
            catch (Exception ex)
            {
                ShowInfoBar(string.Format(Resources.ServiceManagement_InstallError, ex.Message), InfoBarSeverity.Error);
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
            ShowInfoBar(Resources.ServiceManagement_UninstallingService, InfoBarSeverity.Informational);
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
                        if (service is not null && service.Status == ServiceControllerStatus.Running)
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
                    ShowInfoBar(Resources.ServiceManagement_ServiceUninstalled, InfoBarSeverity.Success);
                    await RefreshStatusCommand.ExecuteAsync(null);
                    _logger.LogInformation("Service uninstalled successfully via UI.");
                }
                else
                {
                    ShowInfoBar(string.Format(Resources.ServiceManagement_UninstallServiceFailed, result.output), InfoBarSeverity.Error);
                    _logger.LogWarning("Service uninstall failed: {Output}", result.output);
                }
            }
            catch (OperationCanceledException)
            {
                ShowInfoBar(Resources.ServiceManagement_UninstallCancelled, InfoBarSeverity.Warning);
                _logger.LogInformation("Service uninstallation was cancelled by the user.");
            }
            catch (Exception ex)
            {
                ShowInfoBar(string.Format(Resources.ServiceManagement_UninstallError, ex.Message), InfoBarSeverity.Error);
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
                    if (process is null)
                    {
                        return (false, Resources.ServiceManagement_ElevatedProcessFailed);
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
                    return (false, string.Format(Resources.ServiceManagement_ElevatedCommandError, ex.Message));
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
                    if (process is null)
                    {
                        return (false, Resources.ServiceManagement_ProcessStartFailed);
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

            ShowInfoBar(string.Format(Resources.ServiceManagement_SendingCommand, command, url), InfoBarSeverity.Informational);
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
                    ShowInfoBar(string.Format(Resources.ServiceManagement_CommandSuccess, command), InfoBarSeverity.Success);
                    _logger.LogInformation("{Command} command sent successfully to {Url}", command, url);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ShowInfoBar(Resources.ServiceManagement_CommandUnauthorized, InfoBarSeverity.Warning);
                    _logger.LogWarning("{Command} command returned unauthorized for {Url}", command, url);
                }
                else
                {
                    ShowInfoBar(string.Format(Resources.ServiceManagement_CommandFailed, response.StatusCode, content), InfoBarSeverity.Warning);
                    _logger.LogWarning("{Command} command failed with status {Status} for {Url}", command, response.StatusCode, url);
                }
            }
            catch (OperationCanceledException)
            {
                // Silently ignore cancellation
            }
            catch (HttpRequestException ex)
            {
                ShowInfoBar(string.Format(Resources.ServiceManagement_ConnectionFailed, ex.Message), InfoBarSeverity.Error);
                _logger.LogError(ex, "{Command} command HTTP error for {Url}", command, url);
            }
            catch (Exception ex)
            {
                ShowInfoBar($"{Resources.Error}: {ex.Message}", InfoBarSeverity.Error);
                _logger.LogError(ex, "{Command} command failed for {Url}", command, url);
            }
        }

        private string? FindServiceConfigPath()
        {
            // Always use the writable ProgramData location for configuration
            // This works for both MSIX and unpackaged deployments
            // The ConfigurationPaths helper handles copying defaults from the app directory
            string configPath = CPCRemote.Core.Helpers.ConfigurationPaths.EnsureServiceConfigExists(ConfigFileName);
            _logger.LogInformation("Using service config path: {Path} (IsPackagedApp: {IsPackaged})", 
                configPath, 
                CPCRemote.Core.Helpers.ConfigurationPaths.IsPackagedApp());
            return configPath;
        }

        private string? FindServiceExecutable()
        {
            string? baseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(baseDir)) return null;

            // Search paths for the service executable (in priority order)
            string[] searchPaths =
            [
                // 1. Copied to ServiceBinaries folder (for packaged deployment)
                System.IO.Path.Combine(baseDir, "ServiceBinaries", "CPCRemote.Service.exe"),
                
                // 2. Published side-by-side (same folder)
                System.IO.Path.Combine(baseDir, "CPCRemote.Service.exe"),
                
                // 3. Unified bin structure: bin\{Configuration}\CPCRemote.Service\
                // UI is at: bin\Debug\CPCRemote.UI\ -> Service at: bin\Debug\CPCRemote.Service\
                System.IO.Path.Combine(baseDir, "..", "CPCRemote.Service", "CPCRemote.Service.exe"),
                
                // 4. Legacy structure: bin\Debug\net10.0-windows...\win-x64\
                System.IO.Path.Combine(baseDir, "..", "..", "..", "..", "CPCRemote.Service", "bin", "Debug", "net10.0-windows10.0.26100.0", "win-x64", "CPCRemote.Service.exe"),
                System.IO.Path.Combine(baseDir, "..", "..", "..", "..", "CPCRemote.Service", "bin", "Release", "net10.0-windows10.0.26100.0", "win-x64", "CPCRemote.Service.exe"),
                
                // 5. If running from solution root bin\x64\Debug\...
                System.IO.Path.Combine(baseDir, "..", "..", "..", "bin", "Debug", "CPCRemote.Service", "CPCRemote.Service.exe"),
            ];

            foreach (var path in searchPaths)
            {
                string fullPath = System.IO.Path.GetFullPath(path);
                _logger.LogDebug("Searching for service executable at: {Path}", fullPath);
                if (System.IO.File.Exists(fullPath))
                {
                    _logger.LogInformation("Found service executable at: {Path}", fullPath);
                    return fullPath;
                }
            }

            _logger.LogWarning("Could not find CPCRemote.Service.exe in any expected location.");
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

        private void ShowInfoBar(string message, InfoBarSeverity severity)
        {
            InfoBarMessage = message;
            InfoBarSeverity = severity;
            IsInfoBarOpen = true;
            
            // Fire-and-forget the auto-close with proper exception handling
            _ = AutoCloseInfoBarAsync();
        }

        private async Task AutoCloseInfoBarAsync()
        {
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
                    ShowInfoBar($"{Resources.Success}! Service is responding.", InfoBarSeverity.Success);
                    _logger.LogInformation("Ping test succeeded against {Url}", url);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ShowInfoBar(Resources.ServiceManagement_CommandUnauthorized, InfoBarSeverity.Warning);
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
                ShowInfoBar(string.Format(Resources.ServiceManagement_ConnectionFailed, ex.Message), InfoBarSeverity.Error);
                _logger.LogError(ex, "Ping test HTTP error for {Url}", url);
            }
            catch (Exception ex)
            {
                ShowInfoBar($"{Resources.Error}: {ex.Message}", InfoBarSeverity.Error);
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
