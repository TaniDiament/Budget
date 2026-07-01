param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishDir = Join-Path $projectRoot "dist\publish"
$zipPath = Join-Path $projectRoot "dist\Budget-win-x64.zip"

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

& dotnet publish (Join-Path $projectRoot "Budget.csproj") `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:PublishReadyToRun=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:PublishDir="$publishDir" `
    /p:EnableCompressionInSingleFile=true

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force
Write-Host "Created downloadable package: $zipPath"

