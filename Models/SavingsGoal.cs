using Budget.Infrastructure;

namespace Budget.Models;

public sealed class SavingsGoal : ObservableObject
{
    private string _name = string.Empty;
    private decimal _targetAmount;
    private decimal _savedAmount;
    private string _contributionText = string.Empty;

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

    public decimal TargetAmount
    {
        get => _targetAmount;
        set
        {
            SetProperty(ref _targetAmount, value);
            OnPropertyChanged(nameof(TargetAmountText));
            OnPropertyChanged(nameof(TargetDisplay));
            OnPropertyChanged(nameof(ProgressPercent));
            OnPropertyChanged(nameof(ProgressDisplay));
            OnPropertyChanged(nameof(ProgressRemainingDisplay));
        }
    }

    public decimal SavedAmount
    {
        get => _savedAmount;
        set
        {
            SetProperty(ref _savedAmount, value);
            OnPropertyChanged(nameof(SavedAmountText));
            OnPropertyChanged(nameof(SavedDisplay));
            OnPropertyChanged(nameof(ProgressPercent));
            OnPropertyChanged(nameof(ProgressDisplay));
            OnPropertyChanged(nameof(ProgressRemainingDisplay));
        }
    }

    public string TargetAmountText
    {
        get => MoneyText.Format(TargetAmount);
        set
        {
            if (MoneyText.TryParse(value, out var parsed) && parsed > 0m)
            {
                TargetAmount = parsed;
            }
            else
            {
                OnPropertyChanged();
            }
        }
    }

    public string SavedAmountText
    {
        get => MoneyText.Format(SavedAmount);
        set
        {
            if (MoneyText.TryParse(value, out var parsed) && parsed >= 0m)
            {
                SavedAmount = parsed;
            }
            else
            {
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Scratch input for the per-goal contribution box; intentionally not persisted.</summary>
    public string ContributionText
    {
        get => _contributionText;
        set => SetProperty(ref _contributionText, value);
    }

    public double ProgressPercent => TargetAmount <= 0 ? 0 : (double)Math.Min(100m, (SavedAmount / TargetAmount) * 100m);

    public string ProgressDisplay => $"{ProgressPercent:0}%";

    public string ProgressRemainingDisplay => MoneyText.Format(Math.Max(0m, TargetAmount - SavedAmount));

    public string TargetDisplay => MoneyText.Format(TargetAmount);

    public string SavedDisplay => MoneyText.Format(SavedAmount);
}
