using System.Data;

namespace ETL_ExcelToDatabase.Models
{
    /// <summary>
    /// Represents a batch of data read from an Excel file.
    /// Maintains the same structure as the CSV project for consistency.
    /// </summary>
    public class BatchResult
    {
        // Using DataTable to maintain compatibility with SqlBulkCopy
        private readonly DataTable _data = new();

        /// <summary>
        /// Gets or initializes the batch data.
        /// Throws ArgumentNullException if attempting to set null value.
        /// </summary>
        public DataTable Data
        {
            get => _data;
            init => _data = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Indicates if this is the first batch from the Excel file.
        /// Used for initial table creation and validation.
        /// </summary>
        public bool IsFirstBatch { get; init; }

        /// <summary>
        /// Indicates if this is the last batch from the Excel file.
        /// Used for final processing and cleanup operations.
        /// </summary>
        public bool IsLastBatch { get; init; }

        /// <summary>
        /// Gets the number of columns in the current batch.
        /// </summary>
        public int ColumnCount => Data.Columns.Count;

        /// <summary>
        /// Gets the number of rows in the current batch.
        /// </summary>
        public int RowCount => Data.Rows.Count;

        /// <summary>
        /// Gets an array of column names from the current batch.
        /// Used for validation and mapping operations.
        /// </summary>
        public string[] GetColumnNames()
        {
            string[] columnNames = new string[Data.Columns.Count];
            for (int i = 0; i < Data.Columns.Count; i++)
            {
                columnNames[i] = Data.Columns[i].ColumnName;
            }
            return columnNames;
        }

        /// <summary>
        /// Creates a deep copy of the current batch result.
        /// Useful for operations that need to modify the batch data.
        /// </summary>
        public BatchResult Clone()
        {
            return new BatchResult
            {
                Data = Data.Copy(),
                IsFirstBatch = IsFirstBatch,
                IsLastBatch = IsLastBatch
            };
        }
    }
}
