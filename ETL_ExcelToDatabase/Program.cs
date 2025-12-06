using System.Diagnostics;
using System.Text;
using Microsoft.Data.SqlClient;
using ETL_ExcelToDatabase.Core;
using ETL_ExcelToDatabase.Models;
using ETL_ExcelToDatabase.Services;

namespace ETL_ExcelToDatabase
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
            AppConfig? appConfig = null;

            try
            {
                ConsoleLogger.PrintHeader("1.0.0");

                Console.WriteLine("⏳ Loading ETL system configuration...");
                // Load configuration and initialize logging
                (var dbConfig, var processConfig) = await InitializeConfigurationAsync();
                appConfig = ConfigurationLoader.LoadConfiguration<AppConfig>(ConfigurationLoader.GetConfigPath());
                Console.WriteLine("✓ Configuration loaded successfully");
                Console.WriteLine($"✓ Root Directory: {Directory.GetCurrentDirectory()}");

                // Print configuration summary in a box
                PrintConfigurationSummary(dbConfig, processConfig);

                // Set up database connection
                string connectionString = BuildConnectionString(dbConfig);
                await TestDatabaseConnection(connectionString);
                EnsureExcelFolderExists(processConfig.ExcelFolderPath);

                // Process all Excel files and collect metrics
                (totalRowsProcessed, successfulFiles, failedFiles) = await ProcessFiles(connectionString, processConfig);

                // Display final processing summary
                totalTimer.Stop();
                int totalFilesAttempted = successfulFiles.Count + failedFiles.Count;
                
                // Add comprehensive summary section
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
                // Removed delay for automated orchestrator execution
            }
        }

        private static void PrintConfigurationSummary(DatabaseConfig dbConfig, ProcessConfig processConfig)
        {
            Console.WriteLine();
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                   ETL CONFIGURATION SUMMARY                   ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
            Console.WriteLine($"Database Server: {dbConfig.Server}");
            Console.WriteLine($"Database: {dbConfig.Database}");
            Console.WriteLine($"Authentication: {(dbConfig.IntegratedSecurity ? "Windows Authentication" : "SQL Server Authentication")}");
            Console.WriteLine($"Excel Folder: {processConfig.ExcelFolderPath}");
            Console.WriteLine($"Temp Table: {processConfig.TempTableName}");
            Console.WriteLine($"Destination Table: {processConfig.DestinationTableName}");
            Console.WriteLine($"Batch Size: {processConfig.BatchSize:N0}");
            Console.WriteLine($"Validate Column Mapping: {processConfig.ValidateColumnMapping}");
            Console.WriteLine();
        }

        private static async Task<(DatabaseConfig, ProcessConfig)> InitializeConfigurationAsync()
        {
            string configPath = ConfigurationLoader.GetConfigPath();
            ConsoleLogger.LogInfo("config", $"Loading configuration from: {configPath}");
            AppConfig appConfig = ConfigurationLoader.LoadConfiguration<AppConfig>(configPath);
            
            // REQUIRE dynamic table configuration - no fallback to default values
            var dynamicConfig = await DynamicConfigurationService.LoadDynamicConfigurationAsync();
            
            if (dynamicConfig == null || !DynamicConfigurationService.ValidateDynamicConfiguration(dynamicConfig))
            {
                ConsoleLogger.LogError("DYNAMIC TABLE CONFIGURATION REQUIRED!");
                ConsoleLogger.LogError("No valid dynamic table configuration found.");
                ConsoleLogger.LogError("Please run the Dynamic Table Manager first to configure table names:");
                ConsoleLogger.LogError("  - ETL_DynamicTableManager.exe");
                ConsoleLogger.LogError("  - OR use: start_dynamic_etl.bat");
                ConsoleLogger.LogError("");
                ConsoleLogger.LogError("This ETL process now requires dynamic table configuration.");
                ConsoleLogger.LogError("Static table names in config.json are no longer used.");
                throw new InvalidOperationException("Dynamic table configuration is required. Please run ETL_DynamicTableManager.exe first.");
            }
            
            ConsoleLogger.LogSuccess("Using dynamic table configuration");
            DynamicConfigurationService.DisplayConfigurationComparison(appConfig.ProcessConfig, dynamicConfig);
            
            // Create process config with dynamic values
            ProcessConfig processConfig = DynamicConfigurationService.CreateProcessConfigOverride(appConfig.ProcessConfig, dynamicConfig);
            
            return (appConfig.DatabaseConfig, processConfig);
        }

        private static string BuildConnectionString(DatabaseConfig config)
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

        private static void EnsureExcelFolderExists(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                ConsoleLogger.LogInfo("file", $"Created Excel folder: {folderPath}");
            }
        }

        private static async Task<(long totalRows, List<string> successfulFiles, List<string> failedFiles)> ProcessFiles(string connectionString, ProcessConfig config)
        {
            if (!Directory.Exists(config.ExcelFolderPath))
            {
                throw new DirectoryNotFoundException($"Excel folder not found: {config.ExcelFolderPath}");
            }

            // Get notification settings from unified configuration
            var configManager = UnifiedConfigurationManager.Instance;
            var unifiedConfig = configManager.GetConfiguration();
            bool enableProgressNotifications = unifiedConfig.Notifications?.Excel?.EnableProgressNotifications ?? true;
            int progressNotificationInterval = unifiedConfig.Notifications?.Excel?.ProgressNotificationInterval ?? 50000;

            string[] excelFiles = Directory.GetFiles(config.ExcelFolderPath, "*.*")
                .Where(f => f.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f)
                .ToArray();

            ConsoleLogger.LogInfo("file", $"Found {excelFiles.Length} Excel files to process.");
            if (excelFiles.Length == 0)
            {
                throw new Exception("No Excel files found in the specified folder.");
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
                    
                    // Get the first Excel file to determine table structure
                    if (excelFiles.Length > 0)
                    {
                        await CreateDestinationTableFromExcel(connection, excelFiles[0], config.DestinationTableName);
                        ConsoleLogger.LogSuccess($"Destination table {config.DestinationTableName} created successfully");
                    }
                    else
                    {
                        throw new Exception("Cannot create table: No Excel files found to determine structure");
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

            DatabaseOperations.CreateLogTables(connection, config.ErrorTableName, config.SuccessLogTableName);

            long totalRowsProcessed = 0;
            List<string> successfulFiles = new();
            List<string> failedFiles = new();

            for (int fileIndex = 0; fileIndex < excelFiles.Length; fileIndex++)
            {
                string excelFile = excelFiles[fileIndex];
                string fileName = Path.GetFileName(excelFile);

                // Add separator for each file
                Console.WriteLine("\n" + new string('•', 60));
                ConsoleLogger.LogInfo("file", $"Processing file {fileIndex + 1} of {excelFiles.Length}: {fileName}");
                ConsoleLogger.LogInfo("processing", $"Processing started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine(new string('•', 60));

                try
                {
                    long fileRows = await ProcessSingleFile(connection, excelFile, config);
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
            int exists = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0); // Fix for CS8605
            return exists > 0;
        }

        private static async Task CreateDestinationTableFromExcel(SqlConnection connection, string excelFilePath, string tableName)
        {
            ConsoleLogger.LogInfo("table", $"Analyzing Excel file structure: {Path.GetFileName(excelFilePath)}");
            
            // Read the first batch to determine column structure (disable notifications for sample)
            var sampleData = ExcelProcessor.LoadExcelInBatches(excelFilePath, connection, "", 1, "", false, 1).FirstOrDefault();
            
            if (sampleData?.Data == null || sampleData.Data.Rows.Count == 0)
            {
                throw new Exception($"Cannot determine table structure: Excel file {excelFilePath} appears to be empty");
            }

            // Build CREATE TABLE statement with all columns as NVARCHAR(MAX) for flexibility
            var createTableSql = new StringBuilder();
            createTableSql.AppendLine($"CREATE TABLE [{tableName}] (");
            
            // Add data columns based on Excel structure (matching temp table structure exactly)
            for (int i = 0; i < sampleData.Data.Columns.Count; i++)
            {
                string columnName = sampleData.Data.Columns[i].ColumnName;
                // Clean column name to be SQL-safe (same logic as temp table)
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

            ConsoleLogger.LogInfo("table", $"Creating table with {sampleData.Data.Columns.Count} data columns (matching temp table structure)");
            
            using var cmd = new SqlCommand(createTableSql.ToString(), connection);
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task<long> ProcessSingleFile(SqlConnection connection, string excelFile, ProcessConfig config)
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();
            long totalRows = 0;

            // Get notification settings from unified configuration
            var configManager = UnifiedConfigurationManager.Instance;
            var unifiedConfig = configManager.GetConfiguration();
            bool enableProgressNotifications = unifiedConfig.Notifications?.Excel?.EnableProgressNotifications ?? true;
            int progressNotificationInterval = unifiedConfig.Notifications?.Excel?.ProgressNotificationInterval ?? 50000;

            try
            {
                foreach (BatchResult dataBatch in ExcelProcessor.LoadExcelInBatches(excelFile, connection,
                    config.ErrorTableName, config.BatchSize, config.DefaultSheetName, 
                    enableProgressNotifications, progressNotificationInterval))
                {
                    if (dataBatch.IsFirstBatch)
                    {
                        DatabaseOperations.DropAndCreateTempTable(connection, config.TempTableName, dataBatch.Data);
                    }

                    await DatabaseOperations.LoadDataIntoTempTableAsync(connection, config.TempTableName, dataBatch.Data, progressNotificationInterval, config.BatchSize, enableProgressNotifications);

                    if (dataBatch.IsLastBatch)
                    {
                        // Perform column validation before final processing
                        var validationResult = await DatabaseOperations.ValidateColumnsAsync(
                            connection,
                            config.TempTableName,
                            config.DestinationTableName,
                            config.ErrorTableName,
                            Path.GetFileName(excelFile));

                        if (!validationResult.IsValid)
                        {
                            return 0; // Skip this file and continue with next
                        }

                        totalRows = await HandleFinalBatchProcessing(connection, config, dataBatch, excelFile, stopwatch, validationResult);
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
                        Path.GetFileName(excelFile),
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
            string excelFile,
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
                $"Successfully processed file: {Path.GetFileName(excelFile)}",
                rowsProcessed,
                validationResult.TempColumnCount,
                validationResult.DestColumnCount,
                validationResult.MatchedColumns.Count,
                (decimal)processingTimeSeconds,  // Convert double to decimal
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
    }
}