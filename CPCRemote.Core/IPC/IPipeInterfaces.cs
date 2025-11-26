namespace CPCRemote.Core.IPC;

/// <summary>
/// Defines the contract for a Named Pipes server that handles IPC messages.
/// </summary>
public interface IPipeServer : IAsyncDisposable
{
    /// <summary>
    /// Gets whether the server is currently running and accepting connections.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Starts the pipe server and begins accepting client connections.
    /// </summary>
    /// <param name="cancellationToken">Token to stop the server.</param>
    /// <returns>A task that completes when the server has started.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the pipe server and disconnects all clients.
    /// </summary>
    /// <returns>A task that completes when the server has stopped.</returns>
    Task StopAsync();
}

/// <summary>
/// Defines the contract for a Named Pipes client that sends IPC messages.
/// </summary>
public interface IPipeClient : IAsyncDisposable
{
    /// <summary>
    /// Gets whether the client is currently connected to the server.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connects to the pipe server.
    /// </summary>
    /// <param name="timeout">Connection timeout.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connected successfully, false otherwise.</returns>
    Task<bool> ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the pipe server.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Sends a request and waits for the response.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="timeout">Response timeout.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the server.</returns>
    Task<TResponse> SendRequestAsync<TResponse>(IpcRequest request, TimeSpan timeout, CancellationToken cancellationToken = default)
        where TResponse : IpcResponse;
}
