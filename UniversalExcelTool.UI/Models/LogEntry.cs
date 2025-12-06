using System;

namespace DataForgeETL.UI.Models
{
    /// <summary>
    /// Represents a single log entry in the application
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }
        public string? Details { get; set; }

        public LogEntry(string message, LogLevel level = LogLevel.Info, string category = "general")
        {
            Timestamp = DateTime.Now;
            Message = message;
            Level = level;
            Category = category;
        }

        public LogEntry(string message, LogLevel level, string category, string? details)
            : this(message, level, category)
        {
            Details = details;
        }

        /// <summary>
        /// Gets a formatted string representation of the log entry
        /// </summary>
        public string FormattedMessage => $"[{Timestamp:HH:mm:ss}] [{Level}] {Message}";

        /// <summary>
        /// Gets the emoji icon for the log level
        /// </summary>
        public string Icon => Level switch
        {
            LogLevel.Debug => "🔍",
            LogLevel.Info => "ℹ️",
            LogLevel.Success => "✅",
            LogLevel.Warning => "⚠️",
            LogLevel.Error => "❌",
            LogLevel.Progress => "🔄",
            _ => "•"
        };
    }

    /// <summary>
    /// Log level enumeration
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Success,
        Warning,
        Error,
        Progress
    }
}
