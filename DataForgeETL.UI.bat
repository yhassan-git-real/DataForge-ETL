@echo off
chcp 65001 >nul
echo ========================================
echo DataForge ETL UI
echo ========================================
echo.

REM Kill any running instances
taskkill /F /IM DataForgeETL.UI.exe 2>nul

REM Build Core module first (UI references Core DLL)
echo Building Core module...
dotnet build "Core\DataForgeETL.csproj" -c Release
if errorlevel 1 (
    echo Core build failed!
    pause
    exit /b 1
)

REM Check if Release build exists and rebuild
if exist "DataForgeETL.UI\bin\Release\net8.0\DataForgeETL.UI.exe" (
    echo Release build found. Cleaning and rebuilding...
    dotnet clean "DataForgeETL.UI\DataForgeETL.UI.csproj" -c Release
    dotnet build "DataForgeETL.UI\DataForgeETL.UI.csproj" -c Release
    if errorlevel 1 (
        echo Build failed!
        pause
        exit /b 1
    )
    echo Build completed successfully.
    echo.
) else (
    echo Release build not found. Building UI...
    dotnet build "DataForgeETL.UI\DataForgeETL.UI.csproj" -c Release
    if errorlevel 1 (
        echo Build failed!
        pause
        exit /b 1
    )
    echo Build completed successfully.
    echo.
)

REM Run the application
echo Starting UI Application...
start "" "DataForgeETL.UI\bin\Release\net8.0\DataForgeETL.UI.exe"

echo Application launched successfully.