namespace CPCRemote.Core.Constants;

/// <summary>
/// Contains constant values shared across all projects in the solution.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a single source of truth for values that must be consistent
/// across the Service, UI, and Test projects.
/// </para>
/// <para>
/// <b>Important:</b> Changing these values may require updates to:
/// <list type="bullet">
/// <item>Windows Service registration (sc.exe commands)</item>
/// <item>Service installer/uninstaller scripts</item>
/// <item>Documentation and README files</item>
/// </list>
/// </para>
/// </remarks>
public static class ServiceConstants
{
    /// <summary>
    /// The Windows Service name registered with the Service Control Manager (SCM).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This name is used for:
    /// <list type="bullet">
    /// <item>Service registration: <c>sc.exe create CPCRemote.Service ...</c></item>
    /// <item>Service control: <c>sc.exe start/stop CPCRemote.Service</c></item>
    /// <item>UI status checks via <see cref="System.ServiceProcess.ServiceController"/></item>
    /// </list>
    /// </para>
    /// <para>
    /// Service names must not contain spaces and should be under 256 characters.
    /// </para>
    /// </remarks>
    public const string RemoteShutdownServiceName = "CPCRemote.Service";
}
