using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;

namespace Arent3d.Architecture.Presentation.Behaviors;

public class SyncHorizontalScrollBehavior : Behavior<ScrollViewer>
{
    public static readonly DependencyProperty TargetScrollViewerProperty =
        DependencyProperty.Register(nameof(TargetScrollViewer), typeof(ScrollViewer),
            typeof(SyncHorizontalScrollBehavior));

    public static readonly DependencyProperty DataGridProperty = DependencyProperty.Register(nameof(DataGrid),
        typeof(System.Windows.Controls.DataGrid), typeof(SyncHorizontalScrollBehavior));

    public ScrollViewer? TargetScrollViewer
    {
        get => (ScrollViewer?)GetValue(TargetScrollViewerProperty);
        set => SetValue(TargetScrollViewerProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (TargetScrollViewer is null) return;
        SyncScrollViewer(AssociatedObject, TargetScrollViewer);
        SyncScrollViewer(TargetScrollViewer, AssociatedObject);
    }

    private bool _lockScroll;

    private void SyncScrollViewer(ScrollViewer source, ScrollViewer dest)
    {
        source.ScrollChanged += (_, _) => { SyncScrollViewerImpl(source, dest); };
    }

    public void SyncScrollViewer()
    {
        SyncScrollViewerImpl(AssociatedObject, TargetScrollViewer);
    }

    private void SyncScrollViewerImpl(ScrollViewer source, ScrollViewer? dest)
    {
        if (dest is null || _lockScroll) return;

        var sourceScrollbar = FindClosestHorizontalScrollBar(source);
        if (sourceScrollbar?.IsMouseCaptureWithin == false && source.HorizontalOffset == 0) return;

        _lockScroll = true;
        if (Math.Abs(source.HorizontalOffset - source.ScrollableWidth) < 0.1)
        {
            var offset = source.ScrollableWidth - dest.ScrollableWidth;
            source.ScrollToHorizontalOffset(source.HorizontalOffset - offset);
        }

        dest.ScrollToHorizontalOffset(source.HorizontalOffset);
        _lockScroll = false;
    }

    private static ScrollBar? FindClosestHorizontalScrollBar(DependencyObject prop)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(prop); i++)
        {
            var child = VisualTreeHelper.GetChild(prop, i);
            if (child is ScrollBar castedProp && castedProp.Orientation == Orientation.Horizontal) return castedProp;
            var closestChild = FindClosestHorizontalScrollBar(child);
            if (closestChild != null) return closestChild;
        }

        return null;
    }
}