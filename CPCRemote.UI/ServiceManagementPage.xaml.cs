using CPCRemote.UI.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System;
using System.IO;
using Windows.UI;

namespace CPCRemote.UI
{
    public sealed partial class ServiceManagementPage : Page
    {
        public ServiceManagementViewModel ViewModel { get; }
        public string ServiceExePath { get; }

        public ServiceManagementPage()
        {
            this.InitializeComponent();
            ViewModel = App.GetService<ServiceManagementViewModel>();
            
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            ServiceExePath = Path.Combine(baseDir, "ServiceBinaries", "CPCRemote.Service.exe");
        }
    }
}
