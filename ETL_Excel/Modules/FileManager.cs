using ETL_Excel.Models;
using System;
using System.IO;
using System.Linq;

namespace ETL_Excel.Modules
{
    public static class FileManager
    {
        private static ConfigurationModel _config = ConfigurationManager.GetConfig();

        public static void CreateDirectories()
        {
            Directory.CreateDirectory(_config.InputSettings.InputFolderPath);
            Directory.CreateDirectory(_config.OutputSettings.BaseFolderPath);
            Directory.CreateDirectory(Path.IsPathRooted(_config.OutputSettings.OtherCategoryFolder) 
                ? _config.OutputSettings.OtherCategoryFolder 
                : Path.Combine(Directory.GetCurrentDirectory(), _config.OutputSettings.OtherCategoryFolder));
            Directory.CreateDirectory(_config.LogSettings.LogFolderPath);
        }

        public static void CreateRequiredDirectories(ConfigurationModel config)
        {
            var specialFolderPath = Path.IsPathRooted(config.OutputSettings.OtherCategoryFolder) 
                ? config.OutputSettings.OtherCategoryFolder 
                : Path.Combine(Directory.GetCurrentDirectory(), config.OutputSettings.OtherCategoryFolder);
                
            var directories = new[]
            {
                    config.InputSettings.InputFolderPath,
                    config.OutputSettings.BaseFolderPath,
                    specialFolderPath,
                    config.LogSettings.LogFolderPath
                };

            foreach (var directory in directories)
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    LogManager.LogSuccess($"Created directory: {directory}");
                }
            }
        }

        public static string[] GetExcelFiles()
        {
            return Directory.GetFiles(_config.InputSettings.InputFolderPath, "*.xlsx");
        }

        public static string PrepareOutputFile(string filePath, ConsoleLogger consoleLogger)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string deletionInfo = ConsoleHelper.GetFileDeletion(Path.GetFileName(filePath));
                    consoleLogger.CaptureConsoleOutput(deletionInfo, true);
                    File.Delete(filePath);
                    LogManager.LogSuccess($"Deleted existing file: {filePath}");
                }
                return filePath;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Error preparing output file {filePath}: {ex.Message}");
                throw;
            }
        }

        public static int MoveSpecialFilesToOtherFolder(ConfigurationModel config)
        {
            var basePath = config.OutputSettings.BaseFolderPath;
            var otherPath = Path.IsPathRooted(config.OutputSettings.OtherCategoryFolder) 
                ? config.OutputSettings.OtherCategoryFolder 
                : Path.Combine(Directory.GetCurrentDirectory(), config.OutputSettings.OtherCategoryFolder);

            Directory.CreateDirectory(otherPath);
            int movedCount = 0;

            var excelFiles = Directory.GetFiles(basePath, "*.xlsx");
            foreach (var file in excelFiles)
            {
                var fileName = Path.GetFileName(file);
                bool shouldMove = config.OutputSettings.SpecialSheetKeywords
                    .Any(keyword => fileName.Contains(keyword, StringComparison.OrdinalIgnoreCase));

                if (shouldMove)
                {
                    var destinationPath = Path.Combine(otherPath, fileName);
                    try
                    {
                        if (File.Exists(destinationPath))
                        {
                            File.Delete(destinationPath);
                        }
                        File.Move(file, destinationPath);
                        movedCount++;
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogError($"Error moving file {fileName} to {otherPath}: {ex.Message}");
                    }
                }
            }

            return movedCount;
        }
    }
}

