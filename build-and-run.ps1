# Kairos EDA - Build and Run Script for PowerShell

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  Kairos EDA - Build and Run Script" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Failed to restore packages" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Build failed" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "  Build successful! Launching Kairos EDA..." -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""

dotnet run --project KairosEDA\KairosEDA.csproj --configuration Release

Read-Host "Press Enter to exit"
