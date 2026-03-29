namespace CPCRemote.Service.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using CPCRemote.Core.IPC;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for managing running processes: listing top processes and terminating by PID.
/// Includes security protections against killing critical system processes.
/// </summary>
[SupportedOSPlatform("windows10.0.22621.0")]
public sealed class ProcessService
{
    private readonly ILogger<ProcessService> _logger;

    /// <summary>
    /// Protected system processes that cannot be killed via this service.
    /// </summary>
    private static readonly HashSet<string> ProtectedProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "System", "Idle", "Registry", "smss", "csrss", "wininit",
        "winlogon", "services", "lsass", "lsaiso", "svchost",
        "fontdrvhost", "dwm", "sihost", "explorer", "RuntimeBroker",
        "SecurityHealthService", "MsMpEng", "NisSrv"
    };

    public ProcessService(ILogger<ProcessService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the top processes sorted by memory usage (descending).
    /// </summary>
    /// <param name="count">Maximum number of processes to return.</param>
    public ProcessInfoDto[] GetTopProcesses(int count = 20)
    {
        try
        {
            count = Math.Clamp(count, 1, 50);

            var allProcesses = Process.GetProcesses();
            var processInfos = new List<ProcessInfoDto>(allProcesses.Length);

            foreach (var p in allProcesses)
            {
                using (p)
                {
                    try
                    {
                        string name = p.ProcessName;
                        if (string.IsNullOrEmpty(name))
                        {
                            continue;
                        }

                        processInfos.Add(new ProcessInfoDto
                        {
                            Pid = p.Id,
                            Name = name,
                            MemoryMb = Math.Round(p.WorkingSet64 / (1024.0 * 1024.0), 1),
                            IsProtected = ProtectedProcesses.Contains(name)
                        });
                    }
                    catch
                    {
                        // Some processes might exit while we are iterating or we might lack permissions
                    }
                }
            }

            return processInfos
                .OrderByDescending(p => p.MemoryMb)
                .Take(count)
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top processes");
            return [];
        }
    }

    /// <summary>
    /// Attempts to kill a process by PID.
    /// Returns (success, processName, errorMessage).
    /// </summary>
    public (bool Success, string? ProcessName, string? Error) KillProcess(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            string name = process.ProcessName;

            // Security check
            if (ProtectedProcesses.Contains(name))
            {
                _logger.LogWarning("Denied kill request for protected process: {Name} (PID {Pid})", name, pid);
                return (false, name, $"Cannot kill protected system process: {name}");
            }

            process.Kill(entireProcessTree: true);
            process.WaitForExit(3000);

            _logger.LogInformation("Killed process: {Name} (PID {Pid})", name, pid);
            return (true, name, null);
        }
        catch (ArgumentException)
        {
            return (false, null, $"Process with PID {pid} not found");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot kill process PID {Pid}", pid);
            return (false, null, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to kill process PID {Pid}", pid);
            return (false, null, ex.Message);
        }
    }
}
