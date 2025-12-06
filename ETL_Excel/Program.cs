using ETL_Excel.Modules;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Text;
using ClosedXML.Excel;
using ETL_Excel.Models;

namespace ETL_Excel
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Set UTF-8 encoding for proper emoji and box-drawing character display
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            var config = ConfigurationManager.GetConfig();
            var consoleLogger = new ConsoleLogger(config.LogSettings.LogFolderPath);

            using (var screenManager = new ScreenManager())
            {
                try
                {
                    // Basic console setup and welcome message
                    string welcomeMessage = ConsoleHelper.GetWelcomeMessage();
                    consoleLogger.CaptureConsoleOutput(welcomeMessage, true);

                    // Load configuration and display initial information
                    string configInfo = ConsoleHelper.GetConfigInfo(config);
                    consoleLogger.CaptureConsoleOutput(configInfo, true);

                    // Activate screen sleep prevention before starting the main process
                    screenManager.StartPreventSleep();

                    // Initialize logs and create necessary directories
                    LogManager.InitializeLogs();
                    FileManager.CreateDirectories();

                    // Get list of files to process and display count
                    var files = FileManager.GetExcelFiles();
                    string fileInfo = $"\n🔍 Scanning input directory...\n📊 Found {files.Length} Excel files to process.\n";
                    consoleLogger.CaptureConsoleOutput(fileInfo, true);

                    // Add separator before starting file processing
                    Console.WriteLine("\n" + new string('─', 80));
                    consoleLogger.CaptureConsoleOutput($"🕒 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting file processing", true);
                    Console.WriteLine(new string('─', 80) + "\n");

                    // Start performance monitoring
                    var stopwatch = Stopwatch.StartNew();

                    // Initialize processing statistics
                    int totalSheets = 0;
                    int processedSheets = 0;
                    int errors = 0;
                    int specialSheetsMoved = 0; // Add this counter

                    // Configure parallel processing block
                    var processBlock = new ActionBlock<string>(
                        async filePath =>
                        {
                            try
                            {
                                var result = await ExcelProcessor.ProcessExcelFileAsync(filePath, files.Length, Array.IndexOf(files, filePath) + 1, consoleLogger);
                                totalSheets += result.totalSheets;
                                processedSheets += result.processedSheets;
                                errors += result.errors;
                                specialSheetsMoved += result.specialSheetsMoved; // Increment the special sheets moved counter
                            }
                            catch (Exception ex)
                            {
                                string errorMessage = $"Error processing file {filePath}: {ex.Message}";
                                consoleLogger.CaptureConsoleOutput(errorMessage, true);
                                LogManager.LogError(errorMessage);
                                errors++;
                            }
                        },
                        new ExecutionDataflowBlockOptions
                        {
                            MaxDegreeOfParallelism = config.ProcessingSettings.MaxDegreeOfParallelism
                        });

                    // Process all files
                    foreach (var file in files)
                    {
                        await processBlock.SendAsync(file);
                    }

                    // Wait for all processing to complete
                    processBlock.Complete();
                    await processBlock.Completion;

                    // Stop performance monitoring
                    stopwatch.Stop();

                    // Add separator before summary
                    Console.WriteLine("\n" + new string('─', 80));
                    consoleLogger.CaptureConsoleOutput("📊 Processing completed - generating summary...", true);
                    Console.WriteLine(new string('─', 80));

                    // Display final results and statistics
                    string summary = ConsoleHelper.GetSummary(files.Length, totalSheets, processedSheets, errors, specialSheetsMoved);
                    consoleLogger.CaptureConsoleOutput(summary, true);

                    // Add final completion separator
                    Console.WriteLine("\n" + new string('═', 80));
                    string completionMessage = $"✅ Excel processing completed.\n\n🎉 Processing complete!\n⏱️ Total execution time: {stopwatch.Elapsed.TotalMinutes:F2} Minutes\n🕒 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Application finished";
                    consoleLogger.CaptureConsoleOutput(completionMessage, true);

                    string memoryUsage = ConsoleHelper.GetMemoryUsage();
                    consoleLogger.CaptureConsoleOutput(memoryUsage, true);
                    Console.WriteLine(new string('═', 80));
                }
                catch (Exception ex)
                {
                    // Handle any unexpected errors
                    string errorMessage = $"\n❌ Error in main process: {ex.Message}";
                    consoleLogger.CaptureConsoleOutput(errorMessage, true);
                    LogManager.LogError($"Critical error in main process: {ex.Message}");
                }
                finally
                {
                    // Save the final console output
                    consoleLogger.SaveFinalOutput();
                    // Remove the user input prompt
                    Console.WriteLine("\nProcess completed. Moving to next step...");
                    // Removed delay for automated orchestrator execution
                }
            } // ScreenManager is automatically disposed here
        }
    }
}

