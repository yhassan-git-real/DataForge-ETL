using System;
using System.IO;
using System.Text;

namespace ETL_Excel.Modules
{
    public class ConsoleLogger
    {
        private readonly string _logFolderPath;
        private readonly StringBuilder _consoleOutput;
        private int _captureFrequency;
        private int _operationCount;

        public ConsoleLogger(string logFolderPath, int captureFrequency = 100)
        {
            _logFolderPath = logFolderPath;
            _consoleOutput = new StringBuilder();
            _captureFrequency = captureFrequency;
            _operationCount = 0;
        }

        public void CaptureConsoleOutput(string output, bool forceCapture = false)
        {
            Console.WriteLine(output);
            _operationCount++;

            if (forceCapture || _operationCount % _captureFrequency == 0)
            {
                _consoleOutput.AppendLine(output);
            }
        }

        public void SaveFinalOutput()
        {
            string fileName = $"Console_output_log_{DateTime.Now:yyyyMMdd}.txt";
            string filePath = Path.Combine(_logFolderPath, fileName);

            File.WriteAllText(filePath, _consoleOutput.ToString(), new UTF8Encoding(false));
            Console.WriteLine($"Console output saved to: {filePath}");
        }
    }
}