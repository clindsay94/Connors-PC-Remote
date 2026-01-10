using CPCRemote.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace CPCRemote.UI.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPageViewModel ViewModel { get; }

    public SettingsPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<SettingsPageViewModel>();
    }

    private void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        var color = args.NewColor;
        var hex = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        ViewModel.AccentColor = hex;
        ViewModel.UseSystemAccent = false;
    }
}
