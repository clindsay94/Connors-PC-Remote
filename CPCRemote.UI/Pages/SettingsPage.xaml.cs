namespace CPCRemote.UI.Pages;

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using CPCRemote.UI.Services;

using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Windows.UI;

using WinRT.Interop;

/// <summary>
/// Settings page for configuring application preferences including appearance, typography, behavior, and window options.
/// </summary>
[SupportedOSPlatform("windows10.0.22621.0")]
public sealed partial class SettingsPage : Page
{
    private readonly SettingsService _settingsService;
    private bool _isInitializing = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPage"/> class.
    /// </summary>
    public SettingsPage()
    {
        this.InitializeComponent();
        _settingsService = App.GetService<SettingsService>();

        LoadSettings();
        SetVersionInfo();

        _isInitializing = false;
    }

    private void LoadSettings()
    {
        // ─── Appearance ───────────────────────────────────────────────────────
        string savedTheme = _settingsService.Get("AppTheme", "System");
        SelectSegmentedItemByTag(ThemeSegmented, savedTheme);

        bool useSystemAccent = _settingsService.Get("UseSystemAccent", true);
        UseSystemAccentToggle.IsOn = useSystemAccent;
        CustomAccentCard.IsEnabled = !useSystemAccent;

        string savedAccentColor = _settingsService.Get("AccentColor", string.Empty);
        if (!string.IsNullOrEmpty(savedAccentColor) && !useSystemAccent)
        {
            ApplyAccentColor(savedAccentColor);
            AccentColorText.Text = savedAccentColor;
        }
        else
        {
            AccentColorText.Text = "System Default";
        }

        string savedBackdrop = _settingsService.Get("Backdrop", "Mica");
        SelectComboBoxItemByTag(BackdropComboBox, savedBackdrop);

        // ─── Typography ───────────────────────────────────────────────────────
        string savedFont = _settingsService.Get("FontFamily", "Segoe UI Variable");
        SelectComboBoxItemByTag(FontFamilyComboBox, savedFont);

        int savedFontScale = _settingsService.Get("FontSizeScale", 100);
        FontSizeSlider.Value = savedFontScale;
        FontSizeText.Text = $"{savedFontScale}%";

        // ─── Behavior ─────────────────────────────────────────────────────────
        ConfirmationsToggle.IsOn = _settingsService.Get("ShowConfirmations", true);
        AutoConnectToggle.IsOn = _settingsService.Get("AutoConnect", true);
        SoundEffectsToggle.IsOn = _settingsService.Get("SoundEffects", true);

        // ─── Dashboard ────────────────────────────────────────────────────────
        int savedInterval = _settingsService.Get("RefreshInterval", 2);
        SelectComboBoxItemByTag(RefreshIntervalComboBox, savedInterval.ToString());

        string savedTempUnit = _settingsService.Get("TemperatureUnit", "Celsius");
        SelectSegmentedItemByTag(TempUnitSegmented, savedTempUnit);

        AnimationsToggle.IsOn = _settingsService.Get("DashboardAnimations", true);

        // ─── Window ───────────────────────────────────────────────────────────
        AlwaysOnTopToggle.IsOn = _settingsService.Get("AlwaysOnTop", false);
        StartMinimizedToggle.IsOn = _settingsService.Get("StartMinimized", false);
        RememberPositionToggle.IsOn = _settingsService.Get("RememberPosition", true);
    }

    private void SetVersionInfo()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        string versionString = $"{version?.Major}.{version?.Minor}.{version?.Build}";
        VersionText.Text = versionString;
        VersionSummaryText.Text = $"v{versionString}";
        RuntimeText.Text = $".NET {Environment.Version.Major}";
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // APPEARANCE HANDLERS
    // ═══════════════════════════════════════════════════════════════════════════

    private void ThemeSegmented_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        if (ThemeSegmented.SelectedItem is CommunityToolkit.WinUI.Controls.SegmentedItem item &&
            item.Tag is string theme)
        {
            _settingsService.Set("AppTheme", theme);
            App.ApplyTheme(theme);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // APPEARANCE HELPERS
    // ═══════════════════════════════════════════════════════════════════════════

    private void UseSystemAccentToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        bool useSystem = UseSystemAccentToggle.IsOn;
        _settingsService.Set("UseSystemAccent", useSystem);
        CustomAccentCard.IsEnabled = !useSystem;

        if (useSystem)
        {
            AccentColorText.Text = "System Default";
            // Reset to system accent color by clearing custom color
            _settingsService.Set("AccentColor", string.Empty);
        }
    }

