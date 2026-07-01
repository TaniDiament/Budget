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
    private static readonly string[] DefaultCategories =
    {
        "General",
        "Housing",
        "Utilities",
        "Transportation",
        "Groceries",
        "Dining",
        "Savings",
        "Entertainment",
        "Healthcare",
        "Other"
    };

    private readonly BudgetStateStore _stateStore = new();
    private readonly ThemeSettingsStore _themeSettingsStore = new();
    private bool _isRestoringState;
    private string _monthlyTakeHomePayText = "0";
    private string _newItemName = string.Empty;
    private string _newItemAmountText = string.Empty;
    private string _newItemCategory = "General";
    private string _newGoalName = string.Empty;
    private string _newGoalTargetAmountText = string.Empty;
    private string _newGoalSavedAmountText = string.Empty;
    private string _newIncomeAmountText = string.Empty;
    private DateTime _newIncomeMonth = new(DateTime.Now.Year, DateTime.Now.Month, 1);
    private string _statusMessage = "Add a line item to get started.";
    private ThemeMode _selectedThemeMode;

    public MainViewModel()
    {
        AddLineItemCommand = new RelayCommand(_ => AddLineItem(), _ => CanAddLineItem());
        RemoveLineItemCommand = new RelayCommand(parameter => RemoveLineItem(parameter as BudgetLineItem));
        AddGoalCommand = new RelayCommand(_ => AddGoal(), _ => CanAddGoal());
        RemoveGoalCommand = new RelayCommand(parameter => RemoveGoal(parameter as SavingsGoal));
        AddIncomeEntryCommand = new RelayCommand(_ => AddIncomeEntry(), _ => CanAddIncomeEntry());
        RemoveIncomeEntryCommand = new RelayCommand(parameter => RemoveIncomeEntry(parameter as IncomeEntry));

        ThemeOptions = Enum.GetValues<ThemeMode>();
        CategoryOptions = new ObservableCollection<string>(DefaultCategories);
        _selectedThemeMode = _themeSettingsStore.Load().ThemeMode;
        LineItems.CollectionChanged += OnLineItemsChanged;
        SavingsGoals.CollectionChanged += OnSavingsGoalsChanged;
        IncomeEntries.CollectionChanged += OnIncomeEntriesChanged;
        RestoreSavedState();
        RefreshBudgetSummary();
        ThemeManager.ApplyTheme(_selectedThemeMode);
    }

    public ObservableCollection<BudgetLineItem> LineItems { get; } = new();

    public ObservableCollection<SavingsGoal> SavingsGoals { get; } = new();

    public ObservableCollection<IncomeEntry> IncomeEntries { get; } = new();

    public ObservableCollection<string> CategoryOptions { get; }

    public ObservableCollection<CategorySummaryItem> CategorySummaries { get; } = new();

    public ObservableCollection<IncomeTrendItem> IncomeTrendItems { get; } = new();

    public RelayCommand AddLineItemCommand { get; }

    public RelayCommand RemoveLineItemCommand { get; }

    public RelayCommand AddGoalCommand { get; }

    public RelayCommand RemoveGoalCommand { get; }

    public RelayCommand AddIncomeEntryCommand { get; }

    public RelayCommand RemoveIncomeEntryCommand { get; }

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

    public string NewItemCategory
    {
        get => _newItemCategory;
        set => SetProperty(ref _newItemCategory, value);
    }

    public string NewGoalName
    {
        get => _newGoalName;
        set
        {
            if (SetProperty(ref _newGoalName, value))
            {
                AddGoalCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewGoalTargetAmountText
    {
        get => _newGoalTargetAmountText;
        set
        {
            if (SetProperty(ref _newGoalTargetAmountText, value))
            {
                AddGoalCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewGoalSavedAmountText
    {
        get => _newGoalSavedAmountText;
        set
        {
            if (SetProperty(ref _newGoalSavedAmountText, value))
            {
                AddGoalCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public DateTime NewIncomeMonth
    {
        get => _newIncomeMonth;
        set => SetProperty(ref _newIncomeMonth, new DateTime(value.Year, value.Month, 1));
    }

    public string NewIncomeAmountText
    {
        get => _newIncomeAmountText;
        set
        {
            if (SetProperty(ref _newIncomeAmountText, value))
            {
                AddIncomeEntryCommand.RaiseCanExecuteChanged();
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

    public decimal TotalGoalTargetValue => SavingsGoals.Sum(goal => goal.TargetAmount);

    public decimal TotalGoalSavedValue => SavingsGoals.Sum(goal => goal.SavedAmount);

    public decimal MonthlyIncomeHistoryTotalValue => IncomeEntries.Sum(entry => entry.Amount);

    public decimal MonthlyIncomeHistoryAverageValue => IncomeEntries.Count == 0 ? 0m : MonthlyIncomeHistoryTotalValue / IncomeEntries.Count;

    public string TotalGoalTargetDisplay => TotalGoalTargetValue.ToString("C", CultureInfo.CurrentCulture);

    public string TotalGoalSavedDisplay => TotalGoalSavedValue.ToString("C", CultureInfo.CurrentCulture);

    public string MonthlyIncomeHistoryTotalDisplay => MonthlyIncomeHistoryTotalValue.ToString("C", CultureInfo.CurrentCulture);

    public string MonthlyIncomeHistoryAverageDisplay => MonthlyIncomeHistoryAverageValue.ToString("C", CultureInfo.CurrentCulture);

    public string LatestIncomeDisplay => IncomeEntries
        .OrderBy(entry => entry.SortKey)
        .LastOrDefault()?.DisplayAmount ?? 0m.ToString("C", CultureInfo.CurrentCulture);

    public string SavingsGoalProgressDisplay
    {
        get
        {
            if (TotalGoalTargetValue <= 0)
            {
                return "0%";
            }

            return $"{(TotalGoalSavedValue / TotalGoalTargetValue) * 100m:0}%";
        }
    }

    private void AddLineItem()
    {
        if (!CanAddLineItem())
        {
            StatusMessage = "Enter a valid name and amount before adding a line item.";
            return;
        }

        var category = NormalizeCategory(NewItemCategory);
        var item = new BudgetLineItem(NewItemName.Trim(), category, ParseMoney(NewItemAmountText));
        LineItems.Add(item);
        EnsureCategoryOption(category);
        NewItemName = string.Empty;
        NewItemAmountText = string.Empty;
        NewItemCategory = category;
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

    private void AddGoal()
    {
        if (!CanAddGoal())
        {
            StatusMessage = "Enter a valid savings goal name and amounts before adding.";
            return;
        }

        var goal = new SavingsGoal
        {
            Name = NewGoalName.Trim(),
            TargetAmount = ParseMoney(NewGoalTargetAmountText),
            SavedAmount = ParseMoney(NewGoalSavedAmountText)
        };

        SavingsGoals.Add(goal);
        NewGoalName = string.Empty;
        NewGoalTargetAmountText = string.Empty;
        NewGoalSavedAmountText = string.Empty;
        StatusMessage = $"Added savings goal {goal.Name}.";
        RefreshBudgetSummary();
    }

    private void RemoveGoal(SavingsGoal? goal)
    {
        if (goal is null)
        {
            return;
        }

        if (SavingsGoals.Remove(goal))
        {
            StatusMessage = $"Removed savings goal {goal.Name}.";
            RefreshBudgetSummary();
        }
    }

    private void AddIncomeEntry()
    {
        if (!CanAddIncomeEntry())
        {
            StatusMessage = "Enter a valid month and income amount before adding.";
            return;
        }

        var entry = new IncomeEntry
        {
            Month = NewIncomeMonth,
            Amount = ParseMoney(NewIncomeAmountText)
        };

        var existing = IncomeEntries.FirstOrDefault(x => x.SortKey == entry.SortKey);
        if (existing is not null)
        {
            existing.Amount = entry.Amount;
            StatusMessage = $"Updated income for {entry.MonthLabel}.";
        }
        else
        {
            IncomeEntries.Add(entry);
            StatusMessage = $"Added income for {entry.MonthLabel}.";
        }

        NewIncomeAmountText = string.Empty;
        RefreshBudgetSummary();
    }

    private void RemoveIncomeEntry(IncomeEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        if (IncomeEntries.Remove(entry))
        {
            StatusMessage = $"Removed income for {entry.MonthLabel}.";
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

                var category = NormalizeCategory(item.Category);
                EnsureCategoryOption(category);
                LineItems.Add(new BudgetLineItem(item.Name.Trim(), category, item.Amount));
            }

            SavingsGoals.Clear();
            foreach (var goal in state.SavingsGoals ?? Enumerable.Empty<SavingsGoalState>())
            {
                if (string.IsNullOrWhiteSpace(goal.Name))
                {
                    continue;
                }

                SavingsGoals.Add(new SavingsGoal
                {
                    Name = goal.Name.Trim(),
                    TargetAmount = goal.TargetAmount,
                    SavedAmount = goal.SavedAmount
                });
            }

            IncomeEntries.Clear();
            foreach (var income in state.IncomeEntries ?? Enumerable.Empty<IncomeEntryState>())
            {
                if (!TryParseMonthKey(income.MonthKey, out var month))
                {
                    continue;
                }

                IncomeEntries.Add(new IncomeEntry
                {
                    Month = month,
                    Amount = income.Amount
                });
            }

            StatusMessage = statusMessage ?? $"Restored {LineItems.Count} line item{(LineItems.Count == 1 ? string.Empty : "s")}, {SavingsGoals.Count} goal{(SavingsGoals.Count == 1 ? string.Empty : "s")}, and {IncomeEntries.Count} income record{(IncomeEntries.Count == 1 ? string.Empty : "s")} from your last session.";
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

    private bool CanAddGoal()
    {
        return !string.IsNullOrWhiteSpace(NewGoalName)
               && TryParseMoney(NewGoalTargetAmountText, out var targetAmount)
               && targetAmount > 0m
               && TryParseMoney(NewGoalSavedAmountText, out var savedAmount)
               && savedAmount >= 0m;
    }

    private bool CanAddIncomeEntry()
    {
        return TryParseMoney(NewIncomeAmountText, out var amount) && amount > 0m;
    }

    private void OnLineItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshBudgetSummary();
    }

    private void OnSavingsGoalsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshBudgetSummary();
    }

    private void OnIncomeEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
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
        OnPropertyChanged(nameof(TotalGoalTargetValue));
        OnPropertyChanged(nameof(TotalGoalSavedValue));
        OnPropertyChanged(nameof(MonthlyIncomeHistoryTotalValue));
        OnPropertyChanged(nameof(MonthlyIncomeHistoryAverageValue));
        OnPropertyChanged(nameof(TotalGoalTargetDisplay));
        OnPropertyChanged(nameof(TotalGoalSavedDisplay));
        OnPropertyChanged(nameof(MonthlyIncomeHistoryTotalDisplay));
        OnPropertyChanged(nameof(MonthlyIncomeHistoryAverageDisplay));
        OnPropertyChanged(nameof(LatestIncomeDisplay));
        OnPropertyChanged(nameof(SavingsGoalProgressDisplay));
        UpdateCategoryChart();
        UpdateIncomeChart();
        AddLineItemCommand.RaiseCanExecuteChanged();
        AddGoalCommand.RaiseCanExecuteChanged();
        AddIncomeEntryCommand.RaiseCanExecuteChanged();

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

    private void UpdateCategoryChart()
    {
        CategorySummaries.Clear();

        var totals = LineItems
            .GroupBy(item => string.IsNullOrWhiteSpace(item.Category) ? "General" : item.Category.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new { Category = group.Key, Amount = group.Sum(item => item.Amount) })
            .OrderByDescending(group => group.Amount)
            .ToList();

        var max = totals.Count == 0 ? 0m : totals.Max(item => item.Amount);
        foreach (var item in totals)
        {
            var percent = max <= 0 ? 0d : (double)((item.Amount / max) * 100m);
            CategorySummaries.Add(new CategorySummaryItem(item.Category, item.Amount, percent));
        }
    }

    private void UpdateIncomeChart()
    {
        IncomeTrendItems.Clear();

        var ordered = IncomeEntries
            .OrderBy(entry => entry.SortKey)
            .ToList();

        var max = ordered.Count == 0 ? 0m : ordered.Max(item => item.Amount);
        foreach (var item in ordered)
        {
            var percent = max <= 0 ? 0d : (double)((item.Amount / max) * 100m);
            IncomeTrendItems.Add(new IncomeTrendItem(item.MonthLabel, item.Amount, percent));
        }
    }

    private void EnsureCategoryOption(string category)
    {
        if (CategoryOptions.Any(option => string.Equals(option, category, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        CategoryOptions.Add(category);
    }

    private static string NormalizeCategory(string? category)
    {
        return string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
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
                    Category = item.Category,
                    Amount = item.Amount
                })),
            SavingsGoals = new ObservableCollection<SavingsGoalState>(
                SavingsGoals.Select(goal => new SavingsGoalState
                {
                    Name = goal.Name,
                    TargetAmount = goal.TargetAmount,
                    SavedAmount = goal.SavedAmount
                })),
            IncomeEntries = new ObservableCollection<IncomeEntryState>(
                IncomeEntries.Select(entry => new IncomeEntryState
                {
                    MonthKey = entry.SortKey,
                    Amount = entry.Amount
                }))
        };
    }

    private static bool TryParseMonthKey(string? monthKey, out DateTime month)
    {
        if (DateTime.TryParseExact(monthKey, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            month = new DateTime(parsed.Year, parsed.Month, 1);
            return true;
        }

        if (DateTime.TryParse(monthKey, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed))
        {
            month = new DateTime(parsed.Year, parsed.Month, 1);
            return true;
        }

        month = default;
        return false;
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

