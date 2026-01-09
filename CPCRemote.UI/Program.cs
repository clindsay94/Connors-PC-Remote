using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace CPCRemote.UI;

public static partial class Program
{
    // DPI Awareness constants (WACK Req 26 compliance)
    private const int DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;

    [LibraryImport("Microsoft.ui.xaml.dll")]
    private static partial void XamlCheckProcessRequirements();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetProcessDpiAwarenessContext(nint value);

    [STAThread]
    static void Main(string[] args)
    {
        // With WindowsAppSDKSelfContained=true, we don't need Bootstrap.Initialize
        // The runtime DLLs are bundled with the app
        
        try
        {
            // Set DPI awareness FIRST before any UI operations (WACK Req 26)
            // This ensures the app is properly DPI-aware on high-DPI displays
            SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

            // Initialize COM wrappers - required for WinRT interop
            WinRT.ComWrappersSupport.InitializeComWrappers();

            // Global exception handlers for background/stowed exceptions
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                LogFailure(e.ExceptionObject as Exception, "AppDomain.UnhandledException");
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogFailure(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved(); // Prevent process termination if possible
            };

            // Check XAML requirements
            XamlCheckProcessRequirements();

            // Start the UI Thread
            Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });
        }
        catch (Exception ex)
        {
            LogFailure(ex, "Main Main() Catch");
            throw; // Re-throw to ensure Windows reports it
        }
    }

    /// <summary>
    /// Writes fatal errors to a log file in %LOCALAPPDATA%\CPCRemote.
    /// Uses direct file I/O to maximize chance of success during a crash.
    /// </summary>
    public static void LogFailure(Exception? ex, string source)
    {
        if (ex == null) return;

        try
        {
            string localData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CPCRemote");
            Directory.CreateDirectory(localData);
            string logPath = Path.Combine(localData, "StartupError.log");
            
            string errorMsg = $"[{DateTime.Now}] CRITICAL CRASH ({source}): {ex.GetType().Name}\n" +
                              $"Message: {ex.Message}\n" +
                              $"Stack Trace:\n{ex.StackTrace}\n" +
                              $"Inner Exception:\n{ex.InnerException}\n" +
                              "--------------------------------------------------\n";
            
            File.AppendAllText(logPath, errorMsg);
            Debug.WriteLine($"CRITICAL LOGGED: {errorMsg}");
        }
        catch
        {
            // If logging fails during a crash, there's nothing else we can do.
        }
    }
}