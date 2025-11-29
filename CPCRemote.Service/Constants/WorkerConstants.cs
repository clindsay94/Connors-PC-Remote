namespace CPCRemote.Service.Constants;

/// <summary>
/// Constants used by the <see cref="Worker"/> background service for HTTP listener
/// retry logic and security throttling.
/// </summary>
public static class WorkerConstants
{
    /// <summary>
    /// Maximum number of retry attempts before the service gives up and stops.
    /// Used for HTTP listener binding failures (port conflicts, access denied, etc.).
    /// </summary>
    public const int MaxRetryAttempts = 10;

    /// <summary>
    /// Initial delay in milliseconds before the first retry attempt.
    /// Subsequent retries use exponential backoff up to <see cref="MaxRetryDelayMs"/>.
    /// </summary>
    public const int InitialRetryDelayMs = 1000;

    /// <summary>
    /// Maximum delay in milliseconds between retry attempts.
    /// Caps the exponential backoff at 60 seconds.
    /// </summary>
    public const int MaxRetryDelayMs = 60000;

    /// <summary>
    /// Throttle duration in seconds for logging unauthorized access attempts.
    /// Prevents log flooding from repeated unauthorized requests from the same IP.
    /// </summary>
    public const int UnauthorizedLogThrottlingSeconds = 60;
}