    private async void AccentColorButton_Click(object sender, RoutedEventArgs e)
    {
        var colorPicker = new ColorPicker
        {
            IsColorSpectrumVisible = true,
            IsColorPreviewVisible = true,
            IsColorSliderVisible = true,
            IsAlphaEnabled = false,
            IsHexInputVisible = true
        };

        var dialog = new ContentDialog
        {
            Title = "Choose Accent Color",
            Content = colorPicker,
            PrimaryButtonText = "Apply",
            SecondaryButtonText = "Cancel",
            XamlRoot = this.XamlRoot,
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            Color selectedColor = colorPicker.Color;
            string hexColor = $"#{selectedColor.R:X2}{selectedColor.G:X2}{selectedColor.B:X2}";
            _settingsService.Set("AccentColor", hexColor);
            ApplyAccentColor(hexColor);
            AccentColorText.Text = hexColor;
        }
    }

    private void PresetColor_Click(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        if (sender is Button button && button.Tag is string hexColor)
        {
            UseSystemAccentToggle.IsOn = false;
            _settingsService.Set("UseSystemAccent", false);
            _settingsService.Set("AccentColor", hexColor);
            ApplyAccentColor(hexColor);
            AccentColorText.Text = hexColor;
            CustomAccentCard.IsEnabled = true;
        }
    }

    private void ApplyAccentColor(string hexColor)
    {
        if (string.IsNullOrEmpty(hexColor))
        {
            return;
        }

        try
        {
            var color = ParseHexColor(hexColor);
            var brush = new SolidColorBrush(color);
            AccentColorPreview.Background = brush;
            CurrentAccentPreview.Background = brush;
        }
        catch
        {
            // Ignore invalid color values
        }
    }

