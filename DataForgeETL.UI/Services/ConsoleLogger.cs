using System;
using DataForgeETL.UI.Models;

namespace DataForgeETL.UI.Services
{
    /// <summary>
    /// Console implementation of IUILogger for fallback/testing
    /// </summary>
    public class ConsoleLogger : IUILogger
    {
        public void LogInfo(string message, string category = "info")
        {
            Log(message, ConsoleColor.White, "INFO", category);
        }

        public void LogSuccess(string message)
        {
            Log(message, ConsoleColor.Green, "SUCCESS", "success");
        }

        public void LogError(string message)
        {
            Log(message, ConsoleColor.Red, "ERROR", "error");
        }

        public void LogWarning(string message)
        {
            Log(message, ConsoleColor.Yellow, "WARNING", "warning");
        }

        public void LogProgress(string message, long current, long total)
        {
            var progressMessage = $"{message} - {current:N0}/{total:N0} ({(double)current / total * 100:F1}%)";
            Log(progressMessage, ConsoleColor.Cyan, "PROGRESS", "progress");
        }

        public void LogDebug(string message)
        {
            Log(message, ConsoleColor.Gray, "DEBUG", "debug");
        }

        private void Log(string message, ConsoleColor color, string level, string category)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var emoji = GetEmoji(level);
            
            Console.ForegroundColor = color;
            Console.WriteLine($"{emoji} [{timestamp}] [{level}] {message}");
            Console.ResetColor();
        }

        private string GetEmoji(string level) => level switch
        {
            "DEBUG" => "🔍",
            "INFO" => "ℹ️",
            "SUCCESS" => "✅",
            "WARNING" => "⚠️",
            "ERROR" => "❌",
            "PROGRESS" => "🔄",
            _ => "•"
        };
    }
}
