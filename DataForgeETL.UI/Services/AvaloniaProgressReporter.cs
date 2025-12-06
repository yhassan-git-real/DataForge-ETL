using System;
using Avalonia.Threading;
using DataForgeETL.UI.Models;

namespace DataForgeETL.UI.Services
{
    /// <summary>
    /// Avalonia UI implementation of IProgressReporter
    /// </summary>
    public class AvaloniaProgressReporter : IProgressReporter
    {
        private readonly ProgressInfo _progressInfo;
        private readonly Action _onProgressChanged;

        public AvaloniaProgressReporter(ProgressInfo progressInfo, Action onProgressChanged)
        {
            _progressInfo = progressInfo;
            _onProgressChanged = onProgressChanged;
        }

        public void ReportProgress(double percentage, string status)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _progressInfo.IsRunning = true;
                _progressInfo.OverallProgress = percentage;
                _progressInfo.Status = status;
                _progressInfo.Elapsed = DateTime.Now - _progressInfo.StartTime;
                _progressInfo.UpdateEstimatedTime();
                _onProgressChanged?.Invoke();
            });
        }

        public void ReportFileProgress(int currentFile, int totalFiles, string fileName)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _progressInfo.IsRunning = true;
                _progressInfo.CurrentFile = currentFile;
                _progressInfo.TotalFiles = totalFiles;
                _progressInfo.CurrentFileName = fileName;
                
                if (totalFiles > 0)
                {
                    _progressInfo.OverallProgress = (double)currentFile / totalFiles * 100;
                }
                
                _progressInfo.Elapsed = DateTime.Now - _progressInfo.StartTime;
                _progressInfo.UpdateEstimatedTime();
                _onProgressChanged?.Invoke();
            });
        }

        public void ReportRowProgress(long currentRow, long totalRows)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _progressInfo.CurrentRow = currentRow;
                _progressInfo.TotalRows = totalRows;
                _onProgressChanged?.Invoke();
            });
        }

        public void ReportComplete(bool success, TimeSpan duration, string message = "")
        {
            Dispatcher.UIThread.Post(() =>
            {
                _progressInfo.IsComplete = true;
                _progressInfo.IsRunning = false;
                _progressInfo.OverallProgress = success ? 100 : _progressInfo.OverallProgress;
                _progressInfo.Status = success 
                    ? (string.IsNullOrEmpty(message) ? "Completed successfully" : message)
                    : (string.IsNullOrEmpty(message) ? "Completed with errors" : message);
                _progressInfo.Elapsed = duration;
                _progressInfo.EstimatedTimeRemaining = null;
                _onProgressChanged?.Invoke();
            });
        }

        public void Reset()
        {
            Dispatcher.UIThread.Post(() =>
            {
                _progressInfo.OverallProgress = 0;
                _progressInfo.CurrentFile = 0;
                _progressInfo.TotalFiles = 0;
                _progressInfo.CurrentFileName = null;
                _progressInfo.CurrentRow = 0;
                _progressInfo.TotalRows = 0;
                _progressInfo.Status = "Initializing...";
                _progressInfo.StartTime = DateTime.Now;
                _progressInfo.Elapsed = TimeSpan.Zero;
                _progressInfo.EstimatedTimeRemaining = null;
                _progressInfo.IsComplete = false;
                _progressInfo.IsError = false;
                _progressInfo.ErrorMessage = null;
                _progressInfo.IsRunning = true;
                _onProgressChanged?.Invoke();
            });
        }

        public void ReportError(string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _progressInfo.IsError = true;
                _progressInfo.IsRunning = false;
                _progressInfo.ErrorMessage = message;
                _progressInfo.Status = "Error occurred";
                _onProgressChanged?.Invoke();
            });
        }
    }
}
