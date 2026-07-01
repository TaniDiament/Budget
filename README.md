# Budget
Simple easy to use budgeting app for Windows.

## Features
- One-screen layout with a clean modern style
- Enter your monthly take-home pay at the top
- Add unlimited budget line items with a name and amount
- See take-home pay, planned deductions, leftover cash, and usage at a glance
- Remove line items anytime
- Automatically restores your last saved budget when you reopen the app
- Includes a branded app icon
- Remembers the last window size and position
- Import and export budget data as JSON or CSV
- Choose Light, Dark, or System theme from inside the app
- Add categories to budget items and see spending by category charts
- Track savings goals on their own page with progress bars
- Track monthly income over time on its own page with an income chart

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
Use the Import and Export buttons in the app to move data between machines or make backups.

## Theme selection
Use the Theme dropdown in the top-right area of the app to switch between:
- `Light`
- `Dark`
- `System` (uses your Windows theme)

## App pages
- **Budget**: monthly take-home pay, budget items, and category chart
- **Goals**: savings goals and progress tracking
- **Income**: monthly income history and chart

