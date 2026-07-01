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

if (hadExisting)
{
    File.Copy(statePath, backupPath, true);
}

if (hadPlacementExisting)
{
    File.Copy(placementStatePath, placementBackupPath, true);
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
        Amount = 1500m
    });

    state.LineItems.Add(new BudgetLineItemState
    {
        Name = "Internet",
        Amount = 79.99m
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

    if (File.Exists(backupPath))
    {
        File.Delete(backupPath);
    }

    if (File.Exists(placementBackupPath))
    {
        File.Delete(placementBackupPath);
    }
}

