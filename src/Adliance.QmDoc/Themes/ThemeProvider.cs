using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Adliance.QmDoc.Options;

namespace Adliance.QmDoc.Themes;

public static class ThemeProvider
{
    public static string GetHeader(string theme) =>
        GetSingleContent(theme, "header.html") ?? "";

    public static string GetFooter(string theme) =>
        GetSingleContent(theme, "footer.html") ?? "";

    public static string GetContent(string theme) =>
        GetSingleContent(theme, "index.html") ?? "";

    public static string? GetCss(string theme)
    {
        var sharedCss = GetAllCssFromFilesystem("_shared") ?? GetAllEmbeddedCss("_shared") ?? "";
        var themeCss = GetAllCssFromFilesystem(theme) ?? GetAllEmbeddedCss(theme) ?? "";
        var combined = sharedCss + themeCss;
        return combined.Length > 0 ? combined : null;
    }

    public static ThemeOptions GetOptions(string theme)
    {
        var json = GetSingleContent(theme, "options.json");
        if (!string.IsNullOrWhiteSpace(json))
        {
            return JsonSerializer.Deserialize<ThemeOptions>(json) ?? new ThemeOptions();
        }

        return new ThemeOptions();
    }

    private static string? GetSingleContent(string theme, string fileName) =>
        GetFileContent(theme, fileName)
        ?? GetEmbeddedContentExact(theme, fileName)
        ?? GetFileContent("_shared", fileName)
        ?? GetEmbeddedContentExact("_shared", fileName);

    private static string? GetAllCssFromFilesystem(string theme)
    {
        var themeDir = Path.Combine(OptionsProvider.DataDirectory, "themes", theme);
        if (!Directory.Exists(themeDir)) return null;

        var cssFiles = Directory.GetFiles(themeDir, "*.css")
            .OrderBy(Path.GetFileName)
            .ToList();

        if (cssFiles.Count == 0) return null;
        return string.Concat(cssFiles.Select(File.ReadAllText));
    }

    private static string? GetAllEmbeddedCss(string theme)
    {
        var embeddedTheme = Regex.IsMatch(theme, "^\\d") ? "_" + theme : theme;
        var prefix = "Adliance.QmDoc.Themes." + embeddedTheme + ".";

        var cssResources = typeof(ThemeProvider).Assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(prefix) && n.EndsWith(".css"))
            .Order()
            .ToList();

        if (cssResources.Count == 0) return null;

        var sb = new StringBuilder();
        foreach (var resourceName in cssResources)
        {
            var stream = typeof(ThemeProvider).Assembly.GetManifestResourceStream(resourceName);
            if (stream == null) continue;
            using var sr = new StreamReader(stream);
            sb.Append(sr.ReadToEnd());
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }

    private static string? GetEmbeddedContentExact(string theme, string fileName)
    {
        if (Regex.IsMatch(theme, "^\\d")) theme = "_" + theme;
        var name = "Adliance.QmDoc.Themes." + theme + "." + fileName;
        var stream = typeof(ThemeProvider).Assembly.GetManifestResourceStream(name);
        if (stream == null) return null;
        using var sr = new StreamReader(stream);
        return sr.ReadToEnd();
    }

    private static string? GetFileContent(string theme, string fileName)
    {
        var filePath = Path.Combine(OptionsProvider.DataDirectory, "themes", theme, fileName);
        return File.Exists(filePath) ? File.ReadAllText(filePath) : null;
    }
}
