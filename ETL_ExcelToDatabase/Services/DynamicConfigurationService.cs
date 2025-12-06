using Newtonsoft.Json;
using ETL_ExcelToDatabase.Core;

namespace ETL_ExcelToDatabase.Services
{
    /// <summary>
    /// Service to load dynamic table configuration created by the Dynamic Table Manager
    /// </summary>
    public static class DynamicConfigurationService
    {
        private const string DYNAMIC_CONFIG_FILENAME = "dynamic_table_config.json";

        /// <summary>
        /// Dynamic table configuration model
        /// </summary>
        public class DynamicTableConfig
        {
            public string TempTableName { get; set; } = string.Empty;
            public string DestinationTableName { get; set; } = string.Empty;
            public string ErrorTableName { get; set; } = string.Empty;
            public string SuccessLogTableName { get; set; } = string.Empty;
            public bool TargetTableExists { get; set; }
            public bool ShouldTruncateTable { get; set; }
            public bool CreateNewTable { get; set; }
            public DateTime ConfigurationTimestamp { get; set; }
        }

        /// <summary>
        /// Gets the path for the dynamic table configuration file
        /// </summary>
        private static string GetDynamicConfigFilePath()
        {
            string baseDir = Path.GetDirectoryName(AppContext.BaseDirectory) ?? "";
            while (!string.IsNullOrEmpty(baseDir) && !Path.GetFileName(baseDir).Equals("DataForge ETL", StringComparison.OrdinalIgnoreCase))
            {
                baseDir = Path.GetDirectoryName(baseDir) ?? "";
            }
            
            if (string.IsNullOrEmpty(baseDir))
            {
                baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DataForge ETL");
            }
            
            return Path.Combine(baseDir, DYNAMIC_CONFIG_FILENAME);
        }

        /// <summary>
        /// Loads the dynamic table configuration if it exists
        /// </summary>
        public static async Task<DynamicTableConfig?> LoadDynamicConfigurationAsync()
        {
            try
            {
                string configPath = GetDynamicConfigFilePath();
                
                if (!File.Exists(configPath))
                {
                    ConsoleLogger.LogInfo("config", "Dynamic table configuration not found. Using default configuration from config.json");
                    return null;
                }
                
                string jsonContent = await File.ReadAllTextAsync(configPath);
                var config = JsonConvert.DeserializeObject<DynamicTableConfig>(jsonContent);
                
                if (config != null)
                {
                    ConsoleLogger.LogInfo("config", $"Dynamic table configuration loaded successfully");
                    ConsoleLogger.LogInfo("config", $"Temp Table: {config.TempTableName}");
                    ConsoleLogger.LogInfo("config", $"Destination Table: {config.DestinationTableName}");
                    ConsoleLogger.LogInfo("config", $"Target Table Exists: {config.TargetTableExists}");
                    ConsoleLogger.LogInfo("config", $"Should Truncate: {config.ShouldTruncateTable}");
                    ConsoleLogger.LogInfo("config", $"Create New Table: {config.CreateNewTable}");
                    ConsoleLogger.LogInfo("config", $"Last Updated: {config.ConfigurationTimestamp:yyyy-MM-dd HH:mm:ss}");
                }
                
                return config;
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError($"Error loading dynamic configuration: {ex.Message}");
                ConsoleLogger.LogInfo("config", "Using default configuration from config.json");
                return null;
            }
        }

        /// <summary>
        /// Checks if dynamic configuration exists and is valid
        /// </summary>
        public static bool DynamicConfigurationExists()
        {
            try
            {
                string configPath = GetDynamicConfigFilePath();
                return File.Exists(configPath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates the dynamic configuration - only checks dynamic table names
        /// </summary>
        public static bool ValidateDynamicConfiguration(DynamicTableConfig config)
        {
            if (config == null)
                return false;

            if (string.IsNullOrWhiteSpace(config.TempTableName))
            {
                ConsoleLogger.LogError("Dynamic configuration validation failed: TempTableName is empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(config.DestinationTableName))
            {
                ConsoleLogger.LogError("Dynamic configuration validation failed: DestinationTableName is empty");
                return false;
            }

            // Note: ErrorTableName and SuccessLogTableName are not validated here
            // as they are predefined in config.json and not part of dynamic configuration

            return true;
        }

        /// <summary>
        /// Creates a process configuration override based on dynamic configuration
        /// Only overrides TempTableName and DestinationTableName (dynamic tables)
        /// ErrorTableName and SuccessLogTableName remain from original config (predefined log tables)
        /// </summary>
        public static ProcessConfig CreateProcessConfigOverride(
            ProcessConfig originalConfig, 
            DynamicTableConfig dynamicConfig)
        {
            return new ProcessConfig
            {
                ExcelFolderPath = originalConfig.ExcelFolderPath,
                TempTableName = dynamicConfig.TempTableName,           // DYNAMIC
                DestinationTableName = dynamicConfig.DestinationTableName, // DYNAMIC
                ErrorTableName = originalConfig.ErrorTableName,        // PREDEFINED (from config.json)
                SuccessLogTableName = originalConfig.SuccessLogTableName, // PREDEFINED (from config.json)
                BatchSize = originalConfig.BatchSize,
                ValidateColumnMapping = originalConfig.ValidateColumnMapping,
                DefaultSheetName = originalConfig.DefaultSheetName,
                LogFolderPath = originalConfig.LogFolderPath
            };
        }

        /// <summary>
        /// Displays configuration comparison for dynamic tables only
        /// </summary>
        public static void DisplayConfigurationComparison(
            ProcessConfig originalConfig, 
            DynamicTableConfig dynamicConfig)
        {
            ConsoleLogger.LogInfo("config", "Dynamic Table Configuration Applied:");
            ConsoleLogger.LogInfo("config", $"Temp Table: {dynamicConfig.TempTableName} (Dynamic)");
            ConsoleLogger.LogInfo("config", $"Destination Table: {dynamicConfig.DestinationTableName} (Dynamic)");
            ConsoleLogger.LogInfo("config", $"Error Table: {originalConfig.ErrorTableName} (Predefined)");
            ConsoleLogger.LogInfo("config", $"Success Table: {originalConfig.SuccessLogTableName} (Predefined)");
        }
    }
}