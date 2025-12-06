// Create new file: Models/ScreenStateConfig.cs
namespace ETL_Excel.Models
{
    public class ScreenStateConfig
    {
        public bool PreventSleep { get; set; } = true;  // Default to true to prevent sleep
        public int CheckInterval { get; set; } = 30;    // Check every 30 seconds
    }
}