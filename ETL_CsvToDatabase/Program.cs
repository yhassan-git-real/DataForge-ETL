using System.Diagnostics;
using Microsoft.Data.SqlClient;
using ETL_CsvToDatabase.Core;
using ETL_CsvToDatabase.Models;
using ETL_CsvToDatabase.Services;
using UniversalExcelTool.Core;

namespace ETL_CsvToDatabase
{
    internal static class Program
    {
        static async Task Main()
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            Stopwatch totalTimer = new();
            totalTimer.Start();
            long totalRowsProcessed = 0;
            List<string> successfulFiles = new();
            List<string> failedFiles = new();

            try
            {
                ConsoleLogger.PrintHeader("1.0.0");

                // Step 1: Run Dynamic Table Manager first for configuration
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║             STEP 1: DYNAMIC TABLE CONFIGURATION               ║");
                Console.WriteLine("║                  USER INPUT REQUIRED                          ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
                Console.ResetColor();
                Console.WriteLine();
                
                bool configSuccess = await RunDynamicTableManagerAsync();
                
                if (!configSuccess)
                {
                    ConsoleLogger.LogError("Dynamic Table Manager failed or was cancelled");
                    ConsoleLogger.LogError("Cannot proceed with CSV processing without table configuration");
                    Console.WriteLine("\nPress any key to exit...");
                    Console.ReadKey();
                    return;
                }
                
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║             STEP 2: CSV TO DATABASE PROCESSING                ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
                Console.ResetColor();
                Console.WriteLine();

                Console.WriteLine("⏳ Loading ETL system configuration...");
                // Load configuration and initialize logging
                (var dbConfig, var processConfig) = await InitializeConfigurationAsync();
                Console.WriteLine("✓ Configuration loaded successfully");
                Console.WriteLine($"✓ Root Directory: {Directory.GetCurrentDirectory()}");

                // Initialize console output capture
                ConsoleLogger.InitializeCapture(processConfig.LogFolderPath);

                // Print configuration summary in a box
                PrintConfigurationSummary(dbConfig, processConfig);

                // Set up database connection
                string connectionString = BuildConnectionString(dbConfig);
                await TestDatabaseConnection(connectionString);
                EnsureCsvFolderExists(processConfig.CsvFolderPath);

                // Process all CSV files and collect metrics
                (totalRowsProcessed, successfulFiles, failedFiles) = await ProcessFiles(connectionString, processConfig);

                // Display final processing summary
                totalTimer.Stop();
                int totalFilesAttempted = successfulFiles.Count + failedFiles.Count;
                
                Console.WriteLine("\n" + new string('═', 80));
                Console.WriteLine("                             IMPORT SUMMARY REPORT");
                Console.WriteLine(new string('═', 80));
                ConsoleLogger.LogInfo("summary", $"Total Files Attempted: {totalFilesAttempted}");
                ConsoleLogger.LogInfo("summary", $"Successfully Imported: {successfulFiles.Count} files ({totalRowsProcessed} rows)");
                ConsoleLogger.LogInfo("summary", $"Failed Imports: {failedFiles.Count} files");
                ConsoleLogger.LogInfo("summary", $"Processing Time: {totalTimer.Elapsed.TotalMinutes:F2} minutes");
                
                if (failedFiles.Count > 0)
                {
                    Console.WriteLine("\n" + new string('▼', 40) + " FAILED FILES " + new string('▼', 40));
                    ConsoleLogger.LogInfo("summary", $"Failed files: {string.Join(", ", failedFiles)}");
                    Console.WriteLine(new string('▲', 40) + " END FAILED " + new string('▲', 40));
                }
                
                Console.WriteLine(new string('═', 80));

                ConsoleLogger.LogSuccess("Process completed successfully!");
            }
            catch (SqlException sqlEx)
            {
                LogSqlException(sqlEx);
            }
            catch (Exception ex)
            {
                LogGeneralException(ex);
            }
            finally
            {
                Console.WriteLine("\nProcess completed. Moving to next step...");
                
                // Save console output to log file
                ConsoleLogger.SaveConsoleOutput();
            }
        }

