using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Arent3d.Architecture.Presentation.DataGrid;

public class DataGridResizer : IDataGridResizer
{
    private const double BorderOffset = 2.0;

    public void ResizeLastColumn(System.Windows.Controls.DataGrid dataGrid, ScrollViewer scrollViewer, double actualWidth)
    {
        if (dataGrid.Columns.Count == 0)
            return;

        var lastVisibleColumn = dataGrid.Columns.LastOrDefault(c => c.Visibility == Visibility.Visible);
        if (lastVisibleColumn == null)
            return;

        var availableWidth = CalculateAvailableWidth(dataGrid, lastVisibleColumn, scrollViewer, actualWidth);
        var finalWidth = Math.Max(availableWidth, lastVisibleColumn.MinWidth);
        lastVisibleColumn.Width = new DataGridLength(finalWidth);
    }

    private double CalculateAvailableWidth(System.Windows.Controls.DataGrid dataGrid, DataGridColumn lastVisibleColumn, ScrollViewer scrollViewer, double actualWidth)
    {
        var otherVisibleWidth = dataGrid.Columns
            .Where(c => c.Visibility == Visibility.Visible && c != lastVisibleColumn)
            .Sum(x => x.ActualWidth);
        var scrollBarOffset = GetScrollBarOffset(scrollViewer);

        return actualWidth - otherVisibleWidth - BorderOffset - scrollBarOffset;
    }

    private double GetScrollBarOffset(ScrollViewer scrollViewer)
    {
        var verticalScrollBarOffset = scrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible ? GetScrollBarWidth() : 0;
        var horizontalScrollBarOffset = scrollViewer.ComputedHorizontalScrollBarVisibility == Visibility.Visible ? GetScrollBarHeight() : 0;

        // For DataGrid, we typically only need to account for vertical scrollbar
        // Horizontal scrollbar is usually handled differently
        return verticalScrollBarOffset;
    }

    private double GetScrollBarWidth()
    {
        return SystemParameters.VerticalScrollBarWidth;
    }

    private double GetScrollBarHeight()
    {
        return SystemParameters.HorizontalScrollBarHeight;
    }
}