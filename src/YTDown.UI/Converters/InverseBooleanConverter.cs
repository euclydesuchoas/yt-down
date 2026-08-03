using System.Globalization;
using System.Windows.Data;

namespace YTDown.UI.Converters;

/// <summary>
/// Inverte um valor lógico, para desabilitar um elemento enquanto algo está em curso.
/// </summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not true;
}
