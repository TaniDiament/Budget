namespace Budget.Models;

public sealed class IncomeEntryState
{
    public string MonthKey { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}

