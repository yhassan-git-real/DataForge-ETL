using System;

namespace DataForgeETL.UI.Services
{
    /// <summary>
    /// Console implementation of IProgressReporter for fallback/testing
    /// </summary>
    public class ConsoleProgressReporter : IProgressReporter
    {
        private int _lastPercentage = -1;

        public void ReportProgress(double percentage, string status)
        {
            var currentPercentage = (int)percentage;
            if (currentPercentage != _lastPercentage)
            {
                Console.WriteLine($"Progress: {percentage:F1}% - {status}");
                _lastPercentage = currentPercentage;
            }
        }

        public void ReportFileProgress(int currentFile, int totalFiles, string fileName)
        {
            Console.WriteLine($"Processing file {currentFile} of {totalFiles}: {fileName}");
        }

        public void ReportRowProgress(long currentRow, long totalRows)
        {
            if (currentRow % 10000 == 0 || currentRow == totalRows)
            {
                Console.WriteLine($"Rows processed: {currentRow:N0} / {totalRows:N0}");
            }
        }

        public void ReportComplete(bool success, TimeSpan duration, string message = "")
        {
            Console.ForegroundColor = success ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine(success ? "✅ Process completed successfully!" : "❌ Process failed!");
            Console.ResetColor();
            Console.WriteLine($"Duration: {duration:hh\\:mm\\:ss}");
            if (!string.IsNullOrEmpty(message))
            {
                Console.WriteLine(message);
            }
        }

        public void Reset()
        {
            _lastPercentage = -1;
            Console.WriteLine("Progress reset.");
        }

        public void ReportError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Error: {message}");
            Console.ResetColor();
        }
    }
}
