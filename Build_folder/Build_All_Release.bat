@echo off
echo ========================================
echo Build All Modules - Release
echo ========================================
echo.

echo This will build all modules in Release configuration...
echo Configuration: Release
echo.
pause

REM Build Core module
echo.
echo [1/6] Building Core...
dotnet build "..\Core\DataForgeETL.csproj" -c Release
if errorlevel 1 (
    echo Core build failed!
    pause
    exit /b 1
)
echo Core build completed successfully.

REM Build ETL_CsvToDatabase
echo.
echo [2/6] Building ETL_CsvToDatabase...
dotnet build "..\ETL_CsvToDatabase\ETL_CsvToDatabase.csproj" -c Release
if errorlevel 1 (
    echo ETL_CsvToDatabase build failed!
    pause
    exit /b 1
)
echo ETL_CsvToDatabase build completed successfully.

REM Build ETL_DynamicTableManager
echo.
echo [3/6] Building ETL_DynamicTableManager...
dotnet build "..\ETL_DynamicTableManager\ETL_DynamicTableManager.csproj" -c Release
if errorlevel 1 (
    echo ETL_DynamicTableManager build failed!
    pause
    exit /b 1
)
echo ETL_DynamicTableManager build completed successfully.

REM Build ETL_Excel
echo.
echo [4/6] Building ETL_Excel...
dotnet build "..\ETL_Excel\ETL_Excel.csproj" -c Release
if errorlevel 1 (
    echo ETL_Excel build failed!
    pause
    exit /b 1
)
echo ETL_Excel build completed successfully.

REM Build ETL_ExcelToDatabase
echo.
echo [5/6] Building ETL_ExcelToDatabase...
dotnet build "..\ETL_ExcelToDatabase\ETL_ExcelToDatabase.csproj" -c Release
if errorlevel 1 (
    echo ETL_ExcelToDatabase build failed!
    pause
    exit /b 1
)
echo ETL_ExcelToDatabase build completed successfully.

REM Build DataForgeETL.UI
echo.
echo [6/6] Building DataForgeETL.UI...
dotnet build "..\DataForgeETL.UI\DataForgeETL.UI.csproj" -c Release
if errorlevel 1 (
    echo DataForgeETL.UI build failed!
    pause
    exit /b 1
)
echo DataForgeETL.UI build completed successfully.

echo.
echo ========================================
echo All Builds Completed Successfully!
echo ========================================
echo.
echo Release builds created at:
echo   ..\Core\bin\Release\net8.0\win-x64\
echo   ..\ETL_CsvToDatabase\bin\Release\net8.0\win-x64\
echo   ..\ETL_DynamicTableManager\bin\Release\net8.0\win-x64\
echo   ..\ETL_Excel\bin\Release\net8.0\win-x64\
echo   ..\ETL_ExcelToDatabase\bin\Release\net8.0\win-x64\
echo   ..\DataForgeETL.UI\bin\Release\net8.0\
echo.
pause
