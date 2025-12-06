using ETL_DynamicTableManager.Models;

namespace ETL_DynamicTableManager.Services
{
    /// <summary>
    /// Service for handling user input and interaction
    /// </summary>
    public static class UserInputService
    {
        /// <summary>
        /// Gets yes/no input from user
        /// </summary>
        public static UserInputResult GetYesNoInput(string prompt)
        {
            while (true)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("▼▼▼ USER INPUT REQUIRED ▼▼▼");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{prompt} (Y/N): ");
                Console.ResetColor();
                
                string? input = Console.ReadLine()?.Trim().ToUpperInvariant();
                
                if (string.IsNullOrEmpty(input))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Please enter Y or N.");
                    Console.ResetColor();
                    continue;
                }
                
                if (input == "Y" || input == "YES")
                {
                    return new UserInputResult { IsValid = true, Value = "Yes" };
                }
                
                if (input == "N" || input == "NO")
                {
                    return new UserInputResult { IsValid = true, Value = "No" };
                }
                
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid input. Please enter Y for Yes or N for No.");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Gets table name input from user with validation
        /// </summary>
        public static UserInputResult GetTableNameInput(string prompt, bool isNewTable = false)
        {
            while (true)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("▼▼▼ USER INPUT REQUIRED ▼▼▼");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{prompt}: ");
                Console.ResetColor();
                
                string? input = Console.ReadLine()?.Trim();
                
                if (string.IsNullOrEmpty(input))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Table name cannot be empty. Please enter a valid table name.");
                    Console.ResetColor();
                    continue;
                }
                
                // Basic validation for SQL table names
                if (!IsValidTableName(input))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid table name. Please use only letters, numbers, and underscores. Must start with a letter.");
                    Console.ResetColor();
                    continue;
                }
                
                return new UserInputResult { IsValid = true, Value = input };
            }
        }

        /// <summary>
        /// Gets temporary table name input from user
        /// </summary>
        public static UserInputResult GetTempTableNameInput()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("=================================================================");
            Console.WriteLine("                   TEMPORARY TABLE CONFIGURATION               ");
            Console.WriteLine("=================================================================");
            Console.ResetColor();
            
            Console.WriteLine("First, we need to create a temporary table for preloading data.");
            Console.WriteLine("This table will be used to validate and process data before moving to the final destination.");
            
            return GetTableNameInput("Enter the temporary table name (e.g., Temp_YourData)");
        }

        /// <summary>
        /// Asks user if target table exists
        /// </summary>
        public static UserInputResult AskIfTargetTableExists()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("=================================================================");
            Console.WriteLine("                    TARGET TABLE CONFIGURATION                 ");
            Console.WriteLine("=================================================================");
            Console.ResetColor();
            
            Console.WriteLine("Now we need to determine the destination for your data.");
            
            return GetYesNoInput("Does the target table already exist in the database?");
        }

        /// <summary>
        /// Gets existing table name from user
        /// </summary>
        public static UserInputResult GetExistingTableName()
        {
            Console.WriteLine();
            Console.WriteLine("Since the target table exists, please provide the exact table name.");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Note: Table name is case-sensitive and must match exactly as it exists in the database.");
            Console.ResetColor();
            
            return GetTableNameInput("Enter the exact target table name");
        }

        /// <summary>
        /// Asks if user wants to truncate existing table
        /// </summary>
        public static UserInputResult AskToTruncateTable(string tableName)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("=================================================================");
            Console.WriteLine("                      TABLE TRUNCATION OPTION                  ");
            Console.WriteLine("=================================================================");
            Console.ResetColor();
            
            Console.WriteLine($"Target table: {tableName}");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("WARNING: Truncating will delete ALL existing data in the table!");
            Console.ResetColor();
            
            return GetYesNoInput("Do you want to truncate (clear all data from) the existing table before loading new data?");
        }

        /// <summary>
        /// Asks if user wants to create new table
        /// </summary>
        public static UserInputResult AskToCreateNewTable()
        {
            Console.WriteLine();
            Console.WriteLine("Since the target table doesn't exist, we can create it for you.");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("The new table will be created with the same structure as your Excel data (all columns as NVARCHAR(MAX)).");
            Console.ResetColor();
            
            return GetYesNoInput("Do you want to create a new target table based on your Excel data structure?");
        }

        /// <summary>
        /// Gets new table name from user
        /// </summary>
        public static UserInputResult GetNewTableName()
        {
            Console.WriteLine();
            Console.WriteLine("Please provide a name for the new table that will be created.");
            
            return GetTableNameInput("Enter the new target table name", true);
        }

        /// <summary>
        /// Displays configuration summary and asks for confirmation
        /// </summary>
        public static UserInputResult ConfirmConfiguration(DynamicTableConfig config)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("=================================================================");
            Console.WriteLine("                    CONFIGURATION SUMMARY                      ");
            Console.WriteLine("=================================================================");
            Console.ResetColor();
            
            Console.WriteLine($"Temporary Table Name: {config.TempTableName}");
            Console.WriteLine($"Destination Table Name: {config.DestinationTableName}");
            Console.WriteLine($"Target Table Exists: {(config.TargetTableExists ? "Yes" : "No")}");
            
            if (config.TargetTableExists)
            {
                Console.WriteLine($"Truncate Table: {(config.ShouldTruncateTable ? "Yes" : "No")}");
            }
            else
            {
                Console.WriteLine($"Create New Table: {(config.CreateNewTable ? "Yes" : "No")}");
            }
            
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Note: Error and Success log table names are predefined in the main application configuration.");
            Console.ResetColor();
            
            Console.WriteLine();
            return GetYesNoInput("Is this configuration correct?");
        }

        /// <summary>
        /// Validates if a string is a valid SQL table name
        /// </summary>
        private static bool IsValidTableName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return false;
                
            // Must start with a letter or underscore
            if (!char.IsLetter(tableName[0]) && tableName[0] != '_')
                return false;
                
            // Can only contain letters, digits, and underscores
            return tableName.All(c => char.IsLetterOrDigit(c) || c == '_');
        }

        /// <summary>
        /// Displays a header for the dynamic table manager
        /// </summary>
        public static void DisplayHeader()
        {
            try
            {
                Console.Clear();
                Console.Title = "ETL Dynamic Table Manager - USER INPUT REQUIRED";
            }
            catch (System.IO.IOException)
            {
                // Console.Clear() fails when running from orchestrator or redirected output
                // Just add some newlines instead
                Console.WriteLine();
                Console.WriteLine();
            }
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("=================================================================");
            Console.WriteLine("              ETL DYNAMIC TABLE MANAGER v1.0.0                ");
            Console.WriteLine("                                                               ");
            Console.WriteLine("  This tool will help you configure table names dynamically   ");
            Console.WriteLine("  for your ETL process without modifying configuration files  ");
            Console.WriteLine("=================================================================");
            Console.ResetColor();
            Console.WriteLine();
        }

        /// <summary>
        /// Displays completion message
        /// </summary>
        public static void DisplayCompletionMessage(string configFilePath)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("=================================================================");
            Console.WriteLine("                    CONFIGURATION COMPLETE                     ");
            Console.WriteLine("=================================================================");
            Console.ResetColor();
            
            Console.WriteLine($"Dynamic table configuration has been saved to:");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{configFilePath}");
            Console.ResetColor();
            
            Console.WriteLine();
            Console.WriteLine("The ETL process will now use these dynamically configured table names.");
            Console.WriteLine("You can run this tool again anytime to reconfigure the tables.");
        }
    }
}