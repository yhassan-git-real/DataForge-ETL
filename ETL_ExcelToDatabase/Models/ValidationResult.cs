// ValidationResult.cs
using System.Text;
using System.Collections.Generic;

namespace ETL_ExcelToDatabase.Models
{
    public class ValidationResult
    {
        /// <summary>
        /// Indicates if the validation passed
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// List of columns that matched between temp and destination tables
        /// </summary>
        public List<string> MatchedColumns { get; set; } = new List<string>();

        /// <summary>
        /// List of columns that did not match between temp and destination tables
        /// </summary>
        public List<string> UnmatchedColumns { get; set; } = new List<string>();

        /// <summary>
        /// Total number of columns in the temp table
        /// </summary>
        public int TempColumnCount { get; set; }

        /// <summary>
        /// Total number of columns in the destination table
        /// </summary>
        public int DestColumnCount { get; set; }

        // Lists to track column mappings
        public List<string> SourceColumns { get; set; } = new List<string>();
        public List<string> DestinationColumns { get; set; } = new List<string>();
        public List<string> UnmatchedSourceColumns { get; set; } = new List<string>();
        public List<string> UnmatchedDestColumns { get; set; } = new List<string>();

        // Detailed analysis dictionary - key is exact column name including special characters
        public Dictionary<string, ColumnAnalysis> DetailedAnalysis { get; set; } = new Dictionary<string, ColumnAnalysis>();

        public string GetValidationReport()
        {
            StringBuilder report = new();
            report.AppendLine($"Validation Report ({DateTime.Now:yyyy-MM-dd HH:mm:ss}):");
            report.AppendLine($"Total Source Columns: {SourceColumns.Count}");
            report.AppendLine($"Total Destination Columns: {DestinationColumns.Count}");
            report.AppendLine($"Matched Columns: {MatchedColumns.Count}");

            if (UnmatchedSourceColumns.Any())
            {
                report.AppendLine("\nColumns in Excel not found in destination table:");
                foreach (var column in UnmatchedSourceColumns)
                {
                    report.AppendLine($"- {column}");
                    if (DetailedAnalysis.TryGetValue(column, out var analysis))
                    {
                        foreach (var message in analysis.ValidationMessages)
                        {
                            report.AppendLine($"  • {message}");
                        }
                    }
                }
            }

            if (UnmatchedDestColumns.Any())
            {
                report.AppendLine("\nRequired destination table columns not found in Excel:");
                foreach (var column in UnmatchedDestColumns)
                {
                    report.AppendLine($"- {column}");
                }
            }

            return report.ToString();
        }

        private static bool IsRequiredColumn(string columnName)
        {
            // Define required columns - using exact names including special characters
            var requiredColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Import/Export Indicator",  // Keeping original name with '/'
                // Add other required columns as needed
            };

            return requiredColumns.Contains(columnName);
        }
    }

    public class ColumnAnalysis
    {
        // Keep exact column names including special characters
        public string SourceName { get; set; } = string.Empty;
        public string DestinationName { get; set; } = string.Empty;

        // Track data types for compatibility checking
        public string SourceType { get; set; } = string.Empty;
        public string DestinationType { get; set; } = string.Empty;

        public List<string> ValidationMessages { get; set; } = new List<string>();
        public bool IsRequired { get; set; }
        public bool IsValid { get; set; }

        public override string ToString()
        {
            return $"Column: {SourceName} -> {DestinationName} " +
                   $"(Types: {SourceType} -> {DestinationType}) " +
                   $"Valid: {IsValid}";
        }
    }
}