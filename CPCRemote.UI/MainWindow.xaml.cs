using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Diagnostics;

using CPCRemote.UI.Pages;

namespace CPCRemote.UI
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            // Navigation moved to PerformInitialNavigation() method
            // Called from App.OnLaunched after window activation and service configuration
            
            // Wire up navigation failed handler
            ContentFrame.NavigationFailed += ContentFrame_NavigationFailed;
        }

        private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            Debug.WriteLine($"NAVIGATION FAILED EVENT: {e.SourcePageType?.Name} - {e.Exception}");
            e.Handled = true;
            
            ContentFrame.Content = new TextBlock
            {
                Text = $"Failed to navigate to {e.SourcePageType?.Name}:\n\n{e.Exception?.Message}\n\n{e.Exception?.StackTrace}",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20)
            };
        }

        /// <summary>
        /// Performs the initial navigation after the window is activated and services are configured.
        /// This ensures ApplicationData.Current is available when services are instantiated.
        /// </summary>
        public void PerformInitialNavigation()
        {
            try
            {
                // Start with Dashboard as the first page
                var firstPage = typeof(DashboardPage);

                NavView.SelectedItem = NavView.MenuItems[0];
                bool success = ContentFrame.Navigate(firstPage);

                if (!success)
                {
                    Debug.WriteLine("WARNING: Navigation to DashboardPage failed (returned false).");
                }
            }
            catch (Exception ex)
            {
                // This catches any errors during page construction
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
                    Debug.WriteLine("Navigating to Settings (built-in)");
                    ContentFrame.Navigate(typeof(SettingsPage));
                }
                else if (args.SelectedItem is NavigationViewItem selectedItem && selectedItem.Tag is string pageTag)
                {
                    Debug.WriteLine($"Navigating to: {pageTag}");
                    
                    // Using fully qualified names or explicit types helps avoid "Type not found" errors
                    Type? pageType = pageTag switch
                    {
                        "DashboardPage" => typeof(DashboardPage),
                        "QuickActionsPage" => typeof(QuickActionsPage),
                        "AppCatalogPage" => typeof(AppCatalogPage),
                        "ScheduledTasksPage" => typeof(ScheduledTasksPage),
                        "ServiceManagementPage" => typeof(ServiceManagementPage),
                        "SettingsPage" => typeof(SettingsPage),
                        _ => null
                    };

                    if (pageType != null)
                    {
                        bool success = ContentFrame.Navigate(pageType);
                        Debug.WriteLine($"Navigation to {pageTag} success: {success}");
                    }
                    else
                    {
                        Debug.WriteLine($"Unknown page tag: {pageTag}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NAVIGATION FAILED: {ex}");
                
                // Show error in content area for debugging
                ContentFrame.Content = new TextBlock
                {
                    Text = $"Navigation failed: {ex.Message}\n\n{ex.StackTrace}",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(20)
                };
            }
        }
    }
}