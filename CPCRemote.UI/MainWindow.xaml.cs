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
            try
            {
                this.InitializeComponent();
            }
            catch (Microsoft.UI.Xaml.Markup.XamlParseException ex)
            {
                // Enhanced diagnostic logging for XamlParseException
                var errorBuilder = new System.Text.StringBuilder();
                errorBuilder.AppendLine($"XamlParseException Details:");
                errorBuilder.AppendLine($"Message: {ex.Message}");
                errorBuilder.AppendLine($"HResult: 0x{ex.HResult:X8}");
                errorBuilder.AppendLine($"Source: {ex.Source}");
                
                // Check Data dictionary for additional info
                if (ex.Data != null && ex.Data.Count > 0)
                {
                    errorBuilder.AppendLine("Data Dictionary:");
                    foreach (var key in ex.Data.Keys)
                    {
                        errorBuilder.AppendLine($"  {key}: {ex.Data[key]}");
                    }
                }
                
                if (ex.InnerException != null)
                {
                    errorBuilder.AppendLine($"Inner Exception: {ex.InnerException.GetType().Name}");
                    errorBuilder.AppendLine($"Inner Message: {ex.InnerException.Message}");
                    errorBuilder.AppendLine($"Inner HResult: 0x{ex.InnerException.HResult:X8}");
                }
                
                errorBuilder.AppendLine($"Stack Trace: {ex.StackTrace}");
                
                Program.LogFailure(new Exception(errorBuilder.ToString(), ex), "XamlParse.Enhanced");
                throw;
            }

            // Safely initialize the system backdrop (Mica) in code-behind
            // This prevents XamlParseException on unsupported OS versions
            TrySetSystemBackdrop();
            
            // Navigation moved to PerformInitialNavigation() method
            // Called from App.OnLaunched after window activation and service configuration
            
            // Wire up navigation failed handler
            ContentFrame.NavigationFailed += ContentFrame_NavigationFailed;
        }

        /// <summary>
        /// Attempts to set the Mica system backdrop if supported.
        /// graceful fallback to solid color on failure/unsupported OS.
        /// </summary>
        private void TrySetSystemBackdrop()
        {
            try
            {
                if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
                {
                    this.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
                    Debug.WriteLine("Mica backdrop enabled.");
                }
                else if (Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported())
                {
                    this.SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
                    Debug.WriteLine("DesktopAcrylic backdrop enabled.");
                }
                else
                {
                   Debug.WriteLine("System backdrop not supported. Using default background.");
                }
            }
            catch (Exception ex)
            {
                Program.LogFailure(ex, "Mica Initialization");
                Debug.WriteLine($"Failed to set SystemBackdrop: {ex}");
            }
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