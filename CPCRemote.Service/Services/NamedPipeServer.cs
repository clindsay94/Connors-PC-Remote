namespace CPCRemote.Service.Services;

using System.Buffers;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

using CPCRemote.Core.Enums;
using CPCRemote.Core.Interfaces;
using CPCRemote.Core.IPC;

using Microsoft.Extensions.Logging;

/// <summary>
/// Named Pipe server for handling IPC communication with the UI application.
/// </summary>
public sealed class NamedPipeServer(
    ILogger<NamedPipeServer> logger,
    HardwareMonitor hardwareMonitor,
    AppCatalogService appCatalog,
    ICommandExecutor commandExecutor) : IPipeServer
{
    private readonly ILogger<NamedPipeServer> _logger = logger;
    private readonly HardwareMonitor _hardwareMonitor = hardwareMonitor;
    private readonly AppCatalogService _appCatalog = appCatalog;
    private readonly ICommandExecutor _commandExecutor = commandExecutor;

    private readonly DateTime _startTimeUtc = DateTime.UtcNow;
    private readonly Lock _lock = new();
    private CancellationTokenSource? _cts;
    private Task? _serverTask;
    private volatile bool _isRunning;
    private string _httpListenerAddress = string.Empty;
    private bool _isListening;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Updates the HTTP listener status information for status queries.
    /// </summary>
    /// <param name="address">The HTTP listener address.</param>
    /// <param name="isListening">Whether the HTTP listener is active.</param>
    public void UpdateHttpStatus(string address, bool isListening)
    {
        _httpListenerAddress = address;
        _isListening = isListening;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_isRunning)
            {
                _logger.LogWarning("Pipe server is already running.");
                return Task.CompletedTask;
            }

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _isRunning = true;
            _serverTask = RunServerAsync(_cts.Token);

            _logger.LogInformation("Named pipe server started on pipe: {PipeName}", IpcConstants.PipeName);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        Task? taskToWait;

        lock (_lock)
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;
            _cts?.Cancel();
            taskToWait = _serverTask;
        }

        if (taskToWait is not null)
        {
            try
            {
                await taskToWait.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while stopping pipe server.");
            }
        }

        _cts?.Dispose();
        _cts = null;
        _serverTask = null;

        _logger.LogInformation("Named pipe server stopped.");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }

    private async Task RunServerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            NamedPipeServerStream? pipeServer = null;

            try
            {
                pipeServer = new NamedPipeServerStream(
                    IpcConstants.PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous,
                    IpcConstants.BufferSize,
                    IpcConstants.BufferSize);

                _logger.LogDebug("Waiting for client connection...");

                await pipeServer.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogDebug("Client connected.");

                // Handle this client in a fire-and-forget manner so we can accept more connections
                _ = HandleClientAsync(pipeServer, cancellationToken);
                pipeServer = null; // Ownership transferred
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Clean shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting pipe client connection.");
                await Task.Delay(100, cancellationToken).ConfigureAwait(false); // Prevent tight loop
            }
            finally
            {
                if (pipeServer is not null)
                {
                    await pipeServer.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipeServer, CancellationToken cancellationToken)
    {
        await using (pipeServer)
        {
            try
            {
                while (pipeServer.IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    // Read message length (4 bytes, big-endian)
                    byte[] lengthBuffer = new byte[4];
                    int bytesRead = await ReadExactlyAsync(pipeServer, lengthBuffer, cancellationToken).ConfigureAwait(false);

                    if (bytesRead == 0)
                    {
                        // Client disconnected
                        break;
                    }

                    if (bytesRead != 4)
                    {
                        _logger.LogWarning("Invalid message header received.");
                        break;
                    }

                    int messageLength = (lengthBuffer[0] << 24) | (lengthBuffer[1] << 16) | (lengthBuffer[2] << 8) | lengthBuffer[3];

                    if (messageLength <= 0 || messageLength > IpcConstants.MaxMessageSize)
                    {
                        _logger.LogWarning("Invalid message length: {Length}", messageLength);
                        break;
                    }

                    // Read message body
                    byte[] messageBuffer = ArrayPool<byte>.Shared.Rent(messageLength);
                    try
                    {
                        bytesRead = await ReadExactlyAsync(pipeServer, messageBuffer.AsMemory(0, messageLength), cancellationToken).ConfigureAwait(false);

                        if (bytesRead != messageLength)
                        {
                            _logger.LogWarning("Incomplete message received.");
                            break;
                        }

                        // Process the message
                        string json = Encoding.UTF8.GetString(messageBuffer, 0, messageLength);
                        IpcMessage? request = JsonSerializer.Deserialize<IpcMessage>(json, JsonOptions);

                        if (request is null)
                        {
                            _logger.LogWarning("Failed to deserialize message.");
                            continue;
                        }

                        // Handle the request and send response
                        IpcResponse response = await ProcessRequestAsync(request, cancellationToken).ConfigureAwait(false);
                        await SendResponseAsync(pipeServer, response, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(messageBuffer);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Expected
            }
            catch (IOException)
            {
                // Client disconnected
                _logger.LogDebug("Client disconnected (IOException).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling pipe client.");
            }
        }
    }

    private static async Task<int> ReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer[totalRead..], cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return totalRead; // EOF
            }

            totalRead += read;
        }

        return totalRead;
    }

    private async Task SendResponseAsync(NamedPipeServerStream pipeServer, IpcResponse response, CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize<IpcMessage>(response, JsonOptions);
        byte[] messageBytes = Encoding.UTF8.GetBytes(json);

        // Send length prefix
        byte[] lengthBuffer =
        [
            (byte)(messageBytes.Length >> 24),
            (byte)(messageBytes.Length >> 16),
            (byte)(messageBytes.Length >> 8),
            (byte)messageBytes.Length
        ];

        await pipeServer.WriteAsync(lengthBuffer, cancellationToken).ConfigureAwait(false);
        await pipeServer.WriteAsync(messageBytes, cancellationToken).ConfigureAwait(false);
        await pipeServer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<IpcResponse> ProcessRequestAsync(IpcMessage message, CancellationToken cancellationToken)
    {
        try
        {
            return message switch
            {
                GetStatsRequest => HandleGetStats(),
                GetAppsRequest => HandleGetApps(),
                SaveAppRequest req => await HandleSaveAppAsync(req, cancellationToken).ConfigureAwait(false),
                DeleteAppRequest req => await HandleDeleteAppAsync(req, cancellationToken).ConfigureAwait(false),
                ServiceStatusRequest => HandleServiceStatus(),
                ExecuteCommandRequest req => await HandleExecuteCommandAsync(req, cancellationToken).ConfigureAwait(false),
                _ => new ErrorResponse
                {
                    CorrelationId = message.CorrelationId,
                    Success = false,
                    ErrorMessage = $"Unknown request type: {message.GetType().Name}"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing IPC request: {Type}", message.GetType().Name);

            return new ErrorResponse
            {
                CorrelationId = message.CorrelationId,
                Success = false,
                ErrorMessage = ex.Message,
                ExceptionType = ex.GetType().Name,
#if DEBUG
                StackTrace = ex.StackTrace
#endif
            };
        }
    }

    private GetStatsResponse HandleGetStats()
    {
        var stats = _hardwareMonitor.GetStats();

        return new GetStatsResponse
        {
            Success = true,
            Cpu = stats.Cpu,
            Memory = stats.Memory,
            CpuTemp = stats.CpuTemp,
            GpuTemp = stats.GpuTemp
        };
    }

    private GetAppsResponse HandleGetApps()
    {
        var apps = _appCatalog.GetAllApps();

        return new GetAppsResponse
        {
            Success = true,
            Apps = apps
        };
    }

    private async Task<SaveAppResponse> HandleSaveAppAsync(SaveAppRequest request, CancellationToken cancellationToken)
    {
        await _appCatalog.SaveAppAsync(request.App, cancellationToken).ConfigureAwait(false);

        return new SaveAppResponse
        {
            Success = true,
            App = request.App
        };
    }

    private async Task<DeleteAppResponse> HandleDeleteAppAsync(DeleteAppRequest request, CancellationToken cancellationToken)
    {
        bool removed = await _appCatalog.RemoveAppAsync(request.Slot, cancellationToken).ConfigureAwait(false);

        return new DeleteAppResponse
        {
            Success = removed,
            ErrorMessage = removed ? null : $"Slot '{request.Slot}' not found."
        };
    }

    private ServiceStatusResponse HandleServiceStatus()
    {
        var version = typeof(NamedPipeServer).Assembly.GetName().Version;

        return new ServiceStatusResponse
        {
            Success = true,
            Version = version?.ToString() ?? "Unknown",
            UptimeSeconds = (DateTime.UtcNow - _startTimeUtc).TotalSeconds,
            HttpListenerAddress = _httpListenerAddress,
            IsListening = _isListening,
            IsHardwareMonitoringAvailable = true,
            StartTimeUtc = _startTimeUtc
        };
    }

    private async Task<ExecuteCommandResponse> HandleExecuteCommandAsync(ExecuteCommandRequest request, CancellationToken cancellationToken)
    {
        if (request.CommandType == TrayCommandType.None)
        {
            return new ExecuteCommandResponse
            {
                Success = false,
                ErrorMessage = "Invalid command type: None"
            };
        }

        await _commandExecutor.RunCommandAsync(request.CommandType, cancellationToken).ConfigureAwait(false);

        return new ExecuteCommandResponse
        {
            Success = true
        };
    }
}
