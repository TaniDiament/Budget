using System.Collections.ObjectModel;
using System.Globalization;

namespace Budget.Models;

public sealed class BudgetMonth
{
    public BudgetMonth(DateTime month)
    {
        Month = new DateTime(month.Year, month.Month, 1);
    }

    public DateTime Month { get; }

    public string MonthKey => Month.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    public string TakeHomePayText { get; set; } = "0";

    public ObservableCollection<BudgetLineItem> LineItems { get; } = new();
}
