using CPCRemote.UI.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace CPCRemote.UI.Pages;

public sealed partial class QuickActionsPage : Page
{
    public QuickActionsViewModel ViewModel { get; }

    public QuickActionsPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<QuickActionsViewModel>();
    }
}
