using System.Globalization;

namespace Budget.Models;

public sealed class IncomeTrendItem
{
    public IncomeTrendItem(string monthLabel, decimal amount, double percent)
    {
        MonthLabel = monthLabel;
        Amount = amount;
        Percent = percent;
    }

    public string MonthLabel { get; }

    public decimal Amount { get; }

    public double Percent { get; }

    public string DisplayAmount => Amount.ToString("C", CultureInfo.CurrentCulture);
}

