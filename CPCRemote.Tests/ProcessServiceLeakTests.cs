using System.Diagnostics;
using System.Runtime.Versioning;
using CPCRemote.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace CPCRemote.Tests;

[TestFixture]
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

        long initialHandles = Process.GetCurrentProcess().HandleCount;
        TestContext.Out.WriteLine($"Initial handle count: {initialHandles}");

        for (int i = 0; i < 50; i++)
        {
            service.GetTopProcesses();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();

        long finalHandles = Process.GetCurrentProcess().HandleCount;
        TestContext.Out.WriteLine($"Final handle count: {finalHandles}");

        // Allow for some fluctuation, but 50 calls with ~100-200 processes each
        // would leak thousands of handles if not disposed.
        // Process objects in .NET can be tricky with GC, but a large leak should be obvious.
        Assert.That(finalHandles, Is.LessThan(initialHandles + 100), "Significant handle leak detected");
    }
}
