using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Data ;
using Arent3d.Architecture.Presentation.Converters ;

namespace Arent3d.Architecture.Presentation.DataGrid ;

public class HeaderGridBuilder : IHeaderGridBuilder
{
  private const double BorderOffset = 2.0 ;

  public string[][] BuildHeaderGrid( IDataGridContext context, System.Windows.Controls.DataGrid dataGrid, Grid header,
    out int numberOfRows, out int numberOfColumns )
  {
    var groupList = context.ColumnHeaders ;
    numberOfRows = groupList.Where( x => x is not null ).Max( x => x!.Length ) ;
    numberOfColumns = dataGrid.Columns.Count ;

    CreateColumnDefinitions( dataGrid, header ) ;
    CreateRowDefinitions( header, numberOfRows ) ;

    return groupList ;
  }

  public Dictionary<string, GroupInfo> CreateGroupInfos( string[][] groupList, int numberOfRows, int numberOfColumns )
  {
    var groupMap = new Dictionary<string, GroupInfo>() ;

    for ( var columnIndex = 0; columnIndex < groupList.Length; columnIndex++ ) {
      var groups = groupList[ columnIndex ] ;
      var isFirstCreation = true ;
      var rowIndex = 0 ;
      var prefix = string.Empty ;

      for ( var index = 0; index < groups.Length; index++ ) {
        var groupName = groups[ index ] ;
        var key = prefix + "." + groupName ;

        if ( ! groupMap.TryGetValue( key, out var group ) ) {
          group = CreateNewGroup( groupName, columnIndex, rowIndex, numberOfRows, groups.Length, isFirstCreation ) ;
          groupMap[ key ] = group ;
        }

        group.ColumnSpan++ ;
        rowIndex = group.RowIndex + group.RowSpan ;
        prefix = key ;
        isFirstCreation = false ;
      }
    }

    return groupMap ;
  }

  private void CreateColumnDefinitions( System.Windows.Controls.DataGrid dataGrid, Grid header )
  {
    foreach ( var column in dataGrid.Columns ) {
      var columnDefinition = new ColumnDefinition() ;
      var isLastColumn = dataGrid.Columns.IndexOf( column ) == dataGrid.Columns.Count - 1 ;
      var binding = new Binding( nameof( column.ActualWidth ) ) { Source = column, Converter = new DoubleToDataGridLengthConverter() { Offset = isLastColumn ? BorderOffset : 0 } } ;
      BindingOperations.SetBinding( columnDefinition, ColumnDefinition.WidthProperty, binding ) ;
      header.ColumnDefinitions.Add( columnDefinition ) ;
    }
  }

  private void CreateRowDefinitions( Grid header, int numberOfRows )
  {
    for ( var level = 0; level < numberOfRows; level++ ) {
      header.RowDefinitions.Add( new RowDefinition { Height = GridLength.Auto } ) ;
    }
  }

  private GroupInfo CreateNewGroup( string groupName, int columnIndex, int rowIndex, int numberOfRows, int groupsLength, bool isFirstCreation )
  {
    var newGroup = new GroupInfo( groupName ) { ColumnIndex = columnIndex, RowIndex = rowIndex, RowSpan = isFirstCreation ? numberOfRows - groupsLength + 1 : 1 } ;

    return newGroup ;
  }
}