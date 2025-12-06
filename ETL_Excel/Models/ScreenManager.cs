// Create new file: Modules/ScreenManager.cs
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ETL_Excel.Models;

namespace ETL_Excel.Modules
{
    public class ScreenManager : IDisposable
    {
        // Windows API constants for power management
        private const uint ES_CONTINUOUS = 0x80000000;
        private const uint ES_SYSTEM_REQUIRED = 0x00000001;
        private const uint ES_DISPLAY_REQUIRED = 0x00000002;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ScreenStateConfig _config;
        private bool _isRunning;

        // Import the necessary Windows API function
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint SetThreadExecutionState(uint esFlags);

        public ScreenManager(ScreenStateConfig? config = null)
        {
            _config = config ?? new ScreenStateConfig();
            _cancellationTokenSource = new CancellationTokenSource();
            _isRunning = false;
        }

        public void StartPreventSleep()
        {
            if (!_config.PreventSleep || _isRunning) return;

            _isRunning = true;
            Console.WriteLine("🖥️ Screen sleep prevention activated");

            // Start the background task to keep the screen awake
            Task.Run(async () =>
            {
                try
                {
                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        // Set the execution state to prevent sleep
                        SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED);

                        // Wait for the specified interval before setting the state again
                        await Task.Delay(TimeSpan.FromSeconds(_config.CheckInterval), _cancellationTokenSource.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal cancellation, no action needed
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Warning: Screen management encountered an error: {ex.Message}");
                }
                finally
                {
                    // Reset the execution state when done
                    SetThreadExecutionState(ES_CONTINUOUS);
                }
            });
        }

        public void StopPreventSleep()
        {
            if (!_isRunning) return;

            _cancellationTokenSource.Cancel();
            _isRunning = false;
            SetThreadExecutionState(ES_CONTINUOUS); // Reset to normal state
            Console.WriteLine("🖥️ Screen sleep prevention deactivated");
        }

        public void Dispose()
        {
            StopPreventSleep();
            _cancellationTokenSource.Dispose();
        }
    }
}
