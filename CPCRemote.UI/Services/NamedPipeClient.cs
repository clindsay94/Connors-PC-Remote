namespace CPCRemote.UI.Services;

using System.Buffers;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

using CPCRemote.Core.IPC;

/// <summary>
/// Named Pipe client for communicating with the CPCRemote Service.
/// </summary>
public sealed class NamedPipeClient : IPipeClient
{
    private readonly Lock _lock = new();
    private NamedPipeClientStream? _pipeClient;
    private bool _isConnected;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    /// <inheritdoc />
    public bool IsConnected
    {
        get
        {
            lock (_lock)
            {
                return _isConnected && _pipeClient?.IsConnected == true;
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_isConnected && _pipeClient?.IsConnected == true)
            {
                return true;
            }

            // Dispose old connection if any
            _pipeClient?.Dispose();
            _pipeClient = new NamedPipeClientStream(
                ".",
                IpcConstants.PipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);
        }

        try
        {
            int timeoutMs = (int)Math.Min(timeout.TotalMilliseconds, int.MaxValue);
            await _pipeClient.ConnectAsync(timeoutMs, cancellationToken);

            lock (_lock)
            {
                _isConnected = true;
            }

            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public Task DisconnectAsync()
    {
        lock (_lock)
        {
            _isConnected = false;
            _pipeClient?.Dispose();
            _pipeClient = null;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<TResponse> SendRequestAsync<TResponse>(
        IpcRequest request,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
        where TResponse : IpcResponse
    {
        NamedPipeClientStream? pipe;

        lock (_lock)
        {
            pipe = _pipeClient;

            if (pipe is null || !_isConnected || !pipe.IsConnected)
            {
                throw new InvalidOperationException("Not connected to the service. Call ConnectAsync first.");
            }
        }

        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        // Serialize request
        string json = JsonSerializer.Serialize<IpcMessage>(request, JsonOptions);
        byte[] messageBytes = Encoding.UTF8.GetBytes(json);

        // Send length prefix (4 bytes, big-endian)
        byte[] lengthBuffer =
        [
            (byte)(messageBytes.Length >> 24),
            (byte)(messageBytes.Length >> 16),
            (byte)(messageBytes.Length >> 8),
            (byte)messageBytes.Length
        ];

        await pipe.WriteAsync(lengthBuffer, cts.Token);
        await pipe.WriteAsync(messageBytes, cts.Token);
        await pipe.FlushAsync(cts.Token);

        // Read response length
        byte[] responseLengthBuffer = new byte[4];
        int bytesRead = await ReadExactlyAsync(pipe, responseLengthBuffer, cts.Token);

        if (bytesRead != 4)
        {
            throw new IOException("Failed to read response length.");
        }

        int responseLength = (responseLengthBuffer[0] << 24) |
                             (responseLengthBuffer[1] << 16) |
                             (responseLengthBuffer[2] << 8) |
                             responseLengthBuffer[3];

        if (responseLength <= 0 || responseLength > IpcConstants.MaxMessageSize)
        {
            throw new InvalidOperationException($"Invalid response length: {responseLength}");
        }

        // Read response body
        byte[] responseBuffer = ArrayPool<byte>.Shared.Rent(responseLength);
        try
        {
            bytesRead = await ReadExactlyAsync(pipe, responseBuffer.AsMemory(0, responseLength), cts.Token);

            if (bytesRead != responseLength)
            {
                throw new IOException("Incomplete response received.");
            }

            string responseJson = Encoding.UTF8.GetString(responseBuffer, 0, responseLength);
            IpcMessage? response = JsonSerializer.Deserialize<IpcMessage>(responseJson, JsonOptions);

            if (response is null)
            {
                throw new InvalidOperationException("Failed to deserialize response.");
            }

            // Handle error responses
            if (response is ErrorResponse errorResponse)
            {
                throw new IpcException(
                    errorResponse.ErrorMessage ?? "Unknown error",
                    errorResponse.ExceptionType,
                    errorResponse.StackTrace);
            }

            if (response is not TResponse typedResponse)
            {
                throw new InvalidOperationException(
                    $"Unexpected response type. Expected {typeof(TResponse).Name}, got {response.GetType().Name}");
            }

            return typedResponse;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(responseBuffer);
        }
    }

    private static async Task<int> ReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer[totalRead..], cancellationToken);
            if (read == 0)
            {
                return totalRead; // EOF
            }

            totalRead += read;
        }

        return totalRead;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}

/// <summary>
/// Exception thrown when an IPC operation fails on the server side.
/// </summary>
public sealed class IpcException : Exception
{
    /// <summary>
    /// Gets the original exception type name from the server, if available.
    /// </summary>
    public string? OriginalExceptionType { get; }

    /// <summary>
    /// Gets the server-side stack trace, if available (debug builds only).
    /// </summary>
    public string? ServerStackTrace { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IpcException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="originalExceptionType">The original exception type name.</param>
    /// <param name="serverStackTrace">The server-side stack trace.</param>
    public IpcException(string message, string? originalExceptionType = null, string? serverStackTrace = null)
        : base(message)
    {
        OriginalExceptionType = originalExceptionType;
        ServerStackTrace = serverStackTrace;
    }
}
