using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Arent3d.Architecture.Presentation.DataGrid;

public partial class DataGridWrapper
{
    private const double DefaultHeaderThickness = 2.0;
    private const double DefaultHeaderTextMargin = 3.0;
    private const double DefaultHeaderTextMarginVertical = 5.0;
    // Get the DataGrid's built-in ScrollViewer
    public ScrollViewer? MainScrollViewer => _scrollViewerHandler?.DataGridScrollViewer;

    #region Dependency Properties

    public static readonly DependencyProperty IsHideHorizontalScrollBarProperty =
        DependencyProperty.Register(nameof(IsHideHorizontalScrollBar), typeof(bool), typeof(DataGridWrapper),
            new PropertyMetadata(false, OnIsHideHorizontalScrollBarChanged));

    public bool IsHideHorizontalScrollBar
    {
        get => (bool)GetValue(IsHideHorizontalScrollBarProperty);
        set => SetValue(IsHideHorizontalScrollBarProperty, value);
    }

    public static readonly DependencyProperty HeaderBorderColorProperty =
        DependencyProperty.Register(nameof(HeaderBorderColor), typeof(Brush), typeof(DataGridWrapper),
            new PropertyMetadata(Brushes.Black, OnHeaderPropertyChanged));

    public Brush HeaderBorderColor
    {
        get => (Brush)GetValue(HeaderBorderColorProperty);
        set => SetValue(HeaderBorderColorProperty, value);
    }

    public static readonly DependencyProperty HeaderBackgroundProperty =
        DependencyProperty.Register(nameof(HeaderBackground), typeof(Brush), typeof(DataGridWrapper),
            new PropertyMetadata(Brushes.White, OnHeaderPropertyChanged));

    public Brush HeaderBackground
    {
        get => (Brush)GetValue(HeaderBackgroundProperty);
        set => SetValue(HeaderBackgroundProperty, value);
    }

    public static readonly DependencyProperty HeaderForegroundProperty =
        DependencyProperty.Register(nameof(HeaderForeground), typeof(Brush), typeof(DataGridWrapper),
            new PropertyMetadata(Brushes.Black, OnHeaderPropertyChanged));

    public Brush HeaderForeground
    {
        get => (Brush)GetValue(HeaderForegroundProperty);
        set => SetValue(HeaderForegroundProperty, value);
    }

    public static readonly DependencyProperty HeaderThicknessProperty =
        DependencyProperty.Register(nameof(HeaderThickness), typeof(double), typeof(DataGridWrapper),
            new PropertyMetadata(DefaultHeaderThickness, OnHeaderPropertyChanged));

    public double HeaderThickness
    {
        get => (double)GetValue(HeaderThicknessProperty);
        set => SetValue(HeaderThicknessProperty, value);
    }

    public static readonly DependencyProperty HeaderTextMarginProperty =
        DependencyProperty.Register(nameof(HeaderTextMargin), typeof(Thickness), typeof(DataGridWrapper),
            new PropertyMetadata(new Thickness(DefaultHeaderTextMargin, DefaultHeaderTextMarginVertical,
                DefaultHeaderTextMargin, DefaultHeaderTextMarginVertical), OnHeaderPropertyChanged));

    public Thickness HeaderTextMargin
    {
        get => (Thickness)GetValue(HeaderTextMarginProperty);
        set => SetValue(HeaderTextMarginProperty, value);
    }

    public static readonly DependencyProperty HeaderMarginProperty =
        DependencyProperty.Register(nameof(HeaderMargin), typeof(Thickness), typeof(DataGridWrapper),
            new PropertyMetadata(new Thickness(1, 0, 0, 0), OnHeaderMarginChanged));

    public Thickness HeaderMargin
    {
        get => (Thickness)GetValue(HeaderMarginProperty);
        set => SetValue(HeaderMarginProperty, value);
    }

    public static readonly DependencyProperty FrozenColumnCountProperty =
        DependencyProperty.Register(nameof(FrozenColumnCount), typeof(int), typeof(DataGridWrapper),
            new PropertyMetadata(0, OnFrozenColumnCountChanged));

    public int FrozenColumnCount
    {
        get => (int)GetValue(FrozenColumnCountProperty);
        set => SetValue(FrozenColumnCountProperty, value);
    }

    public static readonly DependencyProperty GroupBackgroundsProperty =
        DependencyProperty.Register(nameof(GroupBackgrounds), typeof(GroupBackgroundCollection), typeof(DataGridWrapper),
            new PropertyMetadata(null, OnHeaderPropertyChanged));

    public GroupBackgroundCollection? GroupBackgrounds
    {
        get => (GroupBackgroundCollection?)GetValue(GroupBackgroundsProperty);
        set => SetValue(GroupBackgroundsProperty, value);
    }

