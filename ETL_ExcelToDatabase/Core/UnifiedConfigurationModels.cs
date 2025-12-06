namespace ETL_ExcelToDatabase.Core
{
    /// <summary>
    /// Unified configuration model for the entire DataForge ETL system
    /// </summary>
    public class UnifiedConfig
    {
        public EnvironmentConfig Environment { get; set; } = new();
        public DatabaseConfig Database { get; set; } = new();
        public PathsConfig Paths { get; set; } = new();
        public ExecutableModulesConfig ExecutableModules { get; set; } = new();
        public ProcessingConfig Processing { get; set; } = new();
        public LoggingConfig Logging { get; set; } = new();
        public TablesConfig Tables { get; set; } = new();
        public NotificationsConfig Notifications { get; set; } = new();
    }

    /// <summary>
    /// Environment-specific configuration
    /// </summary>
    public class EnvironmentConfig
    {
        public string RootDirectory { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
    }

    /// <summary>
    /// Database configuration
    /// </summary>
    public class DatabaseConfig
    {
        public string Server { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public bool IntegratedSecurity { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int ConnectionTimeout { get; set; } = 0;
        public int CommandTimeout { get; set; } = 0;
    }

    /// <summary>
    /// File and directory paths configuration (all relative to root)
    /// </summary>
    public class PathsConfig
    {
        public string InputExcelFiles { get; set; } = string.Empty;      // Input: Raw Excel files to process
        public string OutputExcelFiles { get; set; } = string.Empty;     // Output: Regular processed Excel files
        public string SpecialExcelFiles { get; set; } = string.Empty;    // Output: Special categorized Excel files (SUP, DEM)
        public string LogFiles { get; set; } = string.Empty;             // Logs: Application logs and audit trails
        public string TempFiles { get; set; } = string.Empty;            // Temp: Temporary processing files
        public string ConfigFiles { get; set; } = string.Empty;
    }

    /// <summary>
    /// Executable modules configuration
    /// </summary>
    public class ExecutableModulesConfig
    {
        public ExecutableModuleInfo DynamicTableManager { get; set; } = new();
        public ExecutableModuleInfo ExcelProcessor { get; set; } = new();
        public ExecutableModuleInfo DatabaseLoader { get; set; } = new();
    }

    /// <summary>
    /// Individual executable module information
    /// </summary>
    public class ExecutableModuleInfo
    {
        public string RelativePath { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Order { get; set; }
        public string Arguments { get; set; } = string.Empty;
    }

    /// <summary>
    /// Processing configuration
    /// </summary>
    public class ProcessingConfig
    {
        public int BatchSize { get; set; } = 1000000;
        public bool ValidateColumnMapping { get; set; } = true;
        public string DefaultSheetName { get; set; } = "null";
        public int MaxConcurrentFiles { get; set; } = 5;
        public int RetryAttempts { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 5;
        public int ChunkSize { get; set; } = 10000;
        public int SaveInterval { get; set; } = 50000;
        public int MemoryCleanupInterval { get; set; } = 5;
        public int MaxDegreeOfParallelism { get; set; } = 1;
    }

    /// <summary>
    /// Logging configuration
    /// </summary>
    public class LoggingConfig
    {
        public string Level { get; set; } = string.Empty;
        public bool EnableFileLogging { get; set; } = false;
        public bool EnableConsoleLogging { get; set; } = false;
        public int LogRetentionDays { get; set; } = 0;
        public int MaxLogFileSizeMB { get; set; } = 0;
    }

    /// <summary>
    /// Tables configuration (predefined tables)
    /// </summary>
    public class TablesConfig
    {
        public string ErrorTableName { get; set; } = string.Empty;
        public string SuccessLogTableName { get; set; } = string.Empty;
        public bool AutoCreateLogTables { get; set; } = true;
    }

    /// <summary>
    /// Notification settings for progress updates
    /// </summary>
    public class NotificationsConfig
    {
        public CsvNotificationSettings Csv { get; set; } = new();
        public ExcelNotificationSettings Excel { get; set; } = new();
    }

    /// <summary>
    /// CSV-specific notification settings
    /// </summary>
    public class CsvNotificationSettings
    {
        public bool EnableProgressNotifications { get; set; } = true;
        public int ProgressNotificationInterval { get; set; } = 50000;
    }

    /// <summary>
    /// Excel-specific notification settings
    /// </summary>
    public class ExcelNotificationSettings
    {
        public bool EnableProgressNotifications { get; set; } = true;
        public int ProgressNotificationInterval { get; set; } = 50000;
    }

    /// <summary>
    /// Legacy compatibility wrapper class
    /// </summary>
    public class AppConfig
    {
        public DatabaseConfig DatabaseConfig { get; set; } = new();
        public ProcessConfig ProcessConfig { get; set; } = new();
    }

    /// <summary>
    /// Legacy ProcessConfig for compatibility
    /// </summary>
    public class ProcessConfig
    {
        public string ExcelFolderPath { get; set; } = string.Empty;
        public string TempTableName { get; set; } = string.Empty;
        public string DestinationTableName { get; set; } = string.Empty;
        public string ErrorTableName { get; set; } = string.Empty;
        public string SuccessLogTableName { get; set; } = string.Empty;
        public string LogFolderPath { get; set; } = string.Empty;
        public int BatchSize { get; set; }
        public bool ValidateColumnMapping { get; set; } = true;
        public string DefaultSheetName { get; set; } = "Sheet1";
    }
}