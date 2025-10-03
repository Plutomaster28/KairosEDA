@echo off
echo ================================================
echo   Kairos EDA - Build and Run Script
echo ================================================
echo.

echo Restoring NuGet packages...
dotnet restore
if errorlevel 1 (
    echo [ERROR] Failed to restore packages
    pause
    exit /b 1
)

echo.
echo Building project...
dotnet build --configuration Release
if errorlevel 1 (
    echo [ERROR] Build failed
    pause
    exit /b 1
)

echo.
echo ================================================
echo   Build successful! Launching Kairos EDA...
echo ================================================
echo.

dotnet run --project KairosEDA\KairosEDA.csproj --configuration Release

pause
