// ValidationService.cs
using System.Data;
using Microsoft.Data.SqlClient;
using ETL_CsvToDatabase.Models;
using ETL_CsvToDatabase.Core;

namespace ETL_CsvToDatabase.Services
{
    public static class ValidationService
    {
        public static async Task<ValidationResult> ValidateColumnMapping(
            SqlConnection connection,
            string sourceTable,
            string destTable)
        {
            ValidationResult result = new();

            // Get column information from both tables
            var sourceColumns = await GetTableColumns(connection, sourceTable);
            var destColumns = await GetTableColumns(connection, destTable);

            result.SourceColumns = sourceColumns.Keys.ToList();
            result.DestinationColumns = destColumns.Keys.ToList();

            // Find matching and unmatched columns
            foreach (var sourceCol in sourceColumns)
            {
                string columnName = sourceCol.Key;
                if (destColumns.TryGetValue(columnName, out var destType))
                {
                    result.MatchedColumns.Add(columnName);

                    // Create detailed analysis
                    var analysis = new ColumnAnalysis
                    {
                        SourceName = columnName,
                        DestinationName = columnName,
                        SourceType = sourceCol.Value,
                        DestinationType = destType,
                        IsRequired = IsRequiredColumn(columnName),
                        IsValid = AreCompatibleTypes(sourceCol.Value, destType)
                    };

                    if (!analysis.IsValid)
                    {
                        analysis.ValidationMessages.Add(
                            $"Type mismatch: Source is {sourceCol.Value}, " +
                            $"Destination is {destType}");
                    }

                    result.DetailedAnalysis[columnName] = analysis;
                }
                else
                {
                    result.UnmatchedSourceColumns.Add(columnName);
                }
            }

            // Find destination columns not in source
            result.UnmatchedDestColumns = destColumns.Keys
                .Except(result.MatchedColumns)
                .ToList();

            return result;
        }

        private static async Task<Dictionary<string, string>> GetTableColumns(
            SqlConnection connection,
            string tableName)
        {
            Dictionary<string, string> columns = new(StringComparer.OrdinalIgnoreCase);

            const string columnQuery = @"
                SELECT 
                    COLUMN_NAME,
                    DATA_TYPE,
                    CHARACTER_MAXIMUM_LENGTH,
                    NUMERIC_PRECISION,
                    NUMERIC_SCALE
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = @TableName
                ORDER BY ORDINAL_POSITION";

            await using SqlCommand cmd = new(columnQuery, connection);
            cmd.Parameters.AddWithValue("@TableName", tableName);

            await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string columnName = reader.GetString(0);
                string dataType = BuildColumnTypeDescription(reader);
                columns[columnName] = dataType;
            }

            return columns;
        }

        private static string BuildColumnTypeDescription(SqlDataReader reader)
        {
            string dataType = reader.GetString(1);
            object? maxLength = reader.IsDBNull(2) ? null : reader.GetValue(2);
            object? precision = reader.IsDBNull(3) ? null : reader.GetValue(3);
            object? scale = reader.IsDBNull(4) ? null : reader.GetValue(4);

            return dataType.ToLower() switch
            {
                "nvarchar" or "varchar" or "char" or "nchar"
                    when maxLength != null =>
                    $"{dataType}({(Convert.ToInt32(maxLength) == -1 ? "MAX" : maxLength.ToString())})",

                "decimal" or "numeric"
                    when precision != null && scale != null =>
                    $"{dataType}({precision},{scale})",

                _ => dataType
            };
        }

        private static bool AreCompatibleTypes(string sourceType, string destType)
        {
            if (sourceType == destType) return true;

            // Define type compatibility rules
            Dictionary<string, HashSet<string>> compatibleTypes = new()
            {
                ["nvarchar"] = new() { "varchar", "nvarchar", "nchar", "char", "text", "ntext" },
                ["varchar"] = new() { "nvarchar", "varchar", "nchar", "char", "text" },
                ["int"] = new() { "bigint", "decimal", "numeric" },
                ["decimal"] = new() { "numeric", "float", "real" },
                ["datetime"] = new() { "datetime2", "smalldatetime", "date" }
            };

            static string GetBaseType(string fullType) =>
                fullType.Split('(')[0].ToLower();

            string sourceBaseType = GetBaseType(sourceType);
            string destBaseType = GetBaseType(destType);

            return compatibleTypes.TryGetValue(sourceBaseType, out var compatibleSet) &&
                   compatibleSet.Contains(destBaseType);
        }

        private static bool IsRequiredColumn(string columnName)
        {
            // Define your required columns here - exact matches including special characters
            var requiredColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Import/Export Indicator",
                // Add other required columns as needed
            };

            return requiredColumns.Contains(columnName);
        }
    }
}