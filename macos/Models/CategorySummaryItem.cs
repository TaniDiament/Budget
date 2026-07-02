namespace Budget.Models;

public sealed class CategorySummaryItem
{
    public CategorySummaryItem(string category, decimal plannedAmount, decimal actualAmount, double consumptionPercent, string severity, string detailText)
    {
        Category = category;
        PlannedAmount = plannedAmount;
        ActualAmount = actualAmount;
        ConsumptionPercent = consumptionPercent;
        Severity = severity;
        DetailText = detailText;
    }

    public string Category { get; }

    public decimal PlannedAmount { get; }

    public decimal ActualAmount { get; }

    /// <summary>How much of the planned amount has been spent, clamped to 0-100 for the meter.</summary>
    public double ConsumptionPercent { get; }

    /// <summary>Normal, High (≥85% spent), or Over (spent past the plan).</summary>
    public string Severity { get; }

    public string DetailText { get; }
}
