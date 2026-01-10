using System;
using CPCRemote.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CPCRemote.UI.Pages;

public sealed partial class ServiceManagementPage : Page
{
    public ServiceManagementViewModel ViewModel { get; }

    public ServiceManagementPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<ServiceManagementViewModel>();
        this.Loaded += ServiceManagementPage_Loaded;
    }

    private async void ServiceManagementPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
    }

    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        // Simple file picker logic could go here, for now keeping it simple as per original
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        
        // Retrieve the window handle (HWND) of the current WinUI 3 window.
        var window = App.CurrentMainWindow;
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);

        picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
        picker.FileTypeFilter.Add(".exe");

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            ViewModel.ServiceExecutablePath = file.Path;
        }
    }
}
