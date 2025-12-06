using Newtonsoft.Json;
using ETL_DynamicTableManager.Models;

namespace ETL_DynamicTableManager.Services
{
    /// <summary>
    /// Service for managing dynamic table configuration files
    /// </summary>
    public static class TableConfigurationService
    {
        private const string CONFIG_FILENAME = "dynamic_table_config.json";
        private const string BACKUP_FILENAME = "dynamic_table_config_backup.json";
        
        /// <summary>
        /// Gets the path for the dynamic table configuration file
        /// </summary>
        public static string GetConfigFilePath()
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
            
            return Path.Combine(baseDir, CONFIG_FILENAME);
        }

        /// <summary>
        /// Gets the path for the backup configuration file
        /// </summary>
        public static string GetBackupConfigFilePath()
        {
            string configPath = GetConfigFilePath();
            string directory = Path.GetDirectoryName(configPath) ?? "";
            return Path.Combine(directory, BACKUP_FILENAME);
        }

        /// <summary>
        /// Saves the dynamic table configuration to a JSON file
        /// </summary>
        public static async Task<bool> SaveConfigurationAsync(DynamicTableConfig config)
        {
            try
            {
                string configPath = GetConfigFilePath();
                string directory = Path.GetDirectoryName(configPath) ?? "";
                
                // Ensure directory exists
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Create backup if file exists
                if (File.Exists(configPath))
                {
                    CreateBackup(configPath);
                }
                
                // Update timestamp
                config.ConfigurationTimestamp = DateTime.Now;
                
                // Serialize and save
                string jsonContent = JsonConvert.SerializeObject(config, Formatting.Indented);
                await File.WriteAllTextAsync(configPath, jsonContent);
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Configuration saved successfully to: {configPath}");
                Console.ResetColor();
                
                return true;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Error saving configuration: {ex.Message}");
                Console.ResetColor();
                return false;
            }
        }

        /// <summary>
        /// Loads the dynamic table configuration from the JSON file
        /// </summary>
        public static async Task<DynamicTableConfig?> LoadConfigurationAsync()
        {
            try
            {
                string configPath = GetConfigFilePath();
                
                if (!File.Exists(configPath))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"⚠ Configuration file not found: {configPath}");
                    Console.WriteLine("A new configuration will be created.");
                    Console.ResetColor();
                    return null;
                }
                
                string jsonContent = await File.ReadAllTextAsync(configPath);
                var config = JsonConvert.DeserializeObject<DynamicTableConfig>(jsonContent);
                
                if (config != null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ Configuration loaded from: {configPath}");
                    Console.WriteLine($"  Last updated: {config.ConfigurationTimestamp:yyyy-MM-dd HH:mm:ss}");
                    Console.ResetColor();
                }
                
                return config;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Error loading configuration: {ex.Message}");
                Console.ResetColor();
                return null;
            }
        }

        /// <summary>
        /// Creates a backup of the existing configuration file
        /// </summary>
        private static void CreateBackup(string configPath)
        {
            try
            {
                string backupPath = GetBackupConfigFilePath();
                
                if (File.Exists(configPath))
                {
                    File.Copy(configPath, backupPath, true);
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"ℹ Backup created: {backupPath}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ Warning: Could not create backup - {ex.Message}");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Checks if a configuration file exists
        /// </summary>
        public static bool ConfigurationExists()
        {
            return File.Exists(GetConfigFilePath());
        }

        /// <summary>
        /// Deletes the configuration file
        /// </summary>
        public static bool DeleteConfiguration()
        {
            try
            {
                string configPath = GetConfigFilePath();
                
                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ Configuration file deleted: {configPath}");
                    Console.ResetColor();
                    return true;
                }
                
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠ Configuration file not found.");
                Console.ResetColor();
                return false;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Error deleting configuration: {ex.Message}");
                Console.ResetColor();
                return false;
            }
        }

        /// <summary>
        /// Updates the existing configuration with new table names
        /// </summary>
        public static async Task<bool> UpdateTableNamesAsync(string tempTableName, string destinationTableName)
        {
            try
            {
                var config = await LoadConfigurationAsync();
                
                if (config == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("✗ No existing configuration found to update.");
                    Console.ResetColor();
                    return false;
                }
                
                config.TempTableName = tempTableName;
                config.DestinationTableName = destinationTableName;
                
                return await SaveConfigurationAsync(config);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Error updating configuration: {ex.Message}");
                Console.ResetColor();
                return false;
            }
        }

        /// <summary>
        /// Displays the current configuration
        /// </summary>
        public static async Task DisplayCurrentConfigurationAsync()
        {
            var config = await LoadConfigurationAsync();
            
            if (config == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠ No configuration found.");
                Console.ResetColor();
                return;
            }
            
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    CURRENT CONFIGURATION                      ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            
            Console.WriteLine($"Temporary Table: {config.TempTableName}");
            Console.WriteLine($"Destination Table: {config.DestinationTableName}");
            Console.WriteLine($"Error Table: {config.ErrorTableName}");
            Console.WriteLine($"Success Log Table: {config.SuccessLogTableName}");
            Console.WriteLine($"Target Table Exists: {config.TargetTableExists}");
            Console.WriteLine($"Should Truncate: {config.ShouldTruncateTable}");
            Console.WriteLine($"Create New Table: {config.CreateNewTable}");
            Console.WriteLine($"Last Updated: {config.ConfigurationTimestamp:yyyy-MM-dd HH:mm:ss}");
        }

        /// <summary>
        /// Validates the configuration completeness
        /// Only validates dynamic table names (TempTableName and DestinationTableName)
        /// </summary>
        public static bool ValidateConfiguration(DynamicTableConfig config)
        {
            var errors = new List<string>();
            
            if (string.IsNullOrWhiteSpace(config.TempTableName))
                errors.Add("Temporary table name is required");
                
            if (string.IsNullOrWhiteSpace(config.DestinationTableName))
                errors.Add("Destination table name is required");
            
            // Note: ErrorTableName and SuccessLogTableName are not validated here
            // as they are predefined in the main application's config.json
            
            if (errors.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ Configuration validation failed:");
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
        /// Creates a default configuration template
        /// Note: ErrorTableName and SuccessLogTableName are left empty as they are predefined in config.json
        /// </summary>
        public static DynamicTableConfig CreateDefaultConfig()
        {
            return new DynamicTableConfig
            {
                TempTableName = "",
                DestinationTableName = "",
                ErrorTableName = "",          // Not used - predefined in config.json
                SuccessLogTableName = "",     // Not used - predefined in config.json
                TargetTableExists = false,
                ShouldTruncateTable = false,
                CreateNewTable = false,
                ConfigurationTimestamp = DateTime.Now
            };
        }
    }
}