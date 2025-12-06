using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Reflection;
using Microsoft.Data.SqlClient;

namespace ETL_ExcelToDatabase.Core
{
    /// <summary>
    /// Centralized configuration manager for the DataForge ETL
    /// Handles dynamic path resolution and environment-agnostic configuration
    /// </summary>
    public class UnifiedConfigurationManager
    {
        private static UnifiedConfigurationManager? _instance;
        private static readonly object _lock = new object();
        private UnifiedConfig? _config;
        private string _rootDirectory = string.Empty;

        public static UnifiedConfigurationManager Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance ??= new UnifiedConfigurationManager();
                }
            }
        }

        private UnifiedConfigurationManager()
        {
            LoadConfiguration();
        }

        /// <summary>
        /// Loads the unified configuration from appsettings.json
        /// </summary>
        public void LoadConfiguration()
        {
            try
            {
                string configPath = GetConfigurationFilePath();
                
                if (!File.Exists(configPath))
                {
                    throw new FileNotFoundException($"Configuration file not found: {configPath}");
                }

                string jsonContent = File.ReadAllText(configPath);
                _config = JsonConvert.DeserializeObject<UnifiedConfig>(jsonContent);

                if (_config == null)
                {
                    throw new InvalidOperationException("Failed to deserialize configuration");
                }

                // Set the root directory - try from config first, then auto-detect
                _rootDirectory = !string.IsNullOrEmpty(_config.Environment.RootDirectory) 
                    ? _config.Environment.RootDirectory 
                    : AutoDetectRootDirectory();

                ValidateConfiguration();
                
                Console.WriteLine($"✓ Configuration loaded successfully");
                Console.WriteLine($"✓ Root Directory: {_rootDirectory}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the path to the appsettings.json file
        /// </summary>
        private string GetConfigurationFilePath()
        {
            // First try to find it relative to the executing assembly
            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            
            // Walk up directories to find appsettings.json
            string currentDir = assemblyDir;
            for (int i = 0; i < 10; i++) // Limit search depth
            {
                string configPath = Path.Combine(currentDir, "appsettings.json");
                if (File.Exists(configPath))
                {
                    return configPath;
                }
                
                string? parentDir = Path.GetDirectoryName(currentDir);
                if (parentDir == null || parentDir == currentDir)
                    break;
                    
                currentDir = parentDir;
            }

            // If not found, try the current working directory
            string workingDirConfig = Path.Combine(Environment.CurrentDirectory, "appsettings.json");
            if (File.Exists(workingDirConfig))
            {
                return workingDirConfig;
            }

            throw new FileNotFoundException("appsettings.json not found in any parent directories");
        }

        /// <summary>
        /// Auto-detects the root directory based on the location of appsettings.json
        /// </summary>
        private string AutoDetectRootDirectory()
        {
            string configPath = GetConfigurationFilePath();
            return Path.GetDirectoryName(configPath) ?? Environment.CurrentDirectory;
        }

        /// <summary>
        /// Validates the loaded configuration
        /// </summary>
        private void ValidateConfiguration()
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration is null");

            var errors = new List<string>();

            // Validate database configuration
            if (string.IsNullOrWhiteSpace(_config.Database.Server))
                errors.Add("Database server is required");

            if (string.IsNullOrWhiteSpace(_config.Database.Database))
                errors.Add("Database name is required");

            // Validate root directory
            if (!Directory.Exists(_rootDirectory))
                errors.Add($"Root directory does not exist: {_rootDirectory}");

            if (errors.Any())
            {
                throw new InvalidOperationException($"Configuration validation failed:\n{string.Join("\n", errors)}");
            }
        }

        /// <summary>
        /// Gets the root directory path
        /// </summary>
        public string GetRootDirectory()
        {
            return _rootDirectory;
        }

        /// <summary>
        /// Resolves a relative path to an absolute path based on the root directory
        /// </summary>
        public string ResolvePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return _rootDirectory;

            if (Path.IsPathRooted(relativePath))
                return relativePath;

            return Path.GetFullPath(Path.Combine(_rootDirectory, relativePath));
        }

        /// <summary>
        /// Gets the full path for output Excel files directory (regular processed files)
        /// </summary>
        public string GetOutputExcelFilesPath()
        {
            return ResolvePath(_config?.Paths.OutputExcelFiles ?? throw new InvalidOperationException("OutputExcelFiles path not configured"));
        }

        /// <summary>
        /// Gets the full path for special Excel files directory (categorized sheets like SUP, DEM)
        /// </summary>
        public string GetSpecialExcelFilesPath()
        {
            return ResolvePath(_config?.Paths.SpecialExcelFiles ?? throw new InvalidOperationException("SpecialExcelFiles path not configured"));
        }

        /// <summary>
        /// Gets the full path for log files directory
        /// </summary>
        public string GetLogFilesPath()
        {
            return ResolvePath(_config?.Paths.LogFiles ?? throw new InvalidOperationException("LogFiles path not configured"));
        }

        /// <summary>
        /// Gets the database connection string
        /// </summary>
        public string GetConnectionString()
        {
            if (_config?.Database == null)
                throw new InvalidOperationException("Database configuration not loaded");

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = _config.Database.Server,
                InitialCatalog = _config.Database.Database,
                IntegratedSecurity = _config.Database.IntegratedSecurity,
                ConnectTimeout = _config.Database.ConnectionTimeout,
                TrustServerCertificate = true,
                MultipleActiveResultSets = true,
                MaxPoolSize = 100,
                Encrypt = false
            };

            if (!_config.Database.IntegratedSecurity && !string.IsNullOrEmpty(_config.Database.Username))
            {
                builder.UserID = _config.Database.Username;
                builder.Password = _config.Database.Password;
            }

            return builder.ConnectionString;
        }

        /// <summary>
        /// Gets the current configuration
        /// </summary>
        public UnifiedConfig GetConfiguration()
        {
            return _config ?? throw new InvalidOperationException("Configuration not loaded");
        }
    }
}