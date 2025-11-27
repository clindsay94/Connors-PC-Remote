namespace CPCRemote.Service.Services;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Microsoft.Extensions.Logging;

/// <summary>
/// Launches processes in the interactive user session from Session 0 (Windows Service).
/// This is required because services run in Session 0 and cannot directly show UI.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class UserSessionLauncher(ILogger<UserSessionLauncher> logger)
{
    private readonly ILogger<UserSessionLauncher> _logger = logger;

    /// <summary>
    /// Launches an application in the current interactive user's session.
    /// </summary>
    /// <param name="executablePath">Full path to the executable.</param>
    /// <param name="arguments">Command line arguments (optional).</param>
    /// <param name="workingDirectory">Working directory (optional).</param>
    /// <returns>True if the process was launched successfully.</returns>
    public bool LaunchInUserSession(string executablePath, string? arguments = null, string? workingDirectory = null)
    {
        IntPtr userToken = IntPtr.Zero;
        IntPtr duplicateToken = IntPtr.Zero;
        IntPtr environment = IntPtr.Zero;

        try
        {
            // Get the session ID of the active console session (the user logged in at the physical console)
            uint sessionId = WTSGetActiveConsoleSessionId();
            if (sessionId == 0xFFFFFFFF)
            {
                _logger.LogWarning("No active console session found. User may not be logged in.");
                return false;
            }

            _logger.LogDebug("Active console session ID: {SessionId}", sessionId);

            // Get the user token for the session
            if (!WTSQueryUserToken(sessionId, out userToken))
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogWarning("WTSQueryUserToken failed with error {Error}. Service may need to run as LocalSystem.", error);
                return false;
            }

            // Duplicate the token for CreateProcessAsUser
            var securityAttributes = new SECURITY_ATTRIBUTES
            {
                nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>(),
                bInheritHandle = false,
                lpSecurityDescriptor = IntPtr.Zero
            };

            if (!DuplicateTokenEx(
                userToken,
                TOKEN_ALL_ACCESS,
                ref securityAttributes,
                SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
                TOKEN_TYPE.TokenPrimary,
                out duplicateToken))
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogError("DuplicateTokenEx failed with error {Error}", error);
                return false;
            }

            // Create environment block for the user
            if (!CreateEnvironmentBlock(out environment, duplicateToken, false))
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogWarning("CreateEnvironmentBlock failed with error {Error}, continuing without user environment", error);
                environment = IntPtr.Zero;
            }

            // Build the command line - CreateProcessAsUser requires a mutable string
            // IMPORTANT: When lpApplicationName is NULL, the executable name must be in lpCommandLine
            // and must be quoted if it contains spaces. The entire command line becomes argv[0] + args.
            // This is the most reliable approach for paths with spaces.
            
            // Normalize path separators and ensure proper quoting
            string normalizedPath = executablePath.Replace('/', '\\');
            
            // Build command line: quoted path + optional arguments
            string commandLine = string.IsNullOrEmpty(arguments)
                ? $"\"{normalizedPath}\""
                : $"\"{normalizedPath}\" {arguments}";

            // Determine working directory - use executable's directory if not specified
            string? effectiveWorkingDir = workingDirectory;
            if (string.IsNullOrEmpty(effectiveWorkingDir))
            {
                try
                {
                    effectiveWorkingDir = Path.GetDirectoryName(normalizedPath);
                }
                catch
                {
                    // If we can't get the directory, leave it null
                }
            }

            _logger.LogDebug("Launching: CommandLine={CmdLine}, WorkingDir={WorkDir}",
                commandLine, effectiveWorkingDir ?? "(default)");

            // Set up startup info
            var startupInfo = new STARTUPINFO
            {
                cb = Marshal.SizeOf<STARTUPINFO>(),
                lpDesktop = "winsta0\\default", // Interactive window station and desktop
                dwFlags = STARTF_USESHOWWINDOW,
                wShowWindow = SW_SHOW
            };

            // Set up creation flags
            uint creationFlags = CREATE_NEW_CONSOLE | CREATE_UNICODE_ENVIRONMENT;

            // Create the process in the user's session
            // Using NULL for lpApplicationName is more reliable for paths with spaces
            // The executable path is extracted from the quoted string in lpCommandLine
            bool success = CreateProcessAsUser(
                duplicateToken,
                null,            // lpApplicationName - NULL, executable is in lpCommandLine
                commandLine,     // lpCommandLine - full command line with quoted path and arguments
                ref securityAttributes,
                ref securityAttributes,
                false,
                creationFlags,
                environment,
                effectiveWorkingDir,
                ref startupInfo,
                out PROCESS_INFORMATION processInfo);

            if (success)
            {
                _logger.LogInformation("Successfully launched process in user session: {Path} (PID: {Pid})", 
                    executablePath, processInfo.dwProcessId);
                
                // Close process and thread handles
                CloseHandle(processInfo.hProcess);
                CloseHandle(processInfo.hThread);
                return true;
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogError("CreateProcessAsUser failed with error {Error}: {Message}", 
                    error, new Win32Exception(error).Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while launching process in user session");
            return false;
        }
        finally
        {
            // Clean up handles
            if (environment != IntPtr.Zero)
            {
                DestroyEnvironmentBlock(environment);
            }

            if (duplicateToken != IntPtr.Zero)
            {
                CloseHandle(duplicateToken);
            }

            if (userToken != IntPtr.Zero)
            {
                CloseHandle(userToken);
            }
        }
    }

    #region P/Invoke Declarations

    private const uint TOKEN_ALL_ACCESS = 0xF01FF;
    private const uint CREATE_NEW_CONSOLE = 0x00000010;
    private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
    private const int STARTF_USESHOWWINDOW = 0x00000001;
    private const short SW_SHOW = 5;

    private enum SECURITY_IMPERSONATION_LEVEL
    {
        SecurityAnonymous,
        SecurityIdentification,
        SecurityImpersonation,
        SecurityDelegation
    }

    private enum TOKEN_TYPE
    {
        TokenPrimary = 1,
        TokenImpersonation
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        public bool bInheritHandle;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WTSGetActiveConsoleSessionId();

    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSQueryUserToken(uint sessionId, out IntPtr phToken);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcessAsUser(
        IntPtr hToken,
        string? lpApplicationName,
        string lpCommandLine,
        ref SECURITY_ATTRIBUTES lpProcessAttributes,
        ref SECURITY_ATTRIBUTES lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string? lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool DuplicateTokenEx(
        IntPtr hExistingToken,
        uint dwDesiredAccess,
        ref SECURITY_ATTRIBUTES lpTokenAttributes,
        SECURITY_IMPERSONATION_LEVEL impersonationLevel,
        TOKEN_TYPE tokenType,
        out IntPtr phNewToken);

    [DllImport("userenv.dll", SetLastError = true)]
    private static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

    [DllImport("userenv.dll", SetLastError = true)]
    private static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    #endregion
}
