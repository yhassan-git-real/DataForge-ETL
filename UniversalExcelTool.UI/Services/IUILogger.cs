using System;
using System.Threading.Tasks;

namespace DataForgeETL.UI.Services
{
    /// <summary>
    /// Abstraction for logging that works in both console and UI modes
    /// </summary>
    public interface IUILogger
    {
        /// <summary>
        /// Logs an informational message
        /// </summary>
        void LogInfo(string message, string category = "info");

        /// <summary>
        /// Logs a success message
        /// </summary>
        void LogSuccess(string message);

        /// <summary>
        /// Logs an error message
        /// </summary>
        void LogError(string message);

        /// <summary>
        /// Logs a warning message
        /// </summary>
        void LogWarning(string message);

        /// <summary>
        /// Logs progress information
        /// </summary>
        void LogProgress(string message, long current, long total);

        /// <summary>
        /// Logs debug information (optional)
        /// </summary>
        void LogDebug(string message);
    }
}
