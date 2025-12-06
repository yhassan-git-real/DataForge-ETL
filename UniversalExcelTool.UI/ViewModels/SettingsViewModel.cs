using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniversalExcelTool.UI.Models;
using UniversalExcelTool.UI.Services;
using UniversalExcelTool.UI.Views;
using UniversalExcelTool.Core;
using Newtonsoft.Json.Linq;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Data.SqlClient;

namespace UniversalExcelTool.UI.ViewModels
{
    /// <summary>
    /// View model for application settings and configuration
    /// </summary>
    public partial class SettingsViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _connectionString = string.Empty;

        [ObservableProperty]
        private string _databaseServer = string.Empty;

        [ObservableProperty]
        private string _databaseName = string.Empty;

        [ObservableProperty]
        private int _authenticationMode = 0; // 0 = Windows, 1 = SQL

        [ObservableProperty]
        private bool _trustServerCertificate = true;

        [ObservableProperty]
        private string _databaseUsername = string.Empty;

        [ObservableProperty]
        private string _databasePassword = string.Empty;

        [ObservableProperty]
        private bool _isSqlAuthentication = false;

        [ObservableProperty]
        private bool _showConnectionNotification = false;

        [ObservableProperty]
        private bool _connectionSuccess = false;

        [ObservableProperty]
        private string _connectionMessage = string.Empty;

        [ObservableProperty]
        private bool _isDatabaseConfigOpen = false;

        [ObservableProperty]
        private bool _isConnectionConfigured = false;

        [ObservableProperty]
        private string _databaseConnectionSummary = "Click to configure SQL Server connection";

        [ObservableProperty]
        private string _connectionStatusText = "Not Configured";

        [ObservableProperty]
        private string _inputExcelPath = string.Empty;

        [ObservableProperty]
        private string _inputCsvPath = string.Empty;

        [ObservableProperty]
        private string _outputCsvPath = string.Empty;

        [ObservableProperty]
        private string _logsPath = string.Empty;

        [ObservableProperty]
        private string _dynamicTableManagerPath = string.Empty;

        [ObservableProperty]
        private string _excelProcessorPath = string.Empty;

        [ObservableProperty]
        private string _databaseLoaderPath = string.Empty;

        [ObservableProperty]
        private string _csvToDatabasePath = string.Empty;

        [ObservableProperty]
        private bool _hasUnsavedChanges;

        [ObservableProperty]
        private ObservableCollection<LogEntry> _logEntries = new();

        private readonly IUILogger _logger;
        private readonly UnifiedConfigurationManager _configManager;
        private readonly FileBrowserService _fileBrowserService;
        private readonly string _appSettingsPath;
        private JObject? _currentConfig;
        private DatabaseConfigWindow? _databaseConfigWindow;

        public SettingsViewModel()
        {
            _logger = new AvaloniaLogger(_logEntries);
            _configManager = UnifiedConfigurationManager.Instance;
            _fileBrowserService = new FileBrowserService();
            _appSettingsPath = Path.Combine(_configManager.GetRootDirectory(), "appsettings.json");
            
            LoadSettings();
        }

        [RelayCommand]
        private async Task BrowseInputExcelPath()
        {
            var result = await _fileBrowserService.BrowseFolderAsync("Select Input Excel Directory", InputExcelPath);
            if (!string.IsNullOrEmpty(result))
            {
                InputExcelPath = result;
                HasUnsavedChanges = true;
            }
        }

        [RelayCommand]
        private async Task BrowseInputCsvPath()
        {
            var result = await _fileBrowserService.BrowseFolderAsync("Select Input CSV Directory", InputCsvPath);
            if (!string.IsNullOrEmpty(result))
            {
                InputCsvPath = result;
                HasUnsavedChanges = true;
            }
        }

        [RelayCommand]
        private async Task BrowseOutputCsvPath()
        {
            var result = await _fileBrowserService.BrowseFolderAsync("Select Output Excel Directory", OutputCsvPath);
            if (!string.IsNullOrEmpty(result))
            {
                OutputCsvPath = result;
                HasUnsavedChanges = true;
            }
        }

        [RelayCommand]
        private async Task BrowseLogsPath()
        {
            var result = await _fileBrowserService.BrowseFolderAsync("Select Logs Directory", LogsPath);
            if (!string.IsNullOrEmpty(result))
            {
                LogsPath = result;
                HasUnsavedChanges = true;
            }
        }

        [RelayCommand]
        private async Task BrowseDynamicTableManagerPath()
        {
            var result = await _fileBrowserService.BrowseFileAsync("Executables (*.exe)|*.exe", DynamicTableManagerPath);
            if (!string.IsNullOrEmpty(result))
            {
                DynamicTableManagerPath = result;
                HasUnsavedChanges = true;
            }
        }

        [RelayCommand]
        private async Task BrowseExcelProcessorPath()
        {
            var result = await _fileBrowserService.BrowseFileAsync("Executables (*.exe)|*.exe", ExcelProcessorPath);
            if (!string.IsNullOrEmpty(result))
            {
                ExcelProcessorPath = result;
                HasUnsavedChanges = true;
            }
        }

