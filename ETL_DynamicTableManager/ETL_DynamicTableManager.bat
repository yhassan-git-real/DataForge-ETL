@echo off
chcp 65001 >nul
echo ========================================
echo ETL Dynamic Table Manager
echo ========================================
echo.

REM Check if Release build exists and rebuild
if exist "bin\Release\net8.0\win-x64\ETL_DynamicTableManager.exe" (
    echo Release build found. Cleaning and rebuilding...
    dotnet clean -c Release
    dotnet build -c Release
    if errorlevel 1 (
        echo Build failed!
        pause
        exit /b 1
    )
    echo Build completed successfully.
    echo.
) else (
    echo Release build not found. Building...
    dotnet build -c Release
    if errorlevel 1 (
        echo Build failed!
        pause
        exit /b 1
    )
    echo Build completed successfully.
    echo.
)

REM Run the application
echo Starting Dynamic Table Manager...
"bin\Release\net8.0\win-x64\ETL_DynamicTableManager.exe"

echo.
pause