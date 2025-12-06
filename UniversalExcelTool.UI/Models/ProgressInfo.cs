using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UniversalExcelTool.UI.Models
{
    /// <summary>
    /// Represents progress information for ETL operations
    /// </summary>
    public class ProgressInfo : INotifyPropertyChanged
    {
        private double _overallProgress;
        private int _currentFile;
        private int _totalFiles;
        private string? _currentFileName;
        private long _currentRow;
        private long _totalRows;
        private string _status;
        private DateTime _startTime;
        private TimeSpan _elapsed;
        private TimeSpan? _estimatedTimeRemaining;
        private bool _isComplete;
        private bool _isError;
        private string? _errorMessage;
        private bool _isRunning;

        public event PropertyChangedEventHandler? PropertyChanged;

        public double OverallProgress
        {
            get => _overallProgress;
            set
            {
                if (_overallProgress != value)
                {
                    _overallProgress = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProgressPercentage));
                }
            }
        }

        public int CurrentFile
        {
            get => _currentFile;
            set
            {
                if (_currentFile != value)
                {
                    _currentFile = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FileProgress));
                }
            }
        }

        public int TotalFiles
        {
            get => _totalFiles;
            set
            {
                if (_totalFiles != value)
                {
                    _totalFiles = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FileProgress));
                }
            }
        }

        public string? CurrentFileName
        {
            get => _currentFileName;
            set
            {
                if (_currentFileName != value)
                {
                    _currentFileName = value;
                    OnPropertyChanged();
                }
            }
        }

        public long CurrentRow
        {
            get => _currentRow;
            set
            {
                if (_currentRow != value)
                {
                    _currentRow = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RowProgress));
                }
            }
        }

        public long TotalRows
        {
            get => _totalRows;
            set
            {
                if (_totalRows != value)
                {
                    _totalRows = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RowProgress));
                }
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime StartTime
        {
            get => _startTime;
            set
            {
                if (_startTime != value)
                {
                    _startTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public TimeSpan Elapsed
        {
            get => _elapsed;
            set
            {
                if (_elapsed != value)
                {
                    _elapsed = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ElapsedTime));
                }
            }
        }

        public TimeSpan? EstimatedTimeRemaining
        {
            get => _estimatedTimeRemaining;
            set
            {
                if (_estimatedTimeRemaining != value)
                {
                    _estimatedTimeRemaining = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(EstimatedTime));
                }
            }
        }

        public bool IsComplete
        {
            get => _isComplete;
            set
            {
                if (_isComplete != value)
                {
                    _isComplete = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsError
        {
            get => _isError;
            set
            {
                if (_isError != value)
                {
                    _isError = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    OnPropertyChanged();
                }
            }
        }

        public ProgressInfo()
        {
            _status = "Ready";
            _startTime = DateTime.Now;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Gets progress percentage as string
        /// </summary>
        public string ProgressPercentage => $"{OverallProgress:F1}%";

        /// <summary>
        /// Gets file progress as string
        /// </summary>
        public string FileProgress => TotalFiles > 0 
            ? $"File {CurrentFile} of {TotalFiles}" 
            : "No files";

        /// <summary>
        /// Gets row progress as string
        /// </summary>
        public string RowProgress => TotalRows > 0 
            ? $"{CurrentRow:N0} / {TotalRows:N0} rows" 
            : "Processing...";

        /// <summary>
        /// Gets elapsed time as formatted string
        /// </summary>
        public string ElapsedTime => Elapsed.ToString(@"hh\:mm\:ss");

        /// <summary>
        /// Gets estimated time remaining as formatted string
        /// </summary>
        public string EstimatedTime => EstimatedTimeRemaining.HasValue 
            ? EstimatedTimeRemaining.Value.ToString(@"hh\:mm\:ss") 
            : "Calculating...";

        /// <summary>
        /// Updates estimated time remaining based on progress
        /// </summary>
        public void UpdateEstimatedTime()
        {
            if (OverallProgress > 0 && OverallProgress < 100)
            {
                var totalEstimatedTime = TimeSpan.FromSeconds(
                    Elapsed.TotalSeconds / (OverallProgress / 100.0));
                EstimatedTimeRemaining = totalEstimatedTime - Elapsed;
            }
            else
            {
                EstimatedTimeRemaining = null;
            }
        }
    }
}
