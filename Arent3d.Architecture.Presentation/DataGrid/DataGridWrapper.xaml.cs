using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Arent3d.Architecture.Presentation.DataGrid;

public partial class DataGridWrapper
{
    private const double DefaultHeaderThickness = 1.0;
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

    #endregion

    #region Private Fields

    private readonly IHeaderGridBuilder _headerGridBuilder;
    private readonly IHeaderContentBuilder _headerContentBuilder;
    private readonly IDataGridResizer _dataGridResizer;
    private int _numberOfRows;
    private int _numberOfColumns;
    private DataGridScrollViewerHandler? _scrollViewerHandler;

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

            InitializeHeader();
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

    #endregion

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

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        InitializeHeader();
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
        ClearHeader();

        var groups =
            _headerGridBuilder.BuildHeaderGrid(context, DataGrid, Header, out _numberOfRows, out _numberOfColumns);

        if (_numberOfRows == 0)
            return;

        SetupDataGridBorder();
        var groupInfos = _headerGridBuilder.CreateGroupInfos(groups, _numberOfRows, _numberOfColumns);
        _headerContentBuilder.CreateHeaderContent(groupInfos, Header, this);
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

    private void ClearHeader()
    {
        Header.Children.Clear();
        Header.ColumnDefinitions.Clear();
        Header.RowDefinitions.Clear();
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
            AdjustHeaderScrollViewerWidth(scrollViewer);
            _dataGridResizer.ResizeLastColumn(DataGrid, scrollViewer, ActualWidth - 3);
        }
    }

    private void AdjustHeaderScrollViewerWidth(ScrollViewer dataGridScrollViewer)
    {
        if (dataGridScrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible)
        {
            var scrollBarWidth = SystemParameters.VerticalScrollBarWidth;
            HeaderScrollViewer.Width = ActualWidth - scrollBarWidth - 3;
        }
        else
        {
            HeaderScrollViewer.Width = ActualWidth;
        }
    }

    internal int NumberOfRows => _numberOfRows;
    internal int NumberOfColumns => _numberOfColumns;

    #endregion
}