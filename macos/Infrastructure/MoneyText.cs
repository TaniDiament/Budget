using System.Globalization;

namespace Budget.Infrastructure;

public static class MoneyText
{
    public static bool TryParse(string? text, out decimal value)
    {
        return decimal.TryParse(text, NumberStyles.Currency, CultureInfo.CurrentCulture, out value);
    }

    public static string Format(decimal value)
    {
        return value.ToString("C", CultureInfo.CurrentCulture);
    }
}
