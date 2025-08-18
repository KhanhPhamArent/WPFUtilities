using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Arent3d.Architecture.Presentation.Converters;

namespace Arent3d.Architecture.Presentation.DataGrid;

public class HeaderGridBuilder : IHeaderGridBuilder
{
    private const double BorderOffset = 0;

    public string[][] BuildHeaderGrid(IDataGridContext context, System.Windows.Controls.DataGrid dataGrid, Grid header,
        Grid frozenHeader, int frozenColumnCount,
        out int numberOfRows, out int numberOfColumns)
    {
        var groupList = context.ColumnHeaders;
        numberOfRows = groupList.Where(x => x is not null).Max(x => x!.Length);
        numberOfColumns = dataGrid.Columns.Count;

        CreateColumnDefinitions(dataGrid, header, frozenHeader, frozenColumnCount);
        CreateRowDefinitions(header, frozenHeader, numberOfRows);

        return groupList;
    }

    public Dictionary<string, GroupInfo> CreateGroupInfos(string[][] groupList, int numberOfRows, int numberOfColumns,
        int frozenColumnCount)
    {
        var groupMap = new Dictionary<string, GroupInfo>();

        for (var columnIndex = 0; columnIndex < groupList.Length; columnIndex++)
        {
            var groups = groupList[columnIndex];
            var isFirstCreation = true;
            var rowIndex = 0;
            var prefix = string.Empty;

            for (var index = 0; index < groups.Length; index++)
            {
                var groupName = groups[index];
                var key = prefix + "." + groupName;

                if (!groupMap.TryGetValue(key, out var group))
                {
                    group = CreateNewGroup(groupName, columnIndex, rowIndex, numberOfRows, groups.Length,
                        isFirstCreation, frozenColumnCount);
                    groupMap[key] = group;
                }

                group.ColumnSpan++;
                rowIndex = group.RowIndex + group.RowSpan;
                prefix = key;
                isFirstCreation = false;
            }
        }

        return groupMap;
    }

    private void CreateColumnDefinitions(System.Windows.Controls.DataGrid dataGrid, Grid header, Grid frozenHeader,
        int frozenColumnCount)
    {
        // Create column definitions for frozen header
        CreateColumnDefinitionsForRange(dataGrid, frozenHeader, 0, frozenColumnCount, frozenColumnCount - 1);

        // Create column definitions for scrollable header
        CreateColumnDefinitionsForRange(dataGrid, header, frozenColumnCount, dataGrid.Columns.Count,
            dataGrid.Columns.Count - 1);
    }

    private void CreateColumnDefinitionsForRange(System.Windows.Controls.DataGrid dataGrid, Grid targetGrid,
        int startIndex, int endIndex, int lastColumnIndex)
    {
        for (var i = startIndex; i < endIndex && i < dataGrid.Columns.Count; i++)
        {
            var column = dataGrid.Columns[i];
            var columnDefinition = new ColumnDefinition();
            var isLastColumn = i == lastColumnIndex;
            var binding = new Binding(nameof(column.ActualWidth))
            {
                Source = column,
                Converter = new DoubleToDataGridLengthConverter() { Offset = isLastColumn ? BorderOffset : 0 }
            };
            BindingOperations.SetBinding(columnDefinition, ColumnDefinition.WidthProperty, binding);
            targetGrid.ColumnDefinitions.Add(columnDefinition);
        }
    }

    private void CreateRowDefinitions(Grid header, Grid frozenHeader, int numberOfRows)
    {
        for (var level = 0; level < numberOfRows - 1; level++)
        {
            header.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            frozenHeader.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }
        
        header.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        frozenHeader.RowDefinitions.Add(new RowDefinition { Height =  new GridLength(1, GridUnitType.Star) });
    }

    private GroupInfo CreateNewGroup(string groupName, int columnIndex, int rowIndex, int numberOfRows,
        int groupsLength, bool isFirstCreation, int frozenColumnCount)
    {
        var rowSpan = CalculateRowSpan(isFirstCreation, numberOfRows, groupsLength);
        var newGroup = new GroupInfo(groupName)
        {
            ColumnIndex = columnIndex,
            RowIndex = rowIndex,
            RowSpan = rowSpan,
            IsFrozen = columnIndex < frozenColumnCount
        };

        return newGroup;
    }

    private int CalculateRowSpan(bool isFirstCreation, int numberOfRows, int groupsLength)
    {
        return isFirstCreation ? numberOfRows - groupsLength + 1 : 1;
    }
}