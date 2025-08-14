using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Media ;
using System.Windows.Threading ;
using Arent3d.Architecture.Presentation.Behaviors ;

namespace Arent3d.Architecture.Presentation.DataGrid ;

public class DataGridScrollViewerHandler
{
  private readonly System.Windows.Controls.DataGrid _dataGrid ;
  private readonly ScrollViewer _headerScrollViewer ;
  private DispatcherTimer? _scrollViewerDetectionTimer ;
  private ScrollViewer? _cachedDataGridScrollViewer ;
  private bool _scrollSynchronizationSetup ;
  private bool _isDisposed ;
  private SyncHorizontalScrollBehavior? _syncBehavior ;

  public DataGridScrollViewerHandler( System.Windows.Controls.DataGrid dataGrid, ScrollViewer headerScrollViewer )
  {
    _dataGrid = dataGrid ?? throw new ArgumentNullException( nameof( dataGrid ) ) ;
    _headerScrollViewer = headerScrollViewer ?? throw new ArgumentNullException( nameof( headerScrollViewer ) ) ;
  }

  public ScrollViewer? DataGridScrollViewer => GetDataGridScrollViewer() ;

  public void SetupScrollSynchronization()
  {
    if ( _isDisposed ) return ;

    // Reset synchronization flag
    _scrollSynchronizationSetup = false ;
    _cachedDataGridScrollViewer = null ;
    
    // Stop any existing timer
    _scrollViewerDetectionTimer?.Stop() ;
    
    // Try to setup immediately
    if ( _dataGrid.IsLoaded )
    {
      SetupScrollSynchronizationInternal() ;
    }
    else
    {
      _dataGrid.Loaded += OnDataGridLoaded ;
    }
  }

  public void RefreshDetection()
  {
    if ( _isDisposed ) return ;

    _scrollSynchronizationSetup = false ;
    _cachedDataGridScrollViewer = null ;
    
    // Clean up existing sync behavior
    _syncBehavior?.Detach() ;
    _syncBehavior = null ;
    
    SetupScrollSynchronizationInternal() ;
  }

  public void Dispose()
  {
    if ( _isDisposed ) return ;

    _isDisposed = true ;
    
    // Stop the detection timer
    _scrollViewerDetectionTimer?.Stop() ;
    _scrollViewerDetectionTimer = null ;
    
    // Clean up sync behavior
    _syncBehavior?.Detach() ;
    _syncBehavior = null ;
    
    // Remove event handlers
    _dataGrid.Loaded -= OnDataGridLoaded ;
    _dataGrid.LayoutUpdated -= OnDataGridLayoutUpdated ;
    
    // Remove layout updated event from ScrollViewer
    if ( _cachedDataGridScrollViewer != null )
    {
      _cachedDataGridScrollViewer.LayoutUpdated -= OnLayoutUpdated ;
    }
  }

  private void OnDataGridLoaded( object sender, RoutedEventArgs e )
  {
    _dataGrid.Loaded -= OnDataGridLoaded ;
    SetupScrollSynchronizationInternal() ;
  }

  private void OnDataGridLayoutUpdated( object sender, EventArgs e )
  {
    if ( _isDisposed ) return ;

    // Check if ScrollViewer is now available
    var scrollViewer = GetDataGridScrollViewer() ;
    if ( scrollViewer != null && !_scrollSynchronizationSetup )
    {
      SetupScrollSynchronizationInternal() ;
    }
  }

  private void SetupScrollSynchronizationInternal()
  {
    if ( _isDisposed ) return ;

    var dataGridScrollViewer = GetDataGridScrollViewer() ;
    if ( dataGridScrollViewer != null && !_scrollSynchronizationSetup )
    {
      _cachedDataGridScrollViewer = dataGridScrollViewer ;
      _cachedDataGridScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto ;
      _cachedDataGridScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto ;
      SyncScrollViewers( dataGridScrollViewer, _headerScrollViewer ) ;
      
      // Setup layout updated event
      dataGridScrollViewer.LayoutUpdated += OnLayoutUpdated ;
      
      _scrollSynchronizationSetup = true ;
      
      // Stop the detection timer if it's running
      _scrollViewerDetectionTimer?.Stop() ;
    }
    else if ( !_scrollSynchronizationSetup )
    {
      // Start a timer to periodically check for the ScrollViewer
      StartScrollViewerDetectionTimer() ;
    }
  }

  private void StartScrollViewerDetectionTimer()
  {
    if ( _isDisposed ) return ;

    _scrollViewerDetectionTimer?.Stop() ;
    
    _scrollViewerDetectionTimer = new DispatcherTimer( TimeSpan.FromMilliseconds( 100 ), DispatcherPriority.Background, 
      ( sender, e ) =>
      {
        if ( _isDisposed ) return ;
        
        var scrollViewer = GetDataGridScrollViewer() ;
        if ( scrollViewer != null )
        {
          SetupScrollSynchronizationInternal() ;
        }
      }, Dispatcher.CurrentDispatcher ) ;
    
    _scrollViewerDetectionTimer.Start() ;
  }

  private void SyncScrollViewers( ScrollViewer source, ScrollViewer target )
  {
    // Clean up existing behavior if any
    _syncBehavior?.Detach() ;
    
    // Create and configure the sync behavior
    _syncBehavior = new SyncHorizontalScrollBehavior
    {
      TargetScrollViewer = target,
      DataGrid = _dataGrid
    } ;
    
    // Attach the behavior to the source ScrollViewer
    _syncBehavior.Attach( source ) ;
  }

  private ScrollViewer? GetDataGridScrollViewer()
  {
    if ( _dataGrid == null ) return null ;
    
    // Return cached ScrollViewer if available
    if ( _cachedDataGridScrollViewer != null )
      return _cachedDataGridScrollViewer ;
    
    // Try to find the ScrollViewer in the DataGrid's visual tree
    var scrollViewer = FindScrollViewer( _dataGrid ) ;
    if ( scrollViewer != null )
    {
      _cachedDataGridScrollViewer = scrollViewer ;
    }
    
    return scrollViewer ;
  }

  private ScrollViewer? FindScrollViewer( DependencyObject element )
  {
    for ( int i = 0 ; i < VisualTreeHelper.GetChildrenCount( element ) ; i++ )
    {
      var child = VisualTreeHelper.GetChild( element, i ) ;
      if ( child is ScrollViewer scrollViewer )
      {
        return scrollViewer ;
      }
      
      var result = FindScrollViewer( child ) ;
      if ( result != null )
      {
        return result ;
      }
    }
    return null ;
  }

  public void SetupLayoutUpdatedHandler( Action<object, EventArgs> layoutUpdatedHandler )
  {
    if ( _isDisposed ) return ;

    // Listen for DataGrid content changes that might create the ScrollViewer
    _dataGrid.LayoutUpdated += OnDataGridLayoutUpdated ;
    
    // Store the layout updated handler to be called when the ScrollViewer's layout updates
    _layoutUpdatedHandler = layoutUpdatedHandler ;
  }

  private Action<object, EventArgs>? _layoutUpdatedHandler ;

  private void OnLayoutUpdated( object sender, EventArgs e )
  {
    if ( _isDisposed ) return ;
    _layoutUpdatedHandler?.Invoke( sender, e ) ;
  }
} 