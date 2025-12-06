using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataForgeETL.Core;
using DataForgeETL.UI.Services;

namespace DataForgeETL.UI.ViewModels
{
    /// <summary>
    /// Enhanced ETL Orchestrator that integrates with UI logger and progress reporter
    /// </summary>
    public class ETLOrchestratorWithLogger
    {
        private readonly IUILogger _logger;
        private readonly IProgressReporter _progressReporter;
        private readonly UnifiedConfigurationManager _configManager;

        public ETLOrchestratorWithLogger(IUILogger logger, IProgressReporter progressReporter)
        {
            _logger = logger;
            _progressReporter = progressReporter;
            _configManager = UnifiedConfigurationManager.Instance;
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
                _configManager.EnsureDirectoriesExist();

                _logger.LogInfo("Starting complete ETL process...", "etl");
                _progressReporter.ReportProgress(0, "Initializing...");

                var stopwatch = Stopwatch.StartNew();
                bool success = true;

                // Step 1: Dynamic Table Configuration (10% - 30%)
                if (!options.SkipDynamicTableConfig)
                {
                    _progressReporter.ReportProgress(10, "Step 1: Dynamic Table Configuration");
                    success = await RunDynamicTableManagerAsync(options, cancellationToken);
                    _progressReporter.ReportProgress(30, "Dynamic Table Configuration completed");
                    
                    if (!success && !options.ContinueOnError)
                    {
                        _logger.LogError("Dynamic Table Manager failed. Stopping process.");
                        return false;
                    }
                }

                // Step 2: Excel Processing (30% - 70%)
                if (success || options.ContinueOnError)
                {
                    _progressReporter.ReportProgress(30, "Step 2: Excel Processing");
                    success = await RunExcelProcessorAsync(options, cancellationToken);
                    _progressReporter.ReportProgress(70, "Excel Processing completed");
                    
                    if (!success && !options.ContinueOnError)
                    {
                        _logger.LogError("Excel Processor failed. Stopping process.");
                        return false;
                    }
                }

                // Step 3: Database Loading (70% - 100%)
                if (success || options.ContinueOnError)
                {
                    _progressReporter.ReportProgress(70, "Step 3: Database Loading");
                    success = await RunDatabaseLoaderAsync(options, cancellationToken);
                    _progressReporter.ReportProgress(100, "Database Loading completed");
                }

                stopwatch.Stop();

                if (success)
                {
                    _logger.LogSuccess($"Complete ETL process completed successfully in {stopwatch.Elapsed:hh\\:mm\\:ss}");
                    _progressReporter.ReportComplete(true, stopwatch.Elapsed, "All modules executed successfully");
                }
                else
                {
                    _logger.LogError($"ETL process completed with errors in {stopwatch.Elapsed:hh\\:mm\\:ss}");
                    _progressReporter.ReportComplete(false, stopwatch.Elapsed, "Process completed with errors");
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError($"ETL process failed with exception: {ex.Message}");
                _progressReporter.ReportError(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Runs only the Dynamic Table Manager
        /// </summary>
        public async Task<bool> RunDynamicTableManagerAsync(ETLProcessOptions? options = null, CancellationToken cancellationToken = default)
        {
            options ??= new ETLProcessOptions();

            _logger.LogInfo("=================================================================", "etl");
            _logger.LogInfo("STEP 1: Dynamic Table Configuration", "etl");
            _logger.LogInfo("=================================================================", "etl");

            try
            {
                string executablePath = _configManager.GetExecutablePath("dynamictablemanager");
                var moduleInfo = _configManager.GetConfiguration().ExecutableModules.DynamicTableManager;

                string arguments = options.DynamicTableManagerArgs ?? moduleInfo.Arguments;

                return await ExecuteModuleAsync(executablePath, arguments, moduleInfo.Name, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to run Dynamic Table Manager: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Runs only the Excel Processor
        /// </summary>
        public async Task<bool> RunExcelProcessorAsync(ETLProcessOptions? options = null, CancellationToken cancellationToken = default)
        {
            options ??= new ETLProcessOptions();

            _logger.LogInfo("=================================================================", "etl");
            _logger.LogInfo("STEP 2: Excel Processing", "etl");
            _logger.LogInfo("=================================================================", "etl");

            try
            {
                string executablePath = _configManager.GetExecutablePath("excelprocessor");
                var moduleInfo = _configManager.GetConfiguration().ExecutableModules.ExcelProcessor;

                string arguments = options.ExcelProcessorArgs ?? moduleInfo.Arguments;

                return await ExecuteModuleAsync(executablePath, arguments, moduleInfo.Name, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to run Excel Processor: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Runs only the Database Loader
        /// </summary>
        public async Task<bool> RunDatabaseLoaderAsync(ETLProcessOptions? options = null, CancellationToken cancellationToken = default)
        {
            options ??= new ETLProcessOptions();

            _logger.LogInfo("=================================================================", "etl");
            _logger.LogInfo("STEP 3: Database Loading", "etl");
            _logger.LogInfo("=================================================================", "etl");

            try
            {
                string executablePath = _configManager.GetExecutablePath("databaseloader");
                var moduleInfo = _configManager.GetConfiguration().ExecutableModules.DatabaseLoader;

                string arguments = options.DatabaseLoaderArgs ?? moduleInfo.Arguments;

                return await ExecuteModuleAsync(executablePath, arguments, moduleInfo.Name, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to run Database Loader: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Runs only the CSV to Database module
        /// </summary>
        public async Task<bool> RunCsvToDatabaseAsync(ETLProcessOptions? options = null, CancellationToken cancellationToken = default)
        {
            options ??= new ETLProcessOptions();

            _logger.LogInfo("=================================================================", "etl");
            _logger.LogInfo("CSV to Database Processing", "etl");
            _logger.LogInfo("=================================================================", "etl");

            try
            {
                string executablePath = _configManager.GetExecutablePath("csvtodatabase");
                var moduleInfo = _configManager.GetConfiguration().ExecutableModules.CsvToDatabase;

                string arguments = options.CsvToDatabaseArgs ?? moduleInfo.Arguments;

                return await ExecuteModuleAsync(executablePath, arguments, moduleInfo.Name, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to run CSV to Database: {ex.Message}");
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
                    _logger.LogError($"{moduleName} executable not found: {executablePath}");
                    _logger.LogError("Please build the solution first.");
                    return false;
                }

                _logger.LogInfo($"Starting {moduleName}...", "etl");
                _logger.LogInfo($"Executable: {executablePath}", "etl");
                _logger.LogInfo($"Arguments: {arguments}", "etl");

                // Dynamic Table Manager requires user interaction - run in separate window
                if (moduleName.Contains("Dynamic Table Manager", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Dynamic Table Manager requires user interaction - opening in separate window...");

                    var startInfo = new ProcessStartInfo(executablePath, arguments)
                    {
                        UseShellExecute = true,
                        WorkingDirectory = _configManager.GetRootDirectory()
                    };

                    process = Process.Start(startInfo);
                    if (process != null)
                    {
                        // Polling loop to support cancellation
                        while (!process.HasExited)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                _logger.LogWarning($"Cancellation requested. Terminating {moduleName}...");
                                try
                                {
                                    process.Kill(entireProcessTree: true);
                                    _logger.LogWarning($"{moduleName} terminated.");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError($"Error terminating {moduleName}: {ex.Message}");
                                }
                                return false;
                            }
                            
                            await Task.Delay(100, CancellationToken.None); // Don't pass cancellation token to Delay
                        }
                        
                        bool success = process.ExitCode == 0;
                        
                        if (success)
                            _logger.LogSuccess($"{moduleName} completed successfully");
                        else
                            _logger.LogError($"{moduleName} failed with exit code: {process.ExitCode}");
                        
                        return success;
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
                    WorkingDirectory = _configManager.GetRootDirectory(),
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                };

                process = new Process { StartInfo = processStartInfo };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        _logger.LogInfo($"[{moduleName}] {e.Data}", "module");
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        _logger.LogError($"[{moduleName}] {e.Data}");
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                // Polling loop to support cancellation
                while (!process.HasExited)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning($"Cancellation requested. Terminating {moduleName}...");
                        try
                        {
                            process.Kill(entireProcessTree: true);
                            _logger.LogWarning($"{moduleName} terminated.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error terminating {moduleName}: {ex.Message}");
                        }
                        return false;
                    }
                    
                    await Task.Delay(100, CancellationToken.None); // Don't pass cancellation token to Delay
                }

                if (process.ExitCode == 0)
                {
                    _logger.LogSuccess($"{moduleName} completed successfully");
                    return true;
                }
                else
                {
                    _logger.LogError($"{moduleName} failed with exit code: {process.ExitCode}");
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning($"{moduleName} was cancelled.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing {moduleName}: {ex.Message}");
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
            _logger.LogInfo("=================================================================", "header");
            _logger.LogInfo("           DATAFORGE ETL - Modern ETL Manager v2.0.0          ", "header");
            _logger.LogInfo("                                                               ", "header");
            _logger.LogInfo("  Centralized ETL Process Management with Unified Config      ", "header");
            _logger.LogInfo("  Location-agnostic • Self-contained • Environment-friendly   ", "header");
            _logger.LogInfo("=================================================================", "header");
        }
    }
}
