using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace Arent3d.Architecture.Presentation.DataGrid;

public interface IHeaderGridBuilder
{
    string[][] BuildHeaderGrid(IDataGridContext context, System.Windows.Controls.DataGrid dataGrid, Grid header, Grid frozenHeader, int frozenColumnCount, HashSet<int> hiddenColumns,
      out int numberOfRows, out int numberOfColumns);

    Dictionary<string, GroupInfo> CreateGroupInfos(string[][] groupList, int numberOfRows, int numberOfColumns, int frozenColumnCount, HashSet<int> hiddenColumns, GroupBackgroundCollection? groupBackgrounds);
}
