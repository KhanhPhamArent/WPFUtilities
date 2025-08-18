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

    public System.Windows.Controls.DataGrid? DataGrid
    {
        get => (System.Windows.Controls.DataGrid?)GetValue(DataGridProperty);
        set => SetValue(DataGridProperty, value);
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
        var scrollBar = FindClosestHorizontalScrollBar(source);
        source.ScrollChanged += (_, args) =>
        {
            if (_lockScroll) return;

            if (scrollBar?.IsMouseCaptureWithin == false && args.HorizontalOffset == 0)
            {
                return;
            }

            _lockScroll = true;
            var offset = args.HorizontalOffset < 10 ? 0 : args.HorizontalOffset;
            Console.WriteLine(offset);
            dest.ScrollToHorizontalOffset(offset);
            _lockScroll = false;
        };
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