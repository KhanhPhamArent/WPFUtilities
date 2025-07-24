using System.Windows.Controls ;

namespace Arent3d.Architecture.Presentation.DataGrid ;

public interface IHeaderContentBuilder
{
  void CreateHeaderContent( Dictionary<string, GroupInfo> groupInfos, Grid header, DataGridWrapper wrapper ) ;
} 