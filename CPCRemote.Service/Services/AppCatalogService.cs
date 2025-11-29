namespace CPCRemote.Service.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using CPCRemote.Core.Helpers;
using CPCRemote.Core.Models;

using Microsoft.Extensions.Logging;

/// <summary>
/// Manages an application catalog with support for launching apps by slot ID.
/// The catalog is stored in a JSON file and supports hot-reload.
/// </summary>
public sealed class AppCatalogService
{
    private readonly ILogger<AppCatalogService> _logger;
    private readonly UserSessionLauncher _userSessionLauncher;
    private readonly string _catalogPath;
    private readonly Lock _lock = new();
    private Dictionary<string, AppCatalogEntry> _catalog = new(StringComparer.OrdinalIgnoreCase);
    private DateTime _lastModified = DateTime.MinValue;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public AppCatalogService(ILogger<AppCatalogService> logger, UserSessionLauncher userSessionLauncher)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(userSessionLauncher);

        _logger = logger;
        _userSessionLauncher = userSessionLauncher;
        
        // Store catalog in the writable service data directory (%PROGRAMDATA%\CPCRemote)
        // This ensures the catalog is accessible even in MSIX deployments
        _catalogPath = ConfigurationPaths.EnsureServiceConfigExists("app-catalog.json");
        
        _logger.LogInformation("App catalog path: {Path}", _catalogPath);
        LoadCatalog();
    }

    /// <summary>
    /// Gets all configured applications.
    /// </summary>
    public IReadOnlyList<AppCatalogEntry> GetAllApps()
    {
        RefreshIfModified();
        lock (_lock)
        {
            return [.. _catalog.Values];
        }
    }

    /// <summary>
    /// Gets an application by its slot ID (e.g., "App1").
    /// </summary>
    public AppCatalogEntry? GetAppBySlot(string slot)
    {
        RefreshIfModified();
        lock (_lock)
        {
            return _catalog.GetValueOrDefault(slot);
        }
    }

    /// <summary>
    /// Launches an application by slot ID.
    /// </summary>
    /// <returns>True if launch was successful, false otherwise.</returns>
    public bool LaunchApp(string slot)
    {
        var app = GetAppBySlot(slot);
        
        if (app is null)
        {
            _logger.LogWarning("Slot {Slot} not found in catalog", slot);
            return false;
        }

        if (!app.Enabled)
        {
            _logger.LogWarning("Slot {Slot} ({Name}) is disabled", slot, app.Name);
            return false;
        }

        if (string.IsNullOrWhiteSpace(app.Path))
        {
            _logger.LogWarning("Slot {Slot} ({Name}) has no path configured", slot, app.Name);
            return false;
        }

        if (!File.Exists(app.Path) && !Directory.Exists(app.Path))
        {
            _logger.LogWarning("Path not found for {Slot} ({Name}): {Path}", slot, app.Name, app.Path);
            return false;
        }

        try
        {
            // Use UserSessionLauncher to start the process in the interactive user's session
            // This is required because the service runs in Session 0 and cannot show UI directly
            bool success = _userSessionLauncher.LaunchInUserSession(
                app.Path,
                app.Arguments,
                app.WorkingDirectory,
                app.RunAsAdmin);

            if (success)
            {
                _logger.LogInformation("Launched {Slot}: {Name} ({Path}) [Admin: {Admin}]", slot, app.Name, app.Path, app.RunAsAdmin);
            }
            else
            {
                _logger.LogWarning("Failed to launch {Slot}: {Name} in user session", slot, app.Name);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch {Slot}: {Name}", slot, app.Name);
            return false;
        }
    }

    /// <summary>
    /// Saves a new or updated application entry.
    /// </summary>
    public async Task SaveAppAsync(AppCatalogEntry entry, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _catalog[entry.Slot] = entry;
        }

        await SaveCatalogAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Saved app entry: {Slot} = {Name}", entry.Slot, entry.Name);
    }

    /// <summary>
    /// Removes an application entry by slot.
    /// </summary>
    public async Task<bool> RemoveAppAsync(string slot, CancellationToken cancellationToken = default)
    {
        bool removed;
        lock (_lock)
        {
            removed = _catalog.Remove(slot);
        }

        if (removed)
        {
            await SaveCatalogAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Removed app entry: {Slot}", slot);
        }

        return removed;
    }

    private void LoadCatalog()
    {
        try
        {
            if (!File.Exists(_catalogPath))
            {
                _logger.LogInformation("App catalog not found at {Path}, creating default catalog", _catalogPath);
                CreateDefaultCatalog();
                return;
            }

            string json = File.ReadAllText(_catalogPath);
            var entries = JsonSerializer.Deserialize<List<AppCatalogEntry>>(json, JsonOptions);

            lock (_lock)
            {
                _catalog.Clear();
                if (entries is not null)
                {
                    foreach (var entry in entries)
                    {
                        _catalog[entry.Slot] = entry;
                    }
                }
                _lastModified = File.GetLastWriteTimeUtc(_catalogPath);
            }

            _logger.LogInformation("Loaded {Count} apps from catalog", _catalog.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load app catalog from {Path}", _catalogPath);
            CreateDefaultCatalog();
        }
    }

    private void CreateDefaultCatalog()
    {
        var defaultApps = new List<AppCatalogEntry>
        {
            new() { Slot = "App1", Name = "Chrome", Path = @"C:\Program Files\Google\Chrome\Application\chrome.exe", Category = "Browser" },
            new() { Slot = "App2", Name = "Steam", Path = @"C:\Program Files (x86)\Steam\steam.exe", Category = "Games" },
            new() { Slot = "App3", Name = "Notepad", Path = @"C:\Windows\System32\notepad.exe", Category = "Productivity" },
            new() { Slot = "App4", Name = "Calculator", Path = @"C:\Windows\System32\calc.exe", Category = "Utilities" },
            new() { Slot = "App5", Name = "File Explorer", Path = @"C:\Windows\explorer.exe", Category = "Utilities" },
        };

        lock (_lock)
        {
            _catalog.Clear();
            foreach (var app in defaultApps)
            {
                _catalog[app.Slot] = app;
            }
        }

        // Save synchronously on first creation
        SaveCatalogAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    private async Task SaveCatalogAsync(CancellationToken cancellationToken)
    {
        try
        {
            List<AppCatalogEntry> entries;
            lock (_lock)
            {
                entries = [.. _catalog.Values.OrderBy(e => e.Slot)];
            }

            string json = JsonSerializer.Serialize(entries, JsonOptions);
            await File.WriteAllTextAsync(_catalogPath, json, cancellationToken).ConfigureAwait(false);

            lock (_lock)
            {
                _lastModified = File.GetLastWriteTimeUtc(_catalogPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save app catalog to {Path}", _catalogPath);
        }
    }

    private void RefreshIfModified()
    {
        try
        {
            if (!File.Exists(_catalogPath))
            {
                return;
            }

            DateTime currentModified = File.GetLastWriteTimeUtc(_catalogPath);
            if (currentModified > _lastModified)
            {
                _logger.LogInformation("App catalog file changed, reloading...");
                LoadCatalog();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check catalog modification time");
        }
    }
}
