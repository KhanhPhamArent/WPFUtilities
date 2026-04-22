using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Arent3d.Architecture.Presentation.Converters;

namespace Arent3d.Architecture.Presentation.DataGrid;

public class HeaderGridBuilder : IHeaderGridBuilder
{
    private const double BorderOffset = 0;

    public string[][] BuildHeaderGrid(IDataGridContext context, System.Windows.Controls.DataGrid dataGrid, Grid header,
        Grid frozenHeader, int frozenColumnCount, HashSet<int> hiddenColumns,
        out int numberOfRows, out int numberOfColumns)
    {
        var groupList = context.ColumnHeaders;
        numberOfRows = groupList.Where(x => x is not null).Max(x => x!.Length);
        numberOfColumns = dataGrid.Columns.Count;

        CreateColumnDefinitions(dataGrid, header, frozenHeader, frozenColumnCount, hiddenColumns);
        CreateRowDefinitions(header, frozenHeader, numberOfRows);

        return groupList;
    }

    public Dictionary<string, GroupInfo> CreateGroupInfos(string[][] groupList, int numberOfRows, int numberOfColumns,
        int frozenColumnCount, HashSet<int> hiddenColumns, GroupBackgroundCollection? groupBackgrounds)
    {
        var backgroundLookup = groupBackgrounds?.ToDictionary(e => e.GroupIndex, e => e.Background);
        var groupMap = new Dictionary<string, GroupInfo>();
        var openGroups = new Dictionary<string, GroupInfo>();
        var topGroupKeys = new Dictionary<string, int>();
        var topGroupCounter = 0;

        for (var columnIndex = 0; columnIndex < groupList.Length; columnIndex++)
        {
            var strGroups = groupList[columnIndex];
            var groups = new List<GroupInfo>();
            var isFirstCreation = true;
            var rowIndex = 0;
            var prefix = string.Empty;
            int? topLevelGroupIdx = null;

            for (var index = 0; index < strGroups.Length; index++)
            {
                var groupName = strGroups[index];
                var key = prefix + "." + groupName;

                if (index == 0)
                {
                    if (!topGroupKeys.TryGetValue(key, out var existingIdx))
                    {
                        existingIdx = topGroupCounter++;
                        topGroupKeys[key] = existingIdx;
                    }
                    topLevelGroupIdx = existingIdx;
                }

                GroupInfo group;
                if (openGroups.TryGetValue(key, out var openGroup) && IsAdjacentTo(openGroup, columnIndex))
                {
                    group = openGroup;
                }
                else
                {
                    group = CreateNewGroup(groupName, columnIndex, rowIndex, numberOfRows, strGroups.Length,
                        isFirstCreation, frozenColumnCount);
                    group.Background = ResolveBackground(topLevelGroupIdx, backgroundLookup);
                    groupMap[key + "@" + columnIndex] = group;
                    openGroups[key] = group;
                }

                groups.Add(group);
                group.ColumnSpan++;
                rowIndex = group.RowIndex + group.RowSpan;
                prefix = key;
                isFirstCreation = false;
            }

            if (!groups.Any()) continue;

            var remainingRows = numberOfRows - groups.Sum(x => x.RowSpan);
            if (remainingRows > 0)
            {
                groups.Last().RowSpan += remainingRows;
            }
        }

        if (hiddenColumns.Count > 0)
            ApplyVisibility(groupMap, numberOfRows, hiddenColumns);
        return groupMap;
    }

    private static void ApplyVisibility(Dictionary<string, GroupInfo> groupMap, int numberOfRows, HashSet<int> hiddenColumns)
    {
        foreach (var group in groupMap.Values)
        {
            if (group.IsFrozen) continue;

            var isLeaf = group.RowIndex + group.RowSpan >= numberOfRows;
            group.IsVisible = isLeaf
                ? !hiddenColumns.Contains(group.ColumnIndex)
                : Enumerable.Range(group.ColumnIndex, Math.Max(group.ColumnSpan, 1)).Any(col => !hiddenColumns.Contains(col));
        }
    }

    private void CreateColumnDefinitions(System.Windows.Controls.DataGrid dataGrid, Grid header, Grid frozenHeader,
        int frozenColumnCount, HashSet<int> hiddenColumns)
    {
        CreateColumnDefinitionsForRange(dataGrid, frozenHeader, 0, frozenColumnCount, frozenColumnCount - 1, hiddenColumns);
        CreateColumnDefinitionsForRange(dataGrid, header, frozenColumnCount, dataGrid.Columns.Count,
            dataGrid.Columns.Count - 1, hiddenColumns);
    }

    private void CreateColumnDefinitionsForRange(System.Windows.Controls.DataGrid dataGrid, Grid targetGrid,
        int startIndex, int endIndex, int lastColumnIndex, HashSet<int> hiddenColumns)
    {
        for (var i = startIndex; i < endIndex && i < dataGrid.Columns.Count; i++)
        {
            var columnDefinition = new ColumnDefinition();
            if (hiddenColumns.Contains(i))
            {
                columnDefinition.Width = new GridLength(0);
            }
            else
            {
                var column = dataGrid.Columns[i];
                var isLastColumn = i == lastColumnIndex;
                var binding = new Binding(nameof(column.ActualWidth))
                {
                    Source = column,
                    Converter = new DoubleToDataGridLengthConverter() { Offset = isLastColumn ? BorderOffset : 0 }
                };
                BindingOperations.SetBinding(columnDefinition, ColumnDefinition.WidthProperty, binding);
            }
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
        frozenHeader.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
    }

    private GroupInfo CreateNewGroup(string groupName, int columnIndex, int rowIndex, int numberOfRows,
        int groupsLength, bool isFirstCreation, int frozenColumnCount)
    {
        var rowSpan = CalculateRowSpan(isFirstCreation, numberOfRows, groupsLength);
        return new GroupInfo(groupName)
        {
            ColumnIndex = columnIndex,
            RowIndex = rowIndex,
            RowSpan = rowSpan,
            IsFrozen = columnIndex < frozenColumnCount
        };
    }

    private static bool IsAdjacentTo(GroupInfo group, int columnIndex)
        => group.ColumnIndex + group.ColumnSpan == columnIndex;

    private static Brush? ResolveBackground(int? topLevelGroupIdx, Dictionary<int, Brush?>? backgroundLookup)
    {
        return topLevelGroupIdx.HasValue
            && backgroundLookup != null
            && backgroundLookup.TryGetValue(topLevelGroupIdx.Value, out var bg)
            ? bg : null;
    }

    private int CalculateRowSpan(bool isFirstCreation, int numberOfRows, int groupsLength)
    {
        return isFirstCreation ? numberOfRows - groupsLength + 1 : 1;
    }
}
