using System.Globalization;
using Budget.Infrastructure;

namespace Budget.Models;

public sealed class SavingsGoal : ObservableObject
{
    private string _name = string.Empty;
    private decimal _targetAmount;
    private decimal _savedAmount;

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                OnPropertyChanged(nameof(ProgressPercent));
                OnPropertyChanged(nameof(ProgressDisplay));
                OnPropertyChanged(nameof(ProgressRemainingDisplay));
            }
        }
    }

    public decimal TargetAmount
    {
        get => _targetAmount;
        set
        {
            if (SetProperty(ref _targetAmount, value))
            {
                OnPropertyChanged(nameof(ProgressPercent));
                OnPropertyChanged(nameof(ProgressDisplay));
                OnPropertyChanged(nameof(ProgressRemainingDisplay));
            }
        }
    }

    public decimal SavedAmount
    {
        get => _savedAmount;
        set
        {
            if (SetProperty(ref _savedAmount, value))
            {
                OnPropertyChanged(nameof(ProgressPercent));
                OnPropertyChanged(nameof(ProgressDisplay));
                OnPropertyChanged(nameof(ProgressRemainingDisplay));
            }
        }
    }

    public double ProgressPercent => TargetAmount <= 0 ? 0 : (double)Math.Min(100m, (SavedAmount / TargetAmount) * 100m);

    public string ProgressDisplay => $"{ProgressPercent:0}%";

    public string ProgressRemainingDisplay => Math.Max(0m, TargetAmount - SavedAmount).ToString("C", CultureInfo.CurrentCulture);

    public string TargetDisplay => TargetAmount.ToString("C", CultureInfo.CurrentCulture);

    public string SavedDisplay => SavedAmount.ToString("C", CultureInfo.CurrentCulture);
}

