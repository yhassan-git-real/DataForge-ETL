using System;
using DataForgeETL.UI.Models;

namespace DataForgeETL.UI.Services
{
    /// <summary>
    /// Interface for managing global operation state across the application
    /// </summary>
    public interface IOperationStateService
    {
        /// <summary>
        /// Gets the current operation information, or null if no operation is running
        /// </summary>
        OperationInfo? CurrentOperation { get; }

        /// <summary>
        /// Event raised when the operation state changes
        /// </summary>
        event EventHandler<OperationStateChangedEventArgs>? OperationStateChanged;

        /// <summary>
        /// Attempts to start a new operation
        /// </summary>
        /// <returns>True if operation started successfully, false if another operation is already running</returns>
        bool TryStartOperation(OperationType type, string description);

        /// <summary>
        /// Completes the current operation
        /// </summary>
        void CompleteOperation(bool success, string? message = null);

        /// <summary>
        /// Cancels the current operation
        /// </summary>
        void CancelOperation();

        /// <summary>
        /// Checks if any operation is currently running
        /// </summary>
        bool IsOperationRunning();

        /// <summary>
        /// Gets a human-readable message about the current operation
        /// </summary>
        string? GetOperationStatusMessage();
    }

    /// <summary>
    /// Event args for operation state changes
    /// </summary>
    public class OperationStateChangedEventArgs : EventArgs
    {
        public OperationInfo? Operation { get; set; }
        public OperationState NewState { get; set; }
    }
}
