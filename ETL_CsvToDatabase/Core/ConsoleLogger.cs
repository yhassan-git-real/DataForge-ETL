// ConsoleLogger.cs
using System.Collections.Concurrent;
using System.Text;

namespace ETL_CsvToDatabase.Core
{
    public static class ConsoleLogger
    {
        private static readonly StringBuilder _consoleOutput = new StringBuilder();
        private static string? _logFolderPath;
        private static bool _isCapturing = false;

        static ConsoleLogger()
        {
            // Set console encoding for emoji support
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }

        /// <summary>
        /// Initialize console output capture
        /// </summary>
        public static void InitializeCapture(string logFolderPath)
        {
            _logFolderPath = logFolderPath;
            _isCapturing = true;
            _consoleOutput.Clear();
        }

        /// <summary>
        /// Save captured console output to a log file
        /// </summary>
        public static void SaveConsoleOutput()
        {
            if (!_isCapturing || string.IsNullOrEmpty(_logFolderPath))
                return;

            try
            {
                string fileName = $"Console_output_csv2db_{DateTime.Now:yyyyMMdd}.log";
                string filePath = Path.Combine(_logFolderPath, fileName);

                File.WriteAllText(filePath, _consoleOutput.ToString(), new UTF8Encoding(false));
                Console.WriteLine($"✅ Console output saved to: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to save console output: {ex.Message}");
            }
        }

        private static void CaptureOutput(string message)
        {
            if (_isCapturing)
            {
                _consoleOutput.AppendLine(message);
            }
        }
        private static readonly ConcurrentDictionary<string, string> Emojis = new()
        {
            ["version"] = "📋",
            ["start"] = "📅",
            ["config"] = "🔧",
            ["database"] = "📊",
            ["success"] = "✅",
            ["error"] = "❌",
            ["warning"] = "⚠️",
            ["processing"] = "🔄",
            ["file"] = "📁",
            ["check"] = "🔍",
            ["delete"] = "🗑️",
            ["transfer"] = "🚀",
            ["validation"] = "✔️",
            ["temp-table"] = "📝",
            ["summary"] = "📈"
        };

        public static void LogInfo(string type, string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string emoji = Emojis.GetValueOrDefault(type, "ℹ️");
            string output = $"{emoji} [{timestamp}] {message}";
            Console.WriteLine(output);
            CaptureOutput(output);
        }

        public static void LogSuccess(string message)
            => LogInfo("success", message);

        public static void LogError(string message)
            => LogInfo("error", message);

        public static void LogWarning(string message)
            => LogInfo("warning", message);

        public static void LogProgress(string message, long current, long total)
        {
            double percentage = (double)current / total * 100;
            string output = $"🔄 [{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message} - Progress: {percentage:F2}% ({current:N0}/{total:N0})";
            Console.WriteLine(output);
            CaptureOutput(output);
        }

        public static void PrintHeader(string version)
        {
            string header = $@"=================================================================
              ETL CSV TO DATABASE v{version}
=================================================================
  This tool imports CSV data into SQL Server database
  with automatic schema detection and validation
=";
            Console.WriteLine(header);
            CaptureOutput(header);
        }
    }
}
