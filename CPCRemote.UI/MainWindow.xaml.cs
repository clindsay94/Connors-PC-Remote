using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System;
using System.Diagnostics;

namespace CPCRemote.UI
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            // Safe Activation
            this.Activate();

            // Robust Navigation Setup
            try
            {
                // Ensure the page type exists before navigating
                var firstPage = typeof(QuickActionsPage);

                NavView.SelectedItem = NavView.MenuItems[0];
                bool success = ContentFrame.Navigate(firstPage);

                if (!success)
                {
                    Debug.WriteLine("WARNING: Navigation to QuickActionsPage failed (returned false).");
                }
            }
            catch (Exception ex)
            {
                // This catches the 0x80070002 if it happens during Page construction
                Debug.WriteLine($"CRITICAL NAV ERROR: {ex}");

                // Optional: Show a dialog or fallback content so the app doesn't just vanish
                ContentFrame.Content = new TextBlock
                {
                    Text = $"Failed to load start page: {ex.Message}",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            try
            {
                if (args.IsSettingsSelected)
                {
                    ContentFrame.Navigate(typeof(SettingsPage));
                }
                else if (args.SelectedItem is NavigationViewItem selectedItem && selectedItem.Tag is string pageTag)
                {
                    // Using fully qualified names or explicit types helps avoid "Type not found" errors
                    Type? pageType = pageTag switch
                    {
                        "QuickActionsPage" => typeof(QuickActionsPage),
                        "ScheduledTasksPage" => typeof(ScheduledTasksPage),
                        "ServiceManagementPage" => typeof(ServiceManagementPage),
                        "SettingsPage" => typeof(SettingsPage),
                        _ => null
                    };

                    if (pageType != null)
                    {
                        ContentFrame.Navigate(pageType);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NAVIGATION FAILED: {ex}");
            }
        }
    }
}