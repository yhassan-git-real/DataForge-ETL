using System.Collections.Generic;
using System.Text;
using ETL_ExcelToDatabase.Core;


namespace ETL_ExcelToDatabase.Services
{
    public static class FileLogger
    {
        private static readonly List<string> _logEntries = new();
        private static string _logFilePath = string.Empty;

        public static void Initialize(string logFolderPath)
        {
            if (!Directory.Exists(logFolderPath))
            {
                Directory.CreateDirectory(logFolderPath);
            }

            _logFilePath = Path.Combine(logFolderPath,
                $"Output_Log_{DateTime.Now:yyyyMMdd}.txt");
        }

        public static void LogEntry(string message)
        {
            _logEntries.Add(message);
            Console.WriteLine(message);
        }

        public static void PrintConfigurationSummary(AppConfig config)
        {
            StringBuilder summary = new();
            summary.AppendLine("=".PadRight(80, '='));
            summary.AppendLine("📋 Configuration Summary:");
            summary.AppendLine($"   Server Name: {config.DatabaseConfig.Server}");
            summary.AppendLine($"   Database: {config.DatabaseConfig.Database}");
            summary.AppendLine($"   Authentication: {(config.DatabaseConfig.IntegratedSecurity ? "Windows" : "SQL")}");
            summary.AppendLine($"   Excel Path: {config.ProcessConfig.ExcelFolderPath}");
            summary.AppendLine($"   Batch Size: {config.ProcessConfig.BatchSize:N0}");
            summary.AppendLine("\n📋 Log Tables:");
            summary.AppendLine($"   Error Log: {config.ProcessConfig.ErrorTableName}");
            summary.AppendLine($"   Success Log: {config.ProcessConfig.SuccessLogTableName}");
            summary.AppendLine("   To view logs after import, use these SQL commands:");
            summary.AppendLine($"   SELECT * FROM {config.ProcessConfig.ErrorTableName} ORDER BY Timestamp DESC;");
            summary.AppendLine($"   SELECT * FROM {config.ProcessConfig.SuccessLogTableName} ORDER BY Timestamp DESC;");
            summary.AppendLine("=".PadRight(80, '='));

            LogEntry(summary.ToString());
        }

        public static void PrintProcessSummary(
            AppConfig config,
            int totalFiles,
            long totalRows,
            TimeSpan totalTime,
            List<string> processedFiles)
        {
            StringBuilder summary = new();
            summary.AppendLine("\n=".PadRight(80, '='));
            summary.AppendLine("📊 Import Summary:");
            summary.AppendLine($"   Total files processed: {totalFiles:N0}");
            summary.AppendLine($"   Total rows imported: {totalRows:N0}");
            summary.AppendLine($"   Total processing time: {totalTime.TotalMinutes:N2} minutes");
            summary.AppendLine($"   Average rows per second: {totalRows / totalTime.TotalSeconds:N0}");
            summary.AppendLine("\nTo check for any errors, run:");
            summary.AppendLine($"   SELECT * FROM {config.ProcessConfig.ErrorTableName} WHERE FileName IN ('{string.Join("','", processedFiles)}')");
            summary.AppendLine("=".PadRight(80, '='));

            LogEntry(summary.ToString());
        }

        public static async Task SaveLogFile()
        {
            if (string.IsNullOrEmpty(_logFilePath)) return;

            try
            {
                await File.WriteAllLinesAsync(_logFilePath, _logEntries);
                Console.WriteLine($"\n📝 Log file saved: {_logFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Failed to save log file: {ex.Message}");
            }
        }
    }
}