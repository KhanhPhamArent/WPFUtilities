using System.Windows ;
using System.Windows.Controls ;

namespace Arent3d.Architecture.Presentation.DataGrid ;

public class DataGridResizer : IDataGridResizer
{
  private const double BorderOffset = 2.0 ;

  public void ResizeLastColumn( System.Windows.Controls.DataGrid dataGrid, ScrollViewer scrollViewer, double actualWidth )
  {
    if ( dataGrid.Columns.Count == 0 )
      return ;

    var availableWidth = CalculateAvailableWidth( dataGrid, scrollViewer, actualWidth ) ;
    var lastColumn = dataGrid.Columns.Last() ;

    // Ensure minimum width is respected
    var finalWidth = Math.Max( availableWidth, lastColumn.MinWidth ) ;
    lastColumn.Width = new DataGridLength( finalWidth ) ;
  }

  private double CalculateAvailableWidth( System.Windows.Controls.DataGrid dataGrid, ScrollViewer scrollViewer, double actualWidth )
  {
    if ( dataGrid.Columns.Count <= 1 )
      return actualWidth - BorderOffset - GetScrollBarOffset( scrollViewer ) ;

    // Calculate width of all columns except the last one
    var columnWidths = dataGrid.Columns.Take( dataGrid.Columns.Count - 1 ).Sum( x => x.ActualWidth ) ;
    var scrollBarOffset = GetScrollBarOffset( scrollViewer ) ;

    return actualWidth - columnWidths - BorderOffset - scrollBarOffset ;
  }

  private double GetScrollBarOffset( ScrollViewer scrollViewer )
  {
    var verticalScrollBarOffset = scrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible ? GetScrollBarWidth() : 0 ;
    var horizontalScrollBarOffset = scrollViewer.ComputedHorizontalScrollBarVisibility == Visibility.Visible ? GetScrollBarHeight() : 0 ;

    // For DataGrid, we typically only need to account for vertical scrollbar
    // Horizontal scrollbar is usually handled differently
    return verticalScrollBarOffset ;
  }

  private double GetScrollBarWidth()
  {
    return SystemParameters.VerticalScrollBarWidth ;
  }

  private double GetScrollBarHeight()
  {
    return SystemParameters.HorizontalScrollBarHeight ;
  }
}