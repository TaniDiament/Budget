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

## Create a downloadable Windows build
Run the publish script to generate a self-contained single-file package:

```powershell
.\publish.ps1
```

The zipped download will be created at `dist\Budget-win-x64.zip`.

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

