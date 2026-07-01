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
        writer.WriteLine("Type,Name,Amount");
        writer.WriteLine($"Income,Monthly Take-Home Pay,{EscapeCsv(state.MonthlyTakeHomePayText)}");

        foreach (var item in state.LineItems)
        {
            writer.WriteLine($"Expense,{EscapeCsv(item.Name)},{item.Amount.ToString(CultureInfo.InvariantCulture)}");
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
            var amountText = fields.ElementAtOrDefault(2)?.Trim() ?? string.Empty;

            if (string.Equals(type, "Income", StringComparison.OrdinalIgnoreCase))
            {
                state.MonthlyTakeHomePayText = string.IsNullOrWhiteSpace(amountText) ? "0" : amountText;
                continue;
            }

            if (!string.Equals(type, "Expense", StringComparison.OrdinalIgnoreCase))
            {
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
}

