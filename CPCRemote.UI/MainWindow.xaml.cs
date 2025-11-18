using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

// Ensure this namespace matches the x:Class in your XAML
namespace CPCRemote.UI
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            
            // Optional: Activate the window (makes it visible)
            // In some templates this is done in App.xaml.cs, but it's safe to call here.
            this.Activate();

            // Navigate to the first page by default (Quick Actions)
            NavView.SelectedItem = NavView.MenuItems[0];
            ContentFrame.Navigate(typeof(QuickActionsPage));
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(SettingsPage));
            }
            else if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                string pageTag = (string)selectedItem.Tag;
                
                Type pageType = pageTag switch
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
    }
}