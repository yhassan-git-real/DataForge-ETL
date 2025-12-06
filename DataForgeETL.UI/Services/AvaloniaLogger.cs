using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Threading;
using DataForgeETL.UI.Models;

namespace DataForgeETL.UI.Services
{
    /// <summary>
    /// Avalonia UI implementation of IUILogger with optional file logging
    /// </summary>
    public class AvaloniaLogger : IUILogger
    {
        private readonly ObservableCollection<LogEntry> _logEntries;
        private readonly int _maxLogEntries;
        private readonly string? _logFilePath;
        private readonly StringBuilder _logBuffer;
        private readonly object _fileLock = new object();

        /// <summary>
        /// Creates logger with live UI logs only (no file logging)
        /// </summary>
        public AvaloniaLogger(ObservableCollection<LogEntry> logEntries, int maxLogEntries = 1000)
        {
            _logEntries = logEntries;
            _maxLogEntries = maxLogEntries;
            _logBuffer = new StringBuilder();
        }

        /// <summary>
        /// Creates logger with both live UI logs and file logging
        /// </summary>
        /// <param name="logEntries">ObservableCollection for live UI display</param>
        /// <param name="logFilePath">Full path to log file (e.g., "Logs/UI_Dashboard_20241025_143022.txt")</param>
        /// <param name="maxLogEntries">Maximum in-memory log entries</param>
        public AvaloniaLogger(ObservableCollection<LogEntry> logEntries, string logFilePath, int maxLogEntries = 1000)
        {
            _logEntries = logEntries;
            _maxLogEntries = maxLogEntries;
            _logFilePath = logFilePath;
            _logBuffer = new StringBuilder();

            try
            {
                // Ensure log directory exists
                var directory = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                    
                    // Add diagnostic info to UI logs
                    var diagnosticEntry = new LogEntry($"📁 Log file path: {logFilePath}", LogLevel.Info, "logging");
                    AddLogEntry(diagnosticEntry);
                }

                // Write log header
                WriteToFile($"=================================================================");
                WriteToFile($"  DataForge ETL - UI Session Log");
                WriteToFile($"  Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                WriteToFile($"  Log File: {logFilePath}");
                WriteToFile($"=================================================================════════════════\n");
            }
            catch (Exception ex)
            {
                // Show error in UI logs
                var errorEntry = new LogEntry($"⚠️ Failed to initialize log file: {ex.Message}", LogLevel.Warning, "logging");
                AddLogEntry(errorEntry);
            }
        }

        public void LogInfo(string message, string category = "info")
        {
            // Show only meaningful messages in UI
            if (ShouldShowInUI(message, category))
            {
                var entry = new LogEntry(message, LogLevel.Info, category);
                AddLogEntry(entry);
            }
            
            // Always write to file
            WriteToFile($"ℹ️  [INFO] {message}");
        }

        /// <summary>
        /// Smart filter to show only critical high-level status in UI live logs
        /// </summary>
        private bool ShouldShowInUI(string message, string category)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;

            var trimmed = message.Trim();
            var lowerMessage = trimmed.ToLower();

            // EXCLUDE: Pure separator/decorative lines
            if (trimmed.Length > 10 && trimmed.All(c => c == '=' || c == '-' || c == '_' || c == '•' || char.IsWhiteSpace(c)))
                return false;

            // EXCLUDE: Box drawing characters and decorative patterns
            if (message.Contains("═") || message.Contains("╔") || message.Contains("╗") || 
                message.Contains("╚") || message.Contains("╝") || message.Contains("║") ||
                message.Contains("────────") || message.Contains("••••••"))
                return false;

            // EXCLUDE: Marketing/branding lines
            if (lowerMessage.Contains("dataforge etl") || lowerMessage.Contains("modern etl manager") ||
                lowerMessage.Contains("centralized etl process") ||
                lowerMessage.Contains("location-agnostic") || lowerMessage.Contains("self-contained") ||
                lowerMessage.Contains("environment-friendly"))
                return false;

            // EXCLUDE: Technical execution details
            if (lowerMessage.StartsWith("executable:") || lowerMessage.StartsWith("arguments:") ||
                lowerMessage.Contains("root directory:") || lowerMessage.Contains("working directory:"))
                return false;

            // EXCLUDE: Verbose module output details
            if (message.Contains("[Excel Processor]") || message.Contains("[Database Loader]") || 
                message.Contains("[CSV to Database]") || message.Contains("[Dynamic Table Manager]"))
            {
                // Only show module output with these keywords
                if (!(lowerMessage.Contains("error") || lowerMessage.Contains("failed") ||
                      lowerMessage.Contains("found") && lowerMessage.Contains("file") ||
                      lowerMessage.Contains("completed") || lowerMessage.Contains("finished") ||
                      lowerMessage.Contains("total") || lowerMessage.Contains("summary")))
                    return false;
            }

            // EXCLUDE: Time-based messages without substance
            if (lowerMessage.Contains("🕒") && (lowerMessage.Contains("started") || lowerMessage.Contains("application started")))
                return false;

            // EXCLUDE: Configuration loaded messages
            if (lowerMessage.Contains("configuration loaded") || lowerMessage.Contains("config loaded"))
                return false;

            // EXCLUDE: Path-related verbose messages
            if ((lowerMessage.Contains("input path:") || lowerMessage.Contains("output path:") || 
                 lowerMessage.Contains("input:") || lowerMessage.Contains("output:")) && 
                 message.Contains("\\"))
                return false;

