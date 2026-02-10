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

    private void CpuColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        => ViewModel.CpuColor = args.NewColor;

    private void GpuColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        => ViewModel.GpuColor = args.NewColor;

    private void MemoryColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        => ViewModel.MemoryColor = args.NewColor;

    private void MotherboardColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        => ViewModel.MotherboardColor = args.NewColor;

    private void StorageColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        => ViewModel.StorageColor = args.NewColor;

    private void CoolingColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        => ViewModel.CoolingColor = args.NewColor;

    private void NetworkColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        => ViewModel.NetworkColor = args.NewColor;

    private void OtherColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        => ViewModel.OtherColor = args.NewColor;
}
