using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using System.Numerics;

namespace CPCRemote.UI.Helpers;

/// <summary>
/// Attaches physics-based composition animations to elements.
/// </summary>
public static class CompositionBehavior
{
    public static readonly DependencyProperty IsSpringAnimationEnabledProperty =
        DependencyProperty.RegisterAttached("IsSpringAnimationEnabled", typeof(bool), typeof(CompositionBehavior), new PropertyMetadata(false, OnIsSpringAnimationEnabledChanged));

    public static bool GetIsSpringAnimationEnabled(DependencyObject obj) => (bool)obj.GetValue(IsSpringAnimationEnabledProperty);
    public static void SetIsSpringAnimationEnabled(DependencyObject obj, bool value) => obj.SetValue(IsSpringAnimationEnabledProperty, value);

    private static void OnIsSpringAnimationEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            if ((bool)e.NewValue)
            {
                element.PointerEntered += Element_PointerEntered;
                element.PointerExited += Element_PointerExited;
                element.PointerPressed += Element_PointerPressed;
                element.PointerReleased += Element_PointerReleased;
            }
            else
            {
                element.PointerEntered -= Element_PointerEntered;
                element.PointerExited -= Element_PointerExited;
                element.PointerPressed -= Element_PointerPressed;
                element.PointerReleased -= Element_PointerReleased;
            }
        }
    }

    private static void Element_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is UIElement element)
        {
            Canvas.SetZIndex(element, 1);
            ApplyScaleAnimation(element, 1.05f);
        }
    }

    private static void Element_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is UIElement element)
        {
            Canvas.SetZIndex(element, 0);
            ApplyScaleAnimation(element, 1.0f);
        }
    }

    private static void Element_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is UIElement element)
        {
            Canvas.SetZIndex(element, 1);
            ApplyScaleAnimation(element, 0.95f);
        }
    }

    private static void Element_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (sender is UIElement element)
        {
            Canvas.SetZIndex(element, 1);
            ApplyScaleAnimation(element, 1.05f); // Return to hover state
        }
    }

    private static void ApplyScaleAnimation(UIElement element, float scale)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;

        // Enable translation for this visual
        ElementCompositionPreview.SetIsTranslationEnabled(element, true);

        // Ensure CenterPoint is set to center of element for scaling
        visual.CenterPoint = new Vector3((float)element.RenderSize.Width / 2, (float)element.RenderSize.Height / 2, 0);

        // 1. Scale Animation
        var scaleAnim = compositor.CreateVector3KeyFrameAnimation();
        scaleAnim.Target = "Scale";
        scaleAnim.InsertKeyFrame(1.0f, new Vector3(scale), compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f)));
        scaleAnim.Duration = System.TimeSpan.FromMilliseconds(400);

        // 2. Translation (Lift) Animation
        // Derived from scale: >1 means lift up (-8), <1 means press down (+2), 1 means reset
        float translateY = 0f;
        if (scale > 1.0f) translateY = -8.0f;       // Lift up on hover
        else if (scale < 1.0f) translateY = 2.0f;   // Push down on press

        var transAnim = compositor.CreateVector3KeyFrameAnimation();
        transAnim.Target = "Translation";
        transAnim.InsertKeyFrame(1.0f, new Vector3(0, translateY, 0), compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f)));
        transAnim.Duration = System.TimeSpan.FromMilliseconds(400);

        // Start both
        visual.StartAnimation("Scale", scaleAnim);
        visual.StartAnimation("Translation", transAnim);
    }
}
