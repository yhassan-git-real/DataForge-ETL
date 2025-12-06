using System;
using System.IO;
using Newtonsoft.Json;
using ETL_ExcelToDatabase.Core;
using DataForgeETL.Core;

namespace ETL_ExcelToDatabase.Core
{
    public static class ConfigurationLoader
    {
        private static DataForgeETL.Core.UnifiedConfigurationManager? _unifiedConfig;

        public static T LoadConfiguration<T>(string configPath) where T : class
        {
            if (typeof(T) == typeof(AppConfig))
            {
                return LoadUnifiedConfiguration() as T ?? throw new InvalidOperationException("Failed to load unified configuration");
            }

            throw new NotSupportedException($"Configuration type {typeof(T).Name} is not supported. Use only AppConfig with unified configuration.");
        }

        private static AppConfig LoadUnifiedConfiguration()
        {
            _unifiedConfig = DataForgeETL.Core.UnifiedConfigurationManager.Instance;
            var unifiedConfig = _unifiedConfig.GetConfiguration();

            // Convert unified config to AppConfig format
            return new AppConfig
            {
                DatabaseConfig = new DatabaseConfig
                {
                    Server = unifiedConfig.Database.Server,
                    Database = unifiedConfig.Database.Database,
                    IntegratedSecurity = unifiedConfig.Database.IntegratedSecurity,
                    Username = unifiedConfig.Database.Username,
                    Password = unifiedConfig.Database.Password
                },
                ProcessConfig = new ProcessConfig
                {
                    ExcelFolderPath = _unifiedConfig.GetOutputExcelFilesPath(),
                    TempTableName = "", // Dynamic - will be loaded from dynamic config
                    DestinationTableName = "", // Dynamic - will be loaded from dynamic config
                    ErrorTableName = unifiedConfig.Tables.ErrorTableName,
                    SuccessLogTableName = unifiedConfig.Tables.SuccessLogTableName,
                    BatchSize = unifiedConfig.Processing.BatchSize,
                    ValidateColumnMapping = unifiedConfig.Processing.ValidateColumnMapping,
                    DefaultSheetName = unifiedConfig.Processing.DefaultSheetName,
                    LogFolderPath = _unifiedConfig.GetLogFilesPath()
                }
            };
        }

        public static string GetConfigPath()
        {
            // Always use unified configuration
            _unifiedConfig = DataForgeETL.Core.UnifiedConfigurationManager.Instance;
            return "UNIFIED_CONFIG"; // Special marker for unified config
        }

        public static void EnsureDirectoriesExist(ProcessConfig processConfig)
        {
            try
            {
                // Excel folder creation only - unified logging system handles log folders
                if (!Directory.Exists(processConfig.ExcelFolderPath))
                {
                    Directory.CreateDirectory(processConfig.ExcelFolderPath);
                    Console.WriteLine($"✓ Created Excel folder: {processConfig.ExcelFolderPath}");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create directories: {ex.Message}", ex);
            }
        }
    }
}