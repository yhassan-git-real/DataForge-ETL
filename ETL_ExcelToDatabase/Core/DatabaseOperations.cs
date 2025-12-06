using System.Data;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using ETL_ExcelToDatabase.Models;
using ETL_ExcelToDatabase.Services;

namespace ETL_ExcelToDatabase.Core
{
    public static class DatabaseOperations
    {
        /// <summary>
        /// Creates or verifies the existence of error and success log tables
        /// </summary>

        public static void CreateLogTables(SqlConnection connection, string errorTableName, string successLogTableName)
        {
            ConsoleLogger.LogInfo("database", "Creating/Verifying log tables...");

            // Error log table creation query with simplified structure
            string createErrorTableQuery = $@"
                IF OBJECT_ID('[{errorTableName}]', 'U') IS NULL
                CREATE TABLE [{errorTableName}] (
                    ID INT IDENTITY(1,1) PRIMARY KEY,
                    FileName NVARCHAR(MAX),
                    ColumnName NVARCHAR(MAX),
                    ErrorType NVARCHAR(100),
                    Reason NVARCHAR(MAX),
                    Timestamp DATETIME DEFAULT GETDATE()
                )";

            // Success log table creation query
            string createSuccessTableQuery = $@"
            IF OBJECT_ID('[{successLogTableName}]', 'U') IS NULL
                CREATE TABLE [{successLogTableName}] (
                    ID INT IDENTITY(1,1) PRIMARY KEY,
                    Message NVARCHAR(MAX),
                    TotalRows BIGINT,
                    SourceColumns INT,
                    DestinationColumns INT,
                    MatchedColumns INT,
                    ProcessingTimeSeconds DECIMAL(18,2),
                    RowsPerSecond INT,
                    Timestamp DATETIME DEFAULT GETDATE()
                )";

            using (var command = new SqlCommand(createErrorTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SqlCommand(createSuccessTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }

            ConsoleLogger.LogSuccess("Log tables verified/created successfully");
        }

        /// <summary>
        /// Validates columns between temp table and destination table
        /// </summary>
        public static async Task<ValidationResult> ValidateColumnsAsync(
            SqlConnection connection,
            string tempTableName,
            string destinationTableName,
            string errorTableName,
            string fileName)
        {
            ConsoleLogger.LogInfo("validation", "Validating columns between temp and destination tables...");

            var tempColumns = await GetTableColumnsAsync(connection, tempTableName);
            var destColumns = await GetTableColumnsAsync(connection, destinationTableName);

            var matchedColumns = tempColumns.Intersect(destColumns, StringComparer.OrdinalIgnoreCase).ToList();
            var unmatchedColumns = tempColumns.Except(destColumns, StringComparer.OrdinalIgnoreCase).ToList();
            
            var result = new ValidationResult
            {
                IsValid = unmatchedColumns.Count == 0,
                MatchedColumns = matchedColumns,
                UnmatchedColumns = unmatchedColumns,
                TempColumnCount = tempColumns.Count,
                DestColumnCount = destColumns.Count
            };

            if (!result.IsValid)
            {
                string errorMessage = $"Validation Report ({DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}):\n" +
                    $"Total Source Columns: {result.TempColumnCount}\n" +
                    $"Total Destination Columns: {result.DestColumnCount}\n" +
                    $"Matched Columns: {result.MatchedColumns.Count}\n\n" +
                    $"Columns in Excel not found in destination table:\n" +
                    $"{string.Join("\n", unmatchedColumns.Select(c => $"- {c}"))}\n\n" +
                    $"Required destination table columns not found in Excel:\n" +
                    $"{string.Join("\n", destColumns.Except(tempColumns, StringComparer.OrdinalIgnoreCase).Select(c => $"- {c}"))}";

                await LoggingService.LogError(
                    connection,
                    errorTableName,
                    fileName,
                    "ColumnValidation",
                    "ColumnMismatch",
                    errorMessage);

                // Single error message in console
                ConsoleLogger.LogError($"Error in file '{fileName}': ProcessError - Validation Report ({DateTime.Now:yyyy-MM-dd HH:mm:ss.fff})\n" +
                    $"Total Source Columns: {result.TempColumnCount}\n" +
                    $"Total Destination Columns: {result.DestColumnCount}\n" +
                    $"Matched Columns: {result.MatchedColumns.Count}");
            }
            else
            {
                ConsoleLogger.LogSuccess($"Column validation successful. {result.MatchedColumns.Count} columns matched.");
            }

            return result;
        }

        /// <summary>
        /// Gets column names from a table
        /// </summary>
        private static async Task<List<string>> GetTableColumnsAsync(SqlConnection connection, string tableName)
        {
            var columns = new List<string>();
            string query = @"
                SELECT COLUMN_NAME 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = @TableName";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@TableName", tableName.Replace("[", "").Replace("]", ""));

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(0));
            }

            return columns;
        }

