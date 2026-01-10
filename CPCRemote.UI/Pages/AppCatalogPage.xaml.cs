using System;
using CPCRemote.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace CPCRemote.UI.Pages;

public sealed partial class AppCatalogPage : Page
{
    public AppCatalogViewModel ViewModel { get; }

    public AppCatalogPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<AppCatalogViewModel>();
        this.Name = "RootPage"; // For ElementName binding
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e) => ViewModel.RefreshAppsCommand.Execute(null);
    private void AddButton_Click(object sender, RoutedEventArgs e) => ViewModel.AddNewAppCommand.Execute(null);

    private async void BrowsePathButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        
        // Retrieve the window handle (HWND) of the current WinUI 3 window.
        var window = App.CurrentMainWindow;
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);

        picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
        picker.FileTypeFilter.Add(".exe");
        picker.FileTypeFilter.Add("*"); // Allow all files as apps can be scripts, etc.

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            // Sanitize path (remove any invisible chars/whitespace)
            ViewModel.EditPath = file.Path.Trim().TrimEnd('\0', '\r', '\n');
        }
    }
}


