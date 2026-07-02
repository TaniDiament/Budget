namespace Budget.Models;

public sealed class IncomeTrendItem
{
    public IncomeTrendItem(string monthLabel, decimal amount)
    {
        MonthLabel = monthLabel;
        Amount = amount;
    }

    public string MonthLabel { get; }

    public decimal Amount { get; }
}
