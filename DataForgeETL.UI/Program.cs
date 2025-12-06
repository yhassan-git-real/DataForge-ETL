using Avalonia;
using System;

namespace DataForgeETL.UI;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Check if console mode is requested
        if (args.Length > 0 && (args[0] == "--console" || args[0] == "--cli"))
        {
            RunConsoleMode(args);
            return;
        }

        // Run Avalonia UI mode
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    /// <summary>
    /// Fallback to console mode for automation/scripting
    /// </summary>
    private static void RunConsoleMode(string[] args)
    {
        Console.WriteLine("Running in console mode...");
        Console.WriteLine("For console operations, please use the standalone Core/DataForgeETL.exe");
        Console.WriteLine("This UI application is designed for interactive desktop use.");
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
