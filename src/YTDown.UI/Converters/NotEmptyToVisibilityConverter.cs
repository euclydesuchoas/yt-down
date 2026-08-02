using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace YTDown.UI.Converters;

/// <summary>
/// Exibe o elemento quando o valor existe e recolhe quando e nulo ou vazio.
/// </summary>
public sealed class NotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hasContent = value switch
        {
            null => false,
            string text => !string.IsNullOrWhiteSpace(text),
            System.Collections.ICollection collection => collection.Count > 0,
            _ => true
        };

        return hasContent ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
