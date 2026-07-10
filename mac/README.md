# Budget for macOS — native SwiftUI

A fully native Apple port of the Budget app, written from scratch in Swift/SwiftUI
for **macOS 26 Tahoe** with the Liquid Glass design language. It is a sibling of
the WPF app at the repo root and the Avalonia copy in `macos/` — same features,
same data format, but built the way a first-party Mac app would be.

## Requirements

- macOS 26 (Tahoe) — Liquid Glass APIs are the deployment target
- Xcode 26 or newer

## Build & run

1. Open `mac/Budget.xcodeproj` in Xcode.
2. In the target's *Signing & Capabilities*, pick your team (personal team is fine).
3. Press **⌘R**.

There are no external dependencies — only Apple frameworks (SwiftUI, Charts,
Observation, UniformTypeIdentifiers).

## What's native here

| Area | How it's done on the Mac |
| --- | --- |
| UI | SwiftUI with `NavigationSplitView` sidebar (Budget / Goals / Income) |
| Liquid Glass | Glass toolbar + sidebar (automatic), `glassEffect` stat tiles and floating status bar, `GlassEffectContainer`, `.glass` / `.glassProminent` buttons, `ToolbarSpacer`, soft scroll-edge effects |
| State | `@Observable` (Observation framework) models and store — the MVVM `ObservableObject`/`RelayCommand` plumbing has no Swift equivalent needed |
| Income chart | Swift Charts line + area + point marks with `chartXSelection` hover crosshair and a glass annotation, replacing the custom WPF `IncomeTickerChart` |
| Currency | `Decimal` + `FormatStyle.currency`, locale-aware like the WPF `MoneyText` |
| Menus | Real menu bar: **File ▸ Import/Export** (⇧⌘I, ⌘E, ⇧⌘E) and a **Budget** menu (Previous/Next Month ⌘[ / ⌘], Copy Last Month's Plan, Undo Remove ⌥⌘Z) |
| Theme | System/Light/Dark in **Settings (⌘,)**, stored in `UserDefaults` (the native stand-in for `theme-settings.json`) |
| Window | Frame restoration handled by the system (no `window-state.json`) |
| Files | Sandboxed, with native open/save panels via `fileImporter`/`fileExporter` |

## Feature parity with the Windows app

Everything the WPF app does is here with the same behavior and status messages:
per-month budgets with previous/next navigation, "copy last month's plan",
line items with planned/spent amounts and quick "add spending", live category
meters with the 85%/over-budget severity colors, savings goals with
contributions and progress, monthly income history with the trend chart and
latest-vs-previous change badge, single-level Undo for removals, JSON/CSV
import/export, and autosave on every change (debounced ~250 ms plus a save on
quit).

Source map, if you're coming from the C# side:

| Windows | Mac |
| --- | --- |
| `Models/BudgetState.cs` | `Budget/Models/BudgetStateFile.swift` |
| `Models/*.cs` (live models) | `Budget/Models/DomainModels.swift` |
| `ViewModels/MainViewModel.cs` | `Budget/Store/BudgetStore.swift` |
| `Infrastructure/MoneyText.cs` | `Budget/Support/MoneyText.swift` |
| `Services/BudgetStateStore.cs` | `Budget/Services/BudgetStateStore.swift` |
| `Services/BudgetFileExchangeService.cs` | `Budget/Services/BudgetFileExchange.swift` |
| `MainWindow.xaml` | `Budget/Views/*.swift` |
| `Controls/IncomeTickerChart.cs` | `Budget/Views/IncomeChartView.swift` |

## Data location & migration

State is saved automatically to the app's sandbox container:

```
~/Library/Containers/com.tanidiament.Budget/Data/Library/Application Support/Budget/budget-state.json
```

The JSON schema is byte-compatible with the Windows app's
`%LOCALAPPDATA%\Budget\budget-state.json` (PascalCase keys, same fields, legacy
mirrors included). To move a budget from the PC: **Export** as JSON on Windows
(or just grab `budget-state.json`), then **File ▸ Import Budget…** on the Mac.
CSV round-trips too.

## App icon

`AppIcon` in the asset catalog is an empty placeholder. For a proper Tahoe
icon, design a layered `.icon` file in Apple's **Icon Composer** and drop it
into the project (or add PNG renders of `Assets/Budget.ico` to the app icon
set) — that's the one thing that can't be authored from a Windows machine.

## Caveat

This port was written and reviewed on a Windows machine, so it has not been
compiled. The code sticks to documented, stable API shapes, but if a beta SDK
shifted a niche signature, the likeliest suspects are `ToolbarSpacer(.fixed)`
in `ContentView.swift` and the `.glassEffect(...)` calls — each is a one-line
tweak or deletion, and nothing else depends on them.
