using System;
using System.IO;
using System.Linq;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Reflection;

namespace DataForgeETL.Core
{
    /// <summary>
    /// Centralized configuration manager for DataForge ETL
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
        /// Loads the unified configuration from config folder
        /// </summary>
        public void LoadConfiguration()
        {
            try
            {
                string rootDir = FindRootProjectDirectory();
                string configFolder = Path.Combine(rootDir, "config");
                
                // Config folder is required
                if (!Directory.Exists(configFolder))
                {
                    throw new DirectoryNotFoundException($"Configuration folder not found: {configFolder}. Please ensure the 'config' folder exists in the root directory.");
                }

                _config = LoadSplitConfiguration(configFolder);

                if (_config == null)
                {
                    throw new InvalidOperationException("Failed to deserialize configuration");
                }

                // Set the root directory - try from config first, then auto-detect
                _rootDirectory = !string.IsNullOrEmpty(_config.Environment.RootDirectory) 
                    ? _config.Environment.RootDirectory 
                    : AutoDetectRootDirectory();

                ValidateConfiguration();
                
                Console.WriteLine($"✓ Configuration loaded successfully from config folder");
                Console.WriteLine($"✓ Root Directory: {_rootDirectory}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Loads and merges configuration from multiple JSON files in the config folder
        /// </summary>
        private UnifiedConfig LoadSplitConfiguration(string configFolder)
        {
            var config = new UnifiedConfig();

            // Load each configuration file and merge
            var configFiles = new[]
            {
                "environment.json",
                "database.json",
                "paths.json",
                "modules.json",
                "processing.json",
                "logging.json",
                "tables.json",
                "notifications.json"
            };

            foreach (var configFile in configFiles)
            {
                string filePath = Path.Combine(configFolder, configFile);
                if (File.Exists(filePath))
                {
                    string jsonContent = File.ReadAllText(filePath);
                    var partialConfig = JsonConvert.DeserializeObject<UnifiedConfig>(jsonContent);
                    
                    if (partialConfig != null)
                    {
                        MergeConfiguration(config, partialConfig);
                    }
                }
            }

            return config;
        }

        /// <summary>
        /// Merges partial configuration into the main configuration
        /// </summary>
        private void MergeConfiguration(UnifiedConfig target, UnifiedConfig source)
        {
            // Merge Environment
            if (source.Environment != null && !string.IsNullOrEmpty(source.Environment.RootDirectory))
            {
                target.Environment.RootDirectory = source.Environment.RootDirectory;
                target.Environment.Environment = source.Environment.Environment;
            }

            // Merge Database
            if (source.Database != null && !string.IsNullOrEmpty(source.Database.Server))
            {
                target.Database = source.Database;
            }

            // Merge Paths
            if (source.Paths != null)
            {
                if (!string.IsNullOrEmpty(source.Paths.InputExcelFiles))
                    target.Paths.InputExcelFiles = source.Paths.InputExcelFiles;
                if (!string.IsNullOrEmpty(source.Paths.InputCsvFiles))
                    target.Paths.InputCsvFiles = source.Paths.InputCsvFiles;
                if (!string.IsNullOrEmpty(source.Paths.OutputExcelFiles))
                    target.Paths.OutputExcelFiles = source.Paths.OutputExcelFiles;
                if (!string.IsNullOrEmpty(source.Paths.SpecialExcelFiles))
                    target.Paths.SpecialExcelFiles = source.Paths.SpecialExcelFiles;
                if (!string.IsNullOrEmpty(source.Paths.LogFiles))
                    target.Paths.LogFiles = source.Paths.LogFiles;
                if (!string.IsNullOrEmpty(source.Paths.TempFiles))
                    target.Paths.TempFiles = source.Paths.TempFiles;
            }

            // Merge ExecutableModules (only if source has actual data)
            if (source.ExecutableModules != null && 
                !string.IsNullOrEmpty(source.ExecutableModules.DynamicTableManager?.RelativePath))
            {
                target.ExecutableModules = source.ExecutableModules;
            }

            // Merge Processing (only if source has actual data)
            if (source.Processing != null && source.Processing.BatchSize > 0)
            {
                target.Processing = source.Processing;
            }

            // Merge Logging (only if source has actual data)
            if (source.Logging != null && !string.IsNullOrEmpty(source.Logging.Level))
            {
                target.Logging = source.Logging;
            }

            // Merge Tables (only if source has actual data)
            if (source.Tables != null && !string.IsNullOrEmpty(source.Tables.ErrorTableName))
            {
                target.Tables = source.Tables;
            }

            // Merge Notifications (only if source has actual data - check if CSV or Excel notifications are configured)
            if (source.Notifications != null && 
                (source.Notifications.Csv != null || source.Notifications.Excel != null))
            {
                target.Notifications = source.Notifications;
            }
        }

        /// <summary>
        /// Finds the root project directory
        /// </summary>
        private string FindRootProjectDirectory()
        {
            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            string currentDir = assemblyDir;
            
            for (int i = 0; i < 10; i++)
            {
                bool hasCore = Directory.Exists(Path.Combine(currentDir, "Core"));
                bool hasEtlModules = Directory.Exists(Path.Combine(currentDir, "ETL_CsvToDatabase")) ||
                                    Directory.Exists(Path.Combine(currentDir, "ETL_Excel")) ||
                                    Directory.Exists(Path.Combine(currentDir, "ETL_ExcelToDatabase"));
                
                if (hasCore && hasEtlModules)
                {
                    return currentDir;
                }
                
                string? parentDir = Path.GetDirectoryName(currentDir);
                if (parentDir == null || parentDir == currentDir)
                    break;
                    
                currentDir = parentDir;
            }

            throw new DirectoryNotFoundException("Root project directory not found");
        }

        /// <summary>
        /// Auto-detects the root directory based on the location of config folder
        /// </summary>
        private string AutoDetectRootDirectory()
        {
            try
            {
                return FindRootProjectDirectory();
            }
            catch
            {
                return Environment.CurrentDirectory;
            }
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
                throw new InvalidOperationException($"Configuration validation failed:\\n{string.Join("\\n", errors)}");
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
        /// Gets the full path for input Excel files directory (raw/source files)
        /// </summary>
        public string GetInputExcelFilesPath()
        {
            return ResolvePath(_config?.Paths.InputExcelFiles ?? throw new InvalidOperationException("InputExcelFiles path not configured"));
        }

        /// <summary>
        /// Gets the full path for input CSV files directory (raw/source CSV files)
        /// </summary>
        public string GetInputCsvFilesPath()
        {
            return ResolvePath(_config?.Paths.InputCsvFiles ?? throw new InvalidOperationException("InputCsvFiles path not configured"));
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
        /// Gets the full path for a specific executable module
        /// </summary>
        public string GetExecutablePath(string moduleName)
        {
            if (_config?.ExecutableModules == null)
                throw new InvalidOperationException("Executable modules configuration not loaded");

            var moduleConfig = moduleName.ToLowerInvariant() switch
            {
                "dynamictablemanager" => _config.ExecutableModules.DynamicTableManager,
                "excelprocessor" => _config.ExecutableModules.ExcelProcessor,
                "databaseloader" => _config.ExecutableModules.DatabaseLoader,
                "csvtodatabase" => _config.ExecutableModules.CsvToDatabase,
                _ => throw new ArgumentException($"Unknown module: {moduleName}")
            };

            // Try Release path first
            string releasePath = ResolvePath(moduleConfig.RelativePath);
            if (File.Exists(releasePath))
            {
                return releasePath;
            }

            // Fallback to Debug path
            string debugPath = moduleConfig.RelativePath.Replace("\\Release\\", "\\Debug\\");
            debugPath = ResolvePath(debugPath);
            if (File.Exists(debugPath))
            {
                return debugPath;
            }

            // Return original path for error reporting
            return releasePath;
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
        /// Creates necessary directories if they don't exist
        /// </summary>
        public void EnsureDirectoriesExist()
        {
            try
            {
                var directories = new[]
                {
                    GetOutputExcelFilesPath(),
                    GetSpecialExcelFilesPath(),
                    GetLogFilesPath(),
                    ResolvePath(_config?.Paths.TempFiles ?? throw new InvalidOperationException("TempFiles path not configured"))
                };

                foreach (string dir in directories)
                {
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                        Console.WriteLine($"✓ Created directory: {dir}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create directories: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Ensures a specific directory exists, creating it if necessary
        /// </summary>
        public void EnsureDirectoryExists(string directoryPath)
        {
            try
            {
                var resolvedPath = ResolvePath(directoryPath);
                if (!Directory.Exists(resolvedPath))
                {
                    Directory.CreateDirectory(resolvedPath);
                    Console.WriteLine($"✓ Created directory: {resolvedPath}");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create directory {directoryPath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the current configuration
        /// </summary>
        public UnifiedConfig GetConfiguration()
        {
            return _config ?? throw new InvalidOperationException("Configuration not loaded");
        }

        /// <summary>
        /// Updates the root directory and saves the configuration
        /// </summary>
        public void UpdateRootDirectory(string newRootDirectory)
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration not loaded");

            _config.Environment.RootDirectory = newRootDirectory;
            _rootDirectory = newRootDirectory;
            
            SaveConfiguration();
        }

        /// <summary>
        /// Saves the current configuration back to split config files
        /// </summary>
        public void SaveConfiguration()
        {
            try
            {
                if (_config == null)
                    throw new InvalidOperationException("No configuration to save");

                string rootDir = FindRootProjectDirectory();
                string configFolder = Path.Combine(rootDir, "config");
                
                if (!Directory.Exists(configFolder))
                    throw new DirectoryNotFoundException($"Configuration folder not found: {configFolder}");

                // Save each configuration section to its respective file
                SaveConfigSection(configFolder, "environment.json", new { Environment = _config.Environment });
                SaveConfigSection(configFolder, "database.json", new { Database = _config.Database });
                SaveConfigSection(configFolder, "paths.json", new { Paths = _config.Paths });
                SaveConfigSection(configFolder, "modules.json", new { ExecutableModules = _config.ExecutableModules });
                SaveConfigSection(configFolder, "processing.json", new { Processing = _config.Processing });
                SaveConfigSection(configFolder, "logging.json", new { Logging = _config.Logging });
                SaveConfigSection(configFolder, "tables.json", new { Tables = _config.Tables });
                SaveConfigSection(configFolder, "notifications.json", new { Notifications = _config.Notifications });

                Console.WriteLine($"✓ Configuration saved to config folder: {configFolder}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Saves a configuration section to a specific file
        /// </summary>
        private void SaveConfigSection(string configFolder, string fileName, object configSection)
        {
            string filePath = Path.Combine(configFolder, fileName);
            string jsonContent = JsonConvert.SerializeObject(configSection, Formatting.Indented);
            
            // Create backup
            if (File.Exists(filePath))
            {
                string backupPath = filePath + ".backup";
                File.Copy(filePath, backupPath, true);
            }

            File.WriteAllText(filePath, jsonContent);
        }

        /// <summary>
        /// Displays current configuration summary
        /// </summary>
        public void DisplayConfigurationSummary()
        {
            if (_config == null)
            {
                Console.WriteLine("⚠ No configuration loaded");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("=================================================================");
            Console.WriteLine("                    UNIFIED CONFIGURATION");
            Console.WriteLine("=================================================================");
            Console.WriteLine($"Root Directory: {_rootDirectory}");
            Console.WriteLine($"Environment: {_config.Environment.Environment}");
            Console.WriteLine($"Database Server: {_config.Database.Server}");
            Console.WriteLine($"Database Name: {_config.Database.Database}");
            Console.WriteLine($"Input Excel Path: {GetInputExcelFilesPath()}");
            Console.WriteLine($"Output Excel Path: {GetOutputExcelFilesPath()}");
            Console.WriteLine($"Special Excel Path: {GetSpecialExcelFilesPath()}");
            Console.WriteLine($"Log Files Path: {GetLogFilesPath()}");
            Console.WriteLine($"Batch Size: {_config.Processing.BatchSize:N0}");
            Console.WriteLine();
        }
    }
}