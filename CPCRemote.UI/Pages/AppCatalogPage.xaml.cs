namespace CPCRemote.UI.Pages;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using CPCRemote.Core.Models;
using CPCRemote.UI.ViewModels;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

/// <summary>
/// Page for managing the application catalog.
/// </summary>
[SupportedOSPlatform("windows10.0.22621.0")]
public sealed partial class AppCatalogPage : Page
{
    private readonly AppCatalogViewModel _viewModel;

    // Dialog controls (created programmatically to avoid WinUI 3 ComboBox popup issues)
    private ComboBox? _slotComboBox;
    private TextBox? _nameTextBox;
    private TextBox? _pathTextBox;
    private TextBox? _argumentsTextBox;
    private TextBox? _workingDirTextBox;
    private ComboBox? _categoryComboBox;
    private ToggleSwitch? _runAsAdminToggle;
    private ToggleSwitch? _enabledToggle;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppCatalogPage"/> class.
    /// </summary>
    public AppCatalogPage()
    {
        this.InitializeComponent();
        _viewModel = App.GetService<AppCatalogViewModel>();

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

    private async void LaunchButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button button && button.Tag is AppCatalogEntry app)
            {
                await _viewModel.LaunchAppCommand.ExecuteAsync(app);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }
    }

    private async void AppsListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        // Double-click launches the app (single click just selects)
        // ItemClick fires on single click when IsItemClickEnabled=true
        // We'll use this for a quick launch option
    }

    private async void AppsListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        try
        {
            // Get the new order from the ListView's items
            var newOrder = new System.Collections.Generic.List<AppCatalogEntry>();
            foreach (var item in AppsListView.Items)
            {
                if (item is AppCatalogEntry app)
                {
                    newOrder.Add(app);
                }
            }

            if (newOrder.Count > 0)
            {
                await _viewModel.ReorderAppsAsync(newOrder);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }
    }

    private async void ShowEditDialog()
    {
        // Create dialog content programmatically to avoid WinUI 3 ComboBox popup issues
        _slotComboBox = new ComboBox
        {
            Header = "Slot",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = AppCatalogViewModel.AvailableSlots,
            SelectedItem = _viewModel.EditSlot
        };

        _nameTextBox = new TextBox
        {
            Header = "Name",
            PlaceholderText = "App display name",
            Text = _viewModel.EditName ?? string.Empty
        };

        _pathTextBox = new TextBox
        {
            Header = "Executable Path",
            PlaceholderText = "C:\\path\\to\\app.exe",
            Text = _viewModel.EditPath ?? string.Empty
        };

        var browseButton = new Button { Content = "Browse", VerticalAlignment = VerticalAlignment.Bottom };
        browseButton.Click += BrowseButton_Click;

        var pathGrid = new Grid { ColumnSpacing = 8 };
        pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(_pathTextBox, 0);
        Grid.SetColumn(browseButton, 1);
        pathGrid.Children.Add(_pathTextBox);
        pathGrid.Children.Add(browseButton);

        _argumentsTextBox = new TextBox
        {
            Header = "Arguments (optional)",
            PlaceholderText = "--flag value",
            Text = _viewModel.EditArguments ?? string.Empty
        };

        _workingDirTextBox = new TextBox
        {
            Header = "Working Directory (optional)",
            PlaceholderText = "C:\\path\\to\\working\\dir",
            Text = _viewModel.EditWorkingDirectory ?? string.Empty
        };

        _categoryComboBox = new ComboBox
        {
            Header = "Category",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = AppCatalogViewModel.CommonCategories,
            SelectedItem = _viewModel.EditCategory ?? "Other"
        };

        _runAsAdminToggle = new ToggleSwitch
        {
            Header = "Run as Administrator",
            OffContent = "No",
            OnContent = "Yes",
            IsOn = _viewModel.EditRunAsAdmin
        };

        _enabledToggle = new ToggleSwitch
        {
            Header = "Enabled",
            OffContent = "Disabled",
            OnContent = "Enabled",
            IsOn = _viewModel.EditEnabled
        };

        var contentPanel = new StackPanel { Spacing = 16, MinWidth = 400 };
        contentPanel.Children.Add(_slotComboBox);
        contentPanel.Children.Add(_nameTextBox);
        contentPanel.Children.Add(pathGrid);
        contentPanel.Children.Add(_argumentsTextBox);
        contentPanel.Children.Add(_workingDirTextBox);
        contentPanel.Children.Add(_categoryComboBox);
        contentPanel.Children.Add(_runAsAdminToggle);
        contentPanel.Children.Add(_enabledToggle);

        var dialog = new ContentDialog
        {
            Title = _viewModel.SelectedApp is null ? "Add App" : "Edit App",
            Content = contentPanel,
            PrimaryButtonText = "Save",
            SecondaryButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        dialog.PrimaryButtonClick += EditDialog_PrimaryButtonClick;

        ContentDialogResult result = await dialog.ShowAsync();

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
            _viewModel.EditSlot = _slotComboBox?.SelectedItem as string;
            _viewModel.EditName = _nameTextBox?.Text;
            _viewModel.EditPath = _pathTextBox?.Text;
            _viewModel.EditArguments = _argumentsTextBox?.Text;
            _viewModel.EditWorkingDirectory = _workingDirTextBox?.Text;
            _viewModel.EditCategory = _categoryComboBox?.SelectedItem as string;
            _viewModel.EditRunAsAdmin = _runAsAdminToggle?.IsOn ?? false;
            _viewModel.EditEnabled = _enabledToggle?.IsOn ?? true;

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

    // Native Win32 file dialog P/Invoke
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OPENFILENAME
    {
        public int lStructSize;
        public nint hwndOwner;
        public nint hInstance;
        public string lpstrFilter;
        public string lpstrCustomFilter;
        public int nMaxCustFilter;
        public int nFilterIndex;
        public nint lpstrFile;
        public int nMaxFile;
        public string lpstrFileTitle;
        public int nMaxFileTitle;
        public string lpstrInitialDir;
        public string lpstrTitle;
        public int Flags;
        public short nFileOffset;
        public short nFileExtension;
        public string lpstrDefExt;
        public nint lCustData;
        public nint lpfnHook;
        public string lpTemplateName;
        public nint pvReserved;
        public int dwReserved;
        public int FlagsEx;
    }

    private const int OFN_FILEMUSTEXIST = 0x00001000;
    private const int OFN_PATHMUSTEXIST = 0x00000800;
    private const int OFN_NOCHANGEDIR = 0x00000008;

    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetOpenFileName(ref OPENFILENAME lpofn);

    [DebuggerStepThrough]
    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get window handle
            var hwnd = nint.Zero;
            if (App.CurrentMainWindow is not null)
            {
                hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentMainWindow);
            }

            // Allocate buffer for file path
            var fileBuffer = new char[260];
            var fileHandle = GCHandle.Alloc(fileBuffer, GCHandleType.Pinned);

            try
            {
                var ofn = new OPENFILENAME
                {
                    lStructSize = Marshal.SizeOf<OPENFILENAME>(),
                    hwndOwner = hwnd,
                    lpstrFilter = "Executables (*.exe)\0*.exe\0Batch Files (*.bat;*.cmd)\0*.bat;*.cmd\0PowerShell Scripts (*.ps1)\0*.ps1\0All Files (*.*)\0*.*\0\0",
                    nFilterIndex = 1,
                    lpstrFile = fileHandle.AddrOfPinnedObject(),
                    nMaxFile = 260,
                    lpstrTitle = "Select Executable",
                    Flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR
                };

                if (GetOpenFileName(ref ofn))
                {
                    var selectedPath = new string(fileBuffer).TrimEnd('\0');
                    if (!string.IsNullOrEmpty(selectedPath) && _pathTextBox is not null)
                    {
                        _pathTextBox.Text = selectedPath;
                    }
                }
            }
            finally
            {
                fileHandle.Free();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"BrowseButton_Click Exception: {ex}");
        }
    }
}
