using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniversalExcelTool.UI.Models;
using UniversalExcelTool.UI.Services;
using UniversalExcelTool.Core;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;

namespace UniversalExcelTool.UI.ViewModels
{
    /// <summary>
    /// View model for dynamic table configuration - Step-by-step wizard matching console app
    /// </summary>
    public partial class DynamicTableConfigViewModel : ViewModelBase
    {
        // Configuration Data
        [ObservableProperty]
        private string _tempTableName = string.Empty;

        [ObservableProperty]
        private string _destinationTableName = string.Empty;

        [ObservableProperty]
        private bool _targetTableExists;

        [ObservableProperty]
        private bool _shouldTruncate;

        [ObservableProperty]
        private bool _createNewTable;

        // UI State
        [ObservableProperty]
        private int _currentStep = 1;

        [ObservableProperty]
        private bool _isStep1Enabled = true;

        [ObservableProperty]
        private bool _isStep2Enabled;

        [ObservableProperty]
        private bool _isStep3Enabled;

        [ObservableProperty]
        private bool _configurationComplete;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _tableCheckMessage = string.Empty;

        [ObservableProperty]
        private bool _showTruncateOption;

        [ObservableProperty]
        private bool _showCreateTableInfo;

        [ObservableProperty]
        private ObservableCollection<LogEntry> _logEntries = new();

        private readonly IUILogger _logger;
        private readonly UnifiedConfigurationManager _configManager;
        private readonly string _configFilePath;

        public DynamicTableConfigViewModel()
        {
            _logger = new AvaloniaLogger(_logEntries);
            _configManager = UnifiedConfigurationManager.Instance;
            _configFilePath = Path.Combine(_configManager.GetRootDirectory(), "dynamic_table_config.json");
            
            _logger.LogInfo("🎯 Dynamic Table Configuration Wizard", "wizard");
            _logger.LogInfo("This wizard will guide you through configuring table names for your ETL process", "wizard");
            StatusMessage = "➡️ Step 1: Enter temporary table name and click Next";
            LoadExistingConfiguration();
        }

        [RelayCommand]
        private void Step1_Next()
        {
            if (string.IsNullOrWhiteSpace(TempTableName))
            {
                StatusMessage = "❌ Please enter a temporary table name";
                _logger.LogError("Temporary table name is required");
                return;
            }

            if (!IsValidTableName(TempTableName))
            {
                StatusMessage = "❌ Invalid table name. Use only letters, numbers, and underscores. Must start with a letter.";
                _logger.LogError($"Invalid table name format: {TempTableName}");
                return;
            }

            _logger.LogSuccess($"✓ Temp table name set: {TempTableName}");
            CurrentStep = 2;
            IsStep2Enabled = true;
            StatusMessage = "➡️ Step 2: Enter destination table name and check if it exists";
        }

        [RelayCommand]
        private async Task Step2_CheckTableAsync()
        {
            if (string.IsNullOrWhiteSpace(DestinationTableName))
            {
                StatusMessage = "❌ Please enter a destination table name";
                _logger.LogError("Destination table name is required");
                return;
            }

            if (!IsValidTableName(DestinationTableName))
            {
                StatusMessage = "❌ Invalid table name. Use only letters, numbers, and underscores. Must start with a letter.";
                _logger.LogError($"Invalid table name format: {DestinationTableName}");
                return;
            }

            try
            {
                IsBusy = true;
                BusyMessage = "Checking if table exists in database...";
                _logger.LogInfo($"🔍 Checking if table '{DestinationTableName}' exists...", "database");

                bool tableExists = await Task.Run(() =>
                {
                    string connectionString = _configManager.GetConnectionString();
                    using var connection = new SqlConnection(connectionString);
                    connection.Open();

                    string checkTableQuery = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_NAME = @TableName
                        AND TABLE_TYPE = 'BASE TABLE'";

                    using var command = new SqlCommand(checkTableQuery, connection);
                    command.Parameters.AddWithValue("@TableName", DestinationTableName);
                    int tableCount = (int)command.ExecuteScalar();
                    return tableCount > 0;
                });

                TargetTableExists = tableExists;
                CurrentStep = 3;
                IsStep3Enabled = true;

                if (tableExists)
                {
                    ShowTruncateOption = true;
                    ShowCreateTableInfo = false;
                    CreateNewTable = false;
                    
                    TableCheckMessage = $"✓ Table '{DestinationTableName}' exists in database";
                    StatusMessage = "⚠️ Step 3: Table exists. Do you want to truncate it?";
                    _logger.LogSuccess($"Table '{DestinationTableName}' found in database");
                    _logger.LogWarning("⚠️ Truncating will delete ALL existing data!");
                }
                else
                {
                    ShowTruncateOption = false;
                    ShowCreateTableInfo = true;
                    CreateNewTable = true;
                    ShouldTruncate = false;
                    
                    TableCheckMessage = $"ℹ Table '{DestinationTableName}' does not exist";
                    StatusMessage = "ℹ️ Step 3: Table will be created automatically from Excel structure";
                    _logger.LogInfo($"Table '{DestinationTableName}' not found - will create new table");
                    _logger.LogInfo("New table will be created with structure from Excel data");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error: {ex.Message}";
                _logger.LogError($"Database check failed: {ex.Message}");
                TableCheckMessage = string.Empty;
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
            }
        }

        [RelayCommand]
        private void Step3_Complete()
        {
            ConfigurationComplete = true;
            StatusMessage = "✅ Configuration complete! Click 'Save Configuration' to finalize.";
            _logger.LogSuccess("All steps completed. Ready to save configuration.");
            _logger.LogInfo("═══════════════════════════════════", "summary");
            _logger.LogInfo("Configuration Summary:", "summary");
            _logger.LogInfo($"• Temp Table: {TempTableName}", "summary");
            _logger.LogInfo($"• Destination Table: {DestinationTableName}", "summary");
            _logger.LogInfo($"• Target Exists: {TargetTableExists}", "summary");
            
            if (TargetTableExists)
            {
                _logger.LogInfo($"• Truncate: {ShouldTruncate}", "summary");
            }
            else
            {
                _logger.LogInfo($"• Create New: {CreateNewTable}", "summary");
            }
            _logger.LogInfo("═══════════════════════════════════", "summary");
        }

        [RelayCommand]
        private async Task SaveConfigurationAsync()
        {
            try
            {
                // Final validation
                if (string.IsNullOrWhiteSpace(TempTableName))
                {
                    StatusMessage = "❌ Temporary table name is required";
                    _logger.LogError("Cannot save: Temporary table name is missing");
                    return;
                }

                if (string.IsNullOrWhiteSpace(DestinationTableName))
                {
                    StatusMessage = "❌ Destination table name is required";
                    _logger.LogError("Cannot save: Destination table name is missing");
                    return;
                }

                IsBusy = true;
                BusyMessage = "Saving configuration...";
                _logger.LogInfo("💾 Saving configuration...", "config");

                var config = new
                {
                    TempTableName,
                    DestinationTableName,
                    ErrorTableName = "",
                    SuccessLogTableName = "",
                    TargetTableExists,
                    ShouldTruncateTable = ShouldTruncate,
                    CreateNewTable,
                    ConfigurationTimestamp = DateTime.Now
                };

                await Task.Run(() =>
                {
                    // Create backup if file exists
                    if (File.Exists(_configFilePath))
                    {
                        string backupPath = Path.Combine(_configManager.GetRootDirectory(), "dynamic_table_config_backup.json");
                        File.Copy(_configFilePath, backupPath, true);
                        _logger.LogInfo($"Backup created: {Path.GetFileName(backupPath)}", "config");
                    }

                    string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                    File.WriteAllText(_configFilePath, json);
                });

                StatusMessage = "✅ Configuration saved successfully!";
                _logger.LogSuccess($"✓ Configuration saved to: {Path.GetFileName(_configFilePath)}");
                _logger.LogSuccess("The ETL process will now use these dynamically configured table names.");
                ConfigurationComplete = true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Save failed: {ex.Message}";
                _logger.LogError($"Failed to save configuration: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
            }
        }

        [RelayCommand]
        private void ResetWizard()
        {
            TempTableName = string.Empty;
            DestinationTableName = string.Empty;
            TargetTableExists = false;
            ShouldTruncate = false;
            CreateNewTable = false;
            CurrentStep = 1;
            IsStep2Enabled = false;
            IsStep3Enabled = false;
            ConfigurationComplete = false;
            StatusMessage = "➡️ Step 1: Enter temporary table name and click Next";
            TableCheckMessage = string.Empty;
            ShowTruncateOption = false;
            ShowCreateTableInfo = false;
            LogEntries.Clear();
            _logger.LogInfo("🔄 Wizard reset. Starting fresh configuration...", "wizard");
        }

        [RelayCommand]
        private void LoadExistingConfiguration()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    _logger.LogInfo("No existing configuration found. Starting fresh wizard.", "config");
                    return;
                }

                string json = File.ReadAllText(_configFilePath);
                dynamic? config = JsonConvert.DeserializeObject(json);

                if (config != null)
                {
                    TempTableName = config.TempTableName ?? string.Empty;
                    DestinationTableName = config.DestinationTableName ?? string.Empty;
                    TargetTableExists = config.TargetTableExists ?? false;
                    ShouldTruncate = config.ShouldTruncateTable ?? false;
                    CreateNewTable = config.CreateNewTable ?? false;

                    _logger.LogSuccess($"📄 Existing configuration loaded");
                    _logger.LogInfo($"• Temp Table: {TempTableName}", "config");
                    _logger.LogInfo($"• Destination Table: {DestinationTableName}", "config");
                    StatusMessage = "✅ Existing configuration loaded. You can modify and save again.";
                    
                    // Enable all steps since config exists
                    IsStep2Enabled = true;
                    IsStep3Enabled = true;
                    
                    if (TargetTableExists)
                    {
                        ShowTruncateOption = true;
                        ShowCreateTableInfo = false;
                        TableCheckMessage = $"✓ Table '{DestinationTableName}' exists";
                    }
                    else
                    {
                        ShowTruncateOption = false;
                        ShowCreateTableInfo = true;
                        TableCheckMessage = $"ℹ Table '{DestinationTableName}' will be created";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading configuration: {ex.Message}");
            }
        }

        private bool IsValidTableName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return false;

            // Must start with a letter or underscore
            if (!char.IsLetter(tableName[0]) && tableName[0] != '_')
                return false;

            // Can only contain letters, digits, and underscores
            return tableName.All(c => char.IsLetterOrDigit(c) || c == '_');
        }

        [RelayCommand]
        private void ClearLogs()
        {
            LogEntries.Clear();
        }
    }
}
