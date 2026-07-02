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
            SetProperty(ref _amount, value);
            OnPropertyChanged(nameof(AmountText));
            OnPropertyChanged(nameof(DisplayAmount));
        }
    }

    public string AmountText
    {
        get => MoneyText.Format(Amount);
        set
        {
            if (MoneyText.TryParse(value, out var parsed) && parsed > 0m)
            {
                Amount = parsed;
            }
            else
            {
                OnPropertyChanged();
            }
        }
    }

    public string MonthLabel => Month.ToString("MMM yyyy", CultureInfo.CurrentCulture);

    public string DisplayAmount => MoneyText.Format(Amount);

    public string SortKey => Month.ToString("yyyy-MM");
}
