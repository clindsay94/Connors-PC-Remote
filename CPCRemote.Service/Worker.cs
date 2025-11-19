namespace CPCRemote.Service
{
    using System;
    using System.Collections.Generic;
    using System.Net;
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
                    int port = current.Port;
                    string secret = current.Secret ?? string.Empty;

                    if (port < 1 || port > 65535)
                    {
                        if (_logger.IsEnabled(LogLevel.Error))
                        {
                            _logger.LogError("Invalid port number: {Port}. Port must be between 1 and 65535.", port);
                        }

                        if (++_retryAttempts >= MaxRetryAttempts)
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

                    string prefix = $"http://{ipAddress}:{port}/";
                    if (_currentPrefix != prefix || !_listener.IsListening)
                    {
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
                                    "Listening on {Prefix} (secret configured: {SecretConfigured})",
                                    prefix,
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

                            if (++_retryAttempts >= MaxRetryAttempts)
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

                            if (++_retryAttempts >= MaxRetryAttempts)
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

                        bool authorized = string.IsNullOrEmpty(secret);
                        if (!authorized)
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
                            IPAddress? remoteIp = httpRequest.RemoteEndPoint?.Address;
                            if (remoteIp is not null)
                            {
                                if (!_unauthorizedLogTimestamps.TryGetValue(remoteIp, out DateTime lastLogTime) ||
                                    (DateTime.UtcNow - lastLogTime).TotalSeconds > UnauthorizedLogThrottlingSeconds)
                                {
                                    if (_logger.IsEnabled(LogLevel.Warning))
                                    {
                                        _logger.LogWarning("Unauthorized request from {RemoteEndPoint}", httpRequest.RemoteEndPoint);
                                    }

                                    _unauthorizedLogTimestamps[remoteIp] = DateTime.UtcNow;
                                }
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
            }
        }
    }
}

