using System.Windows.Controls ;

namespace Arent3d.Architecture.Presentation.DataGrid ;

public interface IHeaderGridBuilder
{
  string[][] BuildHeaderGrid( IDataGridContext context, System.Windows.Controls.DataGrid dataGrid, Grid header, Grid frozenHeader, int frozenColumnCount,
    out int numberOfRows, out int numberOfColumns ) ;

  Dictionary<string, GroupInfo> CreateGroupInfos( string[][] groupList, int numberOfRows, int numberOfColumns, int frozenColumnCount ) ;
} 