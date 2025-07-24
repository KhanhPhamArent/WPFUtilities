using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Media ;

namespace Arent3d.Architecture.Presentation.DataGrid ;

public class HeaderContentBuilder : IHeaderContentBuilder
{
  public void CreateHeaderContent( Dictionary<string, GroupInfo> groupInfos, Grid header, DataGridWrapper wrapper )
  {
    foreach ( var group in groupInfos.Values ) {
      var textBlock = CreateHeaderText( group, wrapper ) ;
      var border = CreateHeaderBorder( group, wrapper ) ;
      border.Child = textBlock ;
      header.Children.Add( border ) ;
    }
  }

  private TextBlock CreateHeaderText( GroupInfo groupInfo, DataGridWrapper wrapper )
  {
    return new TextBlock()
    {
      Text = groupInfo.Name,
      VerticalAlignment = VerticalAlignment.Center,
      HorizontalAlignment = HorizontalAlignment.Center,
      TextAlignment = TextAlignment.Center,
      TextWrapping = TextWrapping.Wrap,
      Foreground = wrapper.HeaderForeground,
      Margin = wrapper.HeaderTextMargin
    } ;
  }

  private Border CreateHeaderBorder( GroupInfo groupInfo, DataGridWrapper wrapper )
  {
    var borderThickness = wrapper.HeaderThickness / 2 ;
    var thickness = CalculateBorderThickness( groupInfo, borderThickness, wrapper.NumberOfRows, wrapper.NumberOfColumns ) ;

    var border = new Border { BorderBrush = wrapper.HeaderBorderColor, BorderThickness = thickness, Background = wrapper.HeaderBackground } ;

    SetGridPosition( border, groupInfo ) ;
    return border ;
  }

  private Thickness CalculateBorderThickness( GroupInfo groupInfo, double borderThickness, int numberOfRows, int numberOfColumns )
  {
    double left = borderThickness, right = borderThickness, bottom = borderThickness, top = borderThickness ;

    if ( groupInfo.ColumnIndex == 0 ) left = borderThickness * 2 ;
    if ( groupInfo.ColumnIndex + ( groupInfo.ColumnSpan == 0 ? 1 : groupInfo.ColumnSpan ) == numberOfColumns ) right = borderThickness * 2 ;
    if ( groupInfo.RowIndex == 0 ) top = borderThickness * 2 ;
    if ( groupInfo.RowIndex + ( groupInfo.RowSpan == 0 ? 1 : groupInfo.RowSpan ) == numberOfRows ) bottom = borderThickness * 2 ;

    return new Thickness( left, top, right, bottom ) ;
  }

  private void SetGridPosition( Border border, GroupInfo groupInfo )
  {
    Grid.SetRow( border, groupInfo.RowIndex ) ;
    if ( groupInfo.RowSpan > 0 ) Grid.SetRowSpan( border, groupInfo.RowSpan ) ;
    Grid.SetColumn( border, groupInfo.ColumnIndex ) ;
    if ( groupInfo.ColumnSpan > 0 ) Grid.SetColumnSpan( border, groupInfo.ColumnSpan ) ;
  }
} 