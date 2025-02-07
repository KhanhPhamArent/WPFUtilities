using System.Windows ;
using System.Windows.Controls ;
using Microsoft.Xaml.Behaviors ;

namespace Arent3d.Architecture.Presentation.Behaviors ;

public class SyncHorizontalScrollBehavior : Behavior<ScrollViewer>
{
  public static readonly DependencyProperty TargetScrollViewerProperty = DependencyProperty.Register( nameof( TargetScrollViewer ), typeof( ScrollViewer ), typeof( SyncHorizontalScrollBehavior ) ) ;

  public ScrollViewer? TargetScrollViewer
  {
    get => (ScrollViewer?)GetValue( TargetScrollViewerProperty ) ;
    set => SetValue( TargetScrollViewerProperty, value ) ;
  }

  protected override void OnAttached()
  {
    base.OnAttached() ;
    if ( TargetScrollViewer is null ) return ;
    SyncScrollViewer( AssociatedObject, TargetScrollViewer ) ;
    SyncScrollViewer( TargetScrollViewer, AssociatedObject ) ;
  }

  private bool _lockScroll = false ;
  private void SyncScrollViewer( ScrollViewer source, ScrollViewer dest )
  {
    source.ScrollChanged += ( _, args ) =>
    {
      if ( _lockScroll ) return ;
      _lockScroll = true ;
      dest.ScrollToHorizontalOffset( args.HorizontalOffset ) ;
      _lockScroll = false ;
    } ;
  }
}