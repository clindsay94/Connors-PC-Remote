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
            // Log fatal startup errors to a file in LocalAppData, as Event Viewer often truncates or hides details
            try
            {
                string localData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CPCRemote");
                Directory.CreateDirectory(localData);
                string logPath = Path.Combine(localData, "StartupError.log");
                
                string errorMsg = $"[{DateTime.Now}] FATAL CRASH: {ex}\n\nStack Trace:\n{ex.StackTrace}\n\nInner Exception:\n{ex.InnerException}";
                File.AppendAllText(logPath, errorMsg + "\n--------------------------------------------------\n");
                
                Debug.WriteLine($"FATAL STARTUP ERROR: {ex}");
            }
            catch { /* Best effort logging */ }
            
            throw;
        }
    }
}