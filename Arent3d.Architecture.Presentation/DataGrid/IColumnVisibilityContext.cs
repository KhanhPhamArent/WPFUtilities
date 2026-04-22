using System.Collections.Generic;

namespace Arent3d.Architecture.Presentation.DataGrid;

public interface IColumnVisibilityContext
{
    Dictionary<int, bool> ColumnVisibility { get; }
}