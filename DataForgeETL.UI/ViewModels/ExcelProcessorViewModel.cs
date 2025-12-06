using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataForgeETL.UI.Models;
using DataForgeETL.UI.Services;
using DataForgeETL.Core;

namespace DataForgeETL.UI.ViewModels
{
    /// <summary>
    /// View model for Excel file processing
    /// </summary>
    public partial class ExcelProcessorViewModel : ViewModelBase
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

        public ExcelProcessorViewModel()
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
        private async Task RunExcelProcessorAsync()
        {
            if (IsBusy)
            {
                _logger.LogWarning("Excel Processor is already running");
                return;
            }

            // Check if another operation is already running
            if (!_operationStateService.TryStartOperation(OperationType.ExcelProcessor, "Excel File Processing"))
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
                BusyMessage = "Processing Excel files...";
                _progressReporter.Reset();

                // Create file logger for this session
                var logFileName = $"UI_ExcelProcessor_{DateTime.Now:yyyyMMdd}.txt";
                var logPath = Path.Combine(_configManager.GetLogFilesPath(), logFileName);
                _currentLogFilePath = logPath;
                _processLogger = new AvaloniaLogger(LogEntries, logPath);

                var stopwatch = Stopwatch.StartNew();
                _processLogger.LogInfo("=================================================================", "processor");
                _processLogger.LogInfo($"Starting Excel Processor (ID: {_operationStateService.CurrentOperation?.OperationId:N})", "processor");
                _processLogger.LogInfo("=================================================================", "processor");

                var orchestrator = new ETLOrchestratorWithLogger(_processLogger, _progressReporter);
                var options = new ETLProcessOptions
                {
                    SkipDynamicTableConfig = true
                };

                bool success = await Task.Run(() => orchestrator.RunExcelProcessorAsync(options, _cancellationTokenSource.Token), _cancellationTokenSource.Token);

                stopwatch.Stop();

                // Check if operation was cancelled
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    _processLogger.LogWarning($"Excel processing cancelled after {stopwatch.Elapsed:hh\\:mm\\:ss}");
                    _progressReporter.ReportComplete(false, stopwatch.Elapsed, "Operation cancelled by user");
                    _operationStateService.CancelOperation();
                    
                    Views.MainWindow.NotificationService?.ShowWarning(
                        "Processing Cancelled", 
                        $"Excel processing was cancelled after {stopwatch.Elapsed:mm\\:ss}");
                    return;
                }

                if (success)
                {
                    _processLogger.LogSuccess($"Excel processing completed in {stopwatch.Elapsed:hh\\:mm\\:ss}");
                    _processLogger.LogInfo($"Log file saved: {logPath}", "system");
                    _progressReporter.ReportComplete(true, stopwatch.Elapsed, "Excel files processed successfully");
                    _operationStateService.CompleteOperation(true, "Excel files processed successfully");
                    
                    Views.MainWindow.NotificationService?.ShowSuccess(
                        "Processing Complete", 
                        $"Excel processing completed in {stopwatch.Elapsed:mm\\:ss}");
                }
                else
                {
                    _processLogger.LogError($"Excel processing failed after {stopwatch.Elapsed:hh\\:mm\\:ss}");
                    _processLogger.LogInfo($"Log file saved: {logPath}", "system");
                    _progressReporter.ReportComplete(false, stopwatch.Elapsed, "Processing completed with errors");
                    _operationStateService.CompleteOperation(false, "Processing completed with errors");
                    
                    Views.MainWindow.NotificationService?.ShowError(
                        "Processing Failed", 
                        "Excel processing completed with errors. Check logs for details.");
                }
            }
            catch (OperationCanceledException)
            {
                _processLogger?.LogWarning("Excel processing was cancelled");
                _progressReporter.ReportError("Operation cancelled");
                _operationStateService.CancelOperation();
                
                Views.MainWindow.NotificationService?.ShowWarning(
                    "Processing Cancelled", 
                    "Excel processing was cancelled by user");
            }
            catch (Exception ex)
            {
                _processLogger?.LogError($"Excel processing error: {ex.Message}");
                _progressReporter.ReportError(ex.Message);
                _operationStateService.CompleteOperation(false, $"Error: {ex.Message}");
                
                Views.MainWindow.NotificationService?.ShowError(
                    "Processing Error", 
                    ex.Message);
            }
            finally
            {
                _processLogger?.CloseLog();
                IsBusy = false;
                BusyMessage = string.Empty;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        [RelayCommand]
        private void CancelOperation()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _logger.LogWarning("Cancelling Excel Processor operation...");
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
                if (!string.IsNullOrEmpty(_currentLogFilePath) && File.Exists(_currentLogFilePath))
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = _currentLogFilePath,
                        UseShellExecute = true
                    };
                    Process.Start(startInfo);
                }
                else
                {
                    var logFolderPath = _configManager.GetLogFilesPath();
                    if (!Directory.Exists(logFolderPath))
                    {
                        Directory.CreateDirectory(logFolderPath);
                    }
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
            // Trigger property change notifications
            OnPropertyChanged(nameof(ProgressInfo));
        }
    }
}
