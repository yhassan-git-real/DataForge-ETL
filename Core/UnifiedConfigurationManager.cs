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
        /// Gets the path to the appsettings.json file from the root project directory
        /// </summary>
        private string GetConfigurationFilePath()
        {
            // Find the root project directory (where Core, ETL modules exist)
            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            string currentDir = assemblyDir;
            
            for (int i = 0; i < 10; i++) // Limit search depth
            {
                // Check if this is the root project directory
                bool hasCore = Directory.Exists(Path.Combine(currentDir, "Core"));
                bool hasEtlModules = Directory.Exists(Path.Combine(currentDir, "ETL_CsvToDatabase")) ||
                                    Directory.Exists(Path.Combine(currentDir, "ETL_Excel")) ||
                                    Directory.Exists(Path.Combine(currentDir, "ETL_ExcelToDatabase"));
                
                if (hasCore && hasEtlModules)
                {
                    string rootConfigPath = Path.Combine(currentDir, "appsettings.json");
                    if (File.Exists(rootConfigPath))
                    {
                        return rootConfigPath;
                    }
                }
                
                string? parentDir = Path.GetDirectoryName(currentDir);
                if (parentDir == null || parentDir == currentDir)
                    break;
                    
                currentDir = parentDir;
            }

            throw new FileNotFoundException("appsettings.json not found in root project directory. Ensure the file exists in the directory containing Core and ETL modules.");
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
        /// Saves the current configuration back to appsettings.json
        /// </summary>
        public void SaveConfiguration()
        {
            try
            {
                if (_config == null)
                    throw new InvalidOperationException("No configuration to save");

                string configPath = GetConfigurationFilePath();
                string jsonContent = JsonConvert.SerializeObject(_config, Formatting.Indented);
                
                // Create backup
                string backupPath = configPath + ".backup";
                if (File.Exists(configPath))
                {
                    File.Copy(configPath, backupPath, true);
                }

                File.WriteAllText(configPath, jsonContent);
                Console.WriteLine($"✓ Configuration saved to: {configPath}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save configuration: {ex.Message}", ex);
            }
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
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    UNIFIED CONFIGURATION                      ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
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