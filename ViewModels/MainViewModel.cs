using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using Budget.Infrastructure;
using Budget.Models;
using Budget.Services;

namespace Budget.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly BudgetStateStore _stateStore = new();
    private readonly ThemeSettingsStore _themeSettingsStore = new();
    private bool _isRestoringState;
    private string _monthlyTakeHomePayText = "0";
    private string _newItemName = string.Empty;
    private string _newItemAmountText = string.Empty;
    private string _statusMessage = "Add a line item to get started.";
    private ThemeMode _selectedThemeMode;

    public MainViewModel()
    {
        AddLineItemCommand = new RelayCommand(_ => AddLineItem(), _ => CanAddLineItem());
        RemoveLineItemCommand = new RelayCommand(parameter => RemoveLineItem(parameter as BudgetLineItem));

        ThemeOptions = Enum.GetValues<ThemeMode>();
        _selectedThemeMode = _themeSettingsStore.Load().ThemeMode;
        LineItems.CollectionChanged += OnLineItemsChanged;
        RestoreSavedState();
        RefreshBudgetSummary();
        ThemeManager.ApplyTheme(_selectedThemeMode);
    }

    public ObservableCollection<BudgetLineItem> LineItems { get; } = new();

    public RelayCommand AddLineItemCommand { get; }

    public RelayCommand RemoveLineItemCommand { get; }

    public ThemeMode[] ThemeOptions { get; }

    public ThemeMode SelectedThemeMode
    {
        get => _selectedThemeMode;
        set
        {
            if (SetProperty(ref _selectedThemeMode, value))
            {
                ThemeManager.ApplyTheme(value);
                _themeSettingsStore.Save(new ThemeSettings { ThemeMode = value });
            }
        }
    }

    public string MonthlyTakeHomePayText
    {
        get => _monthlyTakeHomePayText;
        set
        {
            if (SetProperty(ref _monthlyTakeHomePayText, value))
            {
                RefreshBudgetSummary();
            }
        }
    }

    public string NewItemName
    {
        get => _newItemName;
        set
        {
            if (SetProperty(ref _newItemName, value))
            {
                AddLineItemCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewItemAmountText
    {
        get => _newItemAmountText;
        set
        {
            if (SetProperty(ref _newItemAmountText, value))
            {
                AddLineItemCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public decimal MonthlyTakeHomePayValue => TryParseMoney(MonthlyTakeHomePayText, out var value) ? value : 0m;

    public decimal TotalDeductionsValue => LineItems.Sum(item => item.Amount);

    public decimal LeftoverValue => MonthlyTakeHomePayValue - TotalDeductionsValue;

    public string MonthlyTakeHomePayDisplay => MonthlyTakeHomePayValue.ToString("C", CultureInfo.CurrentCulture);

    public string TotalDeductionsDisplay => TotalDeductionsValue.ToString("C", CultureInfo.CurrentCulture);

    public string LeftoverDisplay => LeftoverValue.ToString("C", CultureInfo.CurrentCulture);

    public string BudgetUsageDisplay
    {
        get
        {
            var income = MonthlyTakeHomePayValue;
            if (income <= 0)
            {
                return "0% used";
            }

            var usage = (TotalDeductionsValue / income) * 100m;
            return $"{usage:0}% used";
        }
    }

    public string BudgetSummaryMessage
    {
        get
        {
            var income = MonthlyTakeHomePayValue;
            var leftover = LeftoverValue;

            if (income <= 0)
            {
                return "Enter your monthly take-home pay to see your available budget.";
            }

            if (leftover >= 0)
            {
                return $"You have {leftover.ToString("C", CultureInfo.CurrentCulture)} left after planned expenses.";
            }

            return $"You are over budget by {Math.Abs(leftover).ToString("C", CultureInfo.CurrentCulture)}.";
        }
    }

    private void AddLineItem()
    {
        if (!CanAddLineItem())
        {
            StatusMessage = "Enter a valid name and amount before adding a line item.";
            return;
        }

        var item = new BudgetLineItem(NewItemName.Trim(), ParseMoney(NewItemAmountText));
        LineItems.Add(item);
        NewItemName = string.Empty;
        NewItemAmountText = string.Empty;
        StatusMessage = $"Added {item.Name}.";
        RefreshBudgetSummary();
    }

    private void RemoveLineItem(BudgetLineItem? item)
    {
        if (item is null)
        {
            return;
        }

        if (LineItems.Remove(item))
        {
            StatusMessage = $"Removed {item.Name}.";
            RefreshBudgetSummary();
        }
    }

    public bool SaveState()
    {
        return _stateStore.Save(CaptureState());
    }

    public void SetStatusMessage(string message)
    {
        StatusMessage = message;
    }

    public BudgetState SnapshotState()
    {
        return CaptureState();
    }

    public void ApplyState(BudgetState state, string? statusMessage = null)
    {
        _isRestoringState = true;
        try
        {
            MonthlyTakeHomePayText = string.IsNullOrWhiteSpace(state.MonthlyTakeHomePayText)
                ? "0"
                : state.MonthlyTakeHomePayText;

            LineItems.Clear();
            foreach (var item in state.LineItems ?? Enumerable.Empty<BudgetLineItemState>())
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    continue;
                }

                LineItems.Add(new BudgetLineItem(item.Name.Trim(), item.Amount));
            }

            StatusMessage = statusMessage ?? $"Restored {LineItems.Count} line item{(LineItems.Count == 1 ? string.Empty : "s")} from your last session.";
        }
        finally
        {
            _isRestoringState = false;
            RefreshBudgetSummary();
        }
    }

    private bool CanAddLineItem()
    {
        return !string.IsNullOrWhiteSpace(NewItemName) && TryParseMoney(NewItemAmountText, out var amount) && amount > 0m;
    }

    private void OnLineItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshBudgetSummary();
    }

    private void RefreshBudgetSummary()
    {
        OnPropertyChanged(nameof(MonthlyTakeHomePayValue));
        OnPropertyChanged(nameof(TotalDeductionsValue));
        OnPropertyChanged(nameof(LeftoverValue));
        OnPropertyChanged(nameof(MonthlyTakeHomePayDisplay));
        OnPropertyChanged(nameof(TotalDeductionsDisplay));
        OnPropertyChanged(nameof(LeftoverDisplay));
        OnPropertyChanged(nameof(BudgetUsageDisplay));
        OnPropertyChanged(nameof(BudgetSummaryMessage));
        AddLineItemCommand.RaiseCanExecuteChanged();

        if (!_isRestoringState)
        {
            SaveState();
        }
    }

    private void RestoreSavedState()
    {
        if (!_stateStore.TryLoad(out var state))
        {
            return;
        }

        ApplyState(state);
    }

    private BudgetState CaptureState()
    {
        return new BudgetState
        {
            MonthlyTakeHomePayText = MonthlyTakeHomePayText,
            LineItems = new ObservableCollection<BudgetLineItemState>(
                LineItems.Select(item => new BudgetLineItemState
                {
                    Name = item.Name,
                    Amount = item.Amount
                }))
        };
    }

    private static bool TryParseMoney(string? text, out decimal value)
    {
        return decimal.TryParse(
            text,
            NumberStyles.Currency,
            CultureInfo.CurrentCulture,
            out value);
    }

    private static decimal ParseMoney(string text)
    {
        if (TryParseMoney(text, out var value))
        {
            return value;
        }

        return 0m;
    }
}

