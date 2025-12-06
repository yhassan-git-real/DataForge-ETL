using System;

namespace DataForgeETL.UI.Models
{
    /// <summary>
    /// Represents the execution status of an ETL process
    /// </summary>
    public class ExecutionStatus
    {
        public Guid ExecutionId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => EndTime.HasValue 
            ? EndTime.Value - StartTime 
            : DateTime.Now - StartTime;
        
        public string ModuleName { get; set; }
        public ExecutionState State { get; set; }
        public long RowsProcessed { get; set; }
        public int FilesProcessed { get; set; }
        public int FilesSucceeded { get; set; }
        public int FilesFailed { get; set; }
        public string? ErrorMessage { get; set; }

        public ExecutionStatus(string moduleName)
        {
            ExecutionId = Guid.NewGuid();
            ModuleName = moduleName;
            StartTime = DateTime.Now;
            State = ExecutionState.Running;
        }

        /// <summary>
        /// Gets the status icon
        /// </summary>
        public string StatusIcon => State switch
        {
            ExecutionState.Running => "🔄",
            ExecutionState.Completed => "✅",
            ExecutionState.Failed => "❌",
            ExecutionState.Cancelled => "⏹️",
            ExecutionState.Paused => "⏸️",
            _ => "•"
        };

        /// <summary>
        /// Gets a summary of the execution
        /// </summary>
        public string Summary => State switch
        {
            ExecutionState.Running => $"Processing... {RowsProcessed:N0} rows",
            ExecutionState.Completed => $"Completed: {RowsProcessed:N0} rows in {Duration:mm\\:ss}",
            ExecutionState.Failed => $"Failed: {ErrorMessage}",
            ExecutionState.Cancelled => "Cancelled by user",
            ExecutionState.Paused => "Paused",
            _ => "Unknown"
        };

        /// <summary>
        /// Marks the execution as completed
        /// </summary>
        public void MarkCompleted()
        {
            EndTime = DateTime.Now;
            State = ExecutionState.Completed;
        }

        /// <summary>
        /// Marks the execution as failed
        /// </summary>
        public void MarkFailed(string errorMessage)
        {
            EndTime = DateTime.Now;
            State = ExecutionState.Failed;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Execution state enumeration
    /// </summary>
    public enum ExecutionState
    {
        Running,
        Completed,
        Failed,
        Cancelled,
        Paused
    }
}
