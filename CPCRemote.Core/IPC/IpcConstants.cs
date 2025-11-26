namespace CPCRemote.Core.IPC;

/// <summary>
/// Constants shared between the pipe server and client.
/// </summary>
public static class IpcConstants
{
    /// <summary>
    /// The name of the named pipe used for IPC communication.
    /// </summary>
    public const string PipeName = "CPCRemote_IPC";

    /// <summary>
    /// Default timeout for sending/receiving messages.
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Default timeout for connecting to the pipe.
    /// </summary>
    public static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Maximum message size in bytes (1 MB).
    /// </summary>
    public const int MaxMessageSize = 1024 * 1024;

    /// <summary>
    /// Buffer size for pipe operations.
    /// </summary>
    public const int BufferSize = 65536;
}
