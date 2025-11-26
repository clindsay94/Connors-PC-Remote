namespace CPCRemote.UI.Pages;

using System;
using System.Runtime.Versioning;

using CPCRemote.Core.Models;
using CPCRemote.UI.ViewModels;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Windows.Storage.Pickers;

/// <summary>
/// Page for managing the application catalog.
/// </summary>
[SupportedOSPlatform("windows10.0.22621.0")]
public sealed partial class AppCatalogPage : Page
{
    private readonly AppCatalogViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppCatalogPage"/> class.
    /// </summary>
    public AppCatalogPage()
    {
        this.InitializeComponent();
        _viewModel = App.GetService<AppCatalogViewModel>();

        // Populate combo boxes
        SlotComboBox.ItemsSource = AppCatalogViewModel.AvailableSlots;
        CategoryComboBox.ItemsSource = AppCatalogViewModel.CommonCategories;

        // Wire up property changes
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Bind list
        AppsListView.ItemsSource = _viewModel.Apps;

        // Load data when page loads
        Loaded += AppCatalogPage_Loaded;
    }

    private async void AppCatalogPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.RefreshAppsCommand.ExecuteAsync(null);
            UpdateEmptyState();
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(AppCatalogViewModel.StatusMessage):
                    StatusText.Text = _viewModel.StatusMessage ?? string.Empty;
                    break;
                case nameof(AppCatalogViewModel.IsLoading):
                    LoadingRing.IsActive = _viewModel.IsLoading;
                    break;
                case nameof(AppCatalogViewModel.IsEditDialogOpen):
                    if (_viewModel.IsEditDialogOpen)
                    {
                        ShowEditDialog();
                    }

                    break;
            }
        });
    }

    private void UpdateEmptyState()
    {
        bool isEmpty = _viewModel.Apps.Count == 0;
        EmptyState.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
        AppsListView.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.RefreshAppsCommand.ExecuteAsync(null);
            UpdateEmptyState();
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.AddNewAppCommand.Execute(null);
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is AppCatalogEntry app)
        {
            _viewModel.EditAppCommand.Execute(app);
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button button && button.Tag is AppCatalogEntry app)
            {
                ContentDialog confirmDialog = new()
                {
                    Title = "Delete App",
                    Content = $"Are you sure you want to delete '{app.Name}'?",
                    PrimaryButtonText = "Delete",
                    SecondaryButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Secondary,
                    XamlRoot = this.XamlRoot
                };

                ContentDialogResult result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    _viewModel.SelectedApp = app;
                    await _viewModel.DeleteAppCommand.ExecuteAsync(null);
                    UpdateEmptyState();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }
    }

    private void AppsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _viewModel.SelectedApp = AppsListView.SelectedItem as AppCatalogEntry;
    }

    private async void ShowEditDialog()
    {
        // Populate dialog fields from ViewModel
        SlotComboBox.SelectedItem = _viewModel.EditSlot;
        NameTextBox.Text = _viewModel.EditName ?? string.Empty;
        PathTextBox.Text = _viewModel.EditPath ?? string.Empty;
        ArgumentsTextBox.Text = _viewModel.EditArguments ?? string.Empty;
        WorkingDirTextBox.Text = _viewModel.EditWorkingDirectory ?? string.Empty;
        CategoryComboBox.SelectedItem = _viewModel.EditCategory ?? "Other";
        RunAsAdminToggle.IsOn = _viewModel.EditRunAsAdmin;
        EnabledToggle.IsOn = _viewModel.EditEnabled;

        EditDialog.Title = _viewModel.SelectedApp is null ? "Add App" : "Edit App";
        EditDialog.XamlRoot = this.XamlRoot;

        ContentDialogResult result = await EditDialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
        {
            _viewModel.CancelEditCommand.Execute(null);
        }
    }

    private async void EditDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            // Update ViewModel from dialog fields
            _viewModel.EditSlot = SlotComboBox.SelectedItem as string;
            _viewModel.EditName = NameTextBox.Text;
            _viewModel.EditPath = PathTextBox.Text;
            _viewModel.EditArguments = ArgumentsTextBox.Text;
            _viewModel.EditWorkingDirectory = WorkingDirTextBox.Text;
            _viewModel.EditCategory = CategoryComboBox.SelectedItem as string;
            _viewModel.EditRunAsAdmin = RunAsAdminToggle.IsOn;
            _viewModel.EditEnabled = EnabledToggle.IsOn;

            // Validate
            if (string.IsNullOrWhiteSpace(_viewModel.EditSlot) ||
                string.IsNullOrWhiteSpace(_viewModel.EditName) ||
                string.IsNullOrWhiteSpace(_viewModel.EditPath))
            {
                args.Cancel = true;

                ContentDialog errorDialog = new()
                {
                    Title = "Validation Error",
                    Content = "Slot, Name, and Path are required.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };

                await errorDialog.ShowAsync();

                return;
            }

            await _viewModel.SaveAppCommand.ExecuteAsync(null);
            UpdateEmptyState();
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }
    }

    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
        picker.FileTypeFilter.Add(".exe");
        picker.FileTypeFilter.Add(".bat");
        picker.FileTypeFilter.Add(".cmd");
        picker.FileTypeFilter.Add(".ps1");
        picker.FileTypeFilter.Add("*");

        // Get the window handle for WinUI 3
        var window = App.CurrentMainWindow;
        if (window is null)
        {
            return;
        }

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            PathTextBox.Text = file.Path;
        }
    }
}
