using CPCRemote.UI.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace CPCRemote.UI
{
    public sealed partial class QuickActionsPage : Page
    {
        public QuickActionsViewModel ViewModel { get; }

        public QuickActionsPage()
        {
            this.InitializeComponent();
            ViewModel = App.GetService<QuickActionsViewModel>();
        }
    }
}
