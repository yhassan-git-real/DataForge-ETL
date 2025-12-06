// ConsoleLogger.cs
using System.Collections.Concurrent;

namespace ETL_ExcelToDatabase.Core
{
    public static class ConsoleLogger
    {
        static ConsoleLogger()
        {
            // Set console encoding for emoji support
            Console.OutputEncoding = System.Text.Encoding.UTF8;
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
            ["transfer"] = "🚀"
        };

        public static void LogInfo(string type, string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string emoji = Emojis.GetValueOrDefault(type, "ℹ️");
            Console.WriteLine($"{emoji} [{timestamp}] {message}");
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
            LogInfo("processing", $"{message} - Progress: {percentage:F2}% ({current:N0}/{total:N0})");
        }

        public static void PrintHeader(string version)
        {
            Console.WriteLine("=================================================================");
            Console.WriteLine($"              ETL EXCEL TO DATABASE v{version}");
            Console.WriteLine("=================================================================");
            Console.WriteLine("  This tool imports Excel data into SQL Server database");
            Console.WriteLine("  with automatic schema detection and validation");
            Console.WriteLine("=================================================================");
        }
    }
}