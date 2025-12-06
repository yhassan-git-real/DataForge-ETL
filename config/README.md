# DataForge ETL Configuration Files

This folder contains the split configuration files for DataForge ETL. Each file manages a specific aspect of the application's configuration.

## Configuration Files

### 📁 **environment.json**
Application environment settings
- `RootDirectory`: Base directory for the DataForge ETL application
- `Environment`: Current environment (Production, Development, etc.)

### 🗄️ **database.json**
Database connection configuration
- `Server`: SQL Server instance name
- `Database`: Database name
- `IntegratedSecurity`: Use Windows Authentication (true/false)
- `Username` / `Password`: SQL credentials (if not using integrated security)
- `ConnectionTimeout`: Connection timeout in seconds
- `CommandTimeout`: Command execution timeout (0 = no timeout)

### 📂 **paths.json**
File system paths for input/output operations
- `InputExcelFiles`: Source folder for Excel files
- `InputCsvFiles`: Source folder for CSV files
- `OutputExcelFiles`: Destination for processed Excel files
- `SpecialExcelFiles`: Destination for special categorized files
- `LogFiles`: Application log files location
- `TempFiles`: Temporary files location

### ⚙️ **modules.json**
Executable module definitions and ordering
- `DynamicTableManager`: Table configuration module
- `ExcelProcessor`: Excel file processing module
- `DatabaseLoader`: Excel to database loader
- `CsvToDatabase`: CSV to database loader

Each module includes:
- `RelativePath`: Path to the executable
- `Name`: Display name
- `Description`: Module description
- `Order`: Execution order
- `Arguments`: Command-line arguments

### 🔧 **processing.json**
Data processing parameters
- `BatchSize`: Number of rows per batch (default: 1,000,000)
- `ChunkSize`: Chunk size for Excel operations (default: 10,000)
- `SaveInterval`: Save interval for Excel files (default: 50,000)
- `ValidateColumnMapping`: Enable column validation
- `DefaultSheetName`: Default Excel sheet name
- `SpecialSheetKeywords`: Keywords for special file categorization
- `MemoryCleanupInterval`: Memory cleanup interval
- `MaxDegreeOfParallelism`: Parallel processing threads

### 📝 **logging.json**
Logging configuration
- `Level`: Log level (Information, Debug, Warning, Error)
- `EnableFileLogging`: Write logs to files
- `EnableConsoleLogging`: Display logs in console
- `LogRetentionDays`: Days to keep log files
- `MaxLogFileSizeMB`: Maximum log file size

### 📊 **tables.json**
Database table names for ETL operations
- `ErrorTableName`: Error logging table
- `SuccessLogTableName`: Success logging table
- `AutoCreateLogTables`: Automatically create logging tables

### 🔔 **notifications.json**
Progress notification settings
- `Csv.EnableProgressNotifications`: Enable CSV progress notifications
- `Csv.ProgressNotificationInterval`: Rows between CSV notifications
- `Excel.EnableProgressNotifications`: Enable Excel progress notifications
- `Excel.ProgressNotificationInterval`: Rows between Excel notifications

## How It Works

The `UnifiedConfigurationManager` automatically:
1. Detects the `config` folder in the root directory (required)
2. Loads and merges all JSON files into a unified configuration object
3. The config folder must exist - the old single `appsettings.json` file is no longer supported

## Editing Configuration

1. Open any configuration file in a text editor
2. Modify the values as needed
3. Save the file
4. Restart the application for changes to take effect

## Migration from Old Configuration

If you have an old `appsettings.json` file, it has been renamed to `appsettings.json.old`. The application now exclusively uses the split configuration files in the `config/` folder.

## Best Practices

- ✅ Keep sensitive data (passwords) in `database.json` and exclude from version control if needed
- ✅ Use relative paths where possible for portability
- ✅ Document any custom configurations
- ✅ Back up configuration files before making changes
- ✅ Test configuration changes in a development environment first

## Troubleshooting

If the application fails to load configuration:
1. Verify all JSON files have valid syntax
2. Check that required fields are present in each file
3. Review the application logs for specific error messages
4. Ensure file permissions allow reading the config folder

---

**Version**: 2.0.0  
**Last Updated**: December 2025
