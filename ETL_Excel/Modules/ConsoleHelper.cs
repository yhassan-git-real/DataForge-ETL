using System;
using System.IO;
using System.Text;
using ETL_Excel.Models;

namespace ETL_Excel.Modules
{
    public static class ConsoleHelper
    {
        public static string GetWelcomeMessage()
        {
            return "📊 Welcome to Excel Sheet Splitter! 🔀\n" +
                   $"🕒 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Application started";
        }

        public static string GetConfigInfo(ConfigurationModel config)
        {
            var sb = new StringBuilder();
            sb.AppendLine("✅ Configuration loaded successfully!");
            sb.AppendLine($"📁 Input Path: {config.InputSettings.InputFolderPath}");
            sb.AppendLine("📂 Output Paths:");
            sb.AppendLine($"   📁 Output: {config.OutputSettings.BaseFolderPath}");
            var specialSheetsPath = Path.IsPathRooted(config.OutputSettings.OtherCategoryFolder) 
                ? config.OutputSettings.OtherCategoryFolder 
                : Path.Combine(Directory.GetCurrentDirectory(), config.OutputSettings.OtherCategoryFolder);
            sb.AppendLine($"   📂 Special Sheets: {specialSheetsPath}");
            return sb.ToString();
        }

        public static string GetProgress(int currentFile, int totalFiles, int currentSheet, int totalSheets, double progress)
        {
            int width = 40;
            int completedWidth = (int)(width * progress);
            string progressBar = new string('█', completedWidth) + new string('░', width - completedWidth);
            return $"\rProcessing: [{progressBar}] {progress:P1} (File {currentFile}/{totalFiles}, Sheet {currentSheet}/{totalSheets})";
        }

        public static string GetSheetInfo(string sheetName, int rows, int columns, string outputFileName)
        {
            return $"      ┌─ Rows: {rows:N0} | Columns: {columns}\n" +
                   $"      ├─ ✅ Processed -> {outputFileName}\n" +
                   $"      └─ 🕒 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Completed {sheetName}\n";
        }

        public static string GetFileInfo(string fileName, int currentSheet, int totalSheets)
        {
            var sb = new StringBuilder();
            sb.AppendLine("\n" + new string('•', 60));
            sb.AppendLine($"📑 Processing Excel File: {fileName}");
            sb.AppendLine($"   Sheet: {currentSheet} ({currentSheet}/{totalSheets})");
            sb.AppendLine(new string('•', 60));
            return sb.ToString();
        }

        public static string GetFileDeletion(string fileName)
        {
            return $"   ⚠️ Found existing file: {fileName}\n" +
                   $"   🗑️ Deleting existing file...";
        }

        public static string GetSummary(int totalFiles, int totalSheets, int processedSheets, int errors, int specialFilesMoved)
        {
            var sb = new StringBuilder();
            sb.AppendLine("\n" + new string('═', 80));
            sb.AppendLine("                            PROCESSING SUMMARY REPORT");
            sb.AppendLine(new string('═', 80));
            sb.AppendLine($"📁 Total Excel Files: {totalFiles}");
            sb.AppendLine($"📑 Total Sheets: {totalSheets}");
            sb.AppendLine($"📊 Processed Sheets: {processedSheets}");
            if (errors > 0)
            {
                sb.AppendLine($"❌ Errors: {errors}");
            }
            else
            {
                sb.AppendLine($"✅ Errors: {errors} (No errors!)");
            }
            sb.AppendLine($"📂 Special Files Moved: {specialFilesMoved}");
            sb.AppendLine(new string('═', 80));
            return sb.ToString();
        }

        public static string GetMemoryUsage()
        {
            long memoryUsed = GC.GetTotalMemory(false);
            return $"🧠 Memory usage: {memoryUsed / 1024 / 1024} MB";
        }
    }
}

