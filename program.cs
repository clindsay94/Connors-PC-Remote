using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency; // For Bootstrap

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace CPCRemote.UI
{
    public static class Program
    {
        [DllImport("Microsoft.ui.xaml.dll")]
        private static extern void XamlCheckProcessRequirements();

        [STAThread]
        static void Main(string[] args)
        {
            // 1. BOOTSTRAP FIRST. 
            // You cannot load XAML DLLs until the Windows App SDK runtime is loaded.
            // If your 'BootstrapHelper' just wraps Bootstrap.Initialize, put that logic here.
            // Ideally, use the raw Bootstrap call here to ensure it catches early failures.

            bool isPackaged = IsPackaged();
            if (!isPackaged)
            {
                // Initialize Windows App SDK for unpackaged apps
                // Version 1.6+ syntax (adjust matching your specific SDK version if needed)
                Bootstrap.Initialize(0x00010006);
            }

            try
            {
                // 2. Check XAML requirements
                XamlCheckProcessRequirements();

                // 3. Init COM
                WinRT.ComWrappersSupport.InitializeComWrappers();

                // 4. Start the UI Thread
                Application.Start((p) =>
                {
                    var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                    SynchronizationContext.SetSynchronizationContext(context);
                    new App();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FATAL STARTUP ERROR: {ex}");
            }
            finally
            {
                if (!isPackaged)
                {
                    Bootstrap.Shutdown();
                }
            }
        }

        // Simple check to see if we are running as MSIX or bare EXE
        private static bool IsPackaged()
        {
            try
            {
                var package = Windows.ApplicationModel.Package.Current;
                return package != null;
            }
            catch
            {
                return false;
            }
        }
    }
}