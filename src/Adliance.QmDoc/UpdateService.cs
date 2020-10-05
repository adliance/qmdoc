using System;
using System.Diagnostics;

namespace Adliance.QmDoc
{
    public class UpdateService
    {

        public void Run()
        {
            var pi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "tool update -g Adliance.QmDoc --ignore-failed-sources --no-cache"
            };
            
            var currentConsoleColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            var process = Process.Start(pi);
            if (process == null)
            {
                throw new Exception("Process is null.");
            }

            process.WaitForExit();
            Console.ForegroundColor = currentConsoleColor;
            
            if (process.ExitCode != 0)
            {
                throw new Exception($"Update failed (exit code {process.ExitCode}.");
            }
        }
    }
}