using System.IO;
using System.Text.Json;
using Budget.Models;

namespace Budget.Services;

public sealed class BudgetStateStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _stateFolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Budget");

    private readonly string _stateFilePath;

    public BudgetStateStore()
    {
        _stateFilePath = Path.Combine(_stateFolderPath, "budget-state.json");
    }

    public bool TryLoad(out BudgetState state)
    {
        state = new BudgetState();

        try
        {
            if (!File.Exists(_stateFilePath))
            {
                return false;
            }

            var json = File.ReadAllText(_stateFilePath);
            state = JsonSerializer.Deserialize<BudgetState>(json, SerializerOptions) ?? new BudgetState();
            state.MonthlyTakeHomePayText ??= "0";
            state.LineItems ??= new BudgetState().LineItems;
            return true;
        }
        catch
        {
            state = new BudgetState();
            return false;
        }
    }

    public bool Save(BudgetState state)
    {
        try
        {
            Directory.CreateDirectory(_stateFolderPath);
            var json = JsonSerializer.Serialize(state, SerializerOptions);
            var tempFilePath = _stateFilePath + ".tmp";
            File.WriteAllText(tempFilePath, json);
            File.Copy(tempFilePath, _stateFilePath, true);
            File.Delete(tempFilePath);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

