using System.Globalization;
using System.IO;
using System.Text.Json;
using Budget.Models;
using Microsoft.VisualBasic.FileIO;

namespace Budget.Services;

public sealed class BudgetFileExchangeService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public void Export(BudgetState state, string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        switch (extension)
        {
            case ".csv":
                File.WriteAllText(filePath, SerializeCsv(state));
                break;
            case ".json":
            default:
                File.WriteAllText(filePath, JsonSerializer.Serialize(state, JsonOptions));
                break;
        }
    }

    public BudgetState Import(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".csv" => DeserializeCsv(File.ReadAllText(filePath)),
            _ => JsonSerializer.Deserialize<BudgetState>(File.ReadAllText(filePath), JsonOptions) ?? new BudgetState()
        };
    }

    private static string SerializeCsv(BudgetState state)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        writer.WriteLine("Type,Name,Category,MonthKey,TargetAmount,SavedAmount,Amount");
        writer.WriteLine(WriteCsvRow("Income", "Monthly Take-Home Pay", string.Empty, string.Empty, string.Empty, string.Empty, state.MonthlyTakeHomePayText));

        foreach (var item in state.LineItems)
        {
            writer.WriteLine(WriteCsvRow("Expense", item.Name, item.Category, string.Empty, string.Empty, string.Empty, item.Amount.ToString(CultureInfo.InvariantCulture)));
        }

        foreach (var goal in state.SavingsGoals)
        {
            writer.WriteLine(WriteCsvRow("Goal", goal.Name, string.Empty, string.Empty, goal.TargetAmount.ToString(CultureInfo.InvariantCulture), goal.SavedAmount.ToString(CultureInfo.InvariantCulture), string.Empty));
        }

        foreach (var entry in state.IncomeEntries)
        {
            writer.WriteLine(WriteCsvRow("IncomeEntry", string.Empty, string.Empty, entry.MonthKey, string.Empty, string.Empty, entry.Amount.ToString(CultureInfo.InvariantCulture)));
        }

        return writer.ToString();
    }

    private static BudgetState DeserializeCsv(string csv)
    {
        var state = new BudgetState();
        using var reader = new StringReader(csv);
        using var parser = new TextFieldParser(reader)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = true
        };
        parser.SetDelimiters(",");

        var headerConsumed = false;
        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields is null || fields.Length == 0)
            {
                continue;
            }

            if (!headerConsumed)
            {
                headerConsumed = true;
                continue;
            }

            var type = fields.ElementAtOrDefault(0)?.Trim() ?? string.Empty;
            var name = fields.ElementAtOrDefault(1)?.Trim() ?? string.Empty;
            var category = fields.ElementAtOrDefault(2)?.Trim() ?? string.Empty;
            var monthKey = fields.ElementAtOrDefault(3)?.Trim() ?? string.Empty;
            var targetText = fields.ElementAtOrDefault(4)?.Trim() ?? string.Empty;
            var savedText = fields.ElementAtOrDefault(5)?.Trim() ?? string.Empty;
            var amountText = fields.ElementAtOrDefault(6)?.Trim() ?? string.Empty;

            if (string.Equals(type, "Income", StringComparison.OrdinalIgnoreCase))
            {
                state.MonthlyTakeHomePayText = string.IsNullOrWhiteSpace(amountText) ? "0" : amountText;
                continue;
            }

            if (!string.Equals(type, "Expense", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(type, "Goal", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    if (!decimal.TryParse(targetText, NumberStyles.Currency, CultureInfo.InvariantCulture, out var targetAmount) &&
                        !decimal.TryParse(targetText, NumberStyles.Currency, CultureInfo.CurrentCulture, out targetAmount))
                    {
                        targetAmount = 0m;
                    }

                    if (!decimal.TryParse(savedText, NumberStyles.Currency, CultureInfo.InvariantCulture, out var savedAmount) &&
                        !decimal.TryParse(savedText, NumberStyles.Currency, CultureInfo.CurrentCulture, out savedAmount))
                    {
                        savedAmount = 0m;
                    }

                    state.SavingsGoals.Add(new SavingsGoalState
                    {
                        Name = name,
                        TargetAmount = targetAmount,
                        SavedAmount = savedAmount
                    });

                    continue;
                }

                if (string.Equals(type, "IncomeEntry", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(monthKey))
                    {
                        continue;
                    }

                    if (!decimal.TryParse(amountText, NumberStyles.Currency, CultureInfo.InvariantCulture, out var incomeAmount) &&
                        !decimal.TryParse(amountText, NumberStyles.Currency, CultureInfo.CurrentCulture, out incomeAmount))
                    {
                        incomeAmount = 0m;
                    }

                    state.IncomeEntries.Add(new IncomeEntryState
                    {
                        MonthKey = monthKey,
                        Amount = incomeAmount
                    });

                    continue;
                }

                continue;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (!decimal.TryParse(amountText, NumberStyles.Currency, CultureInfo.InvariantCulture, out var amount) &&
                !decimal.TryParse(amountText, NumberStyles.Currency, CultureInfo.CurrentCulture, out amount))
            {
                amount = 0m;
            }

            state.LineItems.Add(new BudgetLineItemState
            {
                Name = name,
                Category = category,
                Amount = amount
            });
        }

        return state;
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return '"' + value.Replace("\"", "\"\"") + '"';
        }

        return value;
    }

    private static string WriteCsvRow(params string[] fields)
    {
        return string.Join(",", fields.Select(EscapeCsv));
    }
}

