using Budget.Infrastructure;

namespace Budget.Models;

public sealed class BudgetLineItem : ObservableObject
{
    private string _name;
    private string _category;
    private decimal _plannedAmount;
    private decimal _actualAmount;
    private string _spendingText = string.Empty;

    public BudgetLineItem(string name, string category, decimal plannedAmount, decimal actualAmount = 0m)
    {
        _name = name;
        _category = category;
        _plannedAmount = plannedAmount;
        _actualAmount = actualAmount;
    }

    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                OnPropertyChanged();
                return;
            }

            if (!SetProperty(ref _name, value.Trim()))
            {
                OnPropertyChanged();
            }
        }
    }

    public string Category
    {
        get => _category;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? "General" : value.Trim();
            if (!SetProperty(ref _category, normalized))
            {
                OnPropertyChanged();
            }

            OnPropertyChanged(nameof(DisplayCategory));
        }
    }

    public decimal PlannedAmount
    {
        get => _plannedAmount;
        set
        {
            SetProperty(ref _plannedAmount, value);
            OnPropertyChanged(nameof(PlannedAmountText));
            OnPropertyChanged(nameof(DisplayAmount));
        }
    }

    public decimal ActualAmount
    {
        get => _actualAmount;
        set
        {
            SetProperty(ref _actualAmount, value);
            OnPropertyChanged(nameof(ActualAmountText));
        }
    }

    public string PlannedAmountText
    {
        get => MoneyText.Format(PlannedAmount);
        set
        {
            if (MoneyText.TryParse(value, out var parsed) && parsed >= 0m)
            {
                PlannedAmount = parsed;
            }
            else
            {
                OnPropertyChanged();
            }
        }
    }

    public string ActualAmountText
    {
        get => MoneyText.Format(ActualAmount);
        set
        {
            if (MoneyText.TryParse(value, out var parsed) && parsed >= 0m)
            {
                ActualAmount = parsed;
            }
            else
            {
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Scratch input for the per-item "add spending" box; intentionally not persisted.</summary>
    public string SpendingText
    {
        get => _spendingText;
        set => SetProperty(ref _spendingText, value);
    }

    public string DisplayAmount => MoneyText.Format(PlannedAmount);

    public string DisplayCategory => string.IsNullOrWhiteSpace(Category) ? "General" : Category;
}
