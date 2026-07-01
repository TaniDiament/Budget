using System.Globalization;

namespace Budget.Models;

public sealed class CategorySummaryItem
{
    public CategorySummaryItem(string category, decimal amount, double percent)
    {
        Category = category;
        Amount = amount;
        Percent = percent;
    }

    public string Category { get; }

    public decimal Amount { get; }

    public double Percent { get; }

    public string DisplayAmount => Amount.ToString("C", CultureInfo.CurrentCulture);
}

