using System.Collections.Generic;

namespace ETL_Excel.Models
{
    public class ConfigurationModel
    {
        public required InputSettings InputSettings { get; set; }
        public required OutputSettings OutputSettings { get; set; }
        public required LogSettings LogSettings { get; set; }
        public required ProcessingSettings ProcessingSettings { get; set; }
    }

    public class InputSettings
    {
        public string InputFolderPath { get; set; } = string.Empty;
        public string IndicatorColumnName { get; set; } = string.Empty;
        public string IndicatorColumnReference { get; set; } = string.Empty;
        public Dictionary<string, IndicatorValue> IndicatorValues { get; set; } = new();
    }

    public class IndicatorValue
    {
        public string Value { get; set; } = string.Empty;
        public string FolderSuffix { get; set; } = string.Empty;
    }

    public class OutputSettings
    {
        public string BaseFolderPath { get; set; } = string.Empty;
        public string FirstCategoryFolder { get; set; } = string.Empty;
        public string SecondCategoryFolder { get; set; } = string.Empty;
        public string OtherCategoryFolder { get; set; } = string.Empty;  // Add this
        public List<string> SpecialSheetKeywords { get; set; } = new();  // Add this
        public string OtherCategoryImportFolder { get; set; } = "Other_Excel_Import";
        public string OtherCategoryExportFolder { get; set; } = "Other_Excel_Export";
    }

    public class LogSettings
    {
        public required string LogFolderPath { get; set; }
    }

    public class ProcessingSettings
    {
        public int ChunkSize { get; set; }
        public int SaveInterval { get; set; }
        public int MemoryCleanupInterval { get; set; }
        public int MaxDegreeOfParallelism { get; set; }
        public int BatchSize { get; set; }
        public bool EnableProgressNotifications { get; set; } = true;
    }
}

