using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Arent3d.Architecture.Presentation.Converters;

public class DoubleToDataGridLengthConverter : IValueConverter
{
    public double Offset { get; set; } = 0;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double doubleVal && doubleVal > 0) return new GridLength(doubleVal + Offset);
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}