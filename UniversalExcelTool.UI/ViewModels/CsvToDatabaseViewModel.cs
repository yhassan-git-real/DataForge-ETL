using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniversalExcelTool.UI.Models;
using UniversalExcelTool.UI.Services;
using UniversalExcelTool.Core;

namespace UniversalExcelTool.UI.ViewModels
{
    /// <summary>
    /// View model for CSV to Database processing
    /// </summary>
    public partial class CsvToDatabaseViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<LogEntry> _logEntries = new();

        [ObservableProperty]
        private ProgressInfo _progressInfo = new();

        [ObservableProperty]
        private bool _canRunOperation = true;

        [ObservableProperty]
        private string _operationStatusMessage = string.Empty;

        [ObservableProperty]
        private bool _isOperationRunning;

        [ObservableProperty]
        private bool _canCancelOperation;

        private readonly IUILogger _logger;
        private readonly IProgressReporter _progressReporter;
        private readonly UnifiedConfigurationManager _configManager;
        private readonly IOperationStateService _operationStateService;
        private AvaloniaLogger? _processLogger;
        private CancellationTokenSource? _cancellationTokenSource;
        private string? _currentLogFilePath;

        public CsvToDatabaseViewModel()
        {
            _logger = new AvaloniaLogger(_logEntries);
            _progressReporter = new AvaloniaProgressReporter(_progressInfo, OnProgressChanged);
            _configManager = UnifiedConfigurationManager.Instance;
            _operationStateService = OperationStateService.Instance;
            
            // Subscribe to operation state changes
            _operationStateService.OperationStateChanged += OnOperationStateChanged;
            
            // Initialize with current operation state
            var currentOp = _operationStateService.CurrentOperation;
            if (currentOp != null && currentOp.State == OperationState.Running)
            {
                IsOperationRunning = true;
                CanRunOperation = false;
                CanCancelOperation = true;
                OperationStatusMessage = currentOp.GetStatusMessage();
            }
        }

        private void OnOperationStateChanged(object? sender, OperationStateChangedEventArgs e)
        {
            // Ensure UI updates happen on the UI thread
            Dispatcher.UIThread.Post(() =>
            {
                // Update UI based on operation state
                IsOperationRunning = e.Operation != null && e.Operation.State == OperationState.Running;
                CanRunOperation = e.Operation == null || e.Operation.State != OperationState.Running;
                CanCancelOperation = e.Operation != null && e.Operation.State == OperationState.Running;
                
                if (e.Operation != null)
                {
                    OperationStatusMessage = e.Operation.GetStatusMessage();
                }
                else
                {
                    OperationStatusMessage = string.Empty;
                }
            });
        }

        [RelayCommand]
        private async Task RunCsvToDatabaseAsync()
        {
            if (IsBusy)
            {
                _logger.LogWarning("CSV to Database is already running");
                return;
            }

            // Check if another operation is already running
            if (!_operationStateService.TryStartOperation(OperationType.CsvToDatabase, "CSV to Database Processing"))
            {
                var statusMsg = _operationStateService.GetOperationStatusMessage();
                _logger.LogWarning($"Cannot start operation: {statusMsg}");
                
                // Show notification to user
                Views.MainWindow.NotificationService?.ShowWarning(
                    "Operation Already Running",
                    statusMsg ?? "Another operation is currently in progress. Please wait or cancel the existing operation.");
                return;
            }

            // Create cancellation token for this operation
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                IsBusy = true;
                BusyMessage = "Processing CSV files to Database...";
                _progressReporter.Reset();

                // Create file logger for this session
                var logFileName = $"UI_CsvToDatabase_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var logPath = Path.Combine(_configManager.GetLogFilesPath(), logFileName);
                _currentLogFilePath = logPath;
                _processLogger = new AvaloniaLogger(LogEntries, logPath);

                var stopwatch = Stopwatch.StartNew();
                _processLogger.LogInfo("═══════════════════════════════════════════", "processor");
                _processLogger.LogInfo($"Starting CSV to Database (ID: {_operationStateService.CurrentOperation?.OperationId:N})", "processor");
                _processLogger.LogInfo("═══════════════════════════════════════════", "processor");

                var orchestrator = new ETLOrchestratorWithLogger(_processLogger, _progressReporter);
                
                // Run CSV to Database processing (includes Dynamic Table Manager internally)
                _processLogger.LogInfo("Processing CSV files to database...", "processor");
                _progressReporter.ReportProgress(10, "Processing CSV files...");

                bool success = await Task.Run(() => orchestrator.RunCsvToDatabaseAsync(null, _cancellationTokenSource.Token), _cancellationTokenSource.Token);

                stopwatch.Stop();

                // Check if operation was cancelled
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    _processLogger.LogWarning($"CSV processing cancelled after {stopwatch.Elapsed:hh\\:mm\\:ss}");
                    _progressReporter.ReportComplete(false, stopwatch.Elapsed, "Operation cancelled by user");
                    _operationStateService.CancelOperation();
                    
                    Views.MainWindow.NotificationService?.ShowWarning(
                        "Processing Cancelled", 
                        $"CSV processing was cancelled after {stopwatch.Elapsed:mm\\:ss}");
                    return;
                }

                if (success)
                {
                    _processLogger.LogSuccess($"CSV processing completed successfully in {stopwatch.Elapsed:hh\\:mm\\:ss}");
                    _processLogger.LogInfo($"Log file saved: {logPath}", "system");
                    _progressReporter.ReportComplete(true, stopwatch.Elapsed, "CSV processing completed successfully");
                    _operationStateService.CompleteOperation(true, "Processing completed successfully");
                    
                    Views.MainWindow.NotificationService?.ShowSuccess(
                        "CSV Processing Complete", 
                        $"Files processed successfully in {stopwatch.Elapsed:mm\\:ss}");
                }
                else
                {
                    _processLogger.LogError($"CSV processing completed with errors in {stopwatch.Elapsed:hh\\:mm\\:ss}");
                    _processLogger.LogInfo($"Log file saved: {logPath}", "system");
                    _progressReporter.ReportComplete(false, stopwatch.Elapsed, "Processing completed with errors");
                    _operationStateService.CompleteOperation(false, "Processing completed with errors");
                    
                    Views.MainWindow.NotificationService?.ShowError(
                        "CSV Processing Failed", 
                        "Processing completed with errors. Check logs for details.");
                }
            }
            catch (OperationCanceledException)
            {
                _processLogger?.LogWarning("CSV processing was cancelled");
                _progressReporter.ReportError("Operation cancelled");
                _operationStateService.CancelOperation();
                
                Views.MainWindow.NotificationService?.ShowWarning(
                    "Processing Cancelled", 
                    "CSV processing was cancelled by user");
            }
            catch (Exception ex)
            {
                _processLogger?.LogError($"CSV processing failed: {ex.Message}");
                _progressReporter.ReportError(ex.Message);
                _operationStateService.CompleteOperation(false, $"Error: {ex.Message}");
                
                Views.MainWindow.NotificationService?.ShowError(
                    "CSV Processing Error", 
                    ex.Message);
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
                _processLogger?.CloseLog();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        [RelayCommand]
        private void CancelOperation()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _logger.LogWarning("Cancelling CSV to Database operation...");
                _cancellationTokenSource.Cancel();
            }
        }

        [RelayCommand]
        private void ClearLogs()
        {
            LogEntries.Clear();
        }

        [RelayCommand]
        private void ViewLog()
        {
            try
            {
                // If a process is running or has been started, open the current log file
                if (!string.IsNullOrEmpty(_currentLogFilePath) && File.Exists(_currentLogFilePath))
                {
                    // Open the log file with the default text editor
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = _currentLogFilePath,
                        UseShellExecute = true
                    };
                    Process.Start(startInfo);
                }
                else
                {
                    // No process started yet, open the logs folder in File Explorer
                    var logFolderPath = _configManager.GetLogFilesPath();
                    
                    // Ensure the folder exists
                    if (!Directory.Exists(logFolderPath))
                    {
                        Directory.CreateDirectory(logFolderPath);
                    }
                    
                    // Open folder in File Explorer
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = logFolderPath,
                        UseShellExecute = true
                    };
                    Process.Start(startInfo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to open log: {ex.Message}");
            }
        }

        private void OnProgressChanged()
        {
            // Progress changed - can be used for additional UI updates if needed
        }
    }
}
