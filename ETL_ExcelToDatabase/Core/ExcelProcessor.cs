using ExcelDataReader;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using ETL_ExcelToDatabase.Models;
using ETL_ExcelToDatabase.Services;

namespace ETL_ExcelToDatabase.Core
{
    public static class ExcelProcessor
    {
        public static IEnumerable<BatchResult> LoadExcelInBatches(
    string excelFilePath,
    SqlConnection connection,
    string errorTableName,
    int batchSize,
    string defaultSheetName,
    bool enableProgressNotifications = true,
    int progressNotificationInterval = 50000)
        {
            // Register encoding provider for ExcelDataReader
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var stream = File.Open(excelFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            // Move to first sheet
            if (reader == null)
            {
                throw new ArgumentException("Excel file is empty.");
            }

            ConsoleLogger.LogInfo("file", $"Processing sheet: {reader.Name}");

            // Read header row
            if (!reader.Read())
            {
                throw new ArgumentException("Excel file has no header row.");
            }

            var headers = new string[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
            {
                headers[i] = reader.GetValue(i)?.ToString() ?? $"Column_{i + 1}";  
            }

            var currentBatch = CreateDataTable(headers);
            bool isFirstBatch = true;
            int processedRows = 0;
            int totalRows = 0;

            // Count total rows for progress reporting (approximate)
            var currentPosition = stream.Position;
            while (reader.Read()) totalRows++;
            stream.Position = currentPosition;
            reader.Reset();
            reader.Read(); // Skip header again

            // Process data rows
            while (reader.Read())
            {
                bool rowProcessedSuccessfully = true;
                try
                {  
                    ProcessExcelDataReaderRow(reader, currentBatch, headers.Length);
                    processedRows++;

                    if (enableProgressNotifications && processedRows % progressNotificationInterval == 0)
                    {
                        ConsoleLogger.LogProgress("Processing Excel rows", processedRows, totalRows);
                    }
                }
                catch (Exception ex)
                {
                    rowProcessedSuccessfully = false;
                    // Log error and continue processing
                    _ = LoggingService.LogError(
                        connection,
                        errorTableName,
                        Path.GetFileName(excelFilePath),
                        $"Row {processedRows + 2}", // +2 because of 0-index and header
                        "Data Processing Error",
                        ex.Message);

                    ConsoleLogger.LogError($"Error processing row {processedRows + 2}: {ex.Message}");
                }

                // Only check batch size if row was processed successfully
                if (rowProcessedSuccessfully && currentBatch.Rows.Count >= batchSize)
                {
                    yield return new BatchResult
                    {
                        Data = currentBatch,
                        IsFirstBatch = isFirstBatch,
                        IsLastBatch = false
                    };
                    currentBatch = CreateDataTable(headers);
                    isFirstBatch = false;
                }
            }

            // Return the final batch if it contains any rows
            if (currentBatch.Rows.Count > 0)
            {
                yield return new BatchResult
                {
                    Data = currentBatch,
                    IsFirstBatch = isFirstBatch,
                    IsLastBatch = true
                };
            }

            ConsoleLogger.LogSuccess($"Completed processing {processedRows:N0} rows");
        }

        private static DataTable CreateDataTable(string[] headers)
        {
            DataTable dt = new();
            foreach (string header in headers)
            {
                // Keep original column names, including special characters
                dt.Columns.Add(header, typeof(string));
            }
            return dt;
        }

        private static void ProcessExcelDataReaderRow(IExcelDataReader reader, DataTable dt, int expectedColumnCount)
        {
            DataRow dataRow = dt.NewRow();

            // Process each cell in the row
            for (int i = 0; i < expectedColumnCount; i++)
            {
                dataRow[i] = GetCellValueFromReader(reader, i);
            }

            dt.Rows.Add(dataRow);
        }

        private static object GetCellValueFromReader(IExcelDataReader reader, int columnIndex)
        {
            var value = reader.GetValue(columnIndex);
            
            if (value == null || value == DBNull.Value)
                return DBNull.Value;

            // Convert Excel values to appropriate string representations
            return value switch
            {
                DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
                double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
                decimal dec => dec.ToString(System.Globalization.CultureInfo.InvariantCulture),
                bool b => b.ToString(),
                _ => value.ToString()?.Trim() ?? string.Empty
            };
        }
    }
}