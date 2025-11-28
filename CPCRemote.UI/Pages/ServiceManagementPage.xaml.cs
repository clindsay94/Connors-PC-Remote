using CPCRemote.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace CPCRemote.UI.Pages
{
    public sealed partial class ServiceManagementPage : Page
    {
        public ServiceManagementViewModel ViewModel { get; }

        public ServiceManagementPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<ServiceManagementViewModel>();
            
            // Item 8: Call async initialization after page is loaded
            Loaded += ServiceManagementPage_Loaded;
        }

        private async void ServiceManagementPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Only initialize once
            Loaded -= ServiceManagementPage_Loaded;
            await ViewModel.InitializeAsync();
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add(".exe");

            // Get the window handle for the picker
            var window = App.CurrentMainWindow;
            if (window is not null)
            {
                var hwnd = WindowNative.GetWindowHandle(window);
                InitializeWithWindow.Initialize(picker, hwnd);
            }

            var file = await picker.PickSingleFileAsync();
            if (file is not null)
            {
                ViewModel.ServiceExecutablePath = file.Path;
            }
        }
    }
}
