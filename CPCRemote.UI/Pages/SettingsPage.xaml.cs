namespace CPCRemote.UI.Pages
{
    using System.Runtime.Versioning;

    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using Microsoft.Windows.Storage;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame
    /// </summary>
    public
    sealed partial class SettingsPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsPage"/> class.
        /// </summary>
        [SupportedOSPlatform("windows10.0.17763.0")]
        public SettingsPage()
        {
            this.InitializeComponent();
            ThemeComboBox.SelectionChanged += ThemeComboBox_SelectionChanged;
            ConfirmationsToggle.Toggled += ConfirmationsToggle_Toggled;
        }

        /// <summary>
        /// The ThemeComboBox_SelectionChanged
        /// </summary>
        /// <param name="_">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="SelectionChangedEventArgs"/></param>
        [SupportedOSPlatform("windows10.0.17763.0")]
        private void
        ThemeComboBox_SelectionChanged(object _, SelectionChangedEventArgs e)
        {
            string? selectedTheme = (e.AddedItems[0] as string);
            if (App.CurrentMainWindow?.Content is FrameworkElement rootElement)
            {
                switch (selectedTheme)
                {
                    case "Light":
                        rootElement.RequestedTheme = ElementTheme.Light;
                        break;
                    case "Dark":
                        rootElement.RequestedTheme = ElementTheme.Dark;
                        break;
                    case "System":
                        rootElement.RequestedTheme = ElementTheme.Default;
                        break;
                }
            }
        }

        /// <summary>
        /// The ConfirmationsToggle_Toggled
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="_">The e<see cref="RoutedEventArgs"/></param>
        [SupportedOSPlatform("windows10.0.17763.0")]
        private void
        ConfirmationsToggle_Toggled(object sender, RoutedEventArgs _)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {
                ApplicationData.GetDefault().LocalSettings.Values["ShowConfirmations"] =
                    toggleSwitch.IsOn;
            }
        }
    }
}  // namespace CPCRemote.UI.Pages
