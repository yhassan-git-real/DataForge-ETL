@echo off
echo ========================================
echo ETL Excel Orchestrator
echo ========================================
echo.

REM Check if self-contained executable exists and republish
if exist "Core\bin\Release\net8.0\win-x64\publish\DataForgeETL.exe" (
    echo Self-contained executable found. Cleaning and republishing...
    dotnet clean "Core\DataForgeETL.csproj" -c Release
    dotnet publish "Core\DataForgeETL.csproj" -c Release -r win-x64 --self-contained
    if errorlevel 1 (
        echo Publish failed!
        pause
        exit /b 1
    )
    echo Publish completed successfully.
    echo.
) else (
    echo Self-contained executable not found. Publishing...
    dotnet publish "Core\DataForgeETL.csproj" -c Release -r win-x64 --self-contained
    if errorlevel 1 (
        echo Publish failed!
        pause
        exit /b 1
    )
    echo Publish completed successfully.
    echo.
)

REM Run the orchestrator
echo Starting Excel ETL Orchestrator...
"Core\bin\Release\net8.0\win-x64\publish\DataForgeETL.exe"

echo.
if %errorlevel% equ 0 (
    echo Process completed successfully
) else (
    echo Process completed with errors
)

pause