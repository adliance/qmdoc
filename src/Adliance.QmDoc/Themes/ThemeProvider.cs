using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Adliance.QmDoc.Options;

namespace Adliance.QmDoc.Themes;

public static class ThemeProvider
{
    public static string GetHeader(string theme)
    {
        return GetContent(theme, "header.html") ?? GetEmbeddedContent(theme, "header.html") ?? "";
    }

    public static string GetFooter(string theme)
    {
        return GetContent(theme, "footer.html") ?? GetEmbeddedContent(theme, "footer.html") ?? "";
    }

    public static string GetContent(string theme)
    {
        return GetContent(theme, "index.html") ?? GetEmbeddedContent(theme, "index.html") ?? "";
    }

    public static string? GetCss(string theme)
    {
        return GetAllCssFromFilesystem(theme) ?? GetAllEmbeddedCss(theme);
    }

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

    public static ThemeOptions GetOptions(string theme)
    {
        var json = GetContent(theme, "options.json") ?? GetEmbeddedContent(theme, "options.json");
        if (!string.IsNullOrWhiteSpace(json))
        {
            return JsonSerializer.Deserialize<ThemeOptions>(json) ?? new ThemeOptions();
        }

        return new ThemeOptions();
    }


    private static string? _themeErrorLastTheme = "";
    private static string? GetEmbeddedContent(string theme, string fileName, bool printWarning = true)
    {
        if (Regex.IsMatch(theme, "^\\d")) theme = "_" + theme; // if the theme name starts with a digit, the resource name starts with a _ automatically

        var name = "Adliance.QmDoc.Themes." + theme + "." + fileName;
        var stream = typeof(ThemeProvider).Assembly.GetManifestResourceStream(name);

        if (stream == null)
        {
            if (printWarning && _themeErrorLastTheme != theme)
            {
                Program.WriteLine($"\tTheme \"{theme}\" not found. Falling back to default theme");
                _themeErrorLastTheme = theme;
            }

            name = "Adliance.QmDoc.Themes.Default." + fileName;
            stream = typeof(ThemeProvider).Assembly.GetManifestResourceStream(name);
        }

        if (stream == null) return null;
        using var sr = new StreamReader(stream);
        return sr.ReadToEnd();
    }

    private static string? GetContent(string theme, string fileName)
    {
        var filePath = Path.Combine(OptionsProvider.DataDirectory, "themes", theme, fileName);
        return File.Exists(filePath) ? File.ReadAllText(filePath) : null;
    }
}
