using System;

namespace DataForgeETL.UI.Services
{
    /// <summary>
    /// Abstraction for progress reporting
    /// </summary>
    public interface IProgressReporter
    {
        /// <summary>
        /// Reports overall progress
        /// </summary>
        void ReportProgress(double percentage, string status);

        /// <summary>
        /// Reports file processing progress
        /// </summary>
        void ReportFileProgress(int currentFile, int totalFiles, string fileName);

        /// <summary>
        /// Reports row processing progress
        /// </summary>
        void ReportRowProgress(long currentRow, long totalRows);

        /// <summary>
        /// Reports completion with result
        /// </summary>
        void ReportComplete(bool success, TimeSpan duration, string message = "");

        /// <summary>
        /// Resets progress to initial state
        /// </summary>
        void Reset();

        /// <summary>
        /// Reports an error during processing
        /// </summary>
        void ReportError(string message);
    }
}
