using System;
using System.Diagnostics;
using Microsoft.Windows.ApplicationModel.DynamicDependency;

namespace CPCRemote.UI.Helpers
{
    internal static class BootstrapHelper
    {
        private const uint WinAppSdkVersion = 0x00010008; // Windows App SDK 1.8
        private static readonly PackageVersion MinimumRuntimeVersion = new()
        {
            Major = 8000,
            Minor = 642,
            Build = 119,
            Revision = 0,
        };

        /// <summary>
        /// Attempts to initialize the Windows App SDK bootstrapper and logs the HRESULT outcome.
        /// </summary>
        /// <param name="logMessage">Delegate used for logging status messages.</param>
        /// <returns><c>true</c> when initialization succeeds; otherwise <c>false</c>.</returns>
        internal static bool Initialize(Action<string>? logMessage)
        {
            try
            {
                if (TryInitializePrimary(out int initResult))
                {
                    logMessage?.Invoke($"Windows App SDK bootstrap succeeded (HRESULT=0x{initResult:X8} {DescribeResult(initResult)}).");
                    return true;
                }

                logMessage?.Invoke($"Primary Windows App SDK bootstrap attempt failed (HRESULT=0x{initResult:X8} {DescribeResult(initResult)}). Retrying without version gating...");

                if (Bootstrap.TryInitialize(WinAppSdkVersion, out initResult))
                {
                    logMessage?.Invoke($"Fallback bootstrap succeeded (HRESULT=0x{initResult:X8} {DescribeResult(initResult)}).");
                    return true;
                }

                logMessage?.Invoke($"Windows App SDK bootstrap failed after fallback (HRESULT=0x{initResult:X8} {DescribeResult(initResult)}).");
                return false;
            }
            catch (DllNotFoundException ex)
            {
                logMessage?.Invoke($"Bootstrap DLL not found: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                logMessage?.Invoke($"Bootstrap initialization threw an exception: {ex}");
                return false;
            }
        }

        private static bool TryInitializePrimary(out int initResult)
        {
            return Bootstrap.TryInitialize(
                WinAppSdkVersion,
                string.Empty,
                MinimumRuntimeVersion,
                Bootstrap.InitializeOptions.None,
                out initResult);
        }

        private static string DescribeResult(int hresult)
        {
            return hresult switch
            {
                0 => "(S_OK)",
                unchecked((int)0x80070490) => "(ERROR_NOT_FOUND)",
                unchecked((int)0x80073D02) => "(ERROR_PACKAGE_ALREADY_EXISTS)",
                unchecked((int)0x80073CF3) => "(ERROR_INSTALL_RESOLVE_DEPENDENCY_FAILED)",
                unchecked((int)0x80070005) => "(E_ACCESSDENIED)",
                unchecked((int)0x8007000E) => "(E_OUTOFMEMORY)",
                unchecked((int)0x80073CF0) => "(ERROR_INSTALL_FAILED)",
                _ => string.Empty,
            };
        }
    }
}
