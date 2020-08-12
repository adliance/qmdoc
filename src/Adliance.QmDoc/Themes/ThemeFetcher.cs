using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Adliance.QmDoc.Configuration;

namespace Adliance.QmDoc.Themes
{
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
                await File.WriteAllTextAsync(Path.Combine(AppOptionsProvider.DataDirectory, "themes", theme, fileName), content);
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
}