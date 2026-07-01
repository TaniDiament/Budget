using System.Globalization;

namespace Budget.Models;

public sealed class BudgetLineItem
{
    public BudgetLineItem(string name, string category, decimal amount)
    {
        Name = name;
        Category = category;
        Amount = amount;
    }

    public string Name { get; }

    public string Category { get; }

    public decimal Amount { get; }

    public string DisplayAmount => Amount.ToString("C", CultureInfo.CurrentCulture);

    public string DisplayCategory => string.IsNullOrWhiteSpace(Category) ? "General" : Category;
}