            // EXCLUDE: Screen/system messages
            if (lowerMessage.Contains("screen sleep") || lowerMessage.Contains("log directory exists"))
                return false;

            // EXCLUDE: Scanning/checking messages (too verbose)
            if (lowerMessage.Contains("scanning") || lowerMessage.Contains("checking for"))
                return false;

            // === NOW INCLUDE ONLY CRITICAL MESSAGES ===

            // INCLUDE: Step headers
            if (lowerMessage.Contains("step 1:") || lowerMessage.Contains("step 2:") || lowerMessage.Contains("step 3:"))
                return true;

            // INCLUDE: Starting complete ETL process
            if (lowerMessage.Contains("starting complete etl process"))
                return true;

            // INCLUDE: Module start/completion status
            if (lowerMessage.Contains("starting") && (lowerMessage.Contains("dynamic table manager") || 
                lowerMessage.Contains("excel processor") || lowerMessage.Contains("database loader") ||
                lowerMessage.Contains("csv to database")))
                return true;

            if (lowerMessage.Contains("completed successfully") || lowerMessage.Contains("finished successfully"))
                return true;

            // INCLUDE: Critical file counts (not paths)
            if ((lowerMessage.Contains("found") && lowerMessage.Contains("file") && !message.Contains("\\")) ||
                (lowerMessage.Contains("processed") && lowerMessage.Contains("file")))
                return true;

            // INCLUDE: Database connection status
            if (lowerMessage.Contains("database") && (lowerMessage.Contains("connect") || lowerMessage.Contains("connection")))
                return true;

            // INCLUDE: Critical database operations with counts
            if ((lowerMessage.Contains("table") && (lowerMessage.Contains("created") || lowerMessage.Contains("loaded"))) ||
                (lowerMessage.Contains("rows") && (lowerMessage.Contains("inserted") || lowerMessage.Contains("loaded") || lowerMessage.Contains("processed"))))
                return true;

            // INCLUDE: Log file path (important for users)
            if (lowerMessage.Contains("log file path:"))
                return true;

            // INCLUDE: Summary and totals
            if (lowerMessage.Contains("summary") || 
                (lowerMessage.Contains("total") && (lowerMessage.Contains("files") || lowerMessage.Contains("records") || lowerMessage.Contains("rows"))))
                return true;

            // INCLUDE: User interaction required
            if (lowerMessage.Contains("requires user interaction") || lowerMessage.Contains("opening in separate window"))
                return true;

            // DEFAULT: Exclude everything else to keep live log clean
            return false;
        }

        public void LogSuccess(string message)
        {
            // Always show success messages in UI (they're important)
            var entry = new LogEntry(message, LogLevel.Success, "success");
            AddLogEntry(entry);
            WriteToFile($"✅ [SUCCESS] {message}");
        }

        public void LogError(string message)
        {
            var entry = new LogEntry(message, LogLevel.Error, "error");
            AddLogEntry(entry);
            WriteToFile($"❌ [ERROR] {message}");
        }

        public void LogWarning(string message)
        {
            // Always show warnings in UI
            var entry = new LogEntry(message, LogLevel.Warning, "warning");
            AddLogEntry(entry);
            WriteToFile($"⚠️  [WARNING] {message}");
        }

        public void LogProgress(string message, long current, long total)
        {
            var progressMessage = $"{message} - {current:N0}/{total:N0} ({(double)current / total * 100:F1}%)";
            var entry = new LogEntry(progressMessage, LogLevel.Progress, "progress");
            AddLogEntry(entry);
            // Only log progress milestones to file (every 10%)
            var percentage = (double)current / total * 100;
            if (percentage % 10 < 0.1 || current == total)
            {
                WriteToFile($"🔄 [PROGRESS] {progressMessage}");
            }
        }

        public void LogDebug(string message)
        {
            var entry = new LogEntry(message, LogLevel.Debug, "debug");
            AddLogEntry(entry);
            // Debug logs not written to file (too verbose)
        }

        private void AddLogEntry(LogEntry entry)
        {
            // Ensure UI updates happen on UI thread
            Dispatcher.UIThread.Post(() =>
            {
                _logEntries.Add(entry);

                // Limit log entries to prevent memory issues
                while (_logEntries.Count > _maxLogEntries)
                {
                    _logEntries.RemoveAt(0);
                }
            });
        }

        private void WriteToFile(string message)
        {
            if (string.IsNullOrEmpty(_logFilePath))
                return;

            try
            {
                lock (_fileLock)
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logLine = $"[{timestamp}] {message}";
                    _logBuffer.AppendLine(logLine);

                    // Write to file (append mode)
                    File.AppendAllText(_logFilePath, logLine + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                // Log error to debug output AND add to UI log collection
                var errorMsg = $"⚠️ Log file write failed: {ex.Message} (Path: {_logFilePath})";
                System.Diagnostics.Debug.WriteLine(errorMsg);
                
                // Add error to UI logs so user can see it
                try
                {
                    AddLogEntry(new LogEntry(errorMsg, LogLevel.Warning, "logging"));
                }
                catch { /* Prevent infinite recursion */ }
            }
        }

        /// <summary>
        /// Writes a session summary and closes the log file
        /// </summary>
        public void CloseLog()
        {
            if (string.IsNullOrEmpty(_logFilePath))
                return;

            WriteToFile($"\n=================================================================════════════════");
            WriteToFile($"  Session Ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            WriteToFile($"=================================================================════════════════");
        }
    }
}
