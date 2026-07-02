# Budget for macOS

This folder is a self-contained macOS duplicate of the Windows Budget app. It looks and behaves the same: the models, view-model, persistence, and import/export code are exact copies of the Windows version, and the UI is the same layout rebuilt with [Avalonia](https://avaloniaui.net/) (a cross-platform WPF-style framework), so it runs natively on macOS.

## Run it on a Mac
1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) for macOS.
2. From this `macos` folder:
   ```bash
   dotnet run --project ./Budget.Mac.csproj
   ```

## Build a distributable app
For Apple Silicon (M1/M2/M3/M4):
```bash
dotnet publish ./Budget.Mac.csproj -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true
```
For Intel Macs use `-r osx-x64`. The output lands in `bin/Release/net8.0/<runtime>/publish/` — run the `Budget` executable inside.

## What's shared with the Windows app
- All models, the view model, money parsing, and the JSON/CSV import-export service are byte-for-byte copies of the Windows sources.
- Data files use the same names and format (`budget-state.json`, `theme-settings.json`, `window-state.json`) stored in the platform's application-data folder, and exports made on Windows import cleanly on macOS (and vice versa).

## What's platform-specific
- `App.axaml` / `MainWindow.axaml`: the same layout and styling expressed in Avalonia XAML (style classes instead of WPF triggers).
- `Services/ThemeManager.cs`: Light/Dark/System theming via Avalonia's platform settings (System follows macOS appearance).
- `Controls/IncomeTickerChart.cs`: the income ticker chart rendered with Avalonia's drawing API — same gridlines, area wash, crosshair, and tooltip.
- `MainWindow.axaml.cs`: native macOS open/save dialogs and window-placement persistence.
- The category picker is an auto-complete box (type a new category or pick an existing one), standing in for WPF's editable combo box.

## Note
This project also runs on Windows and Linux (`dotnet run` works anywhere .NET 8 does), which is handy for testing changes without a Mac.
