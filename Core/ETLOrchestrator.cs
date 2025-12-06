using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using DataForgeETL.Core;

namespace DataForgeETL.Core
{
    /// <summary>
    /// Centralized orchestrator for the DataForge ETL process
    /// Manages the execution of all modules with unified configuration
    /// </summary>
    public class ETLOrchestrator
    {
        private readonly UnifiedConfigurationManager _configManager;
        private readonly string _logFilePath;

        public ETLOrchestrator()
        {
            _configManager = UnifiedConfigurationManager.Instance;
            _logFilePath = Path.Combine(_configManager.GetLogFilesPath(), $"ETL_Orchestrator_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        }

        /// <summary>
        /// Runs the complete ETL process with all modules
        /// </summary>
        public async Task<bool> RunCompleteETLProcessAsync(ETLProcessOptions? options = null, CancellationToken cancellationToken = default)
        {
            options ??= new ETLProcessOptions();
            
            try
            {
                DisplayHeader();
                _configManager.DisplayConfigurationSummary();
                
                // Ensure directories exist
                _configManager.EnsureDirectoriesExist();
                
                LogInfo("Starting complete ETL process...");
                
                var stopwatch = Stopwatch.StartNew();
                bool success = true;

                // Step 1: Dynamic Table Configuration
                if (!options.SkipDynamicTableConfig)
                {
                    success = await RunDynamicTableManagerAsync(options, cancellationToken);
                    if (!success && !options.ContinueOnError)
                    {
                        LogError("Dynamic Table Manager failed. Stopping process.");
                        return false;
                    }
                }

                // Step 2: Excel Processing
                if (success || options.ContinueOnError)
                {
                    success = await RunExcelProcessorAsync(options, cancellationToken);
                    if (!success && !options.ContinueOnError)
                    {
                        LogError("Excel Processor failed. Stopping process.");
                        return false;
                    }
                }

                // Step 3: Database Loading
                if (success || options.ContinueOnError)
                {
                    success = await RunDatabaseLoaderAsync(options, cancellationToken);
                }

                stopwatch.Stop();
                
                if (success)
                {
                    LogSuccess($"Complete ETL process completed successfully in {stopwatch.Elapsed:hh\\:mm\\:ss}");
                    DisplayCompletionSummary(true, stopwatch.Elapsed);
                }
                else
                {
                    LogError($"ETL process completed with errors in {stopwatch.Elapsed:hh\\:mm\\:ss}");
                    DisplayCompletionSummary(false, stopwatch.Elapsed);
                }

                return success;
            }
            catch (Exception ex)
            {
                LogError($"ETL process failed with exception: {ex.Message}");
                LogError($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Runs only the Dynamic Table Manager
        /// </summary>
        public async Task<bool> RunDynamicTableManagerAsync(ETLProcessOptions? options = null, CancellationToken cancellationToken = default)
        {
            options ??= new ETLProcessOptions();
            
            LogInfo("═══════════════════════════════════════════════════════════════");
            LogInfo("STEP 1: Dynamic Table Configuration");
            LogInfo("═══════════════════════════════════════════════════════════════");

            try
            {
                string executablePath = _configManager.GetExecutablePath("dynamictablemanager");
                var moduleInfo = _configManager.GetConfiguration().ExecutableModules.DynamicTableManager;
                
                string arguments = options.DynamicTableManagerArgs ?? moduleInfo.Arguments;
                
                return await ExecuteModuleAsync(executablePath, arguments, moduleInfo.Name, cancellationToken);
            }
            catch (Exception ex)
            {
                LogError($"Failed to run Dynamic Table Manager: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Runs only the Excel Processor module
        /// </summary>
        public async Task<bool> RunExcelProcessorAsync(ETLProcessOptions? options = null, CancellationToken cancellationToken = default)
        {
            options ??= new ETLProcessOptions();
            
            LogInfo("═══════════════════════════════════════════════════════════════");
            LogInfo("STEP 2: Excel Processing");
            LogInfo("═══════════════════════════════════════════════════════════════");

            try
            {
                string executablePath = _configManager.GetExecutablePath("excelprocessor");
                var moduleInfo = _configManager.GetConfiguration().ExecutableModules.ExcelProcessor;
                
                string arguments = options.ExcelProcessorArgs ?? moduleInfo.Arguments;
                
                return await ExecuteModuleAsync(executablePath, arguments, moduleInfo.Name, cancellationToken);
            }
            catch (Exception ex)
            {
                LogError($"Failed to run Excel Processor: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Runs only the Database Loader module
        /// </summary>
        public async Task<bool> RunDatabaseLoaderAsync(ETLProcessOptions? options = null, CancellationToken cancellationToken = default)
        {
            options ??= new ETLProcessOptions();
            
            LogInfo("═══════════════════════════════════════════════════════════════");
            LogInfo("STEP 3: Database Loading");
            LogInfo("═══════════════════════════════════════════════════════════════");

            try
            {
                string executablePath = _configManager.GetExecutablePath("databaseloader");
                var moduleInfo = _configManager.GetConfiguration().ExecutableModules.DatabaseLoader;
                
                string arguments = options.DatabaseLoaderArgs ?? moduleInfo.Arguments;
                
                return await ExecuteModuleAsync(executablePath, arguments, moduleInfo.Name, cancellationToken);
            }
            catch (Exception ex)
            {
                LogError($"Failed to run Database Loader: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Runs only the CSV to Database module
        /// </summary>
        public async Task<bool> RunCsvToDatabaseAsync(ETLProcessOptions? options = null, CancellationToken cancellationToken = default)
        {
            options ??= new ETLProcessOptions();
            
            LogInfo("═══════════════════════════════════════════════════════════════");
            LogInfo("CSV to Database Processing");
            LogInfo("═══════════════════════════════════════════════════════════════");

            try
            {
                string executablePath = _configManager.GetExecutablePath("csvtodatabase");
                var moduleInfo = _configManager.GetConfiguration().ExecutableModules.CsvToDatabase;
                
                string arguments = options.CsvToDatabaseArgs ?? moduleInfo.Arguments;
                
                return await ExecuteModuleAsync(executablePath, arguments, moduleInfo.Name, cancellationToken);
            }
            catch (Exception ex)
            {
                LogError($"Failed to run CSV to Database: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes a module with the specified parameters
        /// </summary>
        private async Task<bool> ExecuteModuleAsync(string executablePath, string arguments, string moduleName, CancellationToken cancellationToken = default)
        {
            Process? process = null;
            try
            {
                if (!File.Exists(executablePath))
                {
                    LogError($"{moduleName} executable not found: {executablePath}");
                    LogError("Please build the solution first.");
                    return false;
                }

                LogInfo($"Starting {moduleName}...");
                LogInfo($"Executable: {executablePath}");
                LogInfo($"Arguments: {arguments}");

                // Dynamic Table Manager requires user interaction - run in separate window
                if (moduleName.Contains("Dynamic Table Manager", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
                    Console.WriteLine("║                    USER INPUT REQUIRED                       ║");
                    Console.WriteLine("║               Opening in separate window...                   ║");
                    Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
                    Console.ResetColor();

                    var startInfo = new ProcessStartInfo(executablePath, arguments)
                    {
                        UseShellExecute = true,
                        WorkingDirectory = _configManager.GetRootDirectory()
                    };

                    process = Process.Start(startInfo);
                    if (process != null)
                    {
                        // Wait for process with cancellation support
                        while (!process.HasExited)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                LogWarning($"Cancellation requested. Terminating {moduleName}...");
                                process.Kill(entireProcessTree: true);
                                LogWarning($"{moduleName} terminated.");
                                return false;
                            }
                            await Task.Delay(100, cancellationToken);
                        }
                        return process.ExitCode == 0;
                    }
                    return false;
                }

                // Non-interactive modules run with output capture
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = _configManager.GetRootDirectory()
                };

                process = new Process { StartInfo = processStartInfo };
                
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        LogInfo($"[{moduleName}] {e.Data}");
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        LogError($"[{moduleName}] {e.Data}");
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                // Wait for process with cancellation support
                while (!process.HasExited)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        LogWarning($"Cancellation requested. Terminating {moduleName}...");
                        process.Kill(entireProcessTree: true);
                        LogWarning($"{moduleName} terminated.");
                        return false;
                    }
                    await Task.Delay(100, cancellationToken);
                }

                if (process.ExitCode == 0)
                {
                    LogSuccess($"{moduleName} completed successfully");
                    return true;
                }
                else
                {
                    LogError($"{moduleName} failed with exit code: {process.ExitCode}");
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                LogWarning($"{moduleName} was cancelled");
                if (process != null && !process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
                return false;
            }
            catch (Exception ex)
            {
                LogError($"Error executing {moduleName}: {ex.Message}");
                return false;
            }
            finally
            {
                process?.Dispose();
            }
        }

        /// <summary>
        /// Displays the application header
        /// </summary>
        private void DisplayHeader()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                DATAFORGE ETL ORCHESTRATOR v2.0.0             ║");
            Console.WriteLine("║                                                               ║");
            Console.WriteLine("║  Centralized ETL Process Management with Unified Config      ║");
            Console.WriteLine("║  Location-agnostic • Self-contained • Environment-friendly   ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        /// <summary>
        /// Displays the completion summary
        /// </summary>
        private void DisplayCompletionSummary(bool success, TimeSpan duration)
        {
            Console.WriteLine();
            Console.ForegroundColor = success ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║                   {(success ? "PROCESS COMPLETED SUCCESSFULLY" : "PROCESS COMPLETED WITH ERRORS")}              ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            
            Console.WriteLine($"Total Execution Time: {duration:hh\\:mm\\:ss}");
            Console.WriteLine($"Log File: {_logFilePath}");
            Console.WriteLine($"Root Directory: {_configManager.GetRootDirectory()}");
        }

        /// <summary>
        /// Logs an informational message
        /// </summary>
        private void LogInfo(string message)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [INFO] {message}";
            Console.WriteLine(logMessage);
            AppendToLogFile(logMessage);
        }

        /// <summary>
        /// Logs a success message
        /// </summary>
        private void LogSuccess(string message)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [SUCCESS] {message}";
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(logMessage);
            Console.ResetColor();
            AppendToLogFile(logMessage);
        }

        /// <summary>
        /// Logs an error message
        /// </summary>
        private void LogError(string message)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ERROR] {message}";
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(logMessage);
            Console.ResetColor();
            AppendToLogFile(logMessage);
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        private void LogWarning(string message)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [WARNING] {message}";
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(logMessage);
            Console.ResetColor();
            AppendToLogFile(logMessage);
        }

        /// <summary>
        /// Appends a message to the log file
        /// </summary>
        private void AppendToLogFile(string message)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath) ?? "");
                File.AppendAllText(_logFilePath, message + Environment.NewLine);
            }
            catch
            {
                // Ignore file logging errors to prevent cascading failures
            }
        }
    }

    /// <summary>
    /// Options for controlling ETL process execution
    /// </summary>
    public class ETLProcessOptions
    {
        public bool SkipDynamicTableConfig { get; set; } = false;
        public bool ContinueOnError { get; set; } = false;
        public string? DynamicTableManagerArgs { get; set; }
        public string? ExcelProcessorArgs { get; set; }
        public string? DatabaseLoaderArgs { get; set; }
        public string? CsvToDatabaseArgs { get; set; }
    }
}