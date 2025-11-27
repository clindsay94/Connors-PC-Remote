namespace CPCRemote.Service.Services;

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Microsoft.Extensions.Logging;

/// <summary>
/// Launches processes in the interactive user session from Session 0 (Windows Service).
/// This is required because services run in Session 0 and cannot directly show UI.
/// Supports launching elevated (admin) processes when the logged-in user is an administrator.
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
    /// <param name="runAsAdmin">If true, attempts to launch with elevated privileges.</param>
    /// <returns>True if the process was launched successfully.</returns>
    public bool LaunchInUserSession(string executablePath, string? arguments = null, string? workingDirectory = null, bool runAsAdmin = false)
    {
        IntPtr userToken = IntPtr.Zero;
        IntPtr processToken = IntPtr.Zero;
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

            _logger.LogDebug("Active console session ID: {SessionId}, RunAsAdmin: {RunAsAdmin}", sessionId, runAsAdmin);

            // Get the user token for the session
            if (!WTSQueryUserToken(sessionId, out userToken))
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogWarning("WTSQueryUserToken failed with error {Error}. Service may need to run as LocalSystem.", error);
                return false;
            }

            // Determine which token to use
            if (runAsAdmin)
            {
                // Try to get the linked (elevated) token for admin launches
                processToken = GetElevatedToken(userToken, sessionId);
                if (processToken == IntPtr.Zero)
                {
                    _logger.LogWarning("Could not obtain elevated token. User may not be an administrator. Falling back to standard launch.");
                    processToken = DuplicateUserToken(userToken);
                }
            }
            else
            {
                // Standard (non-elevated) launch
                processToken = DuplicateUserToken(userToken);
            }

            if (processToken == IntPtr.Zero)
            {
                _logger.LogError("Failed to obtain a valid process token.");
                return false;
            }

            // Create environment block for the user
            if (!CreateEnvironmentBlock(out environment, processToken, false))
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogWarning("CreateEnvironmentBlock failed with error {Error}, continuing without user environment", error);
                environment = IntPtr.Zero;
            }

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

            _logger.LogDebug("Launching: CommandLine={CmdLine}, WorkingDir={WorkDir}, Elevated={Elevated}",
                commandLine, effectiveWorkingDir ?? "(default)", runAsAdmin);

            // Set up startup info
            var startupInfo = new STARTUPINFO
            {
                cb = Marshal.SizeOf<STARTUPINFO>(),
                lpDesktop = "winsta0\\default", // Interactive window station and desktop
                dwFlags = STARTF_USESHOWWINDOW,
                wShowWindow = SW_SHOW
            };

            // Set up security attributes
            var securityAttributes = new SECURITY_ATTRIBUTES
            {
                nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>(),
                bInheritHandle = false,
                lpSecurityDescriptor = IntPtr.Zero
            };

            // Set up creation flags
            uint creationFlags = CREATE_NEW_CONSOLE | CREATE_UNICODE_ENVIRONMENT;

            // Create the process in the user's session
            bool success = CreateProcessAsUser(
                processToken,
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
                _logger.LogInformation("Successfully launched process in user session: {Path} (PID: {Pid}, Elevated: {Elevated})", 
                    executablePath, processInfo.dwProcessId, runAsAdmin);
                
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

            if (processToken != IntPtr.Zero)
            {
                CloseHandle(processToken);
            }

            if (userToken != IntPtr.Zero)
            {
                CloseHandle(userToken);
            }
        }
    }

    /// <summary>
    /// Gets the elevated (linked) token from a split token when UAC is active.
    /// This allows launching elevated processes without a UAC prompt when the user is an admin.
    /// </summary>
    private IntPtr GetElevatedToken(IntPtr userToken, uint sessionId)
    {
        IntPtr linkedToken = IntPtr.Zero;
        IntPtr duplicatedToken = IntPtr.Zero;

        try
        {
            // First, check the token elevation type
            int elevationType = 0;
            int returnLength = 0;
            
            if (!GetTokenInformation(userToken, TOKEN_INFORMATION_CLASS.TokenElevationType, 
                ref elevationType, sizeof(int), out returnLength))
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogDebug("GetTokenInformation (TokenElevationType) failed with error {Error}", error);
                return IntPtr.Zero;
            }

            _logger.LogDebug("Token elevation type: {Type}", (TOKEN_ELEVATION_TYPE)elevationType);

            // If already elevated (type 2 = TokenElevationTypeFull), just duplicate the token
            if ((TOKEN_ELEVATION_TYPE)elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull)
            {
                _logger.LogDebug("Token is already elevated, duplicating directly");
                return DuplicateUserToken(userToken);
            }

            // If it's a limited token (type 3 = TokenElevationTypeLimited), get the linked elevated token
            if ((TOKEN_ELEVATION_TYPE)elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited)
            {
                // Get the linked token (the elevated token)
                int tokenSize = IntPtr.Size;
                IntPtr linkedTokenBuffer = Marshal.AllocHGlobal(tokenSize);
                
                try
                {
                    if (!GetTokenInformationPtr(userToken, TOKEN_INFORMATION_CLASS.TokenLinkedToken,
                        linkedTokenBuffer, tokenSize, out returnLength))
                    {
                        int error = Marshal.GetLastWin32Error();
                        _logger.LogDebug("GetTokenInformation (TokenLinkedToken) failed with error {Error}", error);
                        return IntPtr.Zero;
                    }

                    linkedToken = Marshal.ReadIntPtr(linkedTokenBuffer);
                    _logger.LogDebug("Successfully obtained linked (elevated) token");
                }
                finally
                {
                    Marshal.FreeHGlobal(linkedTokenBuffer);
                }

                // The linked token needs to have its session ID set to match the interactive session
                if (!SetTokenInformation(linkedToken, TOKEN_INFORMATION_CLASS.TokenSessionId, 
                    ref sessionId, sizeof(uint)))
                {
                    int error = Marshal.GetLastWin32Error();
                    _logger.LogDebug("SetTokenInformation (TokenSessionId) failed with error {Error}", error);
                    // Continue anyway, might still work
                }

                // Duplicate the linked token as a primary token for CreateProcessAsUser
                var securityAttributes = new SECURITY_ATTRIBUTES
                {
                    nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>(),
                    bInheritHandle = false,
                    lpSecurityDescriptor = IntPtr.Zero
                };

                if (!DuplicateTokenEx(
                    linkedToken,
                    TOKEN_ALL_ACCESS,
                    ref securityAttributes,
                    SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                    TOKEN_TYPE.TokenPrimary,
                    out duplicatedToken))
                {
                    int error = Marshal.GetLastWin32Error();
                    _logger.LogDebug("DuplicateTokenEx for linked token failed with error {Error}", error);
                    return IntPtr.Zero;
                }

                _logger.LogDebug("Successfully duplicated elevated token for process creation");
                
                // Transfer ownership - don't close duplicatedToken in finally
                IntPtr result = duplicatedToken;
                duplicatedToken = IntPtr.Zero;
                return result;
            }

            // Type 1 = TokenElevationTypeDefault means UAC is disabled or user is not admin
            _logger.LogDebug("Token elevation type is Default - UAC may be disabled or user is not admin");
            return DuplicateUserToken(userToken);
        }
        finally
        {
            if (linkedToken != IntPtr.Zero)
            {
                CloseHandle(linkedToken);
            }

            if (duplicatedToken != IntPtr.Zero)
            {
                CloseHandle(duplicatedToken);
            }
        }
    }

    /// <summary>
    /// Duplicates a user token for use with CreateProcessAsUser.
    /// </summary>
    private IntPtr DuplicateUserToken(IntPtr userToken)
    {
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
            out IntPtr duplicateToken))
        {
            int error = Marshal.GetLastWin32Error();
            _logger.LogError("DuplicateTokenEx failed with error {Error}", error);
            return IntPtr.Zero;
        }

        return duplicateToken;
    }

    #region P/Invoke Declarations

    private const uint TOKEN_ALL_ACCESS = 0xF01FF;
    private const uint CREATE_NEW_CONSOLE = 0x00000010;
    private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
    private const int STARTF_USESHOWWINDOW = 0x00000001;
    private const short SW_SHOW = 5;

    private enum TOKEN_INFORMATION_CLASS
    {
        TokenUser = 1,
        TokenGroups,
        TokenPrivileges,
        TokenOwner,
        TokenPrimaryGroup,
        TokenDefaultDacl,
        TokenSource,
        TokenType,
        TokenImpersonationLevel,
        TokenStatistics,
        TokenRestrictedSids,
        TokenSessionId,
        TokenGroupsAndPrivileges,
        TokenSessionReference,
        TokenSandBoxInert,
        TokenAuditPolicy,
        TokenOrigin,
        TokenElevationType,
        TokenLinkedToken,
        TokenElevation,
        TokenHasRestrictions,
        TokenAccessInformation,
        TokenVirtualizationAllowed,
        TokenVirtualizationEnabled,
        TokenIntegrityLevel,
        TokenUIAccess,
        TokenMandatoryPolicy,
        TokenLogonSid
    }

    private enum TOKEN_ELEVATION_TYPE
    {
        TokenElevationTypeDefault = 1,
        TokenElevationTypeFull,
        TokenElevationTypeLimited
    }

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

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetTokenInformation(
        IntPtr tokenHandle,
        TOKEN_INFORMATION_CLASS tokenInformationClass,
        ref int tokenInformation,
        int tokenInformationLength,
        out int returnLength);

    [DllImport("advapi32.dll", SetLastError = true, EntryPoint = "GetTokenInformation")]
    private static extern bool GetTokenInformationPtr(
        IntPtr tokenHandle,
        TOKEN_INFORMATION_CLASS tokenInformationClass,
        IntPtr tokenInformation,
        int tokenInformationLength,
        out int returnLength);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool SetTokenInformation(
        IntPtr tokenHandle,
        TOKEN_INFORMATION_CLASS tokenInformationClass,
        ref uint tokenInformation,
        int tokenInformationLength);

    [DllImport("userenv.dll", SetLastError = true)]
    private static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

    [DllImport("userenv.dll", SetLastError = true)]
    private static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    #endregion
}
