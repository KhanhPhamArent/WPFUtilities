using System.Windows ;
using System.Windows.Controls ;

namespace Arent3d.Architecture.Presentation.DataGrid ;

public class HeaderContentBuilder : IHeaderContentBuilder
{
  public void CreateHeaderContent( Dictionary<string, GroupInfo> groupInfos, Grid header, Grid frozenHeader, DataGridWrapper wrapper, int frozenColumnCount )
  {
    int count = 0;
    foreach (var group in groupInfos.Values)
    {
      count++;
      
      var columnTemPlate = wrapper.TryFindResource($"Column{count}Template") as DataTemplate;

      var border = CreateHeaderBorder(group, wrapper);

      if (columnTemPlate != null)
      {
        var control = new ContentControl
        {
          Content = wrapper.DataContext,
          ContentTemplate = columnTemPlate
        };

        border.Child = control;
      }
      else
      {
        var textBlock = CreateHeaderText(group, wrapper);
        border.Child = textBlock;
      }

      // Add to the appropriate grid based on whether it's frozen
      if (group.IsFrozen)
      {
        frozenHeader.Children.Add(border);
      }
      else
      {
        header.Children.Add(border);
      }

      SetGridPosition(border, group, frozenColumnCount);
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

    var border = new Border { BorderBrush = wrapper.HeaderBorderColor, BorderThickness = thickness, Background = wrapper.HeaderBackground} ;

    return border ;
  }

  private Thickness CalculateBorderThickness( GroupInfo groupInfo, double borderThickness, int numberOfRows, int numberOfColumns )
  {
    double left = borderThickness, right = borderThickness, bottom = borderThickness, top = borderThickness ;

    if ( groupInfo.ColumnIndex == 0 ) left = borderThickness * 2 ;
    if ( groupInfo.ColumnIndex + ( groupInfo.ColumnSpan == 0 ? 1 : groupInfo.ColumnSpan ) == numberOfColumns ) right = borderThickness * 2 ;
    if (groupInfo.ColumnIndex == numberOfColumns - 1) right = borderThickness * 2 ;
    if ( groupInfo.RowIndex == 0 ) top = borderThickness * 2 ;
    if ( groupInfo.RowIndex + ( groupInfo.RowSpan == 0 ? 1 : groupInfo.RowSpan ) == numberOfRows ) bottom = borderThickness * 2 ;
    if ( groupInfo.RowIndex == numberOfRows - 1 ) bottom = borderThickness * 2 ;

    return new Thickness( left, top, right, bottom ) ;
  }

  private void SetGridPosition( Border border, GroupInfo groupInfo, int frozenColumnCount )
  {
    Grid.SetRow( border, groupInfo.RowIndex ) ;
    if ( groupInfo.RowSpan > 0 ) Grid.SetRowSpan( border, groupInfo.RowSpan ) ;
    
    // Adjust column index for scrollable columns (subtract frozen column count)
    var adjustedColumnIndex = groupInfo.IsFrozen ? groupInfo.ColumnIndex : groupInfo.ColumnIndex - frozenColumnCount ;
    Grid.SetColumn( border, adjustedColumnIndex ) ;
    if ( groupInfo.ColumnSpan > 0 ) Grid.SetColumnSpan( border, groupInfo.ColumnSpan ) ;
  }
} 