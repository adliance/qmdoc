using System;
using System.Text.Json.Serialization;

namespace Adliance.QmDoc.Options;

public class Options
{
    [JsonPropertyName("theme")] public string Theme { get; set; } = "Default";
    [JsonPropertyName("theme_refreshed")] public DateTime ThemeRefreshedUtc { get; set; }
}
