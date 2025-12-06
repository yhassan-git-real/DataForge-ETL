// LoggingService.cs
using Microsoft.Data.SqlClient;
using ETL_CsvToDatabase.Core;
using DataForgeETL.Core;
using System.Data;

namespace ETL_CsvToDatabase.Services
{
    public static class LoggingService
    {
        private static readonly string LogDirectory;

        static LoggingService()
        {
            // Use UnifiedConfigurationManager to get the correct log path
            var unifiedConfig = DataForgeETL.Core.UnifiedConfigurationManager.Instance;
            LogDirectory = unifiedConfig.GetLogFilesPath();
            
            // Ensure Logs directory exists
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
                ConsoleLogger.LogInfo("logging", $"Created log directory: {LogDirectory}");
            }
            else
            {
                ConsoleLogger.LogInfo("logging", $"Using log directory: {LogDirectory}");
            }
        }

        private static void WriteToLogFile(string fileName, string message)
        {
            try
            {
                string logFilePath = Path.Combine(LogDirectory, fileName);
                // Don't add extra timestamp since detailed logs already have it
                File.AppendAllText(logFilePath, message);
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError($"Failed to write to log file: {ex.Message}");
            }
        }
        public static async Task LogError(
            SqlConnection connection,
            string errorTableName,
            string fileName,
            string columnName,
            string errorType,
            string reason)
        {
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                string insertErrorQuery = $@"
                    INSERT INTO {errorTableName} (
                        FileName, 
                        ColumnName, 
                        ErrorType, 
                        Reason, 
                        Timestamp
                    ) VALUES (
                        @FileName,
                        @ColumnName,
                        @ErrorType,
                        @Reason,
                        GETDATE()
                    )";

                await using var cmd = new SqlCommand(insertErrorQuery, connection);

                cmd.Parameters.AddWithValue("@FileName", fileName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ColumnName", columnName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ErrorType", errorType ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Reason", reason ?? (object)DBNull.Value);

                await cmd.ExecuteNonQueryAsync();

                // Log to console as well
                ConsoleLogger.LogError(
                    $"Error in file '{fileName}': {errorType} - {reason}");
                
                // Also write to error log file with detailed information
                string errorLogFile = $"errorlog_csv2db_{DateTime.Now:yyyyMMdd}.log";
                string detailedErrorLog = $"================== ERROR LOG =================={Environment.NewLine}" +
                    $"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
                    $"File Name: {fileName}{Environment.NewLine}" +
                    $"Column Name: {columnName}{Environment.NewLine}" +
                    $"Error Type: {errorType}{Environment.NewLine}" +
                    $"Reason: {reason}{Environment.NewLine}" +
                    $"==============================================={Environment.NewLine}";
                WriteToLogFile(errorLogFile, detailedErrorLog);
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError($"Failed to log error to database: {ex.Message}");
                throw;
            }
        }

        public static async Task LogSuccess(
            SqlConnection connection,
            string successTableName,
            string message,
            long totalRows,
            int sourceColumns,
            int destColumns,
            int matchedColumns,
            decimal processingTime,
            int rowsPerSecond)
        {
            try
            {
                const string insertSuccessQuery = @"
                    INSERT INTO {0} (
                        Message,
                        TotalRows,
                        SourceColumns,
                        DestinationColumns,
                        MatchedColumns,
                        ProcessingTimeSeconds,
                        RowsPerSecond,
                        Timestamp
                    ) VALUES (
                        @Message,
                        @TotalRows,
                        @SourceColumns,
                        @DestColumns,
                        @MatchedColumns,
                        @ProcessTime,
                        @RowsPerSec,
                        GETDATE()
                    )";

                await using SqlCommand cmd = new(
                    string.Format(insertSuccessQuery, successTableName),
                    connection);

                cmd.Parameters.AddWithValue("@Message", message);
                cmd.Parameters.AddWithValue("@TotalRows", totalRows);
                cmd.Parameters.AddWithValue("@SourceColumns", sourceColumns);
                cmd.Parameters.AddWithValue("@DestColumns", destColumns);
                cmd.Parameters.AddWithValue("@MatchedColumns", matchedColumns);
                cmd.Parameters.AddWithValue("@ProcessTime", processingTime);
                cmd.Parameters.AddWithValue("@RowsPerSec", rowsPerSecond);

                await cmd.ExecuteNonQueryAsync();

                // Log to console as well
                ConsoleLogger.LogSuccess(
                    $"Successfully processed {totalRows:N0} rows with {matchedColumns} columns " +
                    $"in {processingTime:N2} seconds ({rowsPerSecond:N0} rows/sec)");
                
                // Also write to success log file with detailed information
                string successLogFile = $"successlog_csv2db_{DateTime.Now:yyyyMMdd}.log";
                string detailedSuccessLog = $"================== SUCCESS LOG =================={Environment.NewLine}" +
                    $"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
                    $"Status: SUCCESS{Environment.NewLine}" +
                    $"Message: {message}{Environment.NewLine}" +
                    $"Total Rows Processed: {totalRows:N0}{Environment.NewLine}" +
                    $"Source Columns: {sourceColumns}{Environment.NewLine}" +
                    $"Destination Columns: {destColumns}{Environment.NewLine}" +
                    $"Matched Columns: {matchedColumns}{Environment.NewLine}" +
                    $"Processing Time: {processingTime:N2} seconds{Environment.NewLine}" +
                    $"Processing Speed: {rowsPerSecond:N0} rows/second{Environment.NewLine}" +
                    $"Performance: {(totalRows / 1000000.0):F2} million rows in {processingTime:F2}s{Environment.NewLine}" +
                    $"================================================={Environment.NewLine}";
                WriteToLogFile(successLogFile, detailedSuccessLog);
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError($"Failed to log success to database: {ex.Message}");
                throw;
            }
        }
    }
}