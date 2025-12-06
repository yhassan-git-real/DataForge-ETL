using System;
using System.Threading.Tasks;
using DataForgeETL.Core;

namespace DataForgeETL
{
    /// <summary>
    /// Main entry point for the Universal Excel Tool with unified configuration
    /// </summary>
    internal static class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Set console encoding to handle Unicode characters properly
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
            
            try
            {
                // Parse command line arguments
                var options = ParseCommandLineArguments(args);
                
                // Handle special commands
                if (options.ShowConfig)
                {
                    UnifiedConfigurationManager.Instance.DisplayConfigurationSummary();
                    return 0;
                }
                
                if (options.ShowHelp)
                {
                    DisplayHelp();
                    return 0;
                }
                
                if (!string.IsNullOrEmpty(options.UpdateRootDirectory))
                {
                    UnifiedConfigurationManager.Instance.UpdateRootDirectory(options.UpdateRootDirectory);
                    Console.WriteLine($"✓ Root directory updated to: {options.UpdateRootDirectory}");
                    return 0;
                }

                // Ensure all required directories exist before starting any operations
                UnifiedConfigurationManager.Instance.EnsureDirectoriesExist();

                // Create and run the orchestrator
                var orchestrator = new ETLOrchestrator();
                
                var processOptions = new ETLProcessOptions
                {
                    SkipDynamicTableConfig = options.SkipDynamicTableConfig,
                    ContinueOnError = options.ContinueOnError,
                    DynamicTableManagerArgs = options.DynamicTableManagerArgs,
                    ExcelProcessorArgs = options.ExcelProcessorArgs,
                    DatabaseLoaderArgs = options.DatabaseLoaderArgs
                };

                bool success;
                
                if (options.RunDynamicTableManagerOnly)
                {
                    success = await orchestrator.RunDynamicTableManagerAsync(processOptions);
                }
                else if (options.RunExcelProcessorOnly)
                {
                    success = await orchestrator.RunExcelProcessorAsync(processOptions);
                }
                else if (options.RunDatabaseLoaderOnly)
                {
                    success = await orchestrator.RunDatabaseLoaderAsync(processOptions);
                }
                else
                {
                    success = await orchestrator.RunCompleteETLProcessAsync(processOptions);
                }

                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\\nFatal error: {ex.Message}");
                if (args.Contains("--verbose") || args.Contains("-v"))
                {
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                Console.ResetColor();
                return 1;
            }
        }

        /// <summary>
        /// Parses command line arguments
        /// </summary>
        private static CommandLineOptions ParseCommandLineArguments(string[] args)
        {
            var options = new CommandLineOptions();
            
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLowerInvariant();
                
                switch (arg)
                {
                    case "--help":
                    case "-h":
                        options.ShowHelp = true;
                        break;
                        
                    case "--show-config":
                    case "-c":
                        options.ShowConfig = true;
                        break;
                        
                    case "--skip-dynamic-config":
                    case "-s":
                        options.SkipDynamicTableConfig = true;
                        break;
                        
                    case "--continue-on-error":
                        options.ContinueOnError = true;
                        break;
                        
                    case "--dynamic-table-only":
                        options.RunDynamicTableManagerOnly = true;
                        break;
                        
                    case "--excel-only":
                        options.RunExcelProcessorOnly = true;
                        break;
                        
                    case "--database-only":
                        options.RunDatabaseLoaderOnly = true;
                        break;
                        
                    case "--root-directory":
                        if (i + 1 < args.Length)
                        {
                            options.UpdateRootDirectory = args[++i];
                        }
                        break;
                        
                    case "--dynamic-args":
                        if (i + 1 < args.Length)
                        {
                            options.DynamicTableManagerArgs = args[++i];
                        }
                        break;
                        
                    case "--excel-args":
                        if (i + 1 < args.Length)
                        {
                            options.ExcelProcessorArgs = args[++i];
                        }
                        break;
                        
                    case "--database-args":
                        if (i + 1 < args.Length)
                        {
                            options.DatabaseLoaderArgs = args[++i];
                        }
                        break;
                }
            }
            
            return options;
        }

        /// <summary>
        /// Displays help information
        /// </summary>
        private static void DisplayHelp()
        {
            Console.WriteLine();
            Console.WriteLine("Universal Excel Tool - Unified ETL Process Manager");
            Console.WriteLine("================================================");
            Console.WriteLine();
            Console.WriteLine("USAGE:");
            Console.WriteLine("  UniversalExcelTool.exe [OPTIONS]");
            Console.WriteLine();
            Console.WriteLine("OPTIONS:");
            Console.WriteLine("  --help, -h                    Show this help message");
            Console.WriteLine("  --show-config, -c             Display current configuration");
            Console.WriteLine("  --skip-dynamic-config, -s     Skip dynamic table configuration");
            Console.WriteLine("  --continue-on-error           Continue process even if a module fails");
            Console.WriteLine();
            Console.WriteLine("  --dynamic-table-only          Run only the Dynamic Table Manager");
            Console.WriteLine("  --excel-only                  Run only the Excel Processor");
            Console.WriteLine("  --database-only               Run only the Database Loader");
            Console.WriteLine();
            Console.WriteLine("  --root-directory <path>       Update the root directory path");
            Console.WriteLine("  --dynamic-args <args>         Arguments for Dynamic Table Manager");
            Console.WriteLine("  --excel-args <args>           Arguments for Excel Processor");
            Console.WriteLine("  --database-args <args>        Arguments for Database Loader");
            Console.WriteLine();
            Console.WriteLine("EXAMPLES:");
            Console.WriteLine("  # Run complete ETL process");
            Console.WriteLine("  UniversalExcelTool.exe");
            Console.WriteLine();
            Console.WriteLine("  # Run with existing dynamic configuration");
            Console.WriteLine("  UniversalExcelTool.exe --skip-dynamic-config");
            Console.WriteLine();
            Console.WriteLine("  # Update root directory for new environment");
            Console.WriteLine("  UniversalExcelTool.exe --root-directory \"YOUR_PROJECT_PATH\"");
            Console.WriteLine();
            Console.WriteLine("  # Run only table configuration");
            Console.WriteLine("  UniversalExcelTool.exe --dynamic-table-only");
            Console.WriteLine();
            Console.WriteLine("  # Show current configuration");
            Console.WriteLine("  UniversalExcelTool.exe --show-config");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Command line options
    /// </summary>
    internal class CommandLineOptions
    {
        public bool ShowHelp { get; set; }
        public bool ShowConfig { get; set; }
        public bool SkipDynamicTableConfig { get; set; }
        public bool ContinueOnError { get; set; }
        public bool RunDynamicTableManagerOnly { get; set; }
        public bool RunExcelProcessorOnly { get; set; }
        public bool RunDatabaseLoaderOnly { get; set; }
        public string? UpdateRootDirectory { get; set; }
        public string? DynamicTableManagerArgs { get; set; }
        public string? ExcelProcessorArgs { get; set; }
        public string? DatabaseLoaderArgs { get; set; }
    }
}