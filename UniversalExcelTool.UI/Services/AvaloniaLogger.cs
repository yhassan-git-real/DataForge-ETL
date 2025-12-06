using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Avalonia.Threading;
using UniversalExcelTool.UI.Models;

namespace UniversalExcelTool.UI.Services
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
                WriteToFile($"═══════════════════════════════════════════════════════════");
                WriteToFile($"  Universal Excel Tool - UI Session Log");
                WriteToFile($"  Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                WriteToFile($"  Log File: {logFilePath}");
                WriteToFile($"═══════════════════════════════════════════════════════════\n");
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
            // Filter out technical/verbose messages from UI display
            if (ShouldShowInUI(message))
            {
                var entry = new LogEntry(message, LogLevel.Info, category);
                AddLogEntry(entry);
            }
            // Always write to file
            WriteToFile($"ℹ️  [INFO] {message}");
        }

        /// <summary>
        /// Determines if a message should be shown in the UI live logs
        /// </summary>
        private bool ShouldShowInUI(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;

            // Filter out decorative lines and technical details
            var lowerMessage = message.ToLower();
            
            // Exclude: Box drawing characters, separators, and decorative lines
            if (message.Contains("═") || message.Contains("║") || message.Contains("╔") || 
                message.Contains("╚") || message.Contains("╗") || message.Contains("╝") ||
                message.Contains("────") || message.Contains("••••") || 
                message.Contains("▬") || message.Contains("▼") || message.Contains("▲"))
                return false;

            // Exclude: Universal Excel Tool headers and branding
            if (lowerMessage.Contains("universal excel tool") || lowerMessage.Contains("modern etl manager") ||
                lowerMessage.Contains("centralized etl process") || lowerMessage.Contains("location-agnostic") ||
                lowerMessage.Contains("self-contained") || lowerMessage.Contains("environment-friendly"))
                return false;

            // Exclude: Technical configuration details (All modules)
            if (lowerMessage.Contains("executable:") || lowerMessage.Contains("arguments:") ||
                lowerMessage.Contains("root directory:") || lowerMessage.Contains("log file path:") ||
                lowerMessage.Contains("configuration loaded") || lowerMessage.Contains("temp table:") ||
                lowerMessage.Contains("destination table:") || lowerMessage.Contains("error table:") ||
                lowerMessage.Contains("success table:") || lowerMessage.Contains("column mapping:") ||
                lowerMessage.Contains("sql server version:") || lowerMessage.Contains("microsoft sql") ||
                lowerMessage.Contains("working directory:") || lowerMessage.Contains("batch size:") ||
                lowerMessage.Contains("validate column mapping:") || lowerMessage.Contains("authentication:"))
                return false;

            // Exclude: CSV to Database specific technical messages
            if (lowerMessage.Contains("launching dynamic table") || lowerMessage.Contains("please configure") ||
                lowerMessage.Contains("target table exists:") || lowerMessage.Contains("should truncate:") ||
                lowerMessage.Contains("create new table:") || lowerMessage.Contains("last updated:") ||
                lowerMessage.Contains("using dynamic") || lowerMessage.Contains("dynamic table configuration applied") ||
                lowerMessage.Contains("validating columns") || lowerMessage.Contains("validation completed") ||
                lowerMessage.Contains("validation passed") || lowerMessage.Contains("columns matched") ||
                lowerMessage.Contains("isvalid:") || lowerMessage.Contains("rows affected:") ||
                lowerMessage.Contains("source (temp) table rows:") || 
                lowerMessage.Contains("destination table rows (before):") ||
                lowerMessage.Contains("destination table rows (after):") ||
                lowerMessage.Contains("columns to transfer:") ||
                lowerMessage.Contains("executing insert") || lowerMessage.Contains("insert completed") ||
                lowerMessage.Contains("creating temporary table:") || 
                lowerMessage.Contains("creating/verifying log tables") ||
                lowerMessage.Contains("log tables verified") ||
                lowerMessage.Contains("using log directory:"))
                return false;

            // Exclude: Excel Processor specific technical messages
            if (lowerMessage.Contains("excel processor v") || lowerMessage.Contains("processing excel files") ||
                lowerMessage.Contains("splitting excel") || lowerMessage.Contains("sheet extraction") ||
                lowerMessage.Contains("excel file path:") || lowerMessage.Contains("output folder path:") ||
                lowerMessage.Contains("processing mode:") || lowerMessage.Contains("input folder:") ||
                lowerMessage.Contains("checking for excel files") || lowerMessage.Contains("sheet name:") ||
                lowerMessage.Contains("extracting sheet") || lowerMessage.Contains("saving extracted sheet") ||
                lowerMessage.Contains("creating output directory") || lowerMessage.Contains("output directory:") ||
                lowerMessage.Contains("row count:") || lowerMessage.Contains("column count:") ||
                lowerMessage.Contains("workbook opened") || lowerMessage.Contains("worksheet count:"))
                return false;

            // Exclude: Database Loader specific technical messages
            if (lowerMessage.Contains("database loader v") || lowerMessage.Contains("loading csv to database") ||
                lowerMessage.Contains("bulk insert") || lowerMessage.Contains("database loading") ||
                lowerMessage.Contains("connection string:") || lowerMessage.Contains("timeout:") ||
                lowerMessage.Contains("enable retry:") || lowerMessage.Contains("max retry:") ||
                lowerMessage.Contains("csv delimiter:") || lowerMessage.Contains("has header row:") ||
                lowerMessage.Contains("verifying table schema") || lowerMessage.Contains("schema validated") ||
                lowerMessage.Contains("preparing bulk copy") || lowerMessage.Contains("bulk copy completed") ||
                lowerMessage.Contains("transaction committed") || lowerMessage.Contains("rows copied:"))
                return false;

            // Exclude: Headers and section markers (All modules)
            if (lowerMessage.Contains("step 1:") || lowerMessage.Contains("step 2:") || lowerMessage.Contains("step 3:") ||
                lowerMessage.Contains("user input required") || lowerMessage.Contains("etl configuration summary") ||
                lowerMessage.Contains("table creation summary") || lowerMessage.Contains("import summary report") ||
                lowerMessage.Contains("processing summary") || lowerMessage.Contains("execution summary") ||
                lowerMessage.Contains("file processing summary") || lowerMessage.Contains("loading summary"))
                return false;

            // Exclude: Version headers and tool descriptions (All modules)
            if (lowerMessage.Contains("etl csv to database v") || lowerMessage.Contains("this tool imports") ||
                lowerMessage.Contains("automatic schema detection") || lowerMessage.Contains("this module processes") ||
                lowerMessage.Contains("this module loads") || lowerMessage.Contains("this module extracts"))
                return false;

            // Exclude: Verbose processing details with technical terms
            if ((lowerMessage.Contains("columns:") && message.Contains("(")) ||
                lowerMessage.Contains("processing started at:") || lowerMessage.Contains("processing ended at:") ||
                lowerMessage.Contains("elapsed time:") && lowerMessage.Contains("seconds") ||
                lowerMessage.Contains("memory usage:") || lowerMessage.Contains("cpu usage:") ||
                lowerMessage.Contains("thread id:") || lowerMessage.Contains("process id:"))
                return false;

            // Exclude: File path details that are too technical
            if (lowerMessage.Contains("f:\\") || lowerMessage.Contains("c:\\") || 
                lowerMessage.Contains("e:\\") || lowerMessage.Contains("d:\\") ||
                (lowerMessage.Contains("path:") && lowerMessage.Contains("\\")))
                return false;

            // Exclude: Progress notifications that are too granular
            if (lowerMessage.Contains("rows/sec") || lowerMessage.Contains("rows per second") ||
                lowerMessage.Contains("kb/s") || lowerMessage.Contains("mb/s") ||
                lowerMessage.Contains("progress:") && lowerMessage.Contains("%"))
                return false;

            return true;
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
            // Filter warnings but show cancellation messages
            if (ShouldShowInUI(message) || message.ToLower().Contains("cancel"))
            {
                var entry = new LogEntry(message, LogLevel.Warning, "warning");
                AddLogEntry(entry);
            }
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
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
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

            WriteToFile($"\n═══════════════════════════════════════════════════════════");
            WriteToFile($"  Session Ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            WriteToFile($"═══════════════════════════════════════════════════════════");
        }
    }
}
