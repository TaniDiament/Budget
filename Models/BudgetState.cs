using System.Collections.ObjectModel;

namespace Budget.Models;

public sealed class BudgetState
{
    public string MonthlyTakeHomePayText { get; set; } = "0";

    public ObservableCollection<BudgetLineItemState> LineItems { get; set; } = new();

    public ObservableCollection<SavingsGoalState> SavingsGoals { get; set; } = new();

    public ObservableCollection<IncomeEntryState> IncomeEntries { get; set; } = new();
}

public sealed class BudgetLineItemState
{
    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}

