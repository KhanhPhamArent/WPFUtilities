namespace Arent3d.Architecture.Presentation.DataGrid ;

public class GroupInfo
{
  public GroupInfo( string name )
  {
    Name = name ;
  }

  public string Name { get ; }
  public int ColumnIndex { get ; set ; }
  public int RowIndex { get ; set ; }
  public int ColumnSpan { get ; set ; }
  public int RowSpan { get ; set ; }
} 