namespace CPCRemote.Core.Helpers;

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Provides standardized paths for configuration and data files.
/// Handles both packaged (MSIX) and unpackaged deployment scenarios.
/// </summary>
/// <remarks>
/// <para>
/// MSIX packages install to a protected WindowsApps folder that is read-only,
/// even for administrators. This class redirects configuration files to writable
/// locations:
/// </para>
/// <list type="bullet">
/// <item><b>Service data:</b> %PROGRAMDATA%\CPCRemote (accessible by service running as SYSTEM)</item>
/// <item><b>User data:</b> %LOCALAPPDATA%\CPCRemote (per-user settings for UI)</item>
/// </list>
/// </remarks>
public static class ConfigurationPaths
{
    /// <summary>
    /// The application name used for creating subdirectories.
    /// </summary>
    public const string AppName = "CPCRemote";

    /// <summary>
    /// Gets the path for service configuration files.
    /// Uses %PROGRAMDATA%\CPCRemote which is writable by services running as SYSTEM
    /// and by administrators.
    /// </summary>
    /// <remarks>
    /// Creates the directory if it doesn't exist.
    /// </remarks>
    public static string ServiceDataPath
    {
        get
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                AppName);
            
            EnsureDirectoryExists(path);
            return path;
        }
    }

    /// <summary>
    /// Gets the path for user-specific configuration files.
    /// Uses %LOCALAPPDATA%\CPCRemote which is writable by the current user.
    /// </summary>
    /// <remarks>
    /// Creates the directory if it doesn't exist.
    /// </remarks>
    public static string UserDataPath
    {
        get
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppName);
            
            EnsureDirectoryExists(path);
            return path;
        }
    }

    /// <summary>
    /// Gets the full path for a service configuration file.
    /// </summary>
    /// <param name="fileName">The configuration file name (e.g., "appsettings.json").</param>
    /// <returns>The full path to the configuration file in the service data directory.</returns>
    public static string GetServiceConfigPath(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return Path.Combine(ServiceDataPath, fileName);
    }

    /// <summary>
    /// Gets the full path for a user configuration file.
    /// </summary>
    /// <param name="fileName">The configuration file name.</param>
    /// <returns>The full path to the configuration file in the user data directory.</returns>
    public static string GetUserConfigPath(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return Path.Combine(UserDataPath, fileName);
    }

    /// <summary>
    /// Gets the application's base directory (where the executable is located).
    /// </summary>
    /// <remarks>
    /// This is read-only in MSIX deployments. Use for reading bundled resources only,
    /// not for writing configuration files.
    /// </remarks>
    public static string ApplicationDirectory => AppContext.BaseDirectory;

    /// <summary>
    /// Determines whether the application is running as a packaged (MSIX) app.
    /// </summary>
    /// <returns><c>true</c> if running as a packaged app; otherwise, <c>false</c>.</returns>
    public static bool IsPackagedApp()
    {
        try
        {
            int length = 0;
            // APPMODEL_ERROR_NO_PACKAGE = 15700
            return GetCurrentPackageFullName(ref length, null) != 15700;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Ensures a configuration file exists in the service data directory.
    /// If the file doesn't exist but a default exists in the application directory,
    /// it copies the default to the writable location.
    /// </summary>
    /// <param name="fileName">The configuration file name.</param>
    /// <returns>The full path to the configuration file.</returns>
    public static string EnsureServiceConfigExists(string fileName)
    {
        string targetPath = GetServiceConfigPath(fileName);
        
        if (!File.Exists(targetPath))
        {
            // Try to copy from various possible source locations
            string[] possibleSourcePaths =
            [
                // Direct in application directory
                Path.Combine(ApplicationDirectory, fileName),
                // In ServiceBinaries subfolder (MSIX UI deployment)
                Path.Combine(ApplicationDirectory, "ServiceBinaries", fileName),
                // Parent directory (if service is in ServiceBinaries)
                Path.Combine(ApplicationDirectory, "..", fileName),
            ];

            foreach (string sourcePath in possibleSourcePaths)
            {
                string normalizedPath = Path.GetFullPath(sourcePath);
                if (File.Exists(normalizedPath))
                {
                    try
                    {
                        File.Copy(normalizedPath, targetPath, overwrite: false);
                        break;
                    }
                    catch (IOException)
                    {
                        // File was created by another process, that's fine
                        break;
                    }
                }
            }

            // If still doesn't exist, create a minimal default config
            if (!File.Exists(targetPath))
            {
                string defaultConfig = """
                    {
                      "rsm": {
                        "ipAddress": "0.0.0.0",
                        "port": 5005,
                        "secret": "",
                        "useHttps": false,
                        "certificateThumbprint": ""
                      },
                      "wol": {
                        "macAddress": "",
                        "broadcastAddress": "255.255.255.255",
                        "port": 9
                      },
                      "sensors": {
                        "cpuLoad": { "sensorName": "CPU", "labelPattern": "Total" },
                        "memoryLoad": { "sensorName": "Memory", "labelPattern": "Memory" },
                        "cpuTemp": { "sensorName": "CPU", "labelPattern": "Core" },
                        "gpuTemp": { "sensorName": "GPU", "labelPattern": "Temperature" },
                        "customSensors": []
                      },
                      "apps": {
                        "catalogPath": ""
                      }
                    }
                    """;
                try
                {
                    File.WriteAllText(targetPath, defaultConfig);
                }
                catch
                {
                    // If we can't write, return the path anyway - caller will handle the error
                }
            }
        }
        
        return targetPath;
    }

    /// <summary>
    /// Creates the directory if it doesn't exist.
    /// </summary>
    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder? packageFullName);
}
