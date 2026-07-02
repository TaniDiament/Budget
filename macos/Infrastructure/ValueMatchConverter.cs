using System.Globalization;
using Avalonia.Data.Converters;

namespace Budget.Infrastructure;

/// <summary>
/// Stands in for WPF's string-valued DataTriggers: returns whether the bound
/// value's invariant string form equals the converter parameter. Used to toggle
/// style classes (severity meters, the income delta chip) from view-model strings.
/// </summary>
public sealed class ValueMatchConverter : IValueConverter
{
    public bool Negate { get; init; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var matches = string.Equals(
            System.Convert.ToString(value, CultureInfo.InvariantCulture),
            parameter as string,
            StringComparison.Ordinal);
        return matches ^ Negate;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
