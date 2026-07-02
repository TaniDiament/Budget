using System.Collections.ObjectModel;

namespace Budget.Models;

public sealed class BudgetState
{
    /// <summary>Legacy mirror of the selected month's pay, kept so older builds and files stay compatible.</summary>
    public string MonthlyTakeHomePayText { get; set; } = "0";

    /// <summary>Legacy mirror of the selected month's line items, kept so older builds and files stay compatible.</summary>
    public ObservableCollection<BudgetLineItemState> LineItems { get; set; } = new();

    public string? SelectedMonthKey { get; set; }

    public ObservableCollection<BudgetMonthState> Months { get; set; } = new();

    public ObservableCollection<SavingsGoalState> SavingsGoals { get; set; } = new();

    public ObservableCollection<IncomeEntryState> IncomeEntries { get; set; } = new();
}

public sealed class BudgetMonthState
{
    public string MonthKey { get; set; } = string.Empty;

    public string TakeHomePayText { get; set; } = "0";

    public ObservableCollection<BudgetLineItemState> LineItems { get; set; } = new();
}

public sealed class BudgetLineItemState
{
    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public decimal ActualAmount { get; set; }
}