    #endregion

    #region Private Fields

    private readonly IHeaderGridBuilder _headerGridBuilder;
    private readonly IHeaderContentBuilder _headerContentBuilder;
    private readonly IDataGridResizer _dataGridResizer;
    private int _numberOfRows;
    private int _numberOfColumns;
    private DataGridScrollViewerHandler? _scrollViewerHandler;
    private DataGridColumn? _placeholderColumn;

    #endregion

    #region Properties

    public System.Windows.Controls.DataGrid DataGrid
    {
        get => (System.Windows.Controls.DataGrid)ContentControl.Content;
        set
        {
            ContentControl.Content = value;
            value.HeadersVisibility = DataGridHeadersVisibility.None;
            value.HorizontalAlignment = HorizontalAlignment.Left;

            AddPlaceholderColumn(value);
            InitializeHeader();
            UpdateFrozenColumnWidth();
            SetupScrollSynchronization();
        }
    }

    #endregion

    #region Constructor

    public DataGridWrapper()
    {
        InitializeComponent();
        _headerGridBuilder = new HeaderGridBuilder();
        _headerContentBuilder = new HeaderContentBuilder();
        _dataGridResizer = new DataGridResizer();

        DataContextChanged += OnDataContextChanged;
        Unloaded += OnUnloaded;
    }

    #endregion/

    #region Event Handlers

    private static void OnIsHideHorizontalScrollBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGridWrapper dataGridWrapper)
        {
            var scrollViewer = dataGridWrapper._scrollViewerHandler?.DataGridScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            }
        }
    }

    private static void OnHeaderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGridWrapper dataGridWrapper)
        {
            dataGridWrapper.InitializeHeader();
        }
    }

    private static void OnHeaderMarginChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGridWrapper dataGridWrapper)
        {
            dataGridWrapper.Header.Margin = dataGridWrapper.HeaderMargin;
        }
    }

    private static void OnFrozenColumnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGridWrapper dataGridWrapper)
        {
            dataGridWrapper.InitializeHeader();
        }
    }


    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        InitializeHeader();
        if (DataContext is INotifyPropertyChanged viewModel)
        {
            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(IDataGridContext.ColumnHeaders) ||
                    args.PropertyName == nameof(IColumnVisibilityContext.ColumnVisibility))
                {
                    InitializeHeader();
                }
            };
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        Resize();
    }

    private void OnLayoutUpdated(object sender, EventArgs e)
    {
        Resize();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Cleanup();
    }

    private void Cleanup()
    {
        // Dispose the scroll viewer handler
        _scrollViewerHandler?.Dispose();
        _scrollViewerHandler = null;

        SizeChanged -= OnSizeChanged;
        Unloaded -= OnUnloaded;
    }

    #endregion

    #region Public Methods

    public void InitializeHeader()
    {
        if (DataContext is not IDataGridContext context || DataGrid is null)
            return;

        SetupEventHandlers();
        ClearHeaders();

        var realColumnCount = _placeholderColumn != null ? DataGrid.Columns.Count - 1 : DataGrid.Columns.Count;
        var hiddenColumns = GetHiddenLeafColumns(DataContext as IColumnVisibilityContext, realColumnCount, FrozenColumnCount);
        if (_placeholderColumn != null)
            hiddenColumns.Add(DataGrid.Columns.IndexOf(_placeholderColumn));
        ApplyColumnVisibility(hiddenColumns);

        var groups =
            _headerGridBuilder.BuildHeaderGrid(context, DataGrid, Header, FrozenHeader, FrozenColumnCount, hiddenColumns, out _numberOfRows, out _numberOfColumns);

        if (_numberOfRows == 0)
            return;

        SetupDataGridBorder();
        var groupInfos = _headerGridBuilder.CreateGroupInfos(groups, _numberOfRows, _numberOfColumns, FrozenColumnCount, hiddenColumns, GroupBackgrounds);
        _headerContentBuilder.CreateHeaderContent(groupInfos, Header, FrozenHeader, this, FrozenColumnCount);
        if (FrozenColumnCount > 0) FrozenHeader.Margin = new Thickness(0, 0, 1, 0);
    }

    #endregion

    #region Private Methods

    private void SetupEventHandlers()
    {
        SizeChanged += OnSizeChanged;
    }

    private void SetupScrollSynchronization()
    {
        // Dispose existing handler if any
        _scrollViewerHandler?.Dispose();

        // Create new handler
        _scrollViewerHandler = new DataGridScrollViewerHandler(DataGrid, HeaderScrollViewer);
        _scrollViewerHandler.SetupLayoutUpdatedHandler(OnLayoutUpdated);
        _scrollViewerHandler.SetupScrollSynchronization();
    }

    private void ClearHeaders()
    {
        Header.Children.Clear();
        Header.ColumnDefinitions.Clear();
        Header.RowDefinitions.Clear();
        FrozenHeader.Children.Clear();
        FrozenHeader.ColumnDefinitions.Clear();
        FrozenHeader.RowDefinitions.Clear();
    }

    private void SetupDataGridBorder()
    {
        ContentControl.BorderThickness = new Thickness(1, 0, 1, 1);
    }

    private void Resize()
    {
        var scrollViewer = _scrollViewerHandler?.DataGridScrollViewer;
        if (scrollViewer != null)
        {
            _dataGridResizer.SyncColumnWidths(DataGrid, scrollViewer, ActualWidth - 3);
            AdjustHeaderScrollViewerWidth(scrollViewer);
            ClampHorizontalOffset(scrollViewer);
            _scrollViewerHandler?.ExecuteSync();
        }
    }

    private void UpdateFrozenColumnWidth()
    {
        if (DataGrid == null || FrozenColumnCount <= 0)
            return;

        double frozenWidth = 0;
        for (int i = 0; i < FrozenColumnCount && i < DataGrid.Columns.Count; i++)
        {
            if (DataGrid.Columns[i].Visibility == Visibility.Visible)
                frozenWidth += DataGrid.Columns[i].ActualWidth;
        }

        FrozenColumnDefinition.Width = new GridLength(frozenWidth);
    }

    private void ClampHorizontalOffset(ScrollViewer scrollViewer)
    {
        var actualExtentWidth = DataGrid.Columns
            .Where(c => c.Visibility == Visibility.Visible)
            .Sum(c => c.ActualWidth);
        var maxOffset = Math.Max(0, actualExtentWidth - scrollViewer.ViewportWidth);
        if (scrollViewer.HorizontalOffset > maxOffset)
            scrollViewer.ScrollToHorizontalOffset(maxOffset);
    }

    private void AdjustHeaderScrollViewerWidth(ScrollViewer dataGridScrollViewer)
    {
        var extentWidth = dataGridScrollViewer.ExtentWidth;
        var viewportWidth = dataGridScrollViewer.ViewportWidth;
        if (extentWidth <= 0 || viewportWidth <= 0) return;
        // Pin the Header Grid's content width to the DataGrid's scrollable extent so that
        // HeaderScrollViewer.ScrollableWidth = extentWidth - viewportWidth = DataGrid.ScrollableWidth,
        // independent of whether column bindings in Header have settled after a visibility change.
        Header.Width = extentWidth;
        HeaderScrollViewer.Width = viewportWidth;
    }

    internal int NumberOfRows => _numberOfRows;
    internal int NumberOfColumns => _numberOfColumns;

    private static HashSet<int> GetHiddenLeafColumns(IColumnVisibilityContext? context, int totalColumns, int frozenColumnCount)
    {
        if (context?.ColumnVisibility is not { } dict)
            return [];

        return dict
            .Where(kv => !kv.Value && kv.Key >= frozenColumnCount && kv.Key < totalColumns)
            .Select(kv => kv.Key)
            .ToHashSet();
    }

    private void ApplyColumnVisibility(HashSet<int> hiddenColumns)
    {
        var placeholderIndex = _placeholderColumn != null ? DataGrid.Columns.IndexOf(_placeholderColumn) : -1;

        for (var i = 0; i < DataGrid.Columns.Count; i++)
        {
            if (i == placeholderIndex) continue;
            var newVis = hiddenColumns.Contains(i) ? Visibility.Collapsed : Visibility.Visible;
            if (DataGrid.Columns[i].Visibility != newVis)
                DataGrid.Columns[i].Visibility = newVis;
        }

        UpdatePlaceholderVisibility(placeholderIndex);
        Resize();
    }

    private void UpdatePlaceholderVisibility(int placeholderIndex)
    {
        if (_placeholderColumn == null || placeholderIndex < 0) return;

        var anyNonFrozenVisible = DataGrid.Columns
            .Take(placeholderIndex)
            .Skip(FrozenColumnCount)
            .Any(c => c.Visibility == Visibility.Visible);

        _placeholderColumn.Visibility = anyNonFrozenVisible ? Visibility.Collapsed : Visibility.Visible;
    }

    private void AddPlaceholderColumn(System.Windows.Controls.DataGrid dataGrid)
    {
        _placeholderColumn = new DataGridTextColumn
        {
            Visibility = Visibility.Collapsed,
            CanUserResize = false,
            CanUserSort = false,
            CanUserReorder = false,
            IsReadOnly = true,
        };
        dataGrid.Columns.Add(_placeholderColumn);
    }

    #endregion
}