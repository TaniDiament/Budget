# Budget
Simple easy to use budgeting app for Windows.

Also available for macOS: the `macos/` folder contains a native Mac duplicate of the app with the same look, features, and data format — see [macos/README.md](macos/README.md).

## Features
- One-screen layout with a clean modern style and Light, Dark, or System theme (the title bar follows too)
- **Monthly budgets**: step between months with the ◀ ▶ selector — each month keeps its own take-home pay and line items
- **Copy last month's plan** into an empty month with one click (spent amounts start fresh)
- Add unlimited budget line items with a name, category, and planned amount
- **Planned vs. actual**: each line item has a Planned and a Spent box, and the category chart shows how much of each category's budget is used (amber near the limit, red when over)
- **Add spending as you go**: type a purchase into a line item's **Add spending** box and press **Add** to tack it onto the Spent total — just like contributing to a goal
- Category chart also shows each category's share of your pay ("% of pay")
- See take-home pay, planned deductions, leftover cash, and a usage meter at a glance — leftover turns red when you are over budget
- **Edit everything in place**: click any name, category, amount, or income record to change it — no delete-and-retype
- **Undo**: removing a line item, goal, or income record shows an Undo button in the status bar
- Track savings goals on their own page with progress bars, editable saved/target amounts, and a **Contribute** box to put money (like this month's leftover) toward a goal
- Track monthly income over time on its own page with a ticker-style line chart — hover any month for its exact amount, and see the latest month-over-month change at a glance
- Automatically saves as you work and restores everything when you reopen the app
- Remembers the last window size and position
- Import and export budget data as JSON or CSV (older exports still import fine)
- Includes a branded app icon

## App pages
- **Budget**: the selected month's take-home pay, line items with planned/spent amounts, and a spending-by-category chart
- **Goals**: savings goals with progress tracking and per-goal contributions
- **Income**: monthly income history with a ticker-style trend chart

## Working with months
- Use the **Budget month** selector in the header to move between months. Every summary tile, line item, and chart reflects the selected month.
- A new month starts empty. If an earlier month has a plan, the Budget tab offers **Copy last month's plan**, which copies the take-home pay and line items (planned amounts only — Spent starts at zero).
- Months you only browsed past are not saved; only months with data (and the month you're viewing) are kept.

## Tracking spending
- Each line item has **Planned** (what you budgeted) and **Spent** (what you've actually spent so far). Click either box to edit it.
- To log a purchase without doing the math yourself, type the amount into the item's **Add spending** box and press **Add** — it's added onto the Spent total.
- The **Spending by category** chart fills each category's bar by how much of its planned budget is spent: blue is fine, amber means 85%+ used, red means over budget.

## Savings goals
- Click a goal's name, Saved, or Target box to edit it.
- Type an amount into the **Contribute** box and press **Add** to put money toward the goal — the Goals page shows this month's leftover so you know what's available.

## Create a downloadable Windows build
Run the publish script to generate a self-contained single-file package:

```powershell
.\publish.ps1
```

The zipped download will be created at `dist\Budget-win-x64.zip`.

## Put it on GitHub for others to download
If you want other people to install the app from GitHub, publish a release and upload the ZIP file:

1. Run the publish script:
   ```powershell
   .\publish.ps1
   ```
2. Go to your GitHub repository.
3. Open **Releases**.
4. Create a new release.
5. Attach `dist\Budget-win-x64.zip` as the downloadable file.
6. Publish the release.

## How someone downloads and runs it from GitHub
1. Open the GitHub repository.
2. Click **Releases**.
3. Download the latest `Budget-win-x64.zip` file.
4. Extract the ZIP file to a folder on their computer.
5. Double-click `Budget.exe` inside the extracted folder.

If Windows shows a security prompt, choose **More info** and then **Run anyway** if you trust the app.

## Run it
```powershell
dotnet build .\Budget.csproj
dotnet run --project .\Budget.csproj
```

## Smoke test persistence
```powershell
dotnet run --project .\Tests\Budget.PersistenceSmokeTests.csproj
```

## Export or import your budget
Use the Import and Export buttons in the app to move data between machines or make backups. Exports include every month's plan and spending; files exported by older versions of the app import without any changes.

## Theme selection
Use the Theme dropdown in the top-right area of the app to switch between:
- `Light`
- `Dark`
- `System` (uses your Windows theme)
