using System.Diagnostics;
using System.Runtime.Versioning;
using CPCRemote.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace CPCRemote.Tests;

[TestFixture]
[NonParallelizable]
[SupportedOSPlatform("windows")]
public class ProcessServiceLeakTests
{
    [Test]
    public void GetTopProcesses_DoesNotLeakHandles()
    {
        var service = new ProcessService(NullLogger<ProcessService>.Instance);

        // Warm up
        service.GetTopProcesses();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        long initialHandles;
        using (var current = Process.GetCurrentProcess())
        {
            current.Refresh();
            initialHandles = current.HandleCount;
        }

        TestContext.Out.WriteLine($"Initial handle count: {initialHandles}");

        for (int i = 0; i < 50; i++)
        {
            service.GetTopProcesses();
        }

        // Measure immediately after the loop (before GC) to catch leaks not cleaned by finalizers
        long handleCountAfterLoop;
        using (var current = Process.GetCurrentProcess())
        {
            current.Refresh();
            handleCountAfterLoop = current.HandleCount;
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();

        long finalHandles;
        using (var current = Process.GetCurrentProcess())
        {
            current.Refresh();
            finalHandles = current.HandleCount;
        }

        TestContext.Out.WriteLine($"Handle count after loop (pre-GC): {handleCountAfterLoop}");
        TestContext.Out.WriteLine($"Final handle count (post-GC): {finalHandles}");

        // Allow for some fluctuation, but 50 calls with ~100-200 processes each
        // would leak thousands of handles if not disposed.
        Assert.That(handleCountAfterLoop, Is.LessThan(initialHandles + 100), "Significant handle leak detected before GC");
        Assert.That(finalHandles, Is.LessThan(initialHandles + 100), "Significant handle leak detected after GC");
    }
}
