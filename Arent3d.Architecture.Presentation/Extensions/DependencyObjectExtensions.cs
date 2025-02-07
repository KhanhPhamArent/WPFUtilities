using System.Windows;
using System.Windows.Media;

namespace Arent3d.Architecture.Presentation.Extensions;

public static class DependencyObjectExtensions
{
  public static T? FindClosestParentByType<T>( this DependencyObject child ) where T : DependencyObject
  {
    var parent = VisualTreeHelper.GetParent( child );

    while ( parent != null && ! ( parent is T ) ) {
      parent = VisualTreeHelper.GetParent( parent );
      if ( parent is T parentType ) {
        return parentType;
      }
    }

    return null;
  }

  public static T? FindClosestChildByType<T>( this DependencyObject prop ) where T : DependencyObject
  {
    for ( var i = 0 ; i < VisualTreeHelper.GetChildrenCount( prop ) ; i++ ) {
      var child = VisualTreeHelper.GetChild( prop, i );
      if ( child is T castedProp ) return castedProp;
      var closestChild = FindClosestChildByType<T>( child );
      if ( closestChild != null ) return closestChild;
    }

    return null;
  }
}