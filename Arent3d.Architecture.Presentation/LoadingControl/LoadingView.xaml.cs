using System.Windows ;
using System.Windows.Media ;

namespace Arent3d.Architecture.Presentation.LoadingControl ;

public partial class LoadingView
{
  public static readonly DependencyProperty SpinningColorProperty = DependencyProperty.Register( nameof( SpinningColor ), typeof( SolidColorBrush ), typeof( LoadingView ), new PropertyMetadata( Brushes.DarkSlateBlue ) ) ;

  public SolidColorBrush SpinningColor
  {
    get => (SolidColorBrush)GetValue( SpinningColorProperty ) ;
    set => SetValue( SpinningColorProperty, value ) ;
  }

  public LoadingView()
  {
    InitializeComponent() ;
  }
}