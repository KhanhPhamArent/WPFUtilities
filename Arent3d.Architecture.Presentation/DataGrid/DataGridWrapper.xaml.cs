using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Data ;
using System.Windows.Input ;
using System.Windows.Media ;
using Arent3d.Architecture.Presentation.Converters ;

namespace Arent3d.Architecture.Presentation.DataGrid ;

public partial class DataGridWrapper
{
  public ScrollViewer MainScrollViewer => ScrollViewer ;

  public static readonly DependencyProperty IsHideHorizontalScrollBarProperty = DependencyProperty.Register( nameof( IsHideHorizontalScrollBar ), typeof( bool ), typeof( DataGridWrapper ), new PropertyMetadata( false, IsHideHorizontalScrollBarChangedCallback ) ) ;

  private static void IsHideHorizontalScrollBarChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
  {
    if ( d is not DataGridWrapper dataGridWrapper ) return ;
    dataGridWrapper.MainScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden ;
  }

  public bool IsHideHorizontalScrollBar
  {
    get => (bool)GetValue( IsHideHorizontalScrollBarProperty ) ;
    set => SetValue( IsHideHorizontalScrollBarProperty, value ) ;
  }

  public static readonly DependencyProperty HeaderBorderColorProperty = DependencyProperty.Register( nameof( HeaderBorderColor ), typeof( Brush ), typeof( DataGridWrapper ), new PropertyMetadata( Brushes.Black, PropertyChangedCallback ) ) ;

  public Brush HeaderBorderColor
  {
    get => (Brush)GetValue( HeaderBorderColorProperty ) ;
    set => SetValue( HeaderBorderColorProperty, value ) ;
  }

  public static readonly DependencyProperty HeaderBackgroundProperty = DependencyProperty.Register( nameof( HeaderBackground ), typeof( Brush ), typeof( DataGridWrapper ), new PropertyMetadata( Brushes.White, PropertyChangedCallback ) ) ;

  public Brush HeaderBackground
  {
    get => (Brush)GetValue( HeaderBackgroundProperty ) ;
    set => SetValue( HeaderBackgroundProperty, value ) ;
  }

  public static readonly DependencyProperty HeaderForegroundProperty = DependencyProperty.Register( nameof( HeaderForeground ), typeof( Brush ), typeof( DataGridWrapper ), new PropertyMetadata( Brushes.Black, PropertyChangedCallback ) ) ;

  public Brush HeaderForeground
  {
    get => (Brush)GetValue( HeaderForegroundProperty ) ;
    set => SetValue( HeaderForegroundProperty, value ) ;
  }

  public static readonly DependencyProperty HeaderThicknessProperty = DependencyProperty.Register( nameof( HeaderThickness ), typeof( double ), typeof( DataGridWrapper ), new PropertyMetadata( 1d, PropertyChangedCallback ) ) ;

  public double HeaderThickness
  {
    get => (double)GetValue( HeaderThicknessProperty ) ;
    set => SetValue( HeaderThicknessProperty, value ) ;
  }


  public static readonly DependencyProperty HeaderTextMarginProperty = DependencyProperty.Register( nameof( HeaderTextMargin ), typeof( Thickness ), typeof( DataGridWrapper ), new PropertyMetadata( new Thickness( 3, 5, 3, 5 ), PropertyChangedCallback ) ) ;

  public Thickness HeaderTextMargin
  {
    get => (Thickness)GetValue( HeaderTextMarginProperty ) ;
    set => SetValue( HeaderTextMarginProperty, value ) ;
  }

  public static readonly DependencyProperty HeaderMarginProperty = DependencyProperty.Register( nameof( HeaderMargin ), typeof( Thickness ), typeof( DataGridWrapper ), new PropertyMetadata( new Thickness( 1, 0, 0, 0 ), HeaderMarginPropertyChangedCallback ) ) ;

  private static void HeaderMarginPropertyChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
  {
    if ( d is not DataGridWrapper dataGridWrapper ) return ;
    dataGridWrapper.Header.Margin = dataGridWrapper.HeaderMargin ;
  }

  public Thickness HeaderMargin
  {
    get => (Thickness)GetValue( HeaderMarginProperty ) ;
    set => SetValue( HeaderMarginProperty, value ) ;
  }

  private static void PropertyChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
  {
    if ( d is not DataGridWrapper dataGridWrapper ) return ;
    dataGridWrapper.InitializeHeader() ;
  }

  private int _numberOfRow ;
  private int _numberOfColumn ;

  public System.Windows.Controls.DataGrid DataGrid
  {
    get => (System.Windows.Controls.DataGrid)ContentControl.Content ;
    set
    {
      ContentControl.Content = value ;
      value.HeadersVisibility = DataGridHeadersVisibility.None ;
      InitializeHeader() ;
    }
  }

  public DataGridWrapper()
  {
    InitializeComponent() ;
    DataContextChanged += ( _, _ ) => InitializeHeader() ;
  }

  private void InitializeHeader()
  {
    if ( DataContext is not IDataGridContext context || DataGrid is null ) return ;
    DataGrid.PreviewMouseWheel += DataGridOnPreviewMouseWheel ;
    SizeChanged += ( _, _ ) => Resize() ;
    MainScrollViewer.LayoutUpdated += ( _, _ ) => Resize() ;
    Header.Children.Clear() ;
    Header.ColumnDefinitions.Clear() ;
    Header.RowDefinitions.Clear() ;
    var groups = SeparateGroupHeaderGrid( context ) ;
    if ( _numberOfRow == 0 ) return ;
    DataGrid.BorderThickness = new Thickness( 1, 0, 1, 1 ) ;
    var groupInfos = SetGroupInfo( groups ) ;
    SetHeaderGroupContent( groupInfos ) ;
  }

  private void DataGridOnPreviewMouseWheel( object sender, MouseWheelEventArgs e )
  {
    MainScrollViewer.ScrollToVerticalOffset( MainScrollViewer.VerticalOffset - e.Delta ) ;
    e.Handled = true ;
  }

  private void Resize()
  {
    var size = ActualWidth - DataGrid.Columns.Take( DataGrid.Columns.Count - 1 ).Sum( x => x.ActualWidth ) - 2 - ( MainScrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible ? 20 : 0 ) ;
    var lastColumn = DataGrid.Columns.Last() ;
    lastColumn.Width = size > 0 ? new DataGridLength( size ) : new DataGridLength( lastColumn.MinWidth ) ;
  }

  private string[][] SeparateGroupHeaderGrid( IDataGridContext context )
  {
    var groupList = context.ColumnHeaders ;
    _numberOfRow = groupList.Where( x => x is not null ).Max( x => x!.Length ) ;
    _numberOfColumn = DataGrid.Columns.Count ;

    foreach ( var column in DataGrid.Columns ) {
      var columnDefinition = new ColumnDefinition() ;
      var binding = new Binding( nameof( column.ActualWidth ) ) { Source = column, Converter = new DoubleToDataGridLengthConverter() } ;
      BindingOperations.SetBinding( columnDefinition, ColumnDefinition.WidthProperty, binding ) ;
      Header.ColumnDefinitions.Add( columnDefinition ) ;
    }

    for ( var level = 0 ; level < _numberOfRow ; level++ ) {
      Header.RowDefinitions.Add( new RowDefinition { Height = GridLength.Auto } ) ;
    }

    return groupList ;
  }

  private Dictionary<string, GroupInfo> SetGroupInfo( string[][] groupList )
  {
    var groupMap = new Dictionary<string, GroupInfo>() ;

    for ( var columnIndex = 0 ; columnIndex < groupList.Length ; columnIndex++ ) {
      var groups = groupList[ columnIndex ] ;
      var isFirstCreation = true ;
      var rowIndex = 0 ;
      var prefix = string.Empty ;
      for ( var index = 0 ; index < groups.Length ; index++ ) {
        var groupName = groups[ index ] ;
        var key = prefix + "." + groupName ;
        var group = ! groupMap.TryGetValue( key, out var value ) ? CreateNewGroup() : value ;

        group.ColumnSpan++ ;
        rowIndex = group.RowIndex + group.RowSpan ;
        prefix = key ;
        continue ;

        GroupInfo CreateNewGroup()
        {
          var newGroup = new GroupInfo( groupName ) ;
          groupMap[ key ] = newGroup ;
          newGroup.ColumnIndex = columnIndex ;
          newGroup.RowIndex = rowIndex ;
          newGroup.RowSpan = isFirstCreation ? _numberOfRow - groups.Length + 1 : 1 ;
          isFirstCreation = false ;
          return newGroup ;
        }
      }
    }

    return groupMap ;
  }

  private void SetHeaderGroupContent( Dictionary<string, GroupInfo> groupInfos )
  {
    foreach ( var group in groupInfos.Values ) {
      var textBlock = CreateHeaderText( group ) ;
      var border = CreateHeaderBorder( group ) ;
      border.Child = textBlock ;
      Header.Children.Add( border ) ;
    }
  }

  private TextBlock CreateHeaderText( GroupInfo groupInfo )
  {
    return new TextBlock()
    {
      Text = groupInfo.Name,
      VerticalAlignment = VerticalAlignment.Center,
      HorizontalAlignment = HorizontalAlignment.Center,
      TextAlignment = TextAlignment.Center,
      TextWrapping = TextWrapping.Wrap,
      Foreground = HeaderForeground,
      Margin = HeaderTextMargin
    } ;
  }

  private Border CreateHeaderBorder( GroupInfo groupInfo )
  {
    var borderThickness = HeaderThickness / 2 ;
    double left = borderThickness, right = borderThickness, bot = borderThickness, top = borderThickness ;
    if ( groupInfo.ColumnIndex == 0 ) left = borderThickness * 2 ;
    if ( groupInfo.ColumnIndex + ( groupInfo.ColumnSpan == 0 ? 1 : groupInfo.ColumnSpan ) == _numberOfColumn ) right = borderThickness * 2 ;
    if ( groupInfo.RowIndex == 0 ) top = borderThickness * 2 ;
    if ( groupInfo.RowIndex + ( groupInfo.RowSpan == 0 ? 1 : groupInfo.RowSpan ) == _numberOfRow ) bot = borderThickness * 2 ;
    var border = new Border { BorderBrush = HeaderBorderColor, BorderThickness = new Thickness( left, top, right, bot ), Background = HeaderBackground } ;
    Grid.SetRow( border, groupInfo.RowIndex ) ;
    if ( groupInfo.RowSpan > 0 ) Grid.SetRowSpan( border, groupInfo.RowSpan ) ;
    Grid.SetColumn( border, groupInfo.ColumnIndex ) ;
    if ( groupInfo.ColumnSpan > 0 ) Grid.SetColumnSpan( border, groupInfo.ColumnSpan ) ;
    return border ;
  }
}

public class GroupInfo( string name )
{
  public string Name { get ; } = name ;
  public int ColumnIndex { get ; set ; }
  public int RowIndex { get ; set ; }
  public int ColumnSpan { get ; set ; }
  public int RowSpan { get ; set ; }
}