// LoggingService.cs
using Microsoft.Data.SqlClient;
using ETL_ExcelToDatabase.Core;
using System.Data;

namespace ETL_ExcelToDatabase.Services
{
    public static class LoggingService
    {
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
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError($"Failed to log success to database: {ex.Message}");
                throw;
            }
        }
    }
}