using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CPCRemote.UI.Controls;

public sealed partial class AnimatedLinearGauge : UserControl
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register("Value", typeof(double), typeof(AnimatedLinearGauge), new PropertyMetadata(0.0, OnValueChanged));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register("Maximum", typeof(double), typeof(AnimatedLinearGauge), new PropertyMetadata(100.0, OnValueChanged));

    public static readonly DependencyProperty UnitProperty =
        DependencyProperty.Register("Unit", typeof(string), typeof(AnimatedLinearGauge), new PropertyMetadata(string.Empty, OnUnitChanged));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register("Label", typeof(string), typeof(AnimatedLinearGauge), new PropertyMetadata(string.Empty, OnLabelChanged));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public string Unit
    {
        get => (string)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public AnimatedLinearGauge()
    {
        this.InitializeComponent();
        this.SizeChanged += (s, e) => UpdateGauge();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AnimatedLinearGauge gauge)
        {
            gauge.UpdateGauge();
        }
    }

    private static void OnUnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AnimatedLinearGauge gauge)
        {
            gauge.UnitText.Text = (string)e.NewValue;
        }
    }

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AnimatedLinearGauge gauge)
        {
            gauge.LabelText.Text = (string)e.NewValue;
        }
    }

    private void UpdateGauge()
    {
        double percentage = System.Math.Clamp(Value / Maximum, 0, 1);
        ValueText.Text = System.Math.Round(Value).ToString();
        
        // Animate width (using simplified Width assignment for now, implicitly animated by layout updates often)
        // For true smooth animation, we'd use Composition or DoubleAnimation.
        // But let's just set Width. Grid layout will handle it if we used Column definitions, 
        // but here we are using Rectangle inside Grid.
        
        if (ActualWidth > 0) // we need the container width
        {
             // Actually, ActualWidth is the whole control width. We need the Grid Column 0 width.
             // But we can just set Width of FillRect directly if we know the available space.
             // Better: Set Width of FillRect to (Percentage * ContainerWidth).
             // But ContainerWidth depends on layout. 
             // Simplest: Use Grid with ColumnDefinitions and *, then put Rectangle in Column 0 with Width=Auto? No.
             
             // Used approach: Rectangle HorizontalAlignment=Left. Width = ?
             // We need the parent grid's ActualWidth (minus the text column).
             // Let's assume the Grid column 0 has a width.
             
             // Refined: Use a Grid for the track, and a Grid/Border for the fill inside it.
             // The track Grid has known width? No, it's *
        }
        
        // Easier approach: Use a Grid for the bar track. 
        // Inside put a Border for the fill. 
        // Set fill.Width = track.ActualWidth * percentage.
        // But track.ActualWidth is dynamic.
        
        // Let's try to just use a standard ProgressBar with Custom Style! 
        // WinUI ProgressBar supports this easily.
        // But for "AnimatedLinearGauge" I wanted custom text.
        
        // Okay, let's fix the Width logic.
        // The track is the Grid "Grid.Column=0".
        if (FillRect.Parent is FrameworkElement track)
        {
             FillRect.Width = track.ActualWidth * percentage;
        }
    }
}
