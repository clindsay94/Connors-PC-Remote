namespace CPCRemote.Service
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using System.Security.Cryptography;

    using CPCRemote.Core.Enums;
    using CPCRemote.Core.Interfaces;
    using CPCRemote.Service.Options;

    /// <summary>
    /// Background service hosting a minimal HTTP listener to accept remote power commands
    /// </summary>
    public partial class Worker(ILogger<Worker> logger, IOptionsMonitor<RsmOptions> rsmOptionsMonitor, ITrayCommandHelper commandHelper) : BackgroundService
    {
        private readonly ILogger<Worker> _logger = logger;
        private readonly IOptionsMonitor<RsmOptions> _rsmOptionsMonitor = rsmOptionsMonitor;
        private readonly ITrayCommandHelper _commandHelper = commandHelper;

        private static readonly char[] SlashSeparator = ['/'];
        private HttpListener? _listener;
        private string _currentPrefix = string.Empty;
        private string? _boundSslIpPort;
        private X509Certificate2? _serviceCertificate;
        private const string DefaultSslIp = "0.0.0.0";
        private static readonly Guid SslAppId = new("4fbdab34-09c3-4c3c-9219-61bff33f5d80");
        
        // Retry configuration constants
        private const int MaxRetryAttempts = 10;
        private const int InitialRetryDelayMs = 1000;  // 1 second
        private const int MaxRetryDelayMs = 60000;     // 60 seconds
        private int _retryAttempts = 0;

        /// <summary>
        /// Executes the background service logic for the HTTP listener, handling remote power commands.
        /// </summary>
        /// <param name="stoppingToken">A cancellation token that is triggered when the service is stopping.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Create a new HttpListener for each attempt to avoid disposed object issues
                    if (_listener == null || !_listener.IsListening)
                    {
                        _listener?.Close();
                        _listener = new HttpListener();
                    }

                    // Read current values from appsettings.json
                    RsmOptions current = _rsmOptionsMonitor.CurrentValue;
                    string ipAddress = current.IpAddress ?? "localhost";
                    int port = current.UseHttps ? current.HttpsPort : current.Port;
                    string secret = current.Secret ?? string.Empty;
                    bool useHttps = current.UseHttps;

                    // Note: HttpListener has limited HTTPS support. For production HTTPS:
                    // 1. Use netsh to bind certificate to port: 
                    //    netsh http add sslcert ipport=0.0.0.0:PORT certhash=THUMBPRINT appid={GUID}
                    // 2. Or migrate to Kestrel for better HTTPS management
                    if (useHttps && !string.IsNullOrWhiteSpace(current.CertificatePath))
                    {
                        if (_logger.IsEnabled(LogLevel.Warning))
                        {
                            _logger.LogWarning("HTTPS is configured but HttpListener requires manual certificate binding via netsh. See documentation for setup.");
                        }
                    }

                    // Validate port number
                    if (port < 1 || port > 65535)
                    {
                        if (_logger.IsEnabled(LogLevel.Error))
                        {
                            _logger.LogError("Invalid port number: {Port}. Port must be between 1 and 65535.", port);
                        }
                        
                        // Increment retry counter and check if max retries exceeded
                        _retryAttempts++;
                        if (_retryAttempts >= MaxRetryAttempts)
                        {
                            if (_logger.IsEnabled(LogLevel.Critical))
                            {
                                _logger.LogCritical("Maximum retry attempts ({MaxAttempts}) exceeded due to invalid configuration. Service will stop.", MaxRetryAttempts);
                            }
                            throw new InvalidOperationException($"Service failed to start after {MaxRetryAttempts} attempts due to invalid port configuration.");
                        }
                        
                        // Calculate exponential backoff delay
                        int delayMs = Math.Min(InitialRetryDelayMs * (int)Math.Pow(2, _retryAttempts - 1), MaxRetryDelayMs);
                        if (_logger.IsEnabled(LogLevel.Warning))
                        {
                            _logger.LogWarning("Retrying in {DelaySeconds} seconds (attempt {Current}/{Max})...", delayMs / 1000, _retryAttempts, MaxRetryAttempts);
                        }
                        await Task.Delay(delayMs, stoppingToken);
                        continue;
                    }

                    string prefix = useHttps 
                        ? $"https://{ipAddress}:{port}/"
                        : $"http://{ipAddress}:{port}/";

                    // Only reconfigure listener if prefix changed or listener isn't running
                    if (_currentPrefix != prefix || !_listener.IsListening)
                    {
                        if (useHttps)
                        {
                            await EnsureHttpsBindingAsync(ipAddress, port, current, stoppingToken);
                        }

                        try
                        {
                            // Stop listener if running
                            if (_listener.IsListening)
                            {
                                _listener.Stop();
                                if (_logger.IsEnabled(LogLevel.Information))
                                {
                                    _logger.LogInformation("Stopped listener on {OldPrefix}", _currentPrefix);
                                }
                            }

                            // Reconfigure prefix
                            _listener.Prefixes.Clear();
                            _listener.Prefixes.Add(prefix);
                            _currentPrefix = prefix;

                            // Start listener
                            _listener.Start();
                            if (_logger.IsEnabled(LogLevel.Information))
                            {
                                _logger.LogInformation("Listening on {Prefix} (HTTPS: {UseHttps}, secret configured: {SecretConfigured})", 
                                    prefix, useHttps, !string.IsNullOrEmpty(secret));
                            }
                            
                            // Reset retry counter on successful start
                            _retryAttempts = 0;
                        }
                        catch (HttpListenerException ex) when (ex.ErrorCode == 5) // Access Denied
                        {
                            if (_logger.IsEnabled(LogLevel.Error))
                            {
                                _logger.LogError("Access denied when binding to {Prefix}. URL reservation may be required. Run: netsh http add urlacl url={Prefix} user=DOMAIN\\USER", prefix, prefix);
                            }
                            
                            _retryAttempts++;
                            if (_retryAttempts >= MaxRetryAttempts)
                            {
                                if (_logger.IsEnabled(LogLevel.Critical))
                                {
                                    _logger.LogCritical("Maximum retry attempts ({MaxAttempts}) exceeded due to access denied. Service will stop.", MaxRetryAttempts);
                                }
                                throw new InvalidOperationException($"Service failed to start after {MaxRetryAttempts} attempts due to access denied. URL reservation required.", ex);
                            }
                            
                            int delayMs = Math.Min(InitialRetryDelayMs * (int)Math.Pow(2, _retryAttempts - 1), MaxRetryDelayMs);
                            if (_logger.IsEnabled(LogLevel.Warning))
                            {
                                _logger.LogWarning("Retrying in {DelaySeconds} seconds (attempt {Current}/{Max})...", delayMs / 1000, _retryAttempts, MaxRetryAttempts);
                            }
                            await Task.Delay(delayMs, stoppingToken);
                            continue;
                        }
                        catch (Exception ex)
                        {
                            if (_logger.IsEnabled(LogLevel.Error))
                            {
                                _logger.LogError(ex, "Failed to start HttpListener on prefix {Prefix}.", prefix);
                            }
                            
                            _retryAttempts++;
                            if (_retryAttempts >= MaxRetryAttempts)
                            {
                                if (_logger.IsEnabled(LogLevel.Critical))
                                {
                                    _logger.LogCritical(ex, "Maximum retry attempts ({MaxAttempts}) exceeded. Service will stop.", MaxRetryAttempts);
                                }
                                throw new InvalidOperationException($"Service failed to start after {MaxRetryAttempts} attempts.", ex);
                            }
                            
                            int delayMs = Math.Min(InitialRetryDelayMs * (int)Math.Pow(2, _retryAttempts - 1), MaxRetryDelayMs);
                            if (_logger.IsEnabled(LogLevel.Warning))
                            {
                                _logger.LogWarning("Retrying in {DelaySeconds} seconds (attempt {Current}/{Max})...", delayMs / 1000, _retryAttempts, MaxRetryAttempts);
                            }
                            await Task.Delay(delayMs, stoppingToken);
                            continue;
                        }
                    }

                    // Get next request
                    HttpListenerContext context;
                    try
                    {
                        context = await _listener.GetContextAsync().ConfigureAwait(false);
                    }
                    catch (HttpListenerException)
                    {
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception ex) when (stoppingToken.IsCancellationRequested)
                    {
                        if (_logger.IsEnabled(LogLevel.Debug))
                        {
                            _logger.LogDebug(ex, "Listener cancelled");
                        }
                        break;
                    }

                    if (context == null)
                    {
                        continue;
                    }

                    // Process request
                    HttpListenerRequest httpRequest = context.Request;
                    HttpListenerResponse response = context.Response;

                    // Check authorization via header instead of URL path for security
                    bool authorized = false;
                    if (string.IsNullOrEmpty(secret))
                    {
                        // No secret required - allow all requests
                        authorized = true;
                    }
                    else
                    {
                        // Check Authorization header for Bearer token
                        string? authHeader = httpRequest.Headers["Authorization"];
                        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            string token = authHeader.Substring(7); // Remove "Bearer " prefix
                            authorized = string.Equals(token, secret, StringComparison.Ordinal);
                        }
                    }

                    if (!authorized)
                    {
                        if (_logger.IsEnabled(LogLevel.Warning))
                        {
                            _logger.LogWarning("Unauthorized request from {RemoteEndPoint}", httpRequest.RemoteEndPoint);
                        }
                        response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        response.Headers.Add("WWW-Authenticate", "Bearer");
                        response.Close();
                        continue;
                    }

                    // Extract command from URL path
                    string[] urlParts = httpRequest.Url != null
                        ? httpRequest.Url.AbsolutePath.Split(SlashSeparator, StringSplitOptions.RemoveEmptyEntries)
                        : [];

                    string commandStr = urlParts.Length > 0 ? urlParts[0] : string.Empty;

                    // Handle the ping command as a special case for health checks.
                    if (string.Equals(commandStr, "ping", StringComparison.OrdinalIgnoreCase))
                    {
                        if (_logger.IsEnabled(LogLevel.Information))
                        {
                            _logger.LogInformation("Ping received, responding with OK.");
                        }
                        response.StatusCode = (int)HttpStatusCode.OK;
                        response.Close();
                        continue; // Go back to listening for the next request.
                    }
                         
                    if (Enum.TryParse<TrayCommandType>(commandStr, true, out TrayCommandType command))
                    {
                        if (_logger.IsEnabled(LogLevel.Information))
                        {
                            _logger.LogInformation("Executing command: {Command}", command);
                        }
                        try
                        {
                            _commandHelper.RunCommand(command);
                            response.StatusCode = (int)HttpStatusCode.OK;
                        }
                        catch (Exception ex)
                        {
                            if (_logger.IsEnabled(LogLevel.Error))
                            {
                                _logger.LogError(ex, "Error executing command");
                            }
                            response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        }
                    }
                    else
                    {
                        if (_logger.IsEnabled(LogLevel.Warning))
                        {
                            _logger.LogWarning("Invalid command: {CommandStr}", commandStr);
                        }
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }

                    response.Close();
                }
            }
            finally
            {
                // Cleanup
                try
                {
                    if (_listener?.IsListening == true)
                    {
                        _listener.Stop();
                    }
                    _listener?.Close();
                }
                catch (Exception ex)
                {
                    if (_logger.IsEnabled(LogLevel.Error))
                    {
                        _logger.LogError(ex, "Error cleaning up HttpListener");
                    }
                }

                try
                {
                    _serviceCertificate?.Dispose();
                }
                catch (Exception ex)
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug(ex, "Failed to dispose service certificate");
                    }
                }
            }
        }

        private async Task EnsureHttpsBindingAsync(string ipAddress, int port, RsmOptions options, CancellationToken token)
        {
            if (!options.UseHttps)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(options.CertificatePath))
            {
                throw new InvalidOperationException("HTTPS requires a certificatePath configuration.");
            }

            X509Certificate2 certificate = LoadCertificate(options);
            if (_serviceCertificate == null || !_serviceCertificate.Thumbprint.Equals(certificate.Thumbprint, StringComparison.OrdinalIgnoreCase))
            {
                _serviceCertificate?.Dispose();
                _serviceCertificate = certificate;
            }
            else
            {
                certificate.Dispose();
            }

            string bindingIp = DetermineBindingIpAddress(ipAddress);
            string ipPortKey = $"{bindingIp}:{options.HttpsPort}";
            if (_boundSslIpPort == ipPortKey)
            {
                return;
            }

            await EnsureSslBindingAsync(ipPortKey, _serviceCertificate!, token);
            _boundSslIpPort = ipPortKey;
        }

        private static string DetermineBindingIpAddress(string? ipAddress)
        {
            return string.IsNullOrWhiteSpace(ipAddress) || ipAddress is "+" or "*" ? DefaultSslIp : ipAddress;
        }

        private async Task EnsureSslBindingAsync(string ipPort, X509Certificate2 certificate, CancellationToken token)
        {
            string thumbprint = NormalizeThumbprint(certificate.Thumbprint!);
            var (showExitCode, showOutput) = await RunNetshCommandAsync($"http show sslcert ipport={ipPort}", token);

            if (showExitCode == 0 && showOutput.Contains(thumbprint, StringComparison.OrdinalIgnoreCase))
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("SSL binding for {IpPort} already uses thumbprint {Thumbprint}", ipPort, thumbprint);
                }
                return;
            }

            if (showExitCode == 0)
            {
                await RunNetshCommandAsync($"http delete sslcert ipport={ipPort}", token);
            }

            string addArgs = $"http add sslcert ipport={ipPort} certhash={thumbprint} appid={SslAppId:D} certstorename=MY";
            var (addExitCode, addOutput) = await RunNetshCommandAsync(addArgs, token);
            if (addExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to bind SSL certificate via netsh: {addOutput}");
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Bound certificate {Thumbprint} to {IpPort}", thumbprint, ipPort);
            }
        }

        private static string NormalizeThumbprint(string thumbprint)
            => thumbprint.Replace(" ", string.Empty, StringComparison.Ordinal);

        private static X509Certificate2 LoadCertificate(RsmOptions options)
        {
            string path = options.CertificatePath ?? throw new InvalidOperationException("Certificate path is required when configuring HTTPS.");
            string password = options.CertificatePassword ?? string.Empty;

            // Use X509CertificateLoader.LoadPkcs12FromFile for loading PFX files (recommended in .NET 9+)
            X509Certificate2 certificate = X509CertificateLoader.LoadPkcs12FromFile(path, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

            using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            var existing = store.Certificates.Find(X509FindType.FindByThumbprint, certificate.Thumbprint, false);
            X509Certificate2 result;
            if (existing.Count == 0)
            {
                store.Add(certificate);
                result = certificate;
            }
            else
            {
                result = new X509Certificate2(existing[0]);
                certificate.Dispose();
            }

            store.Close();
            return result;
        }

        private static async Task<(int ExitCode, string Output)> RunNetshCommandAsync(string arguments, CancellationToken token)
        {
            var startInfo = new ProcessStartInfo("netsh", arguments)
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start netsh.exe");
            string stdout = await process.StandardOutput.ReadToEndAsync(token);
            string stderr = await process.StandardError.ReadToEndAsync(token);
            await process.WaitForExitAsync(token);

            return (process.ExitCode, string.Join(Environment.NewLine, stdout, stderr).Trim());
        }
    }
}
