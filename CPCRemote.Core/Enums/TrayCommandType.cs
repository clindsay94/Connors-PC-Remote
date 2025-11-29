namespace CPCRemote.Core.Enums;

/// <summary>
/// Defines the types of remote control commands that can be executed on the PC.
/// </summary>
/// <remarks>
/// <para>
/// These commands map to HTTP endpoints exposed by the service.
/// The SmartThings Edge driver sends these commands via HTTP GET requests.
/// </para>
/// <para>
/// <b>Bridge Sync:</b> Any changes to this enum require corresponding updates to:
/// <list type="bullet">
/// <item>Lua driver's <c>pcshutdown</c> capability enum</item>
/// <item>Lua driver's <c>handle_switch</c> function in init.lua</item>
/// </list>
/// </para>
/// </remarks>
public enum TrayCommandType
{
    /// <summary>
    /// No command specified. Used as a default/null value.
    /// </summary>
    None = 0,

    /// <summary>
    /// Gracefully restarts the PC, allowing applications to close cleanly.
    /// </summary>
    /// <remarks>Uses ExitWindowsEx with EWX_REBOOT flag.</remarks>
    Restart,

    /// <summary>
    /// Turns off the display monitor(s) without affecting the PC state.
    /// </summary>
    /// <remarks>Sends WM_SYSCOMMAND with SC_MONITORPOWER to turn off displays.</remarks>
    TurnScreenOff,

    /// <summary>
    /// Gracefully shuts down the PC, allowing applications to close cleanly.
    /// </summary>
    /// <remarks>Uses ExitWindowsEx with EWX_SHUTDOWN flag.</remarks>
    Shutdown,

    /// <summary>
    /// Forces an immediate shutdown after a 10-second delay, terminating all applications.
    /// </summary>
    /// <remarks>
    /// Uses <c>shutdown /s /t 10 /f</c> command. The delay allows the HTTP response to be sent.
    /// </remarks>
    ForceShutdown,

    /// <summary>
    /// Locks the workstation, requiring password entry to resume.
    /// </summary>
    /// <remarks>Uses LockWorkStation Windows API.</remarks>
    Lock,

    /// <summary>
    /// Reboots directly into UEFI/BIOS firmware settings.
    /// </summary>
    /// <remarks>
    /// Uses <c>shutdown /r /fw /t 0</c> command. Requires Windows 8+ and UEFI firmware.
    /// </remarks>
    UEFIReboot,

    /// <summary>
    /// Sends a Wake-on-LAN magic packet to wake a sleeping or powered-off PC.
    /// </summary>
    /// <remarks>
    /// Target MAC address is configured in <see cref="Models.WolOptions"/>.
    /// Requires the target PC to have WoL enabled in BIOS/UEFI.
    /// </remarks>
    WakeOnLan
}