        private static void PrintConfigurationSummary(ETL_CsvToDatabase.Core.DatabaseConfig dbConfig, ProcessConfig processConfig)
        {
            Console.WriteLine();
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                   ETL CONFIGURATION SUMMARY                   ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
            Console.WriteLine($"Database Server: {dbConfig.Server}");
            Console.WriteLine($"Database: {dbConfig.Database}");
            Console.WriteLine($"Authentication: {(dbConfig.IntegratedSecurity ? "Windows Authentication" : "SQL Server Authentication")}");
            Console.WriteLine($"CSV Folder: {processConfig.CsvFolderPath}");
            Console.WriteLine($"Temp Table: {processConfig.TempTableName}");
            Console.WriteLine($"Destination Table: {processConfig.DestinationTableName}");
            Console.WriteLine($"Batch Size: {processConfig.BatchSize:N0}");
            Console.WriteLine($"Validate Column Mapping: {processConfig.ValidateColumnMapping}");
            Console.WriteLine();
        }

        private static async Task<(ETL_CsvToDatabase.Core.DatabaseConfig, ProcessConfig)> InitializeConfigurationAsync()
        {
            string configPath = ConfigurationLoader.GetConfigPath();
            ConsoleLogger.LogInfo("config", $"Loading configuration from: {configPath}");
            AppConfig appConfig = ConfigurationLoader.LoadConfiguration<AppConfig>(configPath);
            
            // REQUIRE dynamic table configuration
            var dynamicConfig = await DynamicConfigurationService.LoadDynamicConfigurationAsync();
            
            if (dynamicConfig == null || !DynamicConfigurationService.ValidateDynamicConfiguration(dynamicConfig))
            {
                ConsoleLogger.LogError("DYNAMIC TABLE CONFIGURATION REQUIRED!");
                ConsoleLogger.LogError("No valid dynamic table configuration found.");
                ConsoleLogger.LogError("Please run the Dynamic Table Manager first to configure table names:");
                ConsoleLogger.LogError("  - ETL_DynamicTableManager.exe");
                ConsoleLogger.LogError("");
                ConsoleLogger.LogError("This ETL process now requires dynamic table configuration.");
                throw new InvalidOperationException("Dynamic table configuration is required. Please run ETL_DynamicTableManager.exe first.");
            }
            
            ConsoleLogger.LogSuccess("Using dynamic table configuration");
            DynamicConfigurationService.DisplayConfigurationComparison(appConfig.ProcessConfig, dynamicConfig);
            
            // Create process config with dynamic values
            ProcessConfig processConfig = DynamicConfigurationService.CreateProcessConfigOverride(appConfig.ProcessConfig, dynamicConfig);
            
            return (appConfig.DatabaseConfig, processConfig);
        }

        private static string BuildConnectionString(ETL_CsvToDatabase.Core.DatabaseConfig config)
        {
            SqlConnectionStringBuilder builder = new()
            {
                DataSource = config.Server,
                InitialCatalog = config.Database,
                IntegratedSecurity = config.IntegratedSecurity,
                TrustServerCertificate = true,
                ConnectTimeout = config.ConnectionTimeout,
                MultipleActiveResultSets = true,
                MaxPoolSize = 100,
                Encrypt = false
            };

            if (!config.IntegratedSecurity && !string.IsNullOrEmpty(config.Username))
            {
                builder.UserID = config.Username;
                builder.Password = config.Password;
            }
            return builder.ConnectionString;
        }

        private static async Task TestDatabaseConnection(string connectionString)
        {
            Console.WriteLine("⏳ Testing database connection...");
            await using SqlConnection connection = new(connectionString);
            try
            {
                await connection.OpenAsync();
                Console.WriteLine("✓ Database connection successful!");
            }
            catch (SqlException)
            {
                Console.WriteLine("❌ Failed to connect to database!");
                throw;
            }
        }

        private static void EnsureCsvFolderExists(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                ConsoleLogger.LogInfo("file", $"Created CSV folder: {folderPath}");
            }
        }

        private static async Task<(long totalRows, List<string> successfulFiles, List<string> failedFiles)> ProcessFiles(string connectionString, ProcessConfig config)
        {
            if (!Directory.Exists(config.CsvFolderPath))
            {
                throw new DirectoryNotFoundException($"CSV folder not found: {config.CsvFolderPath}");
            }

            // Get notification settings from unified configuration
            var configManager = UniversalExcelTool.Core.UnifiedConfigurationManager.Instance;
            var unifiedConfig = configManager.GetConfiguration();
            bool enableProgressNotifications = unifiedConfig.Notifications?.Csv?.EnableProgressNotifications ?? true;
            int progressNotificationInterval = unifiedConfig.Notifications?.Csv?.ProgressNotificationInterval ?? 50000;

            string[] csvFiles = Directory.GetFiles(config.CsvFolderPath, "*.csv", SearchOption.TopDirectoryOnly)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            ConsoleLogger.LogInfo("file", $"Found {csvFiles.Length} CSV files to process.");
            if (csvFiles.Length == 0)
            {
                throw new Exception("No CSV files found in the specified folder.");
            }

            // Add separator before starting file processing
            Console.WriteLine("\n" + new string('─', 80));
            ConsoleLogger.LogInfo("processing", "Starting file processing...");
            Console.WriteLine(new string('─', 80));

            await using SqlConnection connection = new(connectionString);
            await connection.OpenAsync();
            
            // Load dynamic configuration to check table creation requirements
            var dynamicConfig = await DynamicConfigurationService.LoadDynamicConfigurationAsync();
            
            // Handle table creation or verification based on dynamic configuration
            if (dynamicConfig?.CreateNewTable == true)
            {
                Console.WriteLine();
                Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                    TABLE CREATION SUMMARY                     ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
                ConsoleLogger.LogInfo("table", "Dynamic configuration indicates new table should be created");
                
                // Check if destination table exists
                bool tableExists = await VerifyDestinationTableExists(connection, config.DestinationTableName);
                
                if (!tableExists)
                {
                    ConsoleLogger.LogInfo("table", $"Creating destination table: {config.DestinationTableName}");
                    
                    // Get the first CSV file to determine table structure
                    if (csvFiles.Length > 0)
                    {
                        await CreateDestinationTableFromCsv(connection, csvFiles[0], config.DestinationTableName);
                        ConsoleLogger.LogSuccess($"Destination table {config.DestinationTableName} created successfully");
                    }
                    else
                    {
                        throw new Exception("Cannot create table: No CSV files found to determine structure");
                    }
                }
                else
                {
                    ConsoleLogger.LogInfo("table", $"Destination table {config.DestinationTableName} already exists");
                }
                Console.WriteLine();
            }
            else
            {
                // Verify destination table exists for existing table scenario
                if (!await VerifyDestinationTableExists(connection, config.DestinationTableName))
                {
                    throw new Exception($"Destination table {config.DestinationTableName} does not exist and CreateNewTable is not enabled in dynamic configuration.");
                }
            }

            // Handle table truncation if requested
            if (dynamicConfig?.ShouldTruncateTable == true)
            {
                ConsoleLogger.LogInfo("table", $"Truncating destination table: {config.DestinationTableName}");
                await TruncateTable(connection, config.DestinationTableName);
                ConsoleLogger.LogSuccess($"Table {config.DestinationTableName} truncated successfully");
            }

            DatabaseOperations.CreateLogTables(connection, config.ErrorTableName, config.SuccessLogTableName);

            long totalRowsProcessed = 0;
            List<string> successfulFiles = new();
            List<string> failedFiles = new();

            for (int fileIndex = 0; fileIndex < csvFiles.Length; fileIndex++)
            {
                string csvFile = csvFiles[fileIndex];
                string fileName = Path.GetFileName(csvFile);

                // Add separator for each file
                Console.WriteLine("\n" + new string('•', 60));
                ConsoleLogger.LogInfo("file", $"Processing file {fileIndex + 1} of {csvFiles.Length}: {fileName}");
                ConsoleLogger.LogInfo("processing", $"Processing started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine(new string('•', 60));

                try
                {
                    long fileRows = await ProcessSingleFile(connection, csvFile, config);
                    if (fileRows > 0)
                    {
                        totalRowsProcessed += fileRows;
                        successfulFiles.Add(fileName);
                        ConsoleLogger.LogSuccess($"Successfully processed {fileRows} rows");
                    }
                    else
                    {
                        failedFiles.Add(fileName);
                        ConsoleLogger.LogWarning($"File processed but no rows imported (validation or other issues)");
                    }
                }
                catch (Exception ex)
                {
                    failedFiles.Add(fileName);
                    ConsoleLogger.LogError($"Error processing file: {ex.Message}");
                    await LoggingService.LogError(connection, config.ErrorTableName, fileName,
                        "Process", "ProcessError", ex.Message);
                    continue;
                }
            }

            // Add separator after all files processed
            Console.WriteLine("\n" + new string('─', 80));
            ConsoleLogger.LogInfo("processing", "File processing completed");
            Console.WriteLine(new string('─', 80));

            return (totalRowsProcessed, successfulFiles, failedFiles);
        }

        private static async Task<bool> VerifyDestinationTableExists(SqlConnection connection, string tableName)
        {
            string checkTableQuery = @"
                SELECT COUNT(1) 
                FROM sys.tables 
                WHERE name = @TableName";

            using var cmd = new SqlCommand(checkTableQuery, connection);
            cmd.Parameters.AddWithValue("@TableName", tableName.Replace("[", "").Replace("]", ""));
            int exists = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
            return exists > 0;
        }

        private static async Task CreateDestinationTableFromCsv(SqlConnection connection, string csvFilePath, string tableName)
        {
            ConsoleLogger.LogInfo("table", $"Analyzing CSV file structure: {Path.GetFileName(csvFilePath)}");
            
            // Get notification settings - disable notifications for sample read (only 1 row)
            // Read the first batch to determine column structure
            var sampleData = CsvProcessor.LoadCsvInBatches(csvFilePath, connection, "", 1, false, 1).FirstOrDefault();
            
            if (sampleData?.Data == null || sampleData.Data.Columns.Count == 0)
            {
                throw new Exception($"Cannot determine table structure: CSV file {csvFilePath} appears to be empty or invalid");
            }

            // Build CREATE TABLE statement with all columns as NVARCHAR(MAX) for flexibility
            var createTableSql = new System.Text.StringBuilder();
            createTableSql.AppendLine($"CREATE TABLE [{tableName}] (");
            
            // Add data columns based on CSV structure (matching temp table column names exactly)
            for (int i = 0; i < sampleData.Data.Columns.Count; i++)
            {
                string originalName = sampleData.Data.Columns[i].ColumnName;
                string columnName = originalName;
                // Clean column name to be SQL-safe (same logic as temp table creation)
                columnName = System.Text.RegularExpressions.Regex.Replace(columnName, @"[^\w]", "_");
                if (string.IsNullOrWhiteSpace(columnName) || char.IsDigit(columnName[0]))
                {
                    columnName = $"Column_{i + 1}";
                }
                
                createTableSql.AppendLine($"    [{columnName}] NVARCHAR(MAX),");
            }
            
            // Remove the last comma and close the CREATE TABLE statement
            createTableSql.Length -= 3; // Remove ",\r\n"
            createTableSql.AppendLine();
            createTableSql.AppendLine(");");

            ConsoleLogger.LogInfo("table", $"Creating table with {sampleData.Data.Columns.Count} columns");
            
            using var cmd = new SqlCommand(createTableSql.ToString(), connection);
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task TruncateTable(SqlConnection connection, string tableName)
        {
            string truncateQuery = $"TRUNCATE TABLE [{tableName}]";
            using var cmd = new SqlCommand(truncateQuery, connection);
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task<long> ProcessSingleFile(SqlConnection connection, string csvFile, ProcessConfig config)
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();
            long totalRows = 0;

            // Get notification settings from unified configuration
            var configManager = UniversalExcelTool.Core.UnifiedConfigurationManager.Instance;
            var unifiedConfig = configManager.GetConfiguration();
            bool enableProgressNotifications = unifiedConfig.Notifications?.Csv?.EnableProgressNotifications ?? true;
            int progressNotificationInterval = unifiedConfig.Notifications?.Csv?.ProgressNotificationInterval ?? 50000;

            try
            {
                foreach (BatchResult dataBatch in CsvProcessor.LoadCsvInBatches(csvFile, connection,
                    config.ErrorTableName, config.BatchSize, enableProgressNotifications, progressNotificationInterval))
                {
                    if (dataBatch.IsFirstBatch && dataBatch.Data.Rows.Count > 0)
                    {
                        DatabaseOperations.DropAndCreateTempTable(connection, config.TempTableName, dataBatch.Data);
                    }

                    // Only load data if the batch contains rows
                    if (dataBatch.Data.Rows.Count > 0)
                    {
                        await DatabaseOperations.LoadDataIntoTempTableAsync(connection, config.TempTableName, dataBatch.Data, progressNotificationInterval, config.BatchSize, enableProgressNotifications);
                    }

                    if (dataBatch.IsLastBatch)
                    {
                        ConsoleLogger.LogInfo("processing", "Last batch detected - starting validation...");
                        
                        // Perform column validation before final processing
                        var validationResult = await DatabaseOperations.ValidateColumnsAsync(
                            connection,
                            config.TempTableName,
                            config.DestinationTableName,
                            config.ErrorTableName,
                            Path.GetFileName(csvFile));

                        ConsoleLogger.LogInfo("processing", $"Validation completed. IsValid: {validationResult.IsValid}");

                        if (!validationResult.IsValid)
                        {
                            ConsoleLogger.LogError($"Column validation failed for file: {Path.GetFileName(csvFile)}");
                            ConsoleLogger.LogError($"Matched columns: {validationResult.MatchedColumns.Count}, Unmatched: {validationResult.UnmatchedColumns.Count}");
                            if (validationResult.UnmatchedColumns.Count > 0)
                            {
                                ConsoleLogger.LogError($"Unmatched columns: {string.Join(", ", validationResult.UnmatchedColumns)}");
                            }
                            break; // Exit the batch processing loop
                        }

                        ConsoleLogger.LogInfo("processing", "✓ Validation passed - Starting data transfer to destination table...");
                        long transferredRows = await HandleFinalBatchProcessing(connection, config, dataBatch, csvFile, stopwatch, validationResult);
                        totalRows = transferredRows;
                        ConsoleLogger.LogInfo("processing", $"✓ Data transfer completed successfully. Rows transferred: {transferredRows:N0}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Only log errors that are not validation related
                if (!ex.Message.Contains("Validation Report"))
                {
                    ConsoleLogger.LogError($"Error processing file: {ex.Message}");
                    await LoggingService.LogError(
                        connection,
                        config.ErrorTableName,
                        Path.GetFileName(csvFile),
                        "ProcessError",
                        "UnexpectedError",
                        ex.Message);
                }
                return 0;
            }

            return totalRows;
        }

        private static async Task<long> HandleFinalBatchProcessing(
            SqlConnection connection,
            ProcessConfig config,
            BatchResult dataBatch,
            string csvFile,
            Stopwatch stopwatch,
            ValidationResult validationResult)
        {
            long rowsProcessed = await DatabaseOperations.TransferDataToDestinationAsync(
                connection,
                config.TempTableName,
                config.DestinationTableName,
                validationResult);

            stopwatch.Stop();
            double processingTimeSeconds = stopwatch.Elapsed.TotalSeconds;
            int rowsPerSecond = processingTimeSeconds > 0 ? (int)(rowsProcessed / processingTimeSeconds) : 0;

            // Log success details
            await LoggingService.LogSuccess(
                connection,
                config.SuccessLogTableName,
                $"Successfully processed file: {Path.GetFileName(csvFile)}",
                rowsProcessed,
                validationResult.TempColumnCount,
                validationResult.DestColumnCount,
                validationResult.MatchedColumns.Count,
                (decimal)processingTimeSeconds,
                rowsPerSecond);

            return rowsProcessed;
        }

        private static void LogSqlException(SqlException sqlEx)
        {
            ConsoleLogger.LogError("SQL Server Error:");
            ConsoleLogger.LogError($"Error Number: {sqlEx.Number}");
            ConsoleLogger.LogError($"Error Message: {sqlEx.Message}");
            ConsoleLogger.LogError($"Error State: {sqlEx.State}");
            ConsoleLogger.LogError($"Server: {sqlEx.Server}");
        }

        private static void LogGeneralException(Exception ex)
        {
            ConsoleLogger.LogError("General Error:");
            ConsoleLogger.LogError($"Error Type: {ex.GetType().Name}");
            ConsoleLogger.LogError($"Message: {ex.Message}");
            ConsoleLogger.LogError($"Stack Trace: {ex.StackTrace}");
        }

        private static async Task<bool> RunDynamicTableManagerAsync()
        {
            try
            {
                // Get the path to Dynamic Table Manager executable using UnifiedConfigurationManager
                var configManager = UniversalExcelTool.Core.UnifiedConfigurationManager.Instance;
                string dynamicTableManagerPath = configManager.GetExecutablePath("dynamictablemanager");

                if (!File.Exists(dynamicTableManagerPath))
                {
                    ConsoleLogger.LogError($"Dynamic Table Manager executable not found");
                    ConsoleLogger.LogError($"Expected location: {dynamicTableManagerPath}");
                    ConsoleLogger.LogError("Please build the ETL_DynamicTableManager project first");
                    return false;
                }

                ConsoleLogger.LogInfo("config", $"Launching Dynamic Table Manager: {dynamicTableManagerPath}");
                ConsoleLogger.LogInfo("config", "Please configure the table settings in the window that opens...");

                var startInfo = new ProcessStartInfo(dynamicTableManagerPath)
                {
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(dynamicTableManagerPath)
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    ConsoleLogger.LogError("Failed to start Dynamic Table Manager process");
                    return false;
                }

                // Wait for the process to complete
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    ConsoleLogger.LogSuccess("Dynamic Table Manager completed successfully");
                    return true;
                }
                else
                {
                    ConsoleLogger.LogError($"Dynamic Table Manager failed with exit code: {process.ExitCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError($"Error running Dynamic Table Manager: {ex.Message}");
                return false;
            }
        }
    }
}
