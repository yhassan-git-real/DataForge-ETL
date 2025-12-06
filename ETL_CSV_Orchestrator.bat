@echo off
chcp 65001 >nul
echo ========================================
echo ETL CSV Orchestrator
echo ========================================
echo.

REM Check if CSV to Database executable exists and rebuild
if exist "ETL_CsvToDatabase\bin\Release\net8.0\win-x64\ETL_CsvToDatabase.exe" (
    echo Release build found. Cleaning and rebuilding...
    dotnet clean "ETL_CsvToDatabase\ETL_CsvToDatabase.csproj" -c Release
    dotnet build "ETL_CsvToDatabase\ETL_CsvToDatabase.csproj" -c Release
    if errorlevel 1 (
        echo Build failed!
        pause
        exit /b 1
    )
    echo Build completed successfully.
    echo.
) else (
    echo Release build not found. Building...
    dotnet build "ETL_CsvToDatabase\ETL_CsvToDatabase.csproj" -c Release
    if errorlevel 1 (
        echo Build failed!
        pause
        exit /b 1
    )
    echo Build completed successfully.
    echo.
)

REM Run CSV to Database (includes Dynamic Table Manager step)
echo Starting CSV to Database...
"ETL_CsvToDatabase\bin\Release\net8.0\win-x64\ETL_CsvToDatabase.exe"

echo.
if %errorlevel% equ 0 (
    echo Process completed successfully
) else (
    echo Process completed with errors
)

pause
