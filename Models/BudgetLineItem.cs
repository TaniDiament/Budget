using System.Globalization;

namespace Budget.Models;

public sealed class BudgetLineItem
{
    public BudgetLineItem(string name, decimal amount)
    {
        Name = name;
        Amount = amount;
    }

    public string Name { get; }

    public decimal Amount { get; }

    public string DisplayAmount => Amount.ToString("C", CultureInfo.CurrentCulture);
}

