namespace CPCRemote.Service
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using CPCRemote.Core.Enums;
    using CPCRemote.Core.Interfaces;
    using CPCRemote.Service.Options;

    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Background service hosting a minimal HTTP listener to accept remote power commands.
    /// </summary>
    public partial class Worker(
        ILogger<Worker> logger,
        IOptionsMonitor<RsmOptions> rsmOptionsMonitor,
        ICommandCatalog commandCatalog,
        ICommandExecutor commandExecutor) : BackgroundService
    {
        private readonly ILogger<Worker> _logger = logger;
        private readonly IOptionsMonitor<RsmOptions> _rsmOptionsMonitor = rsmOptionsMonitor;
        private readonly ICommandCatalog _commandCatalog = commandCatalog;
        private readonly ICommandExecutor _commandExecutor = commandExecutor;

        private static readonly char[] SlashSeparator = ['/'];
        private HttpListener? _listener;
        private string _currentPrefix = string.Empty;
        private string? _boundSslIpPort;
        private X509Certificate2? _serviceCertificate;
        private const string DefaultSslIp = "0.0.0.0";
        private static readonly Guid SslAppId = new("4fbdab34-09c3-4c3c-9219-61bff33f5d80");

        // Retry configuration constants
        private const int MaxRetryAttempts = 10;
        private const int InitialRetryDelayMs = 1000;
        private const int MaxRetryDelayMs = 60000;
        private int _retryAttempts;
        private readonly Dictionary<IPAddress, DateTime> _unauthorizedLogTimestamps = new();
        private const int UnauthorizedLogThrottlingSeconds = 60;

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (_listener == null || !_listener.IsListening)
                    {
                        _listener?.Close();
                        _listener = new HttpListener();
                    }

                    RsmOptions current = _rsmOptionsMonitor.CurrentValue;
                    string ipAddress = current.IpAddress ?? "localhost";
                    int port = current.UseHttps ? current.HttpsPort : current.Port;
                    string secret = current.Secret ?? string.Empty;
                    bool useHttps = current.UseHttps;

                    if (useHttps && !string.IsNullOrWhiteSpace(current.CertificatePath))
                    {
                        if (_logger.IsEnabled(LogLevel.Warning))
                        {
                            _logger.LogWarning("HTTPS is configured but HttpListener requires manual certificate binding via netsh. See documentation for setup.");
                        }
                    }

                    if (port < 1 || port > 65535)
                    {
                        if (_logger.IsEnabled(LogLevel.Error))
                        {
                            _logger.LogError("Invalid port number: {Port}. Port must be between 1 and 65535.", port);
                        }

                        _retryAttempts++;
                        if (_retryAttempts >= MaxRetryAttempts)
                        {
                            if (_logger.IsEnabled(LogLevel.Critical))
                            {
                                _logger.LogCritical("Maximum retry attempts ({MaxAttempts}) exceeded due to invalid configuration. Service will stop.", MaxRetryAttempts);
                            }

                            throw new InvalidOperationException($"Service failed to start after {MaxRetryAttempts} attempts due to invalid port configuration.");
                        }

                        int invalidDelayMs = Math.Min(InitialRetryDelayMs * (int)Math.Pow(2, _retryAttempts - 1), MaxRetryDelayMs);
                        if (_logger.IsEnabled(LogLevel.Warning))
                        {
                            _logger.LogWarning("Retrying in {DelaySeconds} seconds (attempt {Current}/{Max})...", invalidDelayMs / 1000, _retryAttempts, MaxRetryAttempts);
                        }

                        await Task.Delay(invalidDelayMs, stoppingToken).ConfigureAwait(false);
                        continue;
                    }

                    string prefix = useHttps
                        ? $"https://{ipAddress}:{port}/"
                        : $"http://{ipAddress}:{port}/";

                    if (_currentPrefix != prefix || !_listener.IsListening)
                    {
                        if (useHttps)
                        {
                            await EnsureHttpsBindingAsync(ipAddress, port, current, stoppingToken).ConfigureAwait(false);
                        }

                        try
                        {
                            if (_listener.IsListening)
                            {
                                _listener.Stop();
                                if (_logger.IsEnabled(LogLevel.Information))
                                {
                                    _logger.LogInformation("Stopped listener on {OldPrefix}", _currentPrefix);
                                }
                            }

                            _listener.Prefixes.Clear();
                            _listener.Prefixes.Add(prefix);
                            _currentPrefix = prefix;

                            _listener.Start();
                            if (_logger.IsEnabled(LogLevel.Information))
                            {
                                _logger.LogInformation(
                                    "Listening on {Prefix} (HTTPS: {UseHttps}, secret configured: {SecretConfigured})",
                                    prefix,
                                    useHttps,
                                    !string.IsNullOrEmpty(secret));
                            }

                            _retryAttempts = 0;
                        }
                        catch (HttpListenerException ex) when (ex.ErrorCode == 5)
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

                            int accessDelayMs = Math.Min(InitialRetryDelayMs * (int)Math.Pow(2, _retryAttempts - 1), MaxRetryDelayMs);
                            if (_logger.IsEnabled(LogLevel.Warning))
                            {
                                _logger.LogWarning("Retrying in {DelaySeconds} seconds (attempt {Current}/{Max})...", accessDelayMs / 1000, _retryAttempts, MaxRetryAttempts);
                            }

                            await Task.Delay(accessDelayMs, stoppingToken).ConfigureAwait(false);
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

                            int generalDelayMs = Math.Min(InitialRetryDelayMs * (int)Math.Pow(2, _retryAttempts - 1), MaxRetryDelayMs);
                            if (_logger.IsEnabled(LogLevel.Warning))
                            {
                                _logger.LogWarning("Retrying in {DelaySeconds} seconds (attempt {Current}/{Max})...", generalDelayMs / 1000, _retryAttempts, MaxRetryAttempts);
                            }

                            await Task.Delay(generalDelayMs, stoppingToken).ConfigureAwait(false);
                            continue;
                        }
                    }

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

                    if (context is null)
                    {
                        continue;
                    }

                    using (_logger.BeginScope(new Dictionary<string, object> { ["RemoteEndPoint"] = context.Request.RemoteEndPoint }))
                    {
                        HttpListenerRequest httpRequest = context.Request;
                        HttpListenerResponse response = context.Response;

                        bool authorized = false;
                        if (string.IsNullOrEmpty(secret))
                        {
                            authorized = true;
                        }
                        else
                        {
                            string? authHeader = httpRequest.Headers["Authorization"];
                            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                string token = authHeader[7..];
                                authorized = string.Equals(token, secret, StringComparison.Ordinal);
                            }
                        }

                        if (!authorized)
                        {
                            var remoteIp = httpRequest.RemoteEndPoint.Address;
                            if (!_unauthorizedLogTimestamps.TryGetValue(remoteIp, out var lastLogTime) || (DateTime.UtcNow - lastLogTime).TotalSeconds > UnauthorizedLogThrottlingSeconds)
                            {
                                if (_logger.IsEnabled(LogLevel.Warning))
                                {
                                    _logger.LogWarning("Unauthorized request from {RemoteEndPoint}", httpRequest.RemoteEndPoint);
                                }
                                _unauthorizedLogTimestamps[remoteIp] = DateTime.UtcNow;
                            }

                            response.StatusCode = (int)HttpStatusCode.Unauthorized;
                            response.Headers.Add("WWW-Authenticate", "Bearer");
                            response.Close();
                            continue;
                        }

                        string[] urlParts = httpRequest.Url != null
                            ? httpRequest.Url.AbsolutePath.Split(SlashSeparator, StringSplitOptions.RemoveEmptyEntries)
                            : Array.Empty<string>();

                        string commandStr = urlParts.Length > 0 ? urlParts[0] : string.Empty;

                        if (string.Equals(commandStr, "ping", StringComparison.OrdinalIgnoreCase))
                        {
                            if (_logger.IsEnabled(LogLevel.Information))
                            {
                                _logger.LogInformation("Ping received, responding with OK.");
                            }

                            response.StatusCode = (int)HttpStatusCode.OK;
                            response.Close();
                            continue;
                        }

                        TrayCommandType? command = _commandCatalog.GetCommandType(commandStr);
                        if (command.HasValue)
                        {
                            if (_logger.IsEnabled(LogLevel.Information))
                            {
                                _logger.LogInformation("Executing command: {Command}", command.Value);
                            }

                            try
                            {
                                await _commandExecutor.RunCommandAsync(command.Value, stoppingToken).ConfigureAwait(false);
                                response.StatusCode = (int)HttpStatusCode.OK;
                            }
                            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                            {
                                if (_logger.IsEnabled(LogLevel.Information))
                                {
                                    _logger.LogInformation("Command execution cancelled while shutting down.");
                                }

                                response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                            }
                            catch (Exception ex)
                            {
                                if (_logger.IsEnabled(LogLevel.Error))
                                {
                                    _logger.LogError(ex, "Error executing command {Command}", command.Value);
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
            }
            finally
            {
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

            await EnsureSslBindingAsync(ipPortKey, _serviceCertificate!, token).ConfigureAwait(false);
            _boundSslIpPort = ipPortKey;
        }

        private static string DetermineBindingIpAddress(string? ipAddress)
        {
            return string.IsNullOrWhiteSpace(ipAddress) || ipAddress is "+" or "*" ? DefaultSslIp : ipAddress;
        }

        private async Task EnsureSslBindingAsync(string ipPort, X509Certificate2 certificate, CancellationToken token)
        {
            string thumbprint = NormalizeThumbprint(certificate.Thumbprint!);
            var (showExitCode, showOutput) = await RunNetshCommandAsync($"http show sslcert ipport={ipPort}", token).ConfigureAwait(false);

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
                await RunNetshCommandAsync($"http delete sslcert ipport={ipPort}", token).ConfigureAwait(false);
            }

            string addArgs = $"http add sslcert ipport={ipPort} certhash={thumbprint} appid={SslAppId:D} certstorename=MY";
            var (addExitCode, addOutput) = await RunNetshCommandAsync(addArgs, token).ConfigureAwait(false);
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
        {
            return thumbprint.Replace(" ", string.Empty, StringComparison.Ordinal);
        }

        private static X509Certificate2 LoadCertificate(RsmOptions options)
        {
            string path = options.CertificatePath ?? throw new InvalidOperationException("Certificate path is required when configuring HTTPS.");
            string password = options.CertificatePassword ?? string.Empty;

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
            string stdout = await process.StandardOutput.ReadToEndAsync(token).ConfigureAwait(false);
            string stderr = await process.StandardError.ReadToEndAsync(token).ConfigureAwait(false);
            await process.WaitForExitAsync(token).ConfigureAwait(false);

            return (process.ExitCode, string.Join(Environment.NewLine, stdout, stderr).Trim());
        }

    }
}

