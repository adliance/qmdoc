using System.IO;
using System.Text.Json;
using Adliance.QmDoc.Options;
using Microsoft.AspNetCore.Routing;

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

    public static string GetScss(string theme)
    {
        return GetContent(theme, "style.scss") ?? GetEmbeddedContent(theme, "style.scss") ?? "";
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

    private static string? GetEmbeddedContent(string theme, string fileName)
    {
        var name = "Adliance.QmDoc.Themes." + theme + "." + fileName;
        var stream = typeof(ThemeProvider).Assembly.GetManifestResourceStream(name);

        if (stream == null)
        {
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