        /// <summary>
        /// Creates a temporary table for staging data from Excel
        /// </summary>
        public static void DropAndCreateTempTable(
            SqlConnection connection,
            string tempTableName,
            DataTable dataTable)
        {
            ConsoleLogger.LogInfo("processing", $"Creating temporary table: {tempTableName}");

            StringBuilder sql = new();
            sql.AppendLine("SET NOCOUNT ON;");
            sql.AppendLine($"IF OBJECT_ID('[{tempTableName}]', 'U') IS NOT NULL DROP TABLE [{tempTableName}];");
            sql.Append($"CREATE TABLE [{tempTableName}] (");

            // Create columns with cleaned names matching destination table format
            foreach (DataColumn column in dataTable.Columns)
            {
                string originalName = column.ColumnName;
                string columnName = column.ColumnName;
                // Clean column name to be SQL-safe (same logic as destination table creation)
                columnName = System.Text.RegularExpressions.Regex.Replace(columnName, @"[^\w]", "_");
                if (string.IsNullOrWhiteSpace(columnName) || char.IsDigit(columnName[0]))
                {
                    columnName = $"Column_{Array.IndexOf(dataTable.Columns.Cast<DataColumn>().ToArray(), column) + 1}";
                }
                ConsoleLogger.LogInfo("temp-table", $"Column mapping: '{originalName}' -> '{columnName}'");
                sql.Append($"[{columnName}] NVARCHAR(MAX),");
            }
            sql.Length--; // Remove last comma
            sql.Append(");");

            using var cmd = new SqlCommand(sql.ToString(), connection);
            cmd.ExecuteNonQuery();

            ConsoleLogger.LogSuccess($"Temporary table {tempTableName} created successfully");
        }

        /// <summary>
        /// Loads data into the temporary table using SqlBulkCopy
        /// </summary>
        public static async Task LoadDataIntoTempTableAsync(
            SqlConnection connection,
            string tempTableName,
            DataTable dataTable,
            int progressNotificationInterval = 50000,
            int bulkCopyBatchSize = 100000,
            bool enableProgressNotifications = true)
        {
            using SqlBulkCopy bulkCopy = new(
                connection,
                SqlBulkCopyOptions.TableLock |
                SqlBulkCopyOptions.KeepIdentity |
                SqlBulkCopyOptions.KeepNulls,
                null)
            {
                DestinationTableName = $"[{tempTableName}]",
                BatchSize = bulkCopyBatchSize,
                BulkCopyTimeout = 0,
                EnableStreaming = true,
                NotifyAfter = progressNotificationInterval
            };

            // Set up progress reporting only if enabled
            if (enableProgressNotifications)
            {
                bulkCopy.SqlRowsCopied += (sender, e) =>
                {
                    ConsoleLogger.LogProgress(
                        "Copying to temp table",
                        e.RowsCopied,
                        dataTable.Rows.Count);
                };
            }

            // Set up column mappings from Excel names to cleaned database names
            foreach (DataColumn column in dataTable.Columns)
            {
                string sourceColumnName = column.ColumnName; // Original Excel column name
                string targetColumnName = column.ColumnName;
                // Clean column name to match temp table structure
                targetColumnName = System.Text.RegularExpressions.Regex.Replace(targetColumnName, @"[^\w]", "_");
                if (string.IsNullOrWhiteSpace(targetColumnName) || char.IsDigit(targetColumnName[0]))
                {
                    targetColumnName = $"Column_{Array.IndexOf(dataTable.Columns.Cast<DataColumn>().ToArray(), column) + 1}";
                }
                bulkCopy.ColumnMappings.Add(sourceColumnName, targetColumnName);
            }

            await bulkCopy.WriteToServerAsync(dataTable);
            ConsoleLogger.LogSuccess($"Data loaded into temp table: {tempTableName}");
        }

        /// <summary>
        /// Truncates the destination table for the first file
        /// </summary>
        public static async Task<long> TransferDataToDestinationAsync(
    SqlConnection connection,
    string tempTableName,
    string destinationTableName,
    ValidationResult validationResult,
    int commandTimeout = 0)
        {
            try
            {
                ConsoleLogger.LogInfo("transfer", "Starting data transfer to destination table");

                // Get initial count
                long initialCount = await GetTableRowCountAsync(connection, destinationTableName);

                var matchedColumns = string.Join(", ",
                    validationResult.MatchedColumns.Select(c => $"[{c}]"));

                string transferQuery = $@"
            INSERT INTO [{destinationTableName}] ({matchedColumns})
            SELECT {matchedColumns}
            FROM [{tempTableName}]";

                using var cmd = new SqlCommand(transferQuery, connection);
                cmd.CommandTimeout = commandTimeout;
                await cmd.ExecuteNonQueryAsync();

                // Get final count and calculate rows transferred
                long finalCount = await GetTableRowCountAsync(connection, destinationTableName);
                long rowsTransferred = finalCount - initialCount;

                ConsoleLogger.LogSuccess(
                    $"Data transfer completed successfully. Rows transferred: {rowsTransferred:N0}");

                return rowsTransferred;  // Now properly returns the value
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError($"Error during data transfer: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the count of rows in a table
        /// </summary>
        public static async Task<long> GetTableRowCountAsync(
            SqlConnection connection,
            string tableName)
        {
            using var cmd = new SqlCommand(
                $"SELECT COUNT_BIG(*) FROM [{tableName}] WITH (NOLOCK)",
                connection);
            return (long)await cmd.ExecuteScalarAsync();
        }

        /// <summary>
        /// Verifies that all data was transferred correctly
        /// </summary>
        public static async Task VerifyDataTransferAsync(
            SqlConnection connection,
            string sourceTable,
            string destinationTable)
        {
            long sourceCount = await GetTableRowCountAsync(connection, sourceTable);
            long destCount = await GetTableRowCountAsync(connection, destinationTable);

            if (sourceCount != destCount)
            {
                throw new InvalidOperationException(
                    $"Data transfer verification failed. Source count: {sourceCount:N0}, " +
                    $"Destination count: {destCount:N0}");
            }
        }
    }
}