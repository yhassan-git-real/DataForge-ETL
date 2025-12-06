using Newtonsoft.Json;
using ETL_DynamicTableManager.Models;
using ETL_DynamicTableManager.Core;
using DataForgeETL.Core;

namespace ETL_DynamicTableManager.Configuration
{
    /// <summary>
    /// Configuration loader for the ETL system
    /// </summary>
    public static class ConfigurationLoader
    {
        /// <summary>
        /// Gets the ETL configuration using unified configuration system
        /// </summary>
        public static Models.EtlConfig LoadEtlConfiguration()
        {
            var unifiedConfig = DataForgeETL.Core.UnifiedConfigurationManager.Instance;
            var config = unifiedConfig.GetConfiguration();
            
            Console.WriteLine($"✓ ETL configuration loaded from unified configuration");
            
            return new Models.EtlConfig
            {
                DatabaseConfig = new Models.DatabaseConfig
                {
                    Server = config.Database.Server,
                    Database = config.Database.Database,
                    IntegratedSecurity = config.Database.IntegratedSecurity,
                    Username = config.Database.Username,
                    Password = config.Database.Password
                },
                ProcessConfig = new Models.ProcessConfig
                {
                    ExcelFolderPath = config.Paths.InputExcelFiles,
                    TempTableName = "",
                    DestinationTableName = "",
                    ErrorTableName = config.Tables.ErrorTableName,
                    SuccessLogTableName = config.Tables.SuccessLogTableName,
                    BatchSize = config.Processing.BatchSize,
                    ValidateColumnMapping = config.Processing.ValidateColumnMapping,
                    DefaultSheetName = config.Processing.DefaultSheetName,
                    LogFolderPath = config.Paths.LogFiles
                }
            };
        }

        /// <summary>
        /// Loads the ETL configuration asynchronously using unified configuration system
        /// </summary>
        public static async Task<Models.EtlConfig> LoadEtlConfigurationAsync()
        {
            return await Task.FromResult(LoadEtlConfiguration());
        }

        /// <summary>
        /// Builds connection string from database configuration
        /// </summary>
        public static string BuildConnectionString(Models.DatabaseConfig config)
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
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

        /// <summary>
        /// Validates the ETL configuration
        /// </summary>
        public static bool ValidateEtlConfiguration(Models.EtlConfig config)
        {
            var errors = new List<string>();
            
            // Validate database configuration
            if (string.IsNullOrWhiteSpace(config.DatabaseConfig.Server))
                errors.Add("Database server is required");
                
            if (string.IsNullOrWhiteSpace(config.DatabaseConfig.Database))
                errors.Add("Database name is required");
                
            if (!config.DatabaseConfig.IntegratedSecurity)
            {
                if (string.IsNullOrWhiteSpace(config.DatabaseConfig.Username))
                    errors.Add("Username is required when not using integrated security");
            }
            
            // Validate process configuration
            if (string.IsNullOrWhiteSpace(config.ProcessConfig.ExcelFolderPath))
                errors.Add("Excel folder path is required");
                
            if (string.IsNullOrWhiteSpace(config.ProcessConfig.LogFolderPath))
                errors.Add("Log folder path is required");
            
            if (errors.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ ETL configuration validation failed:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"  - {error}");
                }
                Console.ResetColor();
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Creates directories if they don't exist
        /// </summary>
        public static void EnsureDirectoriesExist(Models.ProcessConfig processConfig)
        {
            try
            {
                if (!Directory.Exists(processConfig.ExcelFolderPath))
                {
                    Directory.CreateDirectory(processConfig.ExcelFolderPath);
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"ℹ Created Excel folder: {processConfig.ExcelFolderPath}");
                    Console.ResetColor();
                }
                
                if (!Directory.Exists(processConfig.LogFolderPath))
                {
                    Directory.CreateDirectory(processConfig.LogFolderPath);
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"ℹ Created log folder: {processConfig.LogFolderPath}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Error creating directories: {ex.Message}");
                Console.ResetColor();
                throw;
            }
        }

        /// <summary>
        /// Displays configuration summary
        /// </summary>
        public static void DisplayConfigurationSummary(Models.EtlConfig config)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    ETL CONFIGURATION SUMMARY                  ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            
            Console.WriteLine($"Database Server: {config.DatabaseConfig.Server}");
            Console.WriteLine($"Database: {config.DatabaseConfig.Database}");
            Console.WriteLine($"Authentication: {(config.DatabaseConfig.IntegratedSecurity ? "Windows Authentication" : "SQL Server Authentication")}");
            Console.WriteLine($"Excel Folder: {config.ProcessConfig.ExcelFolderPath}");
            Console.WriteLine($"Log Folder: {config.ProcessConfig.LogFolderPath}");
            Console.WriteLine($"Batch Size: {config.ProcessConfig.BatchSize:N0}");
            Console.WriteLine($"Validate Columns: {config.ProcessConfig.ValidateColumnMapping}");
        }
    }
}