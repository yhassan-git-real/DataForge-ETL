using ETL_Excel.Models;
using ClosedXML.Excel;
using ExcelDataReader;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Data;

namespace ETL_Excel.Modules
{
    public static class ExcelProcessor
    {
        private static ConfigurationModel _config = ConfigurationManager.GetConfig();
        private static int specialSheetsMoved = 0; // Add this counter

        public static async Task<(int totalSheets, int processedSheets, int errors, int specialSheetsMoved)> ProcessExcelFileAsync(string filePath, int totalFiles, int currentFileIndex, ConsoleLogger consoleLogger)
        {
            // Register encoding provider for ExcelDataReader
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            int totalSheets = 0;
            int processedSheets = 0;
            int errors = 0;

            try
            {
                // Use ExcelDataReader for fast reading
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = ExcelReaderFactory.CreateReader(stream);
                
                var dataSetConfig = new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = false // We'll handle headers manually
                    }
                };

                var dataSet = reader.AsDataSet(dataSetConfig);
                totalSheets = dataSet.Tables.Count;

                for (int i = 0; i < totalSheets; i++)
                {
                    var dataTable = dataSet.Tables[i];
                    string sheetName = dataTable.TableName;
                    string fileInfo = ConsoleHelper.GetFileInfo(Path.GetFileName(filePath), i + 1, totalSheets);
                    consoleLogger.CaptureConsoleOutput(fileInfo, true);

                    var isProcessed = await SplitSheetAsync(filePath, dataTable, sheetName, i + 1, consoleLogger);

                    if (isProcessed)
                    {
                        processedSheets++;
                    }
                    else
                    {
                        errors++;
                    }

                    if ((i + 1) % 10 == 0 || (i + 1) == totalSheets) // Update progress less frequently
                    {
                        string progress = ConsoleHelper.GetProgress(currentFileIndex, totalFiles, i + 1, totalSheets, (double)(i + 1) / totalSheets);
                        consoleLogger.CaptureConsoleOutput(progress);
                    }
                }
                
                LogManager.LogSuccess($"Processed file: {filePath}");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Error processing file {filePath}: {ex.Message}");
                errors++;
            }

            return (totalSheets, processedSheets, errors, specialSheetsMoved);
        }

        private static async Task<bool> SplitSheetAsync(string sourceFilePath, DataTable sheetData, string sheetName, int sheetPosition, ConsoleLogger consoleLogger)
        {
            try
            {
                string outputFolder = DetermineOutputFolder(sheetName, _config);
                string outputFilePath = Path.Combine(outputFolder,
                    $"{Path.GetFileNameWithoutExtension(sourceFilePath)}_{sheetName}_{sheetPosition:D2}.xlsx");

                outputFilePath = FileManager.PrepareOutputFile(outputFilePath, consoleLogger);

                // Use ClosedXML only for writing the small individual sheet
                using (var destWorkbook = new XLWorkbook())
                {
                    var destWorksheet = destWorkbook.Worksheets.Add(sheetName);
                    
                    // Copy data from DataTable to worksheet (data only, no formatting)
                    CopyDataTableToWorksheet(sheetData, destWorksheet);

                    // Configure workbook settings
                    destWorkbook.CalculateMode = XLCalculateMode.Manual;
                    await Task.Run(() => destWorkbook.SaveAs(outputFilePath));
                }

                if (!File.Exists(outputFilePath))
                {
                    throw new Exception($"Failed to create output file: {outputFilePath}");
                }

                string sheetInfo = ConsoleHelper.GetSheetInfo(sheetName, sheetData.Rows.Count,
                    sheetData.Columns.Count, Path.GetFileName(outputFilePath));
                consoleLogger.CaptureConsoleOutput(sheetInfo, true);
                LogManager.LogSuccess($"Created file: {outputFilePath}");

                // Increment the special sheets moved counter if the sheet is special
                if (outputFolder.Contains(_config.OutputSettings.OtherCategoryFolder))
                {
                    specialSheetsMoved++;
                }

                return true;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Error processing sheet {sheetName}: {ex.Message}");
                return false;
            }
        }

        private static void CopyDataTableToWorksheet(DataTable source, IXLWorksheet destination)
        {
            try
            {
                if (source == null || source.Rows.Count == 0)
                    return;

                // Copy data row by row (values only, no formatting overhead)
                for (int rowIndex = 0; rowIndex < source.Rows.Count; rowIndex++)
                {
                    var sourceRow = source.Rows[rowIndex];
                    
                    for (int colIndex = 0; colIndex < source.Columns.Count; colIndex++)
                    {
                        var cellValue = sourceRow[colIndex];
                        var destCell = destination.Cell(rowIndex + 1, colIndex + 1);
                        
                        if (cellValue == null || cellValue == DBNull.Value)
                        {
                            destCell.Value = string.Empty;
                        }
                        else
                        {
                            // Set value directly without style copying
                            destCell.Value = cellValue.ToString();
                        }
                    }

                    // Progress logging for very large sheets
                    if (_config.ProcessingSettings.EnableProgressNotifications && rowIndex > 0 && rowIndex % _config.ProcessingSettings.SaveInterval == 0)
                    {
                        LogManager.LogSuccess($"Writing progress: {rowIndex:N0} rows written");
                    }
                }

                LogManager.LogSuccess($"Finished copying sheet with {source.Rows.Count:N0} rows");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Error in CopyDataTableToWorksheet: {ex.Message}");
                throw;
            }
        }

        private static string DetermineOutputFolder(string sheetName, ConfigurationModel config)
        {
            // Check for special keywords first
            bool isSpecialSheet = config.OutputSettings.SpecialSheetKeywords
                .Any(keyword => sheetName.Contains(keyword, StringComparison.OrdinalIgnoreCase));

            if (isSpecialSheet)
            {
                return Path.Combine(
                    config.OutputSettings.BaseFolderPath,
                    config.OutputSettings.OtherCategoryFolder
                );
            }

            return config.OutputSettings.BaseFolderPath;
        }
    }
}

