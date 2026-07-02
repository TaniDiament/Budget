using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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
    private readonly Dictionary<string, BudgetMonth> _months = new();
    private BudgetMonth _currentMonth = null!; // assigned in the constructor before any command can run
    private bool _isRestoringState;
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
    private RemovedEntry? _lastRemoval;

    private sealed record RemovedEntry(string Kind, object Item, int Index, string? MonthKey);

    public MainViewModel()
    {
        AddLineItemCommand = new RelayCommand(_ => AddLineItem(), _ => CanAddLineItem());
        RemoveLineItemCommand = new RelayCommand(parameter => RemoveLineItem(parameter as BudgetLineItem));
        AddGoalCommand = new RelayCommand(_ => AddGoal(), _ => CanAddGoal());
        RemoveGoalCommand = new RelayCommand(parameter => RemoveGoal(parameter as SavingsGoal));
        AddIncomeEntryCommand = new RelayCommand(_ => AddIncomeEntry(), _ => CanAddIncomeEntry());
        RemoveIncomeEntryCommand = new RelayCommand(parameter => RemoveIncomeEntry(parameter as IncomeEntry));
        PreviousMonthCommand = new RelayCommand(_ => SwitchToMonth(_currentMonth.Month.AddMonths(-1)));
        NextMonthCommand = new RelayCommand(_ => SwitchToMonth(_currentMonth.Month.AddMonths(1)));
        CopyPreviousMonthCommand = new RelayCommand(_ => CopyPreviousMonth(), _ => CanCopyPreviousMonth());
        UndoRemoveCommand = new RelayCommand(_ => UndoRemove());
        ContributeToGoalCommand = new RelayCommand(parameter => ContributeToGoal(parameter as SavingsGoal));

        ThemeOptions = Enum.GetValues<ThemeMode>();
        CategoryOptions = new ObservableCollection<string>(DefaultCategories);
        _selectedThemeMode = _themeSettingsStore.Load().ThemeMode;
        SavingsGoals.CollectionChanged += OnSavingsGoalsChanged;
        IncomeEntries.CollectionChanged += OnIncomeEntriesChanged;
        _currentMonth = GetOrCreateMonth(DateTime.Now);
        RestoreSavedState();
        RefreshBudgetSummary();
        ThemeManager.ApplyTheme(_selectedThemeMode);
    }

    public ObservableCollection<BudgetLineItem> LineItems => _currentMonth.LineItems;

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

    public RelayCommand PreviousMonthCommand { get; }

    public RelayCommand NextMonthCommand { get; }

    public RelayCommand CopyPreviousMonthCommand { get; }

    public RelayCommand UndoRemoveCommand { get; }

    public RelayCommand ContributeToGoalCommand { get; }

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

    public DateTime CurrentMonth => _currentMonth.Month;

    public string CurrentMonthLabel => _currentMonth.Month.ToString("MMMM yyyy", CultureInfo.CurrentCulture);

    public string MonthlyTakeHomePayText
    {
        get => _currentMonth.TakeHomePayText;
        set
        {
            if (_currentMonth.TakeHomePayText == value)
            {
                return;
            }

            _currentMonth.TakeHomePayText = value;
            OnPropertyChanged();
            RefreshBudgetSummary();
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

    public bool IsUndoAvailable => _lastRemoval is not null;

    public decimal MonthlyTakeHomePayValue => TryParseMoney(MonthlyTakeHomePayText, out var value) ? value : 0m;

    public decimal TotalDeductionsValue => LineItems.Sum(item => item.PlannedAmount);

    public decimal TotalActualValue => LineItems.Sum(item => item.ActualAmount);

    public decimal LeftoverValue => MonthlyTakeHomePayValue - TotalDeductionsValue;

    public string MonthlyTakeHomePayDisplay => MoneyText.Format(MonthlyTakeHomePayValue);

    public string TotalDeductionsDisplay => MoneyText.Format(TotalDeductionsValue);

    public string LeftoverDisplay => MoneyText.Format(LeftoverValue);

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

    public bool IsOverBudget => MonthlyTakeHomePayValue > 0m && LeftoverValue < 0m;

    public double BudgetUsagePercent
    {
        get
        {
            var income = MonthlyTakeHomePayValue;
            if (income <= 0)
            {
                return 0d;
            }

            return Math.Min(100d, (double)((TotalDeductionsValue / income) * 100m));
        }
    }

    public string UsageSeverity
    {
        get
        {
            var income = MonthlyTakeHomePayValue;
            if (income <= 0)
            {
                return "Normal";
            }

            var usage = (TotalDeductionsValue / income) * 100m;
            if (usage > 100m)
            {
                return "Over";
            }

            return usage >= 85m ? "High" : "Normal";
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
                return $"Enter your take-home pay for {CurrentMonthLabel} to see your available budget.";
            }

            if (leftover >= 0)
            {
                return $"You have {MoneyText.Format(leftover)} left after planned expenses.";
            }

            return $"You are over budget by {MoneyText.Format(Math.Abs(leftover))}.";
        }
    }

    public string SpendingSummaryMessage
    {
        get
        {
            if (LineItems.Count == 0)
            {
                return "Your categories are summarized as you add line items.";
            }

            if (TotalActualValue <= 0)
            {
                return $"Nothing spent yet against {TotalDeductionsDisplay} planned. Edit an item's Spent box to track it.";
            }

            return $"Spent {MoneyText.Format(TotalActualValue)} of {TotalDeductionsDisplay} planned so far.";
        }
    }

    public decimal TotalGoalTargetValue => SavingsGoals.Sum(goal => goal.TargetAmount);

    public decimal TotalGoalSavedValue => SavingsGoals.Sum(goal => goal.SavedAmount);

    public decimal MonthlyIncomeHistoryTotalValue => IncomeEntries.Sum(entry => entry.Amount);

    public decimal MonthlyIncomeHistoryAverageValue => IncomeEntries.Count == 0 ? 0m : MonthlyIncomeHistoryTotalValue / IncomeEntries.Count;

    public string TotalGoalTargetDisplay => MoneyText.Format(TotalGoalTargetValue);

    public string TotalGoalSavedDisplay => MoneyText.Format(TotalGoalSavedValue);

    public string MonthlyIncomeHistoryTotalDisplay => MoneyText.Format(MonthlyIncomeHistoryTotalValue);

    public string MonthlyIncomeHistoryAverageDisplay => MoneyText.Format(MonthlyIncomeHistoryAverageValue);

    public string LatestIncomeDisplay => IncomeEntries
        .OrderBy(entry => entry.SortKey)
        .LastOrDefault()?.DisplayAmount ?? MoneyText.Format(0m);

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

    private void SwitchToMonth(DateTime target)
    {
        _currentMonth = GetOrCreateMonth(target);
        OnPropertyChanged(nameof(LineItems));
        OnPropertyChanged(nameof(MonthlyTakeHomePayText));
        OnPropertyChanged(nameof(CurrentMonth));
        OnPropertyChanged(nameof(CurrentMonthLabel));
        RefreshBudgetSummary();
    }

    private BudgetMonth GetOrCreateMonth(DateTime month)
    {
        var key = new DateTime(month.Year, month.Month, 1).ToString("yyyy-MM", CultureInfo.InvariantCulture);
        if (!_months.TryGetValue(key, out var budgetMonth))
        {
            budgetMonth = new BudgetMonth(month);
            budgetMonth.LineItems.CollectionChanged += OnLineItemsChanged;
            _months[key] = budgetMonth;
        }

        return budgetMonth;
    }

    private BudgetMonth? FindNearestEarlierMonthWithItems()
    {
        return _months.Values
            .Where(month => month.Month < _currentMonth.Month && month.LineItems.Count > 0)
            .OrderByDescending(month => month.Month)
            .FirstOrDefault();
    }

    private bool CanCopyPreviousMonth()
    {
        return LineItems.Count == 0 && FindNearestEarlierMonthWithItems() is not null;
    }

    private void CopyPreviousMonth()
    {
        var source = FindNearestEarlierMonthWithItems();
        if (source is null || LineItems.Count > 0)
        {
            return;
        }

        _isRestoringState = true;
        try
        {
            _currentMonth.TakeHomePayText = source.TakeHomePayText;
            foreach (var item in source.LineItems)
            {
                LineItems.Add(new BudgetLineItem(item.Name, item.Category, item.PlannedAmount));
            }
        }
        finally
        {
            _isRestoringState = false;
        }

        OnPropertyChanged(nameof(MonthlyTakeHomePayText));
        StatusMessage = $"Copied the {source.Month.ToString("MMMM yyyy", CultureInfo.CurrentCulture)} plan into {CurrentMonthLabel}. Spent amounts start fresh.";
        RefreshBudgetSummary();
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
        StatusMessage = $"Added {item.Name} to {CurrentMonthLabel}.";
    }

    private void RemoveLineItem(BudgetLineItem? item)
    {
        if (item is null)
        {
            return;
        }

        var index = LineItems.IndexOf(item);
        if (LineItems.Remove(item))
        {
            SetLastRemoval(new RemovedEntry("LineItem", item, index, _currentMonth.MonthKey));
            StatusMessage = $"Removed {item.Name}.";
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
    }

    private void RemoveGoal(SavingsGoal? goal)
    {
        if (goal is null)
        {
            return;
        }

        var index = SavingsGoals.IndexOf(goal);
        if (SavingsGoals.Remove(goal))
        {
            SetLastRemoval(new RemovedEntry("Goal", goal, index, null));
            StatusMessage = $"Removed savings goal {goal.Name}.";
        }
    }

    private void ContributeToGoal(SavingsGoal? goal)
    {
        if (goal is null)
        {
            return;
        }

        if (!TryParseMoney(goal.ContributionText, out var amount) || amount <= 0m)
        {
            StatusMessage = "Enter a contribution amount above zero first.";
            return;
        }

        goal.SavedAmount += amount;
        goal.ContributionText = string.Empty;
        StatusMessage = $"Added {MoneyText.Format(amount)} to {goal.Name}.";
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
    }

    private void RemoveIncomeEntry(IncomeEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        var index = IncomeEntries.IndexOf(entry);
        if (IncomeEntries.Remove(entry))
        {
            SetLastRemoval(new RemovedEntry("Income", entry, index, null));
            StatusMessage = $"Removed income for {entry.MonthLabel}.";
        }
    }

    private void SetLastRemoval(RemovedEntry? entry)
    {
        _lastRemoval = entry;
        OnPropertyChanged(nameof(IsUndoAvailable));
    }

    private void UndoRemove()
    {
        var removal = _lastRemoval;
        if (removal is null)
        {
            return;
        }

        switch (removal.Kind)
        {
            case "LineItem" when removal.Item is BudgetLineItem item:
                if (removal.MonthKey is not null &&
                    TryParseMonthKey(removal.MonthKey, out var month) &&
                    GetOrCreateMonth(month) != _currentMonth)
                {
                    SwitchToMonth(month);
                }

                LineItems.Insert(Math.Min(removal.Index < 0 ? 0 : removal.Index, LineItems.Count), item);
                StatusMessage = $"Restored {item.Name}.";
                break;
            case "Goal" when removal.Item is SavingsGoal goal:
                SavingsGoals.Insert(Math.Min(removal.Index < 0 ? 0 : removal.Index, SavingsGoals.Count), goal);
                StatusMessage = $"Restored savings goal {goal.Name}.";
                break;
            case "Income" when removal.Item is IncomeEntry entry:
                IncomeEntries.Insert(Math.Min(removal.Index < 0 ? 0 : removal.Index, IncomeEntries.Count), entry);
                StatusMessage = $"Restored income for {entry.MonthLabel}.";
                break;
        }

        SetLastRemoval(null);
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
            DetachAll();
            _months.Clear();

            if (state.Months is { Count: > 0 })
            {
                foreach (var monthState in state.Months)
                {
                    if (!TryParseMonthKey(monthState.MonthKey, out var monthDate))
                    {
                        continue;
                    }

                    var month = GetOrCreateMonth(monthDate);
                    month.TakeHomePayText = string.IsNullOrWhiteSpace(monthState.TakeHomePayText)
                        ? "0"
                        : monthState.TakeHomePayText;
                    AddLineItemsFromState(month, monthState.LineItems);
                }
            }
            else
            {
                var monthDate = TryParseMonthKey(state.SelectedMonthKey, out var parsed) ? parsed : DateTime.Now;
                var month = GetOrCreateMonth(monthDate);
                month.TakeHomePayText = string.IsNullOrWhiteSpace(state.MonthlyTakeHomePayText)
                    ? "0"
                    : state.MonthlyTakeHomePayText;
                AddLineItemsFromState(month, state.LineItems);
            }

            if (TryParseMonthKey(state.SelectedMonthKey, out var selected) &&
                _months.ContainsKey(selected.ToString("yyyy-MM", CultureInfo.InvariantCulture)))
            {
                _currentMonth = GetOrCreateMonth(selected);
            }
            else
            {
                var latest = _months.Values.OrderBy(month => month.Month).LastOrDefault();
                _currentMonth = latest ?? GetOrCreateMonth(DateTime.Now);
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

            SetLastRemoval(null);

            var itemCount = _months.Values.Sum(month => month.LineItems.Count);
            StatusMessage = statusMessage ?? $"Restored {itemCount} line item{(itemCount == 1 ? string.Empty : "s")}, {SavingsGoals.Count} goal{(SavingsGoals.Count == 1 ? string.Empty : "s")}, and {IncomeEntries.Count} income record{(IncomeEntries.Count == 1 ? string.Empty : "s")} from your last session.";
        }
        finally
        {
            _isRestoringState = false;
            OnPropertyChanged(nameof(LineItems));
            OnPropertyChanged(nameof(MonthlyTakeHomePayText));
            OnPropertyChanged(nameof(CurrentMonth));
            OnPropertyChanged(nameof(CurrentMonthLabel));
            RefreshBudgetSummary();
        }
    }

    private void AddLineItemsFromState(BudgetMonth month, IEnumerable<BudgetLineItemState>? items)
    {
        foreach (var item in items ?? Enumerable.Empty<BudgetLineItemState>())
        {
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                continue;
            }

            var category = NormalizeCategory(item.Category);
            EnsureCategoryOption(category);
            month.LineItems.Add(new BudgetLineItem(item.Name.Trim(), category, item.Amount, item.ActualAmount));
        }
    }

    private void DetachAll()
    {
        foreach (var month in _months.Values)
        {
            foreach (var item in month.LineItems)
            {
                item.PropertyChanged -= OnLineItemPropertyChanged;
            }

            month.LineItems.CollectionChanged -= OnLineItemsChanged;
        }

        foreach (var goal in SavingsGoals)
        {
            goal.PropertyChanged -= OnGoalPropertyChanged;
        }

        foreach (var entry in IncomeEntries)
        {
            entry.PropertyChanged -= OnIncomeEntryPropertyChanged;
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
        if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems.OfType<BudgetLineItem>())
            {
                item.PropertyChanged += OnLineItemPropertyChanged;
            }
        }

        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems.OfType<BudgetLineItem>())
            {
                item.PropertyChanged -= OnLineItemPropertyChanged;
            }
        }

        RefreshBudgetSummary();
    }

    private void OnLineItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isRestoringState)
        {
            return;
        }

        if (e.PropertyName == nameof(BudgetLineItem.Category) && sender is BudgetLineItem item)
        {
            EnsureCategoryOption(item.Category);
        }

        if (e.PropertyName is nameof(BudgetLineItem.Name)
            or nameof(BudgetLineItem.Category)
            or nameof(BudgetLineItem.PlannedAmount)
            or nameof(BudgetLineItem.ActualAmount))
        {
            RefreshBudgetSummary();
        }
    }

    private void OnSavingsGoalsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (var goal in e.NewItems.OfType<SavingsGoal>())
            {
                goal.PropertyChanged += OnGoalPropertyChanged;
            }
        }

        if (e.OldItems is not null)
        {
            foreach (var goal in e.OldItems.OfType<SavingsGoal>())
            {
                goal.PropertyChanged -= OnGoalPropertyChanged;
            }
        }

        RefreshBudgetSummary();
    }

    private void OnGoalPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isRestoringState)
        {
            return;
        }

        if (e.PropertyName is nameof(SavingsGoal.Name)
            or nameof(SavingsGoal.TargetAmount)
            or nameof(SavingsGoal.SavedAmount))
        {
            RefreshBudgetSummary();
        }
    }

    private void OnIncomeEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (var entry in e.NewItems.OfType<IncomeEntry>())
            {
                entry.PropertyChanged += OnIncomeEntryPropertyChanged;
            }
        }

        if (e.OldItems is not null)
        {
            foreach (var entry in e.OldItems.OfType<IncomeEntry>())
            {
                entry.PropertyChanged -= OnIncomeEntryPropertyChanged;
            }
        }

        RefreshBudgetSummary();
    }

    private void OnIncomeEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isRestoringState)
        {
            return;
        }

        if (e.PropertyName == nameof(IncomeEntry.Amount))
        {
            RefreshBudgetSummary();
        }
    }

    private void RefreshBudgetSummary()
    {
        OnPropertyChanged(nameof(MonthlyTakeHomePayValue));
        OnPropertyChanged(nameof(TotalDeductionsValue));
        OnPropertyChanged(nameof(TotalActualValue));
        OnPropertyChanged(nameof(LeftoverValue));
        OnPropertyChanged(nameof(MonthlyTakeHomePayDisplay));
        OnPropertyChanged(nameof(TotalDeductionsDisplay));
        OnPropertyChanged(nameof(LeftoverDisplay));
        OnPropertyChanged(nameof(BudgetUsageDisplay));
        OnPropertyChanged(nameof(IsOverBudget));
        OnPropertyChanged(nameof(BudgetUsagePercent));
        OnPropertyChanged(nameof(UsageSeverity));
        OnPropertyChanged(nameof(BudgetSummaryMessage));
        OnPropertyChanged(nameof(SpendingSummaryMessage));
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
        CopyPreviousMonthCommand.RaiseCanExecuteChanged();

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

        var income = MonthlyTakeHomePayValue;
        var totals = LineItems
            .GroupBy(item => string.IsNullOrWhiteSpace(item.Category) ? "General" : item.Category.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                Category = group.Key,
                Planned = group.Sum(item => item.PlannedAmount),
                Actual = group.Sum(item => item.ActualAmount)
            })
            .OrderByDescending(group => group.Planned)
            .ToList();

        foreach (var total in totals)
        {
            var consumption = total.Planned > 0m
                ? (double)((total.Actual / total.Planned) * 100m)
                : total.Actual > 0m ? 100d : 0d;
            var percent = Math.Min(100d, consumption);
            var severity = (total.Planned <= 0m && total.Actual > 0m) || total.Actual > total.Planned
                ? "Over"
                : consumption >= 85d ? "High" : "Normal";

            var detail = $"{MoneyText.Format(total.Actual)} of {MoneyText.Format(total.Planned)}";
            if (income > 0m && total.Planned > 0m)
            {
                detail += $" · {(total.Planned / income) * 100m:0}% of pay";
            }

            CategorySummaries.Add(new CategorySummaryItem(total.Category, total.Planned, total.Actual, percent, severity, detail));
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
        var months = _months.Values
            .Where(month => month == _currentMonth
                            || month.LineItems.Count > 0
                            || (TryParseMoney(month.TakeHomePayText, out var pay) && pay > 0m))
            .OrderBy(month => month.Month)
            .Select(month => new BudgetMonthState
            {
                MonthKey = month.MonthKey,
                TakeHomePayText = month.TakeHomePayText,
                LineItems = new ObservableCollection<BudgetLineItemState>(
                    month.LineItems.Select(item => new BudgetLineItemState
                    {
                        Name = item.Name,
                        Category = item.Category,
                        Amount = item.PlannedAmount,
                        ActualAmount = item.ActualAmount
                    }))
            });

        return new BudgetState
        {
            SelectedMonthKey = _currentMonth.MonthKey,
            Months = new ObservableCollection<BudgetMonthState>(months),
            MonthlyTakeHomePayText = _currentMonth.TakeHomePayText,
            LineItems = new ObservableCollection<BudgetLineItemState>(
                LineItems.Select(item => new BudgetLineItemState
                {
                    Name = item.Name,
                    Category = item.Category,
                    Amount = item.PlannedAmount,
                    ActualAmount = item.ActualAmount
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
        return MoneyText.TryParse(text, out value);
    }

    private static decimal ParseMoney(string text)
    {
        return TryParseMoney(text, out var value) ? value : 0m;
    }
}
