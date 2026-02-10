using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CPCRemote.UI.Controls;

/// <summary>
/// A GridView that properly binds VariableSizedWrapGrid attached properties from the item container.
/// </summary>
public class VariableSizedGridView : GridView
{
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);

        if (item is ViewModels.DashboardWidgetViewModel widget && element is GridViewItem container)
        {
            // Bind the attached properties to the ViewModel's properties
            // We use direct setting here for simplicity, or we could set up bindings
            
            // Set initial values
            VariableSizedWrapGrid.SetColumnSpan(container, widget.ColumnSpan);
            VariableSizedWrapGrid.SetRowSpan(container, widget.RowSpan);

            // Listen for changes? 
            // For a robust implementation, we should set up a Binding.
            
            var columnSpanBinding = new Microsoft.UI.Xaml.Data.Binding
            {
                Source = widget,
                Path = new PropertyPath("ColumnSpan"),
                Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay
            };
            container.SetBinding(VariableSizedWrapGrid.ColumnSpanProperty, columnSpanBinding);

            var rowSpanBinding = new Microsoft.UI.Xaml.Data.Binding
            {
                Source = widget,
                Path = new PropertyPath("RowSpan"),
                Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay
            };
            container.SetBinding(VariableSizedWrapGrid.RowSpanProperty, rowSpanBinding);

            // Force a re-layout when spans change
            widget.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName is "ColumnSpan" or "RowSpan")
                {
                    // Invalidate measure on the panel to force re-layout
                    if (this.ItemsPanelRoot is VariableSizedWrapGrid wrapGrid)
                    {
                        wrapGrid.InvalidateMeasure();
                    }
                }
            };
        }
    }
}
