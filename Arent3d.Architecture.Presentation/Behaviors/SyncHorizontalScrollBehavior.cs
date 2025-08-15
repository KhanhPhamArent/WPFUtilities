using System.Windows;
using System.Windows.Controls;
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
        source.ScrollChanged += (_, args) =>
        {
            if (_lockScroll) return;

            if (args.HorizontalOffset == 0)
            {
                return;
            }

            _lockScroll = true;
            dest.ScrollToHorizontalOffset(args.HorizontalOffset);
            _lockScroll = false;
        };
    }
}