namespace CPCRemote.UI.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CPCRemote.Core.IPC;
using CPCRemote.Core.Models;
using CPCRemote.UI.Services;
using CPCRemote.UI.Strings;

using Microsoft.Extensions.Logging;

/// <summary>
/// ViewModel for the App Catalog management page.
/// </summary>
public sealed partial class AppCatalogViewModel : ObservableObject
{
    private readonly IPipeClient _pipeClient;
    private readonly ILogger<AppCatalogViewModel> _logger;

    /// <summary>
    /// Gets the collection of app catalog entries.
    /// </summary>
    public ObservableCollection<AppCatalogEntry> Apps { get; } = [];

    /// <summary>
    /// Gets or sets the currently selected app for editing.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteAppCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveAppCommand))]
    public partial AppCatalogEntry? SelectedApp { get; set; }

    /// <summary>
    /// Gets or sets whether data is currently being loaded.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshAppsCommand))]
    public partial bool IsLoading { get; set; }

    /// <summary>
    /// Gets or sets whether the service is connected.
    /// </summary>
    [ObservableProperty]
    public partial bool IsServiceConnected { get; set; }

    /// <summary>
    /// Gets or sets the status message to display.
    /// </summary>
    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    /// <summary>
    /// Gets or sets whether the edit dialog is open.
    /// </summary>
    [ObservableProperty]
    public partial bool IsEditDialogOpen { get; set; }

    // Edit form fields
    [ObservableProperty]
    public partial string? EditSlot { get; set; }

    [ObservableProperty]
    public partial string? EditName { get; set; }

    [ObservableProperty]
    public partial string? EditPath { get; set; }

    [ObservableProperty]
    public partial string? EditArguments { get; set; }

    [ObservableProperty]
    public partial string? EditWorkingDirectory { get; set; }

    [ObservableProperty]
    public partial string? EditCategory { get; set; }

    [ObservableProperty]
    public partial bool EditRunAsAdmin { get; set; }

    [ObservableProperty]
    public partial bool EditEnabled { get; set; } = true;

    /// <summary>
    /// Available slot options for the dropdown.
    /// </summary>
    public static string[] AvailableSlots { get; } =
    [
        "App1", "App2", "App3", "App4", "App5",
        "App6", "App7", "App8", "App9", "App10"
    ];

    /// <summary>
    /// Common category options for the dropdown.
    /// </summary>
    public static string[] CommonCategories { get; } =
    [
        "Browser", "Games", "Media", "Productivity", "Utilities", "Development", "Communication", "Other"
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="AppCatalogViewModel"/> class.
    /// </summary>
    /// <param name="pipeClient">The IPC pipe client.</param>
    /// <param name="logger">The logger instance.</param>
    public AppCatalogViewModel(IPipeClient pipeClient, ILogger<AppCatalogViewModel> logger)
    {
        _pipeClient = pipeClient;
        _logger = logger;
    }

    /// <summary>
    /// Refreshes the app catalog from the service.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRefresh))]
    public async Task RefreshAppsAsync()
    {
        IsLoading = true;
        StatusMessage = Resources.AppCatalog_LoadingApps;

        try
        {
            if (!_pipeClient.IsConnected)
            {
                bool connected = await _pipeClient.ConnectAsync(IpcConstants.DefaultConnectTimeout);
                if (!connected)
                {
                    IsServiceConnected = false;
                    StatusMessage = Resources.Dashboard_CannotConnect;

                    return;
                }
            }

            IsServiceConnected = true;

            var response = await _pipeClient.SendRequestAsync<GetAppsResponse>(
                new GetAppsRequest(),
                IpcConstants.DefaultTimeout);

            if (response.Success)
            {
                Apps.Clear();

                foreach (var app in response.Apps)
                {
                    Apps.Add(app);
                }

                StatusMessage = string.Format(Resources.AppCatalog_LoadedApps, Apps.Count);
                _logger.LogInformation("Loaded {Count} apps from service.", Apps.Count);
            }
            else
            {
                StatusMessage = string.Format(Resources.AppCatalog_LoadFailed, response.ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            // Silently ignore cancellation (includes TaskCanceledException)
        }
        catch (IpcException ex)
        {
            StatusMessage = $"{Resources.Error}: {ex.Message}";
            _logger.LogError(ex, "Failed to load app catalog.");
        }
        catch (Exception ex)
        {
            StatusMessage = $"{Resources.Error}: {ex.Message}";
            _logger.LogError(ex, "Unexpected error loading app catalog.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanRefresh() => !IsLoading;

    /// <summary>
    /// Opens the add dialog for a new app.
    /// </summary>
    [RelayCommand]
    public void AddNewApp()
    {
        // Find first available slot
        string? availableSlot = null;
        foreach (var slot in AvailableSlots)
        {
            bool slotUsed = false;
            foreach (var app in Apps)
            {
                if (string.Equals(app.Slot, slot, StringComparison.OrdinalIgnoreCase))
                {
                    slotUsed = true;
                    break;
                }
            }

            if (!slotUsed)
            {
                availableSlot = slot;
                break;
            }
        }

        EditSlot = availableSlot ?? "App1";
        EditName = string.Empty;
        EditPath = string.Empty;
        EditArguments = string.Empty;
        EditWorkingDirectory = string.Empty;
        EditCategory = "Other";
        EditRunAsAdmin = false;
        EditEnabled = true;
        SelectedApp = null;
        IsEditDialogOpen = true;
    }

    /// <summary>
    /// Opens the edit dialog for the selected app.
    /// </summary>
    [RelayCommand]
    public void EditApp(AppCatalogEntry? app)
    {
        if (app is null)
        {
            return;
        }

        EditSlot = app.Slot;
        EditName = app.Name;
        EditPath = app.Path;
        EditArguments = app.Arguments;
        EditWorkingDirectory = app.WorkingDirectory;
        EditCategory = app.Category;
        EditRunAsAdmin = app.RunAsAdmin;
        EditEnabled = app.Enabled;
        SelectedApp = app;
        IsEditDialogOpen = true;
    }

    /// <summary>
    /// Saves the currently edited app.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSaveApp))]
    public async Task SaveAppAsync()
    {
        if (string.IsNullOrWhiteSpace(EditSlot) || string.IsNullOrWhiteSpace(EditName) || string.IsNullOrWhiteSpace(EditPath))
        {
            StatusMessage = Resources.AppCatalog_RequiredFields;

            return;
        }

        IsLoading = true;
        StatusMessage = Resources.AppCatalog_SavingApp;

        try
        {
            var entry = new AppCatalogEntry
            {
                Slot = EditSlot,
                Name = EditName,
                Path = EditPath,
                Arguments = EditArguments,
                WorkingDirectory = EditWorkingDirectory,
                Category = EditCategory,
                RunAsAdmin = EditRunAsAdmin,
                Enabled = EditEnabled
            };

            var response = await _pipeClient.SendRequestAsync<SaveAppResponse>(
                new SaveAppRequest { App = entry },
                IpcConstants.DefaultTimeout);

            if (response.Success)
            {
                IsEditDialogOpen = false;
                StatusMessage = string.Format(Resources.AppCatalog_SavedApp, EditName);
                await RefreshAppsAsync();
            }
            else
            {
                StatusMessage = string.Format(Resources.AppCatalog_SaveFailed, response.ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            // Silently ignore cancellation (includes TaskCanceledException)
        }
        catch (IpcException ex)
        {
            StatusMessage = $"{Resources.Error}: {ex.Message}";
            _logger.LogError(ex, "Failed to save app.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanSaveApp() => SelectedApp is not null || (!string.IsNullOrWhiteSpace(EditSlot) && !string.IsNullOrWhiteSpace(EditName));

    /// <summary>
    /// Deletes the selected app.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteApp))]
    public async Task DeleteAppAsync()
    {
        if (SelectedApp is null)
        {
            return;
        }

        IsLoading = true;
        StatusMessage = string.Format(Resources.AppCatalog_DeletingApp, SelectedApp.Name);

        try
        {
            var response = await _pipeClient.SendRequestAsync<DeleteAppResponse>(
                new DeleteAppRequest { Slot = SelectedApp.Slot },
                IpcConstants.DefaultTimeout);

            if (response.Success)
            {
                StatusMessage = string.Format(Resources.AppCatalog_DeletedApp, SelectedApp.Name);
                SelectedApp = null;
                await RefreshAppsAsync();
            }
            else
            {
                StatusMessage = string.Format(Resources.AppCatalog_DeleteFailed, response.ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            // Silently ignore cancellation (includes TaskCanceledException)
        }
        catch (IpcException ex)
        {
            StatusMessage = $"{Resources.Error}: {ex.Message}";
            _logger.LogError(ex, "Failed to delete app.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanDeleteApp() => SelectedApp is not null;

    /// <summary>
    /// Closes the edit dialog without saving.
    /// </summary>
    [RelayCommand]
    public void CancelEdit()
    {
        IsEditDialogOpen = false;
        SelectedApp = null;
    }

    /// <summary>
    /// Launches an application directly from the UI process.
    /// This approach avoids Session 0 isolation issues since the UI runs in the user's interactive session.
    /// </summary>
    [RelayCommand]
    public Task LaunchAppAsync(AppCatalogEntry? app)
    {
        if (app is null)
        {
            return Task.CompletedTask;
        }

        if (!app.Enabled)
        {
            StatusMessage = string.Format(Resources.AppCatalog_AppDisabled, app.Name);
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(app.Path))
        {
            StatusMessage = string.Format(Resources.AppCatalog_NoPathConfigured, app.Name);
            return Task.CompletedTask;
        }

        if (!File.Exists(app.Path) && !Directory.Exists(app.Path))
        {
            StatusMessage = string.Format(Resources.AppCatalog_PathNotFound, app.Path);
            return Task.CompletedTask;
        }

        StatusMessage = string.Format(Resources.AppCatalog_LaunchingApp, app.Name);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = app.Path,
                UseShellExecute = true,
                Arguments = app.Arguments ?? string.Empty
            };

            if (!string.IsNullOrWhiteSpace(app.WorkingDirectory))
            {
                startInfo.WorkingDirectory = app.WorkingDirectory;
            }

            if (app.RunAsAdmin)
            {
                startInfo.Verb = "runas";
            }

            Process.Start(startInfo);
            StatusMessage = string.Format(Resources.AppCatalog_LaunchedApp, app.Name);
            _logger.LogInformation("Launched app {Slot}: {Name} ({Path})", app.Slot, app.Name, app.Path);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // User cancelled the UAC prompt
            StatusMessage = Resources.AppCatalog_LaunchCancelled;
            _logger.LogInformation("User cancelled UAC prompt for {Slot}: {Name}", app.Slot, app.Name);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Resources.AppCatalog_LaunchFailed, ex.Message);
            _logger.LogError(ex, "Failed to launch app {Slot}: {Name}", app.Slot, app.Name);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Reorders apps after drag-and-drop by reassigning slot numbers.
    /// </summary>
    public async Task ReorderAppsAsync(IList<AppCatalogEntry> newOrder)
    {
        IsLoading = true;
        StatusMessage = Resources.AppCatalog_ReorderingApps;

        try
        {
            // Reassign slots based on new order
            for (int i = 0; i < newOrder.Count && i < AvailableSlots.Length; i++)
            {
                var app = newOrder[i];
                var newSlot = AvailableSlots[i];
                
                if (app.Slot != newSlot)
                {
                    app.Slot = newSlot;
                    
                    var response = await _pipeClient.SendRequestAsync<SaveAppResponse>(
                        new SaveAppRequest { App = app },
                        IpcConstants.DefaultTimeout);

                    if (!response.Success)
                    {
                        _logger.LogWarning("Failed to save reordered app {Name}: {Error}", app.Name, response.ErrorMessage);
                    }
                }
            }

            StatusMessage = Resources.AppCatalog_ReorderedApps;
            await RefreshAppsAsync();
        }
        catch (OperationCanceledException)
        {
            // Silently ignore cancellation
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Resources.AppCatalog_ReorderFailed, ex.Message);
            _logger.LogError(ex, "Failed to reorder apps.");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
