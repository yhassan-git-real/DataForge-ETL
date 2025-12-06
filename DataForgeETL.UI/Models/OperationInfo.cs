using System;

namespace DataForgeETL.UI.Models
{
    /// <summary>
    /// Represents information about a running operation
    /// </summary>
    public class OperationInfo
    {
        /// <summary>
        /// Unique identifier for the operation
        /// </summary>
        public Guid OperationId { get; set; }

        /// <summary>
        /// Type of operation being executed
        /// </summary>
        public OperationType Type { get; set; }

        /// <summary>
        /// Description of the operation
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Current state of the operation
        /// </summary>
        public OperationState State { get; set; }

        /// <summary>
        /// When the operation started
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// When the operation ended (if completed)
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Result message (success or error)
        /// </summary>
        public string? ResultMessage { get; set; }

        /// <summary>
        /// Tab/View that initiated the operation
        /// </summary>
        public string SourceView { get; set; }

        public OperationInfo(OperationType type, string description, string sourceView)
        {
            OperationId = Guid.NewGuid();
            Type = type;
            Description = description;
            SourceView = sourceView;
            State = OperationState.Running;
            StartTime = DateTime.Now;
        }

        /// <summary>
        /// Gets a formatted status message for display
        /// </summary>
        public string GetStatusMessage()
        {
            string icon = Type switch
            {
                OperationType.CompleteETL => "🔄",
                OperationType.ExcelProcessor => "📊",
                OperationType.DatabaseLoader => "📤",
                OperationType.CsvToDatabase => "📁",
                _ => "⚙️"
            };

            string stateText = State switch
            {
                OperationState.Running => "Running",
                OperationState.Completed => "Completed",
                OperationState.Failed => "Failed",
                OperationState.Cancelled => "Cancelled",
                _ => "Unknown"
            };

            var duration = EndTime.HasValue 
                ? (EndTime.Value - StartTime).ToString(@"hh\:mm\:ss")
                : (DateTime.Now - StartTime).ToString(@"hh\:mm\:ss");

            return $"{icon} {Description} - {stateText} (Duration: {duration})";
        }
    }

    /// <summary>
    /// Types of operations that can be executed
    /// </summary>
    public enum OperationType
    {
        CompleteETL,
        ExcelProcessor,
        DatabaseLoader,
        DynamicTableConfig,
        CsvToDatabase
    }

    /// <summary>
    /// States an operation can be in
    /// </summary>
    public enum OperationState
    {
        Running,
        Completed,
        Failed,
        Cancelled
    }
}