    private static Color ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');
        return Color.FromArgb(
            255,
            byte.Parse(hex[..2], System.Globalization.NumberStyles.HexNumber),
            byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
            byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber));
    }

    private void BackdropComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        if (BackdropComboBox.SelectedItem is ComboBoxItem item && item.Tag is string backdrop)
        {
            _settingsService.Set("Backdrop", backdrop);
            ApplyBackdrop(backdrop);
        }
    }

    private static void ApplyBackdrop(string backdrop)
    {
        if (App.CurrentMainWindow is null)
        {
            return;
        }

        App.CurrentMainWindow.SystemBackdrop = backdrop switch
        {
            "Mica" => new MicaBackdrop { Kind = MicaKind.Base },
            "MicaAlt" => new MicaBackdrop { Kind = MicaKind.BaseAlt },
            "Acrylic" => new DesktopAcrylicBackdrop(),
            _ => null
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TYPOGRAPHY HANDLERS
    // ═══════════════════════════════════════════════════════════════════════════

    private void FontFamilyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        if (FontFamilyComboBox.SelectedItem is ComboBoxItem item && item.Tag is string fontFamily)
        {
            _settingsService.Set("FontFamily", fontFamily);
            App.ApplyFontFamily(fontFamily);
        }
    }

    private void FontSizeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_isInitializing || FontSizeText is null)
        {
            return;
        }

        int scale = (int)FontSizeSlider.Value;
        FontSizeText.Text = $"{scale}%";
        _settingsService.Set("FontSizeScale", scale);
        App.ApplyFontScale(scale);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BEHAVIOR HANDLERS
    // ═══════════════════════════════════════════════════════════════════════════

    private void ConfirmationsToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        _settingsService.Set("ShowConfirmations", ConfirmationsToggle.IsOn);
    }

    private void AutoConnectToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        _settingsService.Set("AutoConnect", AutoConnectToggle.IsOn);
    }

    private void SoundEffectsToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        _settingsService.Set("SoundEffects", SoundEffectsToggle.IsOn);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DASHBOARD HANDLERS
    // ═══════════════════════════════════════════════════════════════════════════

    private void RefreshIntervalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        if (RefreshIntervalComboBox.SelectedItem is ComboBoxItem item && 
            item.Tag is string tagValue &&
            int.TryParse(tagValue, out int interval))
        {
            _settingsService.Set("RefreshInterval", interval);
        }
    }

    private void TempUnitSegmented_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        if (TempUnitSegmented.SelectedItem is CommunityToolkit.WinUI.Controls.SegmentedItem item &&
            item.Tag is string unit)
        {
            _settingsService.Set("TemperatureUnit", unit);
        }
    }

    private void AnimationsToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        _settingsService.Set("DashboardAnimations", AnimationsToggle.IsOn);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // WINDOW HANDLERS
    // ═══════════════════════════════════════════════════════════════════════════

    private void AlwaysOnTopToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        bool alwaysOnTop = AlwaysOnTopToggle.IsOn;
        _settingsService.Set("AlwaysOnTop", alwaysOnTop);
        ApplyAlwaysOnTop(alwaysOnTop);
    }

    private static void ApplyAlwaysOnTop(bool alwaysOnTop)
    {
        if (App.CurrentMainWindow is null)
        {
            return;
        }

        var hwnd = WindowNative.GetWindowHandle(App.CurrentMainWindow);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = alwaysOnTop;
        }
    }

    private void StartMinimizedToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        _settingsService.Set("StartMinimized", StartMinimizedToggle.IsOn);
    }

    private void RememberPositionToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        _settingsService.Set("RememberPosition", RememberPositionToggle.IsOn);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RESET HANDLER
    // ═══════════════════════════════════════════════════════════════════════════

    private async void ResetSettings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Reset Settings",
            Content = "Are you sure you want to reset all settings to their default values? This cannot be undone.",
            PrimaryButtonText = "Reset",
            SecondaryButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Secondary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            ResetAllSettings();
        }
    }

    private void ResetAllSettings()
    {
        _isInitializing = true;

        // Appearance
        _settingsService.Set("AppTheme", "System");
        _settingsService.Set("UseSystemAccent", true);
        _settingsService.Set("AccentColor", string.Empty);
        _settingsService.Set("Backdrop", "Mica");

        // Typography
        _settingsService.Set("FontFamily", "Segoe UI Variable");
        _settingsService.Set("FontSizeScale", 100);

        // Behavior
        _settingsService.Set("ShowConfirmations", true);
        _settingsService.Set("AutoConnect", true);
        _settingsService.Set("SoundEffects", true);

        // Dashboard
        _settingsService.Set("RefreshInterval", 2);
        _settingsService.Set("TemperatureUnit", "Celsius");
        _settingsService.Set("DashboardAnimations", true);

        // Window
        _settingsService.Set("AlwaysOnTop", false);
        _settingsService.Set("StartMinimized", false);
        _settingsService.Set("RememberPosition", true);

        // Reload UI
        LoadSettings();
        App.ApplyTheme("System");
        App.ApplyFontFamily("Segoe UI Variable");
        App.ApplyFontScale(100);
        ApplyBackdrop("Mica");
        ApplyAlwaysOnTop(false);

        _isInitializing = false;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ABOUT HANDLERS
    // ═══════════════════════════════════════════════════════════════════════════

    private async void OpenGitHub_Click(object sender, RoutedEventArgs e)
    {
        await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/clindsay94/Connors-PC-Remote"));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HELPER METHODS
    // ═══════════════════════════════════════════════════════════════════════════

    private static void SelectComboBoxItemByTag(ComboBox comboBox, string tag)
    {
        foreach (var item in comboBox.Items)
        {
            if (item is ComboBoxItem comboBoxItem && comboBoxItem.Tag is string itemTag && itemTag == tag)
            {
                comboBox.SelectedItem = item;
                return;
            }
        }

        // Default to first item if not found
        if (comboBox.Items.Count > 0)
        {
            comboBox.SelectedIndex = 0;
        }
    }

    private static void SelectSegmentedItemByTag(CommunityToolkit.WinUI.Controls.Segmented segmented, string tag)
    {
        foreach (var item in segmented.Items)
        {
            if (item is CommunityToolkit.WinUI.Controls.SegmentedItem segmentedItem && 
                segmentedItem.Tag is string itemTag && 
                itemTag == tag)
            {
                segmented.SelectedItem = item;
                return;
            }
        }

        // Default to first item if not found
        if (segmented.Items.Count > 0)
        {
            segmented.SelectedIndex = 0;
        }
    }
}
