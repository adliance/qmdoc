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
                Arguments = "tool update -g Adliance.QmDoc --ignore-failed-sources --no-cache",
                WindowStyle = ProcessWindowStyle.Normal,
                UseShellExecute = true,
                CreateNoWindow = false
            };
            
            var process = Process.Start(pi);
            if (process == null)
            {
                throw new Exception("Process is null.");
            }
        }
    }
}