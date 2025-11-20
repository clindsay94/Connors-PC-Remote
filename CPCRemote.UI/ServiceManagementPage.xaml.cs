using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;
using Windows.UI;

namespace CPCRemote.UI
{
    public sealed partial class ServiceManagementPage : Page
    {
        private const string ServiceName = "Remote Shutdown Service";
        private const string ServiceExeName = "CPCRemote.Service.exe";

        public ServiceManagementPage()
        {
            this.InitializeComponent();
            this.Loaded += ServiceManagementPage_Loaded;
        }

        private async void ServiceManagementPage_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshStatusAsync();
        }

        private async Task RefreshStatusAsync()
        {
            StatusText.Text = "Checking...";
            InstallBtn.IsEnabled = false;
            UninstallBtn.IsEnabled = false;
            StartBtn.IsEnabled = false;
            StopBtn.IsEnabled = false;

            string status = "Not Installed";
            bool installed = false;
            bool running = false;

            await Task.Run(() =>
            {
                try
                {
                    using ServiceController sc = new(ServiceName);
                    try
                    {
                        ServiceControllerStatus s = sc.Status;
                        installed = true;
                        running = s == ServiceControllerStatus.Running;
                        status = s.ToString();
                    }
                    catch (InvalidOperationException)
                    {
                        installed = false;
                    }
                }
                catch (Exception ex)
                {
                    status = $"Error: {ex.Message}";
                }
            });

            StatusText.Text = status;
            
            if (installed)
            {
                StatusIcon.Glyph = running ? "\uE7F1" : "\uE7F2"; // Checkmark or Error/Stop
                StatusIcon.Foreground = running ? new SolidColorBrush(Colors.LightGreen) : new SolidColorBrush(Colors.Yellow);
                
                UninstallBtn.IsEnabled = true;
                StartBtn.IsEnabled = !running;
                StopBtn.IsEnabled = running;
            }
            else
            {
                StatusIcon.Glyph = "\uE7E7"; // Info/Warning
                StatusIcon.Foreground = new SolidColorBrush(Colors.Gray);
                
                InstallBtn.IsEnabled = true;
            }
        }

        private async void InstallBtn_Click(object sender, RoutedEventArgs e)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // Path to the service executable. Adjust logic if structure differs.
            string exePath = Path.Combine(baseDir, "ServiceBinaries", ServiceExeName);

            if (!File.Exists(exePath))
            {
                Log($"Error: Service executable not found at {exePath}");
                return;
            }

            Log($"Installing service from: {exePath}...");
            // Note: space after binPath= is required by sc.exe
            await RunScCommandAsync("create", $"\"{ServiceName}\" binPath= \"{exePath}\" start= auto");
            await RefreshStatusAsync();
        }

        private async void UninstallBtn_Click(object sender, RoutedEventArgs e)
        {
            Log("Uninstalling service...");
            await RunScCommandAsync("delete", $"\"{ServiceName}\" ");
            await RefreshStatusAsync();
        }

        private async void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            Log("Starting service...");
            await RunScCommandAsync("start", $"\"{ServiceName}\" ");
            await RefreshStatusAsync();
        }

        private async void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            Log("Stopping service...");
            await RunScCommandAsync("stop", $"\"{ServiceName}\" ");
            await RefreshStatusAsync();
        }

        private async Task RunScCommandAsync(string command, string args)
        {
            await Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new()
                    {
                        FileName = "sc.exe",
                        Arguments = $"{command} {args}",
                        Verb = "runas", // Request Admin
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true
                    };

                    Process? p = Process.Start(psi);
                    if (p != null)
                    {
                        p.WaitForExit();
                        if (p.ExitCode == 0)
                        {
                            DispatcherQueue.TryEnqueue(() => Log($"Command 'sc {command}' succeeded."));
                        }
                        else
                        {
                            DispatcherQueue.TryEnqueue(() => Log($"Command 'sc {command}' failed with exit code {p.ExitCode}."));
                        }
                    }
                }
                catch (Exception ex)
                {
                    DispatcherQueue.TryEnqueue(() => Log($"Exception running sc: {ex.Message}"));
                }
            });
        }

        private void Log(string message)
        {
            OutputLog.Text += $"{DateTime.Now:HH:mm:ss}: {message}\n";
            // Auto-scroll
            OutputLog.Select(OutputLog.Text.Length, 0);
        }
    }
}
