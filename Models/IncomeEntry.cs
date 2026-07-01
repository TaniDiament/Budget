using System.Globalization;
using Budget.Infrastructure;

namespace Budget.Models;

public sealed class IncomeEntry : ObservableObject
{
    private DateTime _month = new(DateTime.Now.Year, DateTime.Now.Month, 1);
    private decimal _amount;

    public DateTime Month
    {
        get => _month;
        set
        {
            if (SetProperty(ref _month, new DateTime(value.Year, value.Month, 1)))
            {
                OnPropertyChanged(nameof(MonthLabel));
            }
        }
    }

    public decimal Amount
    {
        get => _amount;
        set
        {
            if (SetProperty(ref _amount, value))
            {
                OnPropertyChanged(nameof(DisplayAmount));
            }
        }
    }

    public string MonthLabel => Month.ToString("MMM yyyy", CultureInfo.CurrentCulture);

    public string DisplayAmount => Amount.ToString("C", CultureInfo.CurrentCulture);

    public string SortKey => Month.ToString("yyyy-MM");
}

