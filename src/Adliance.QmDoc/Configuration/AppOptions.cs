using System;
using System.Text.Json.Serialization;

namespace Adliance.QmDoc.Configuration
{
    public class AppOptions
    {
        [JsonPropertyName("theme")] public string Theme { get; set; } = "";
        [JsonPropertyName("theme_refreshed")] public DateTime ThemeRefreshedUtc { get; set; }
    }
}