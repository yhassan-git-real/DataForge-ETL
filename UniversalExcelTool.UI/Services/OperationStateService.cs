using System;
using System.Threading;
using DataForgeETL.UI.Models;

namespace DataForgeETL.UI.Services
{
    /// <summary>
    /// Singleton service for managing global operation state across the application
    /// </summary>
    public class OperationStateService : IOperationStateService
    {
        private static readonly Lazy<OperationStateService> _instance = 
            new Lazy<OperationStateService>(() => new OperationStateService(), LazyThreadSafetyMode.ExecutionAndPublication);

        private readonly object _lockObject = new object();
        private OperationInfo? _currentOperation;

        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        public static OperationStateService Instance => _instance.Value;

        private OperationStateService()
        {
            // Private constructor for singleton
        }

        /// <inheritdoc/>
        public OperationInfo? CurrentOperation
        {
            get
            {
                lock (_lockObject)
                {
                    return _currentOperation;
                }
            }
        }

        /// <inheritdoc/>
        public event EventHandler<OperationStateChangedEventArgs>? OperationStateChanged;

        /// <inheritdoc/>
        public bool TryStartOperation(OperationType type, string description)
        {
            lock (_lockObject)
            {
                // Check if an operation is already running
                if (_currentOperation != null && _currentOperation.State == OperationState.Running)
                {
                    return false;
                }

                // Determine source view based on operation type
                string sourceView = type switch
                {
                    OperationType.CompleteETL => "Dashboard",
                    OperationType.ExcelProcessor => "Excel Processor",
                    OperationType.DatabaseLoader => "Database Loader",
                    OperationType.DynamicTableConfig => "Table Configuration",
                    _ => "Unknown"
                };

                // Create new operation
                _currentOperation = new OperationInfo(type, description, sourceView);

                // Notify subscribers
                RaiseOperationStateChanged(OperationState.Running);

                return true;
            }
        }

        /// <inheritdoc/>
        public void CompleteOperation(bool success, string? message = null)
        {
            lock (_lockObject)
            {
                if (_currentOperation == null)
                    return;

                _currentOperation.State = success ? OperationState.Completed : OperationState.Failed;
                _currentOperation.EndTime = DateTime.Now;
                _currentOperation.ResultMessage = message;

                // Notify subscribers
                RaiseOperationStateChanged(_currentOperation.State);

                // Clear operation after a short delay to allow UI to update
                // (In a real implementation, you might keep a history)
            }
        }

        /// <inheritdoc/>
        public void CancelOperation()
        {
            lock (_lockObject)
            {
                if (_currentOperation == null || _currentOperation.State != OperationState.Running)
                    return;

                _currentOperation.State = OperationState.Cancelled;
                _currentOperation.EndTime = DateTime.Now;
                _currentOperation.ResultMessage = "Operation cancelled by user";

                // Notify subscribers
                RaiseOperationStateChanged(OperationState.Cancelled);
            }
        }

        /// <inheritdoc/>
        public bool IsOperationRunning()
        {
            lock (_lockObject)
            {
                return _currentOperation != null && _currentOperation.State == OperationState.Running;
            }
        }

        /// <inheritdoc/>
        public string? GetOperationStatusMessage()
        {
            lock (_lockObject)
            {
                if (_currentOperation == null)
                    return null;

                if (_currentOperation.State != OperationState.Running)
                    return null;

                return $"An operation is currently running: {_currentOperation.Description} " +
                       $"(ID: {_currentOperation.OperationId:N}) started from {_currentOperation.SourceView}. " +
                       $"Please wait for completion or cancel the existing operation before starting a new one.";
            }
        }

        /// <summary>
        /// Clears the current operation (called after completion/cancellation)
        /// </summary>
        public void ClearOperation()
        {
            lock (_lockObject)
            {
                if (_currentOperation != null && _currentOperation.State != OperationState.Running)
                {
                    _currentOperation = null;
                    RaiseOperationStateChanged(OperationState.Completed);
                }
            }
        }

        private void RaiseOperationStateChanged(OperationState newState)
        {
            OperationStateChanged?.Invoke(this, new OperationStateChangedEventArgs
            {
                Operation = _currentOperation,
                NewState = newState
            });
        }
    }
}
