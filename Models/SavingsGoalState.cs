namespace Budget.Models;

public sealed class SavingsGoalState
{
    public string Name { get; set; } = string.Empty;

    public decimal TargetAmount { get; set; }

    public decimal SavedAmount { get; set; }
}

