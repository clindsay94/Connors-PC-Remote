using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace CPCRemote.UI;

public static class Program
{
    [DllImport("Microsoft.ui.xaml.dll")]
    private static extern void XamlCheckProcessRequirements();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder? packageFullName);

    [STAThread]
    static void Main(string[] args)
    {
        // With WindowsAppSDKSelfContained=true, we don't need Bootstrap.Initialize
        // The runtime DLLs are bundled with the app
        
        try
        {
            // Initialize COM wrappers FIRST - required for WinRT interop
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
            Debug.WriteLine($"FATAL STARTUP ERROR: {ex}");
            throw;
        }
    }
}