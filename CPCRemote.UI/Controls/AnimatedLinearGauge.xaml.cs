using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

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
        this.SizeChanged += (s, e) => UpdateGauge(false);
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

    private void UpdateGauge(bool animate = true)
    {
        double percentage = Math.Clamp(Value / Maximum, 0, 1);
        ValueText.Text = Math.Round(Value).ToString();

        if (FillRect.Parent is FrameworkElement track)
        {
            double targetWidth = track.ActualWidth * percentage;

            if (animate && targetWidth > 0)
            {
                DoubleAnimation widthAnimation = new DoubleAnimation
                {
                    To = targetWidth,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EnableDependentAnimation = true,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                Storyboard storyboard = new Storyboard();
                Storyboard.SetTarget(widthAnimation, FillRect);
                Storyboard.SetTargetProperty(widthAnimation, "Width");
                storyboard.Children.Add(widthAnimation);
                storyboard.Begin();
            }
            else
            {
                FillRect.Width = targetWidth;
            }
        }
    }
}
