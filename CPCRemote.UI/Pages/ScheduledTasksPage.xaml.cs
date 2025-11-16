namespace CPCRemote.UI.Pages
{
    using System.Runtime.Versioning;
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;

    /// <summary>
    /// Page for managing scheduled power management tasks
    /// </summary>
    [SupportedOSPlatform("windows10.0.22621.0")]
    public sealed partial class ScheduledTasksPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledTasksPage"/> class.
        /// </summary>
        public ScheduledTasksPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Handles adding a new scheduled task
        /// </summary>
        private async void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(e);
            ContentDialog dialog = new()
            {
                Title = "Add Scheduled Task",
                Content = "Scheduled task functionality will be implemented in a future release.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
