# KairosEDA Release Build Script
# Creates a self-contained single-file executable for distribution

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  KairosEDA Release Builder" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Clean previous builds
Write-Host "[1/4] Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean -c Release

# Restore dependencies
Write-Host "[2/4] Restoring dependencies..." -ForegroundColor Yellow
dotnet restore

# Build in Release mode
Write-Host "[3/4] Building Release configuration..." -ForegroundColor Yellow
dotnet build -c Release

# Publish self-contained single-file
Write-Host "[4/4] Publishing self-contained executable..." -ForegroundColor Yellow
dotnet publish KairosEDA/KairosEDA.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

$outputPath = "KairosEDA\bin\Release\net8.0-windows\win-x64\publish\KairosEDA.exe"
if (Test-Path $outputPath) {
    $fileSize = (Get-Item $outputPath).Length / 1MB
    Write-Host "Executable created at:" -ForegroundColor Cyan
    Write-Host "  $outputPath" -ForegroundColor White
    Write-Host ""
    Write-Host "File size: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "This single .exe file can be distributed to testers." -ForegroundColor Green
    Write-Host "No installation required - just run it!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Requirements for testers:" -ForegroundColor Yellow
    Write-Host "  - Windows 10/11 (64-bit)" -ForegroundColor White
    Write-Host "  - WSL2 (will be prompted to install if missing)" -ForegroundColor White
} else {
    Write-Host "ERROR: Build failed - output file not found" -ForegroundColor Red
    exit 1
}
