using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace Arent3d.Architecture.Presentation.Behaviors;

public class SyncHorizontalScrollBehavior : Behavior<ScrollViewer>
{
    public static readonly DependencyProperty TargetScrollViewerProperty =
        DependencyProperty.Register(nameof(TargetScrollViewer), typeof(ScrollViewer),
            typeof(SyncHorizontalScrollBehavior));

    public ScrollViewer? TargetScrollViewer
    {
        get => (ScrollViewer?)GetValue(TargetScrollViewerProperty);
        set => SetValue(TargetScrollViewerProperty, value);
    }

    private bool _lockScroll;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.ScrollChanged += OnSourceScrollChanged;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.ScrollChanged -= OnSourceScrollChanged;
    }

    private void OnSourceScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.HorizontalChange == 0 && e.ViewportWidthChange == 0) return;
        SyncScrollViewerImpl(AssociatedObject, TargetScrollViewer);
    }

    public void SyncScrollViewer()
    {
        SyncScrollViewerImpl(AssociatedObject, TargetScrollViewer);
    }

    private void SyncScrollViewerImpl(ScrollViewer source, ScrollViewer? dest)
    {
        if (dest is null || _lockScroll) return;

        _lockScroll = true;

        // When source is at max scroll, snap dest to its own max to account for
        // the small ScrollableWidth difference caused by border/scrollbar offsets.
        var targetOffset = source.ScrollableWidth > 0 && source.HorizontalOffset >= source.ScrollableWidth
            ? dest.ScrollableWidth
            : source.HorizontalOffset;

        dest.ScrollToHorizontalOffset(targetOffset);
        _lockScroll = false;
    }
}
