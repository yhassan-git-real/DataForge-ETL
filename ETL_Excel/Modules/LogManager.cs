using ETL_Excel.Models;
using System;
using System.IO;

namespace ETL_Excel.Modules
{
    public static class LogManager
    {
        private static ConfigurationModel _config = ConfigurationManager.GetConfig();
        private static string? _successLogPath;
        private static string? _errorLogPath;
        private static bool _isInitialized = false;

        public static void InitializeLogs()
        {
            try
            {
                if (!_isInitialized)
                {
                    // Check and create log directory only once during initialization
                    EnsureLogDirectoryExists();

                    string timestamp = DateTime.Now.ToString("yyyyMMdd");
                    _successLogPath = Path.Combine(_config.LogSettings.LogFolderPath, $"successlog_{timestamp}.txt");
                    _errorLogPath = Path.Combine(_config.LogSettings.LogFolderPath, $"errorlog_{timestamp}.txt");

                    _isInitialized = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error initializing logs: {ex.Message}");
                throw new DirectoryNotFoundException($"Failed to initialize logs: {ex.Message}");
            }
        }

        private static void EnsureLogDirectoryExists()
        {
            if (string.IsNullOrWhiteSpace(_config.LogSettings.LogFolderPath))
            {
                throw new DirectoryNotFoundException("Log folder path is not configured in settings.json");
            }

            if (!Directory.Exists(_config.LogSettings.LogFolderPath))
            {
                Directory.CreateDirectory(_config.LogSettings.LogFolderPath);
                Console.WriteLine($"📁 Log directory created: {_config.LogSettings.LogFolderPath}");
            }
            else
            {
                Console.WriteLine($"✅ Log directory exists: {_config.LogSettings.LogFolderPath}");
            }
        }

        public static void LogSuccess(string message)
        {
            Log(_successLogPath, message);
        }

        public static void LogError(string message)
        {
            Log(_errorLogPath, message);
        }

        private static void Log(string path, string message)
        {
            try
            {
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine($"{DateTime.Now:dd-MM-yyyy HH:mm:ss}: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error writing to log file: {ex.Message}");
                Console.WriteLine($"🔍 Original message: {message}");
            }
        }
    }
}