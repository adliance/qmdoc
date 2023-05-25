using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Adliance.QmDoc.Options;

namespace Adliance.QmDoc.Themes;

public static class ThemeFetcher
{
    public static async Task Fetch(string theme)
    {
        await DownloadAndSave(theme, "README.md");
        await DownloadAndSave(theme, "footer.html");
        await DownloadAndSave(theme, "header.html");
        await DownloadAndSave(theme, "index.html");
        await DownloadAndSave(theme, "style.scss");
        await DownloadAndSave(theme, "options.json");
    }

    private static async Task DownloadAndSave(string theme, string fileName)
    {
        var content = await Download(theme, fileName);
        if (content != null)
        {
            var directory = Path.Combine(OptionsProvider.DataDirectory, "themes", theme);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var filePath = Path.Combine(directory, fileName);
            if (!File.Exists(filePath) || content != File.ReadAllText(filePath))
            {
                await File.WriteAllTextAsync(filePath, content);
                Console.WriteLine($"Updated file \"{fileName}\" of theme \"{theme}\".");
            }
        }
    }

    private static async Task<string?> Download(string theme, string fileName)
    {
        using (var client = new HttpClient())
        {
            var response = await client.GetAsync($"https://raw.githubusercontent.com/adliance/qmdoc/master/themes/{theme}/{fileName}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            return null;
        }
    }
}