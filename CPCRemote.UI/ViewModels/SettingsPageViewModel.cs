using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CPCRemote.UI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.InteropServices;
using Windows.UI;

namespace CPCRemote.UI.ViewModels;

public partial class SettingsPageViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private bool _isInitializing = true;

    [ObservableProperty]
    public partial string AppTheme { get; set; } = "System";

    [ObservableProperty]
    public partial bool UseSystemAccent { get; set; } = true;

    [ObservableProperty]
    public partial string AccentColor { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Backdrop { get; set; } = "Mica";

    [ObservableProperty]
    public partial string SelectedFontFamily { get; set; } = "Segoe UI Variable";

    [ObservableProperty]
    public partial int FontSizeScale { get; set; } = 100;

    [ObservableProperty]
    public partial bool ShowConfirmations { get; set; } = true;

    [ObservableProperty]
    public partial bool AutoConnect { get; set; } = true;

    [ObservableProperty]
    public partial bool SoundEffects { get; set; } = true;

    [ObservableProperty]
    public partial int RefreshInterval { get; set; } = 2;

    [ObservableProperty]
    public partial string TemperatureUnit { get; set; } = "Celsius";

    [ObservableProperty]
    public partial bool DashboardAnimations { get; set; } = true;

    [ObservableProperty]
    public partial bool AlwaysOnTop { get; set; } = false;

    [ObservableProperty]
    public partial bool StartMinimized { get; set; } = false;

    [ObservableProperty]
    public partial bool RememberPosition { get; set; } = true;

    [ObservableProperty]
    public partial string VersionString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RuntimeString { get; set; } = string.Empty;

    public ObservableCollection<string> InstalledFonts { get; } = new();

    public SettingsPageViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        
        LoadSettings();
        PopulateFonts();
        SetVersionInfo();
        
        _isInitializing = false;
    }

    private void LoadSettings()
    {
        AppTheme = _settingsService.Get("AppTheme", "System");
        UseSystemAccent = _settingsService.Get("UseSystemAccent", true);
        AccentColor = _settingsService.Get("AccentColor", string.Empty);
        Backdrop = _settingsService.Get("Backdrop", "Mica");
        SelectedFontFamily = _settingsService.Get("FontFamily", "Segoe UI Variable");
        FontSizeScale = _settingsService.Get("FontSizeScale", 100);
        ShowConfirmations = _settingsService.Get("ShowConfirmations", true);
        AutoConnect = _settingsService.Get("AutoConnect", true);
        SoundEffects = _settingsService.Get("SoundEffects", true);
        RefreshInterval = _settingsService.Get("RefreshInterval", 2);
        TemperatureUnit = _settingsService.Get("TemperatureUnit", "Celsius");
        DashboardAnimations = _settingsService.Get("DashboardAnimations", true);
        AlwaysOnTop = _settingsService.Get("AlwaysOnTop", false);
        StartMinimized = _settingsService.Get("StartMinimized", false);
        RememberPosition = _settingsService.Get("RememberPosition", true);
    }

    private void SetVersionInfo()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionString = $"{version?.Major}.{version?.Minor}.{version?.Build}";
        RuntimeString = $".NET {Environment.Version.Major}";
    }

    partial void OnAppThemeChanged(string value)
    {
        if (_isInitializing) return;
        _settingsService.Set("AppTheme", value);
        App.ApplyTheme(value);
    }

    partial void OnUseSystemAccentChanged(bool value)
    {
        if (_isInitializing) return;
        _settingsService.Set("UseSystemAccent", value);
        if (value)
        {
            AccentColor = string.Empty; // Reset custom color
            _settingsService.Set("AccentColor", string.Empty);
        }
    }

    partial void OnAccentColorChanged(string value)
    {
        if (_isInitializing) return;
        _settingsService.Set("AccentColor", value);
        App.ApplyAccentColor(value);
    }

    partial void OnBackdropChanged(string value)
    {
        if (_isInitializing) return;
        _settingsService.Set("Backdrop", value);
        App.ApplyBackdrop(value);
    }

    partial void OnSelectedFontFamilyChanged(string value)
    {
        if (_isInitializing) return;
        _settingsService.Set("FontFamily", value);
        App.ApplyFontFamily(value);
    }

    partial void OnFontSizeScaleChanged(int value)
    {
        if (_isInitializing) return;
        _settingsService.Set("FontSizeScale", value);
        App.ApplyFontScale(value);
    }

    partial void OnShowConfirmationsChanged(bool value)
    {
        if (_isInitializing) return;
        _settingsService.Set("ShowConfirmations", value);
    }

    partial void OnAutoConnectChanged(bool value)
    {
        if (_isInitializing) return;
        _settingsService.Set("AutoConnect", value);
    }

    partial void OnSoundEffectsChanged(bool value)
    {
        if (_isInitializing) return;
        _settingsService.Set("SoundEffects", value);
    }

    partial void OnRefreshIntervalChanged(int value)
    {
        if (_isInitializing) return;
        _settingsService.Set("RefreshInterval", value);
    }

    partial void OnTemperatureUnitChanged(string value)
    {
        if (_isInitializing) return;
        _settingsService.Set("TemperatureUnit", value);
    }

    partial void OnDashboardAnimationsChanged(bool value)
    {
        if (_isInitializing) return;
        _settingsService.Set("DashboardAnimations", value);
    }

    partial void OnAlwaysOnTopChanged(bool value)
    {
        if (_isInitializing) return;
        _settingsService.Set("AlwaysOnTop", value);
        
        // This requires UI thread access to window handle, might need a service/helper call
        // For now, relying on App.CurrentMainWindow check inside helper
        ApplyAlwaysOnTop(value);
    }

    partial void OnStartMinimizedChanged(bool value)
    {
        if (_isInitializing) return;
        _settingsService.Set("StartMinimized", value);
    }

    partial void OnRememberPositionChanged(bool value)
    {
        if (_isInitializing) return;
        _settingsService.Set("RememberPosition", value);
    }

    [RelayCommand]
    private void ResetSettings()
    {
        _isInitializing = true;

        AppTheme = "System";
        UseSystemAccent = true;
        AccentColor = string.Empty;
        Backdrop = "Mica";
        SelectedFontFamily = "Segoe UI Variable";
        FontSizeScale = 100;
        ShowConfirmations = true;
        AutoConnect = true;
        SoundEffects = true;
        RefreshInterval = 2;
        TemperatureUnit = "Celsius";
        DashboardAnimations = true;
        AlwaysOnTop = false;
        StartMinimized = false;
        RememberPosition = true;

        // Persist default values
        _settingsService.Set("AppTheme", "System");
        _settingsService.Set("UseSystemAccent", true);
        _settingsService.Set("AccentColor", string.Empty);
        _settingsService.Set("Backdrop", "Mica");
        _settingsService.Set("FontFamily", "Segoe UI Variable");
        _settingsService.Set("FontSizeScale", 100);
        _settingsService.Set("ShowConfirmations", true);
        _settingsService.Set("AutoConnect", true);
        _settingsService.Set("SoundEffects", true);
        _settingsService.Set("RefreshInterval", 2);
        _settingsService.Set("TemperatureUnit", "Celsius");
        _settingsService.Set("DashboardAnimations", true);
        _settingsService.Set("AlwaysOnTop", false);
        _settingsService.Set("StartMinimized", false);
        _settingsService.Set("RememberPosition", true);

        // Apply immediately
        App.ApplyTheme("System");
        App.ApplyFontFamily("Segoe UI Variable");
        App.ApplyFontScale(100);
        App.ApplyBackdrop("Mica");
        ApplyAlwaysOnTop(false);

        _isInitializing = false;
    }

    [RelayCommand]
    private async Task OpenGitHub()
    {
        await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/clindsay94/Connors-PC-Remote"));
    }
    
    // Logic for applying window top-most state
    private static void ApplyAlwaysOnTop(bool alwaysOnTop)
    {
        if (App.CurrentMainWindow is null) return;

        try 
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentMainWindow);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                presenter.IsAlwaysOnTop = alwaysOnTop;
            }
        }
        catch { /* Handle/Log error */ }
    }

    private void PopulateFonts()
    {
        try
        {
            var fonts = GetInstalledFontFamilies();
            var commonFonts = new[] { "Segoe UI Variable", "Segoe UI", "Cascadia Code", "Consolas" };
            
            foreach (var font in commonFonts.Where(f => fonts.Contains(f, StringComparer.OrdinalIgnoreCase)))
            {
                InstalledFonts.Add(font);
            }
            
            // Separator logic handled in UI with DataTemplateSelector or similar, 
            // or just mix them here. For simplicity, just adding rest sorted.
            
            foreach (var font in fonts.OrderBy(f => f))
            {
                if (!commonFonts.Contains(font, StringComparer.OrdinalIgnoreCase))
                {
                    InstalledFonts.Add(font);
                }
            }
        }
        catch
        {
            // Fallback
            InstalledFonts.Add("Segoe UI Variable");
            InstalledFonts.Add("Segoe UI");
            InstalledFonts.Add("Arial");
        }
    }

    // Font Enumeration P/Invoke
    private static List<string> GetInstalledFontFamilies()
    {
        var fonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        IntPtr hdc = GetDC(IntPtr.Zero);
        try
        {
            var logFont = new LOGFONT { lfCharSet = 1 }; 
            EnumFontFamiliesEx(hdc, ref logFont, (ref ENUMLOGFONTEX lpelfe, ref NEWTEXTMETRICEX lpntme, uint fontType, IntPtr lParam) =>
            {
                string fontName = lpelfe.elfLogFont.lfFaceName;
                if (!string.IsNullOrEmpty(fontName) && !fontName.StartsWith("@"))
                {
                    fonts.Add(fontName);
                }
                return 1; 
            }, IntPtr.Zero, 0);
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, hdc);
        }
        return [.. fonts];
    }
    
    private delegate int EnumFontFamExProc(ref ENUMLOGFONTEX lpelfe, ref NEWTEXTMETRICEX lpntme, uint fontType, IntPtr lParam);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] private static extern int EnumFontFamiliesEx(IntPtr hdc, ref LOGFONT lpLogfont, EnumFontFamExProc lpProc, IntPtr lParam, uint dwFlags);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct LOGFONT { public int lfHeight; public int lfWidth; public int lfEscapement; public int lfOrientation; public int lfWeight; public byte lfItalic; public byte lfUnderline; public byte lfStrikeOut; public byte lfCharSet; public byte lfOutPrecision; public byte lfClipPrecision; public byte lfQuality; public byte lfPitchAndFamily; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string lfFaceName; }
    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ENUMLOGFONTEX { public LOGFONT elfLogFont; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string elfFullName; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string elfStyle; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string elfScript; }
    
    [StructLayout(LayoutKind.Sequential)] private struct NEWTEXTMETRICEX { public NEWTEXTMETRIC ntmTm; public FONTSIGNATURE ntmFontSig; }
    [StructLayout(LayoutKind.Sequential)] private struct NEWTEXTMETRIC { public int tmHeight; public int tmAscent; public int tmDescent; public int tmInternalLeading; public int tmExternalLeading; public int tmAveCharWidth; public int tmMaxCharWidth; public int tmWeight; public int tmOverhang; public int tmDigitizedAspectX; public int tmDigitizedAspectY; public char tmFirstChar; public char tmLastChar; public char tmDefaultChar; public char tmBreakChar; public byte tmItalic; public byte tmUnderlined; public byte tmStruckOut; public byte tmPitchAndFamily; public byte tmCharSet; public uint ntmFlags; public uint ntmSizeEM; public uint ntmCellHeight; public uint ntmAvgWidth; }
    [StructLayout(LayoutKind.Sequential)] private struct FONTSIGNATURE { [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public uint[] fsUsb; [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public uint[] fsCsb; }
}
