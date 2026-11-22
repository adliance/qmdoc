using System.Text.Json.Serialization;

namespace Adliance.QmDoc.Options;

public class Options
{
    [JsonPropertyName("theme")] public string Theme { get; set; } = Themes.ThemeProvider.DefaultTheme;
}