        [RelayCommand]
        private async Task BrowseDatabaseLoaderPath()
        {
            var result = await _fileBrowserService.BrowseFileAsync("Executables (*.exe)|*.exe", DatabaseLoaderPath);
            if (!string.IsNullOrEmpty(result))
            {
                DatabaseLoaderPath = result;
                HasUnsavedChanges = true;
            }
        }

        [RelayCommand]
        private async Task BrowseCsvToDatabasePath()
        {
            var result = await _fileBrowserService.BrowseFileAsync("Executables (*.exe)|*.exe", CsvToDatabasePath);
            if (!string.IsNullOrEmpty(result))
            {
                CsvToDatabasePath = result;
                HasUnsavedChanges = true;
            }
        }

        [RelayCommand]
        private void LoadSettings()
        {
            try
            {
                _logger.LogInfo("Loading settings from appsettings.json...", "settings");

                if (!File.Exists(_appSettingsPath))
                {
                    _logger.LogError($"Configuration file not found: {_appSettingsPath}");
                    return;
                }

                string json = File.ReadAllText(_appSettingsPath);
                _currentConfig = JObject.Parse(json);

                // Load database settings from config
                var dbConfig = _currentConfig["Database"];
                if (dbConfig != null)
                {
                    DatabaseServer = dbConfig["Server"]?.ToString() ?? string.Empty;
                    DatabaseName = dbConfig["Database"]?.ToString() ?? string.Empty;
                    bool integratedSecurity = dbConfig["IntegratedSecurity"]?.Value<bool>() ?? true;
                    AuthenticationMode = integratedSecurity ? 0 : 1;
                    DatabaseUsername = dbConfig["Username"]?.ToString() ?? string.Empty;
                    DatabasePassword = dbConfig["Password"]?.ToString() ?? string.Empty;
                    TrustServerCertificate = true; // Default to true for local development
                }

                // Build connection string from components
                BuildConnectionString();
                
                // Update connection summary after loading
                UpdateConnectionSummary();

                // Load paths
                var pathsConfig = _currentConfig["Paths"];
                if (pathsConfig != null)
                {
                    InputExcelPath = pathsConfig["InputExcelFiles"]?.ToString() ?? string.Empty;
                    InputCsvPath = pathsConfig["InputCsvFiles"]?.ToString() ?? string.Empty;
                    OutputCsvPath = pathsConfig["OutputExcelFiles"]?.ToString() ?? string.Empty;
                    LogsPath = pathsConfig["LogFiles"]?.ToString() ?? string.Empty;
                }

                // Load executable paths
                var modulesConfig = _currentConfig["ExecutableModules"];
                if (modulesConfig != null)
                {
                    DynamicTableManagerPath = modulesConfig["DynamicTableManager"]?["ExecutablePath"]?.ToString() ?? string.Empty;
                    ExcelProcessorPath = modulesConfig["ExcelProcessor"]?["ExecutablePath"]?.ToString() ?? string.Empty;
                    DatabaseLoaderPath = modulesConfig["DatabaseLoader"]?["ExecutablePath"]?.ToString() ?? string.Empty;
                    CsvToDatabasePath = modulesConfig["CsvToDatabase"]?["ExecutablePath"]?.ToString() ?? string.Empty;
                }

                HasUnsavedChanges = false;
                _logger.LogInfo("Settings loaded successfully", "settings");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading settings: {ex.Message}");
            }
        }

        [RelayCommand]
        private void OpenDatabaseConfig()
        {
            // Always create a new window for simplicity
            _databaseConfigWindow = new DatabaseConfigWindow(this);
            
            // Get the main window as owner
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _databaseConfigWindow.ShowDialog(desktop.MainWindow);
            }
            else
            {
                _databaseConfigWindow.Show();
            }
            
