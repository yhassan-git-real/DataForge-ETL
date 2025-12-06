using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DataForgeETL.UI.Services
{
    public class FileBrowserService
    {
        public async Task<string?> BrowseFolderAsync(string description, string defaultPath = "")
        {
            // Escape backslashes for the PowerShell string
            var escapedPath = defaultPath.Replace("\\", "\\\\");
            
            var script = $@"
                Add-Type -AssemblyName System.Windows.Forms
                $folderBrowser = New-Object System.Windows.Forms.FolderBrowserDialog
                $folderBrowser.Description = '{description}'
                $folderBrowser.ShowNewFolderButton = $true
                if ('{escapedPath}' -ne '' -and (Test-Path '{escapedPath}')) {{
                    $folderBrowser.SelectedPath = '{escapedPath}'
                }}
                $result = $folderBrowser.ShowDialog()
                if ($result -eq 'OK') {{
                    Write-Output $folderBrowser.SelectedPath
                }}
                else {{
                    Write-Output 'CANCELLED'
                }}
            ";

            return await RunPowerShellScriptAsync(script);
        }

        public async Task<string?> BrowseFileAsync(string filter, string defaultPath = "")
        {
            // Escape backslashes for the PowerShell string
            var escapedPath = defaultPath.Replace("\\", "\\\\");
            
            var script = $@"
                Add-Type -AssemblyName System.Windows.Forms
                $fileBrowser = New-Object System.Windows.Forms.OpenFileDialog
                $fileBrowser.Filter = '{filter}'
                if ('{escapedPath}' -ne '' -and (Test-Path '{escapedPath}')) {{
                    $fileBrowser.InitialDirectory = '{escapedPath}'
                }}
                $result = $fileBrowser.ShowDialog()
                if ($result -eq 'OK') {{
                    Write-Output $fileBrowser.FileName
                }}
                else {{
                    Write-Output 'CANCELLED'
                }}
            ";

            return await RunPowerShellScriptAsync(script);
        }

        private async Task<string?> RunPowerShellScriptAsync(string script)
        {
            try
            {
                // Replace newlines with semicolons to ensure commands are separated correctly
                script = script.Replace(Environment.NewLine, "; ").Replace("\n", "; ");
                
                var processInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    // Add -Sta for Windows Forms compatibility
                    Arguments = $"-NoProfile -Sta -ExecutionPolicy Bypass -Command \"{script}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true, // Capture errors too
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (!string.IsNullOrWhiteSpace(error))
                {
                    // Log error or handle it? For now, just return null so we don't fill the textbox with error text
                    System.Diagnostics.Debug.WriteLine($"PowerShell Error: {error}");
                    return null;
                }

                var result = output.Trim();
                return result == "CANCELLED" || string.IsNullOrWhiteSpace(result) ? null : result;
            }
            catch
            {
                return null;
            }
        }
    }
}
