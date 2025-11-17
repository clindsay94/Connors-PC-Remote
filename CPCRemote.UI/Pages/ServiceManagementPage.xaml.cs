using CPCRemote.UI.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace CPCRemote.UI.Pages
{
    public sealed partial class ServiceManagementPage : Page
    {
        public ServiceManagementViewModel ViewModel { get; }

        public ServiceManagementPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<ServiceManagementViewModel>();
        }
    }
}
