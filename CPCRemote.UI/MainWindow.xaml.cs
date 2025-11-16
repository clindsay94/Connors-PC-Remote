namespace CPCRemote.UI
{
    using System;
    using System.Runtime.Versioning; // Add this using

    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;

    using CPCRemote.UI.Pages;

    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame
    /// </summary>
    [SupportedOSPlatform("windows10.0.17763.0")] // Add this attribute to the class
    public sealed partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
            NavView.SelectionChanged += NavView_SelectionChanged;
            ContentFrame.Navigate(typeof(QuickActionsPage));
        }

        /// <summary>
        /// The NavView_SelectionChanged
        /// </summary>
        /// <param name="sender">The sender<see cref="NavigationView"/></param>
        /// <param name="args">The args<see cref="NavigationViewSelectionChangedEventArgs"/></param>
        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(SettingsPage));
            }
            else if (args.SelectedItemContainer != null)
            {
                var navItemTag = args.SelectedItemContainer.Tag.ToString();
                Type? pageType = Type.GetType($"CPCRemote.UI.Pages.{navItemTag}");
                if (pageType != null)
                {
                    ContentFrame.Navigate(pageType);
                }
                else
                {
                    // Handle the case where the page type is not found, e.g., navigate to a default page
                    ContentFrame.Navigate(typeof(QuickActionsPage));
                }
            }
        }
    }
}
