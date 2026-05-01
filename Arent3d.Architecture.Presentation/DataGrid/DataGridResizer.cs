using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Arent3d.Architecture.Presentation.DataGrid;

public class DataGridResizer : IDataGridResizer
{
    private const double BorderOffset = 2.0;
    private readonly Dictionary<DataGridColumn, double> _originalWidths = new();
    private DataGridColumn? _previousLastColumn;

    public void SyncColumnWidths(System.Windows.Controls.DataGrid dataGrid, ScrollViewer scrollViewer, double actualWidth)
    {
        if (dataGrid.Columns.Count == 0)
            return;

        var visibleColumns = dataGrid.Columns.Where(c => c.Visibility == Visibility.Visible).ToList();
        if (visibleColumns.Count == 0)
            return;

        var lastVisibleColumn = visibleColumns.Last();

        // Store original width of last column on first encounter
        if (!_originalWidths.ContainsKey(lastVisibleColumn))
            _originalWidths[lastVisibleColumn] = lastVisibleColumn.ActualWidth;

        // When last column changes, restore the previous last column to its original width
        if (_previousLastColumn != null
            && _previousLastColumn != lastVisibleColumn
            && _previousLastColumn.Visibility == Visibility.Visible
            && _originalWidths.TryGetValue(_previousLastColumn, out var originalWidth))
        {
            _previousLastColumn.Width = new DataGridLength(originalWidth);
        }

        _previousLastColumn = lastVisibleColumn;

        // Resize last column to fill available space
        var availableWidth = CalculateAvailableWidth(dataGrid, lastVisibleColumn, scrollViewer, actualWidth);
        var finalWidth = Math.Max(availableWidth, _originalWidths[lastVisibleColumn]);
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
        return scrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible ? GetScrollBarWidth() : 0;
    }

    private double GetScrollBarWidth()
    {
        return SystemParameters.VerticalScrollBarWidth;
    }
}