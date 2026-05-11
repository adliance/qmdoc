using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Adliance.QmDoc;

public class UpdateService
{

    public async Task Run()
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

        await process.WaitForExitAsync();
    }
}
