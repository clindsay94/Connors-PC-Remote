using CPCRemote.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CPCRemote.UI.Controls;

/// <summary>
/// Dashboard widget control that adapts its visual presentation based on widget size.
/// </summary>
public sealed partial class DashboardWidget : UserControl
{
    public DashboardWidget()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Handles the Cycle Size button click by delegating to the parent Home Page.
    /// </summary>
    private void CycleSizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DashboardWidgetViewModel widget)
        {
            // Find the parent HomePage and invoke cycle command
            var homePage = FindParentPage();
            if (homePage is Pages.HomePage page)
            {
                page.ViewModel.CycleWidgetSizeCommand.Execute(widget);
            }
        }
    }

    private void MoveLeftButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DashboardWidgetViewModel widget)
        {
            if (FindParentPage() is Pages.HomePage page)
            {
                page.ViewModel.MoveWidgetLeftCommand.Execute(widget);
            }
        }
    }

    private void MoveRightButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DashboardWidgetViewModel widget)
        {
            if (FindParentPage() is Pages.HomePage page)
            {
                page.ViewModel.MoveWidgetRightCommand.Execute(widget);
            }
        }
    }

    /// <summary>
    /// Walks the visual tree to find the parent Page.
    /// </summary>
    private Page? FindParentPage()
    {
        DependencyObject? current = this;
        while (current is not null)
        {
            if (current is Page page) return page;
            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
