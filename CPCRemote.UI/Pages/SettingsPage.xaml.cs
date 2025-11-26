namespace CPCRemote.UI.Pages;

using System;
using System.Reflection;
using System.Runtime.Versioning;

using CPCRemote.UI.Services;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

/// <summary>
/// Settings page for configuring application preferences.
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

        // Load saved settings
        LoadSettings();

        // Wire up event handlers
        ThemeComboBox.SelectionChanged += ThemeComboBox_SelectionChanged;
        ConfirmationsToggle.Toggled += ConfirmationsToggle_Toggled;
        AutoConnectToggle.Toggled += AutoConnectToggle_Toggled;

        // Set version info
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"Version {version?.Major}.{version?.Minor}.{version?.Build}";

        _isInitializing = false;
    }

    private void LoadSettings()
    {
        // Load theme preference
        string savedTheme = _settingsService.Get("AppTheme", "System");
        foreach (ComboBoxItem item in ThemeComboBox.Items)
        {
            if (item.Tag is string tag && tag == savedTheme)
            {
                ThemeComboBox.SelectedItem = item;
                break;
            }
        }

        // Apply theme immediately
        ApplyTheme(savedTheme);

        // Load other settings
        ConfirmationsToggle.IsOn = _settingsService.Get("ShowConfirmations", true);
        AutoConnectToggle.IsOn = _settingsService.Get("AutoConnect", true);
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        if (ThemeComboBox.SelectedItem is ComboBoxItem selectedItem &&
            selectedItem.Tag is string selectedTheme)
        {
            _settingsService.Set("AppTheme", selectedTheme);
            ApplyTheme(selectedTheme);
        }
    }

    private static void ApplyTheme(string theme)
    {
        if (App.CurrentMainWindow?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }
    }

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
}
