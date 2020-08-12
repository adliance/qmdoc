using System.Text.Json.Serialization;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Adliance.QmDoc.Themes
{
    public class ThemeOptions
    {
        [JsonPropertyName("pdf")] public PdfSettings Pdf { get; set; } = new PdfSettings();

        public class PdfSettings
        {
            [JsonPropertyName("margin_top")] public int MarginTop { get; set; }
            [JsonPropertyName("margin_bottom")] public int MarginBottom { get; set; }
            [JsonPropertyName("margin_left")] public int MarginLeft { get; set; }
            [JsonPropertyName("margin_right")] public int MarginRight { get; set; }
            [JsonPropertyName("header_spacing")] public int HeaderSpacing { get; set; }
            [JsonPropertyName("footer_spacing")] public int FooterSpacing { get; set; }
        }
    }
}