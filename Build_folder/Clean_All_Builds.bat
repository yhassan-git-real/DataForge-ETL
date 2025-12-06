@echo off
echo ========================================
echo Clean All Build Artifacts
echo ========================================
echo.

echo This will delete all bin and obj folders from all projects...
echo This includes Debug and Release configurations.
echo.
echo WARNING: This action cannot be undone!
echo.
pause

echo.
echo Starting cleanup process...
echo.

REM Clean Core module
if exist "..\Core\bin" (
    echo [1/6] Cleaning Core\bin...
    rmdir /s /q "..\Core\bin"
    echo   ✓ Removed Core\bin
) else (
    echo [1/6] Core\bin - not found (already clean)
)

if exist "..\Core\obj" (
    echo       Cleaning Core\obj...
    rmdir /s /q "..\Core\obj"
    echo   ✓ Removed Core\obj
) else (
    echo       Core\obj - not found (already clean)
)

REM Clean ETL_CsvToDatabase
if exist "..\ETL_CsvToDatabase\bin" (
    echo [2/6] Cleaning ETL_CsvToDatabase\bin...
    rmdir /s /q "..\ETL_CsvToDatabase\bin"
    echo   ✓ Removed ETL_CsvToDatabase\bin
) else (
    echo [2/6] ETL_CsvToDatabase\bin - not found (already clean)
)

if exist "..\ETL_CsvToDatabase\obj" (
    echo       Cleaning ETL_CsvToDatabase\obj...
    rmdir /s /q "..\ETL_CsvToDatabase\obj"
    echo   ✓ Removed ETL_CsvToDatabase\obj
) else (
    echo       ETL_CsvToDatabase\obj - not found (already clean)
)

REM Clean ETL_DynamicTableManager
if exist "..\ETL_DynamicTableManager\bin" (
    echo [3/6] Cleaning ETL_DynamicTableManager\bin...
    rmdir /s /q "..\ETL_DynamicTableManager\bin"
    echo   ✓ Removed ETL_DynamicTableManager\bin
) else (
    echo [3/6] ETL_DynamicTableManager\bin - not found (already clean)
)

if exist "..\ETL_DynamicTableManager\obj" (
    echo       Cleaning ETL_DynamicTableManager\obj...
    rmdir /s /q "..\ETL_DynamicTableManager\obj"
    echo   ✓ Removed ETL_DynamicTableManager\obj
) else (
    echo       ETL_DynamicTableManager\obj - not found (already clean)
)

REM Clean ETL_Excel
if exist "..\ETL_Excel\bin" (
    echo [4/6] Cleaning ETL_Excel\bin...
    rmdir /s /q "..\ETL_Excel\bin"
    echo   ✓ Removed ETL_Excel\bin
) else (
    echo [4/6] ETL_Excel\bin - not found (already clean)
)

if exist "..\ETL_Excel\obj" (
    echo       Cleaning ETL_Excel\obj...
    rmdir /s /q "..\ETL_Excel\obj"
    echo   ✓ Removed ETL_Excel\obj
) else (
    echo       ETL_Excel\obj - not found (already clean)
)

REM Clean ETL_ExcelToDatabase
if exist "..\ETL_ExcelToDatabase\bin" (
    echo [5/6] Cleaning ETL_ExcelToDatabase\bin...
    rmdir /s /q "..\ETL_ExcelToDatabase\bin"
    echo   ✓ Removed ETL_ExcelToDatabase\bin
) else (
    echo [5/6] ETL_ExcelToDatabase\bin - not found (already clean)
)

if exist "..\ETL_ExcelToDatabase\obj" (
    echo       Cleaning ETL_ExcelToDatabase\obj...
    rmdir /s /q "..\ETL_ExcelToDatabase\obj"
    echo   ✓ Removed ETL_ExcelToDatabase\obj
) else (
    echo       ETL_ExcelToDatabase\obj - not found (already clean)
)

REM Clean DataForgeETL.UI
if exist "..\DataForgeETL.UI\bin" (
    echo [6/6] Cleaning DataForgeETL.UI\bin...
    rmdir /s /q "..\DataForgeETL.UI\bin"
    echo   ✓ Removed DataForgeETL.UI\bin
) else (
    echo [6/6] DataForgeETL.UI\bin - not found (already clean)
)

if exist "..\DataForgeETL.UI\obj" (
    echo       Cleaning DataForgeETL.UI\obj...
    rmdir /s /q "..\DataForgeETL.UI\obj"
    echo   ✓ Removed DataForgeETL.UI\obj
) else (
    echo       DataForgeETL.UI\obj - not found (already clean)
)

echo.
echo ========================================
echo Cleanup Completed Successfully!
echo ========================================
echo.
echo All bin and obj folders have been removed.
echo You can now run a fresh build using:
echo   - Build_All_Release.bat
echo   - Build_All_SelfContained.bat
echo.
pause
