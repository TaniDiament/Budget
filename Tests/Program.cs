using Budget.Models;
using Budget.Services;

var statePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "Budget",
    "budget-state.json");

var backupPath = Path.Combine(Path.GetTempPath(), "budget-state-backup.json");
var hadExisting = File.Exists(statePath);
var placementStore = new WindowPlacementStore();
var placementStatePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "Budget",
    "window-state.json");
var placementBackupPath = Path.Combine(Path.GetTempPath(), "budget-window-state-backup.json");
var hadPlacementExisting = File.Exists(placementStatePath);
var themeSettingsStore = new ThemeSettingsStore();
var themeSettingsPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "Budget",
    "theme-settings.json");
var themeSettingsBackupPath = Path.Combine(Path.GetTempPath(), "budget-theme-settings-backup.json");
var hadThemeSettingsExisting = File.Exists(themeSettingsPath);

if (hadExisting)
{
    File.Copy(statePath, backupPath, true);
}

if (hadPlacementExisting)
{
    File.Copy(placementStatePath, placementBackupPath, true);
}

if (hadThemeSettingsExisting)
{
    File.Copy(themeSettingsPath, themeSettingsBackupPath, true);
}

try
{
    var store = new BudgetStateStore();
    var exchange = new BudgetFileExchangeService();
    var state = new BudgetState
    {
        MonthlyTakeHomePayText = "$4,200.50"
    };

    state.LineItems.Add(new BudgetLineItemState
    {
        Name = "Rent",
        Category = "Housing",
        Amount = 1500m
    });

    state.LineItems.Add(new BudgetLineItemState
    {
        Name = "Internet",
        Category = "Utilities",
        Amount = 79.99m
    });

    state.SavingsGoals.Add(new SavingsGoalState
    {
        Name = "Emergency Fund",
        TargetAmount = 5000m,
        SavedAmount = 1200m
    });

    state.IncomeEntries.Add(new IncomeEntryState
    {
        MonthKey = "2026-07",
        Amount = 4200.50m
    });

    if (!store.Save(state))
    {
        throw new InvalidOperationException("Save returned false.");
    }

    if (!store.TryLoad(out var loaded))
    {
        throw new InvalidOperationException("TryLoad returned false.");
    }

    if (loaded.MonthlyTakeHomePayText != "$4,200.50")
    {
        throw new InvalidOperationException($"Unexpected monthly pay: {loaded.MonthlyTakeHomePayText}");
    }

    if (loaded.LineItems.Count != 2)
    {
        throw new InvalidOperationException($"Expected 2 line items, found {loaded.LineItems.Count}.");
    }

    if (loaded.LineItems[0].Name != "Rent" || loaded.LineItems[0].Amount != 1500m)
    {
        throw new InvalidOperationException("First line item did not round-trip correctly.");
    }

    if (loaded.LineItems[1].Name != "Internet" || loaded.LineItems[1].Amount != 79.99m)
    {
        throw new InvalidOperationException("Second line item did not round-trip correctly.");
    }

    if (loaded.LineItems[0].Category != "Housing" || loaded.LineItems[1].Category != "Utilities")
    {
        throw new InvalidOperationException("Line item categories did not round-trip correctly.");
    }

    if (loaded.SavingsGoals.Count != 1 || loaded.SavingsGoals[0].Name != "Emergency Fund" || loaded.SavingsGoals[0].SavedAmount != 1200m)
    {
        throw new InvalidOperationException("Savings goals did not round-trip correctly.");
    }

    if (loaded.IncomeEntries.Count != 1 || loaded.IncomeEntries[0].MonthKey != "2026-07" || loaded.IncomeEntries[0].Amount != 4200.50m)
    {
        throw new InvalidOperationException("Income entries did not round-trip correctly.");
    }

    var tempFolder = Path.Combine(Path.GetTempPath(), "budget-exchange-tests");
    Directory.CreateDirectory(tempFolder);

    var jsonPath = Path.Combine(tempFolder, "budget.json");
    var csvPath = Path.Combine(tempFolder, "budget.csv");

    exchange.Export(state, jsonPath);
    exchange.Export(state, csvPath);

    var importedJson = exchange.Import(jsonPath);
    var importedCsv = exchange.Import(csvPath);

    if (importedJson.MonthlyTakeHomePayText != state.MonthlyTakeHomePayText || importedJson.LineItems.Count != state.LineItems.Count)
    {
        throw new InvalidOperationException("JSON export/import did not round-trip correctly.");
    }

    if (importedCsv.MonthlyTakeHomePayText != state.MonthlyTakeHomePayText || importedCsv.LineItems.Count != state.LineItems.Count)
    {
        throw new InvalidOperationException("CSV export/import did not round-trip correctly.");
    }

    if (importedJson.SavingsGoals.Count != state.SavingsGoals.Count || importedJson.IncomeEntries.Count != state.IncomeEntries.Count)
    {
        throw new InvalidOperationException("JSON export/import lost goal or income history data.");
    }

    if (importedCsv.SavingsGoals.Count != state.SavingsGoals.Count || importedCsv.IncomeEntries.Count != state.IncomeEntries.Count)
    {
        throw new InvalidOperationException("CSV export/import lost goal or income history data.");
    }

    var placement = new WindowPlacement
    {
        Left = 120,
        Top = 140,
        Width = 1280,
        Height = 760,
        WindowState = "Maximized"
    };

    if (!placementStore.Save(placement))
    {
        throw new InvalidOperationException("Window placement save returned false.");
    }

    if (!placementStore.TryLoad(out var loadedPlacement))
    {
        throw new InvalidOperationException("Window placement load returned false.");
    }

    if (loadedPlacement.Left != 120 || loadedPlacement.Top != 140 || loadedPlacement.Width != 1280 || loadedPlacement.Height != 760 || loadedPlacement.WindowState != "Maximized")
    {
        throw new InvalidOperationException("Window placement did not round-trip correctly.");
    }

    themeSettingsStore.Save(new ThemeSettings { ThemeMode = ThemeMode.Dark });
    var loadedThemeSettings = themeSettingsStore.Load();

    if (loadedThemeSettings.ThemeMode != ThemeMode.Dark)
    {
        throw new InvalidOperationException("Theme settings did not round-trip correctly.");
    }

    Console.WriteLine("Persistence smoke test passed.");
}
finally
{
    if (hadExisting)
    {
        File.Copy(backupPath, statePath, true);
    }
    else if (File.Exists(statePath))
    {
        File.Delete(statePath);
    }

    if (hadPlacementExisting)
    {
        File.Copy(placementBackupPath, placementStatePath, true);
    }
    else if (File.Exists(placementStatePath))
    {
        File.Delete(placementStatePath);
    }

    if (hadThemeSettingsExisting)
    {
        File.Copy(themeSettingsBackupPath, themeSettingsPath, true);
    }
    else if (File.Exists(themeSettingsPath))
    {
        File.Delete(themeSettingsPath);
    }

    if (File.Exists(backupPath))
    {
        File.Delete(backupPath);
    }

    if (File.Exists(placementBackupPath))
    {
        File.Delete(placementBackupPath);
    }

    if (File.Exists(themeSettingsBackupPath))
    {
        File.Delete(themeSettingsBackupPath);
    }
}

