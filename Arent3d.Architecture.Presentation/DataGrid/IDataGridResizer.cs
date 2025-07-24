using System.Windows.Controls ;

namespace Arent3d.Architecture.Presentation.DataGrid ;

public interface IDataGridResizer
{
  void ResizeLastColumn( System.Windows.Controls.DataGrid dataGrid, ScrollViewer scrollViewer, double actualWidth ) ;
} 