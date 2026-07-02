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

    var june = new BudgetMonthState
    {
        MonthKey = "2026-06",
        TakeHomePayText = "$4,100.00"
    };
    june.LineItems.Add(new BudgetLineItemState
    {
        Name = "Rent",
        Category = "Housing",
        Amount = 1500m,
        ActualAmount = 1500m
    });
    june.LineItems.Add(new BudgetLineItemState
    {
        Name = "Internet",
        Category = "Utilities",
        Amount = 79.99m,
        ActualAmount = 75.50m
    });

    var july = new BudgetMonthState
    {
        MonthKey = "2026-07",
        TakeHomePayText = "$4,200.50"
    };
    july.LineItems.Add(new BudgetLineItemState
    {
        Name = "Rent",
        Category = "Housing",
        Amount = 1500m
    });

    var state = new BudgetState
    {
        SelectedMonthKey = "2026-07",
        MonthlyTakeHomePayText = "$4,200.50"
    };
    state.Months.Add(june);
    state.Months.Add(july);
    state.LineItems.Add(new BudgetLineItemState
    {
        Name = "Rent",
        Category = "Housing",
        Amount = 1500m
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

    if (loaded.SelectedMonthKey != "2026-07")
    {
        throw new InvalidOperationException($"Unexpected selected month: {loaded.SelectedMonthKey}");
    }

    if (loaded.Months.Count != 2)
    {
        throw new InvalidOperationException($"Expected 2 months, found {loaded.Months.Count}.");
    }

    var loadedJune = loaded.Months.First(m => m.MonthKey == "2026-06");
    var loadedJuly = loaded.Months.First(m => m.MonthKey == "2026-07");

    if (loadedJune.TakeHomePayText != "$4,100.00" || loadedJuly.TakeHomePayText != "$4,200.50")
    {
        throw new InvalidOperationException("Per-month take-home pay did not round-trip correctly.");
    }

    if (loadedJune.LineItems.Count != 2 || loadedJuly.LineItems.Count != 1)
    {
        throw new InvalidOperationException("Per-month line item counts did not round-trip correctly.");
    }

    if (loadedJune.LineItems[0].Name != "Rent" || loadedJune.LineItems[0].Amount != 1500m || loadedJune.LineItems[0].ActualAmount != 1500m)
    {
        throw new InvalidOperationException("First June line item did not round-trip correctly.");
    }

    if (loadedJune.LineItems[1].Amount != 79.99m || loadedJune.LineItems[1].ActualAmount != 75.50m)
    {
        throw new InvalidOperationException("Second June line item (planned/actual) did not round-trip correctly.");
    }

    if (loadedJune.LineItems[0].Category != "Housing" || loadedJune.LineItems[1].Category != "Utilities")
    {
        throw new InvalidOperationException("Line item categories did not round-trip correctly.");
    }

    if (loadedJuly.LineItems[0].ActualAmount != 0m)
    {
        throw new InvalidOperationException("July line item actual amount should default to zero.");
    }

    if (loaded.MonthlyTakeHomePayText != "$4,200.50" || loaded.LineItems.Count != 1)
    {
        throw new InvalidOperationException("Legacy mirror fields did not round-trip correctly.");
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

    if (importedJson.Months.Count != 2 || importedJson.SelectedMonthKey != "2026-07")
    {
        throw new InvalidOperationException("JSON export/import did not round-trip months correctly.");
    }

    if (importedJson.Months.First(m => m.MonthKey == "2026-06").LineItems[1].ActualAmount != 75.50m)
    {
        throw new InvalidOperationException("JSON export/import did not round-trip actual amounts.");
    }

    if (importedCsv.Months.Count != 2)
    {
        throw new InvalidOperationException($"CSV export/import expected 2 months, found {importedCsv.Months.Count}.");
    }

    var csvJune = importedCsv.Months.First(m => m.MonthKey == "2026-06");
    if (csvJune.TakeHomePayText != "$4,100.00" || csvJune.LineItems.Count != 2)
    {
        throw new InvalidOperationException("CSV export/import did not round-trip June correctly.");
    }

    if (csvJune.LineItems[1].Amount != 79.99m || csvJune.LineItems[1].ActualAmount != 75.50m)
    {
        throw new InvalidOperationException("CSV export/import did not round-trip planned/actual amounts.");
    }

    if (importedJson.SavingsGoals.Count != state.SavingsGoals.Count || importedJson.IncomeEntries.Count != state.IncomeEntries.Count)
    {
        throw new InvalidOperationException("JSON export/import lost goal or income history data.");
    }

    if (importedCsv.SavingsGoals.Count != state.SavingsGoals.Count || importedCsv.IncomeEntries.Count != state.IncomeEntries.Count)
    {
        throw new InvalidOperationException("CSV export/import lost goal or income history data.");
    }

    // Old-format files must still load: legacy JSON state (no Months property).
    var legacyJson = """
    {
      "MonthlyTakeHomePayText": "$3,000.00",
      "LineItems": [ { "Name": "Rent", "Category": "Housing", "Amount": 1200 } ],
      "SavingsGoals": [],
      "IncomeEntries": []
    }
    """;
    File.WriteAllText(statePath, legacyJson);

    if (!store.TryLoad(out var legacyLoaded))
    {
        throw new InvalidOperationException("Legacy JSON state failed to load.");
    }

    if (legacyLoaded.Months.Count != 0 || legacyLoaded.MonthlyTakeHomePayText != "$3,000.00" || legacyLoaded.LineItems.Count != 1 || legacyLoaded.LineItems[0].ActualAmount != 0m)
    {
        throw new InvalidOperationException("Legacy JSON state did not load with expected values.");
    }

    // Old-format CSV (7 columns, no MonthKey/ActualAmount on expenses) must still import.
    var legacyCsvPath = Path.Combine(tempFolder, "legacy.csv");
    File.WriteAllText(legacyCsvPath,
        "Type,Name,Category,MonthKey,TargetAmount,SavedAmount,Amount\r\n" +
        "Income,Monthly Take-Home Pay,,,,,\"$3,100.00\"\r\n" +
        "Expense,Groceries,Food,,,,250.75\r\n");

    var legacyCsv = exchange.Import(legacyCsvPath);
    if (legacyCsv.Months.Count != 0 || legacyCsv.MonthlyTakeHomePayText != "$3,100.00" || legacyCsv.LineItems.Count != 1 || legacyCsv.LineItems[0].Amount != 250.75m)
    {
        throw new InvalidOperationException("Legacy CSV did not import with expected values.");
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
