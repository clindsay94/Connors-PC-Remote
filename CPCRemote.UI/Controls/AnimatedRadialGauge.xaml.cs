using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using Windows.Foundation;

namespace CPCRemote.UI.Controls;

public sealed partial class AnimatedRadialGauge : UserControl
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register("Value", typeof(double), typeof(AnimatedRadialGauge), new PropertyMetadata(0.0, OnValueChanged));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register("Maximum", typeof(double), typeof(AnimatedRadialGauge), new PropertyMetadata(100.0, OnValueChanged));

    public static readonly DependencyProperty UnitProperty =
        DependencyProperty.Register("Unit", typeof(string), typeof(AnimatedRadialGauge), new PropertyMetadata(string.Empty, OnUnitChanged));

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

    public AnimatedRadialGauge()
    {
        this.InitializeComponent();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AnimatedRadialGauge gauge)
        {
            gauge.UpdateGauge();
        }
    }

    private static void OnUnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AnimatedRadialGauge gauge)
        {
            gauge.UnitText.Text = (string)e.NewValue;
        }
    }

    private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateGauge();
        // Resize rings
        double size = Math.Min(e.NewSize.Width, e.NewSize.Height);
        if (size > 0)
        {
            TrackRing.Width = size;
            TrackRing.Height = size;
            ProgressPath.Width = size;
            ProgressPath.Height = size;
        }
    }

    private void UpdateGauge()
    {
        double percentage = Math.Clamp(Value / Maximum, 0, 1);
        ValueText.Text = Math.Round(Value).ToString();

        double angle = percentage * 360;
        
        // Render Arc
        double size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 0) size = 100;
        
        double radius = (size - TrackRing.StrokeThickness) / 2;
        Point center = new Point(size / 2, size / 2);

        // Start at top (adjust -90 degrees)
        double startAngle = -90;
        double endAngle = startAngle + angle;

        Point startPoint = GetPointOnCircle(center, radius, startAngle);
        Point endPoint = GetPointOnCircle(center, radius, endAngle);

        bool isLargeArc = angle > 180;

        PathGeometry geo = new PathGeometry();
        PathFigure fig = new PathFigure { StartPoint = startPoint, IsClosed = false };
        ArcSegment arc = new ArcSegment 
        { 
            Point = endPoint, 
            Size = new Size(radius, radius), 
            IsLargeArc = isLargeArc, 
            SweepDirection = SweepDirection.Clockwise 
        };
        
        fig.Segments.Add(arc);
        geo.Figures.Add(fig);
        
        ProgressPath.Data = geo;
        
        // Color transition based on value
        // Simple logic: Green -> Yellow -> Red? Or just use the VibrantMesh gradient?
        // Let's stick to VibrantMesh for the "Hyper-Dynamic" look defined in ThemeResources.
    }

    private static Point GetPointOnCircle(Point center, double radius, double angleInDegrees)
    {
        double angleInRadians = angleInDegrees * (Math.PI / 180);
        double x = center.X + radius * Math.Cos(angleInRadians);
        double y = center.Y + radius * Math.Sin(angleInRadians);
        return new Point(x, y);
    }
}