            _logger.LogInfo("Database configuration window opened", "settings");
        }

        [RelayCommand]
        private void CloseDatabaseConfig()
        {
            _databaseConfigWindow?.Close();
            _logger.LogInfo("Database configuration window closed", "settings");
        }

        [RelayCommand]
        private void CancelDatabaseConfig()
        {
            _databaseConfigWindow?.Close();
            _logger.LogInfo("Database configuration cancelled", "settings");
        }

        [RelayCommand]
        private void SaveDatabaseConfig()
        {
            try
            {
                // Update connection summary and status
                UpdateConnectionSummary();
                
                // Close the window
                _databaseConfigWindow?.Close();
                
                // Mark as having unsaved changes
                HasUnsavedChanges = true;
                
                _logger.LogInfo("Database configuration saved", "settings");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving database configuration: {ex.Message}");
            }
        }

        private void UpdateConnectionSummary()
        {
            try
            {
                var hasServer = !string.IsNullOrWhiteSpace(DatabaseServer);
                var hasDatabase = !string.IsNullOrWhiteSpace(DatabaseName);
                var hasCredentials = AuthenticationMode == 0 || (!string.IsNullOrWhiteSpace(DatabaseUsername) && !string.IsNullOrWhiteSpace(DatabasePassword));
                
                IsConnectionConfigured = hasServer && hasDatabase && hasCredentials;
                
                if (IsConnectionConfigured)
                {
                    var authType = AuthenticationMode == 0 ? "Windows Auth" : "SQL Auth";
                    DatabaseConnectionSummary = $"{DatabaseServer}/{DatabaseName} ({authType})";
                    ConnectionStatusText = "Configured";
                }
                else
                {
                    DatabaseConnectionSummary = "Click to configure SQL Server connection";
                    ConnectionStatusText = "Not Configured";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating connection summary: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            try
            {
                _logger.LogInfo("Saving settings...", "settings");

                if (_currentConfig == null)
                {
                    _logger.LogError("No configuration loaded to save");
                    return;
                }

                // Update database configuration
                var dbConfig = _currentConfig["Database"];
                if (dbConfig != null)
                {
                    dbConfig["Server"] = DatabaseServer;
                    dbConfig["Database"] = DatabaseName;
                    dbConfig["IntegratedSecurity"] = AuthenticationMode == 0;
                    dbConfig["Username"] = DatabaseUsername;
                    dbConfig["Password"] = DatabasePassword;
                }

                // Update paths configuration
                var pathsConfig = _currentConfig["Paths"];
                if (pathsConfig != null)
                {
                    pathsConfig["InputExcelFiles"] = InputExcelPath;
                    pathsConfig["InputCsvFiles"] = InputCsvPath;
                    pathsConfig["OutputExcelFiles"] = OutputCsvPath;
                    pathsConfig["LogFiles"] = LogsPath;
                }

                // Update executable paths
                var modulesConfig = _currentConfig["ExecutableModules"];
                if (modulesConfig != null)
                {
                    if (modulesConfig["DynamicTableManager"] != null)
                        modulesConfig["DynamicTableManager"]!["ExecutablePath"] = DynamicTableManagerPath;
                    if (modulesConfig["ExcelProcessor"] != null)
                        modulesConfig["ExcelProcessor"]!["ExecutablePath"] = ExcelProcessorPath;
                    if (modulesConfig["DatabaseLoader"] != null)
                        modulesConfig["DatabaseLoader"]!["ExecutablePath"] = DatabaseLoaderPath;
                    if (modulesConfig["CsvToDatabase"] != null)
                        modulesConfig["CsvToDatabase"]!["ExecutablePath"] = CsvToDatabasePath;
                }

                // Write to file
                await File.WriteAllTextAsync(_appSettingsPath, _currentConfig.ToString());

                // Reload configuration in manager to apply changes immediately
                _configManager.LoadConfiguration();

                HasUnsavedChanges = false;
                _logger.LogInfo("Settings saved successfully", "settings");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving settings: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task TestConnectionAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                _logger.LogInfo("Testing database connection...", "settings");
                BuildConnectionString();

                // Reset status
                ShowConnectionNotification = false;

                // Real connection test
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                }

                ShowConnectionNotification = true;
                ConnectionSuccess = true;
                ConnectionMessage = "Connection test successful!";
                
                _logger.LogInfo("Database connection test completed successfully", "settings");
            }
            catch (Exception ex)
            {
                ShowConnectionNotification = true;
                ConnectionSuccess = false;
                ConnectionMessage = $"Connection failed: {ex.Message}";
                _logger.LogError($"Database connection test failed: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        partial void OnAuthenticationModeChanged(int value)
        {
            IsSqlAuthentication = value == 1;
            BuildConnectionString();
        }

        partial void OnDatabaseServerChanged(string value)
        {
            BuildConnectionString();
            HasUnsavedChanges = true;
        }

        partial void OnDatabaseNameChanged(string value)
        {
            BuildConnectionString();
            HasUnsavedChanges = true;
        }

        partial void OnDatabaseUsernameChanged(string value)
        {
            BuildConnectionString();
            HasUnsavedChanges = true;
        }

        partial void OnDatabasePasswordChanged(string value)
        {
            BuildConnectionString();
            HasUnsavedChanges = true;
        }

        partial void OnTrustServerCertificateChanged(bool value)
        {
            BuildConnectionString();
            HasUnsavedChanges = true;
        }

        private void BuildConnectionString()
        {
            var builder = new System.Text.StringBuilder();
            
            if (!string.IsNullOrWhiteSpace(DatabaseServer))
            {
                builder.Append($"Server={DatabaseServer};");
            }

            if (!string.IsNullOrWhiteSpace(DatabaseName))
            {
                builder.Append($"Database={DatabaseName};");
            }

            if (AuthenticationMode == 0)
            {
                builder.Append("Integrated Security=true;");
            }
            else
            {
                builder.Append("Integrated Security=false;");
                if (!string.IsNullOrWhiteSpace(DatabaseUsername))
                {
                    builder.Append($"User ID={DatabaseUsername};");
                }
                if (!string.IsNullOrWhiteSpace(DatabasePassword))
                {
                    builder.Append($"Password={DatabasePassword};");
                }
            }

            if (TrustServerCertificate)
            {
                builder.Append("TrustServerCertificate=true;");
            }

            ConnectionString = builder.ToString();
        }
    }
}