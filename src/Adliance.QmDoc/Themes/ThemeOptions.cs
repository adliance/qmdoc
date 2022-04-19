using System.Text.Json.Serialization;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Adliance.QmDoc.Themes
{
    public class ThemeOptions
    {
        [JsonPropertyName("pdf")] public PdfSettings Pdf { get; set; } = new PdfSettings();

        public class PdfSettings
        {
            [JsonPropertyName("header_height")] public int HeaderHeight { get; set; }
            [JsonPropertyName("footer_height")] public int FooterHeight { get; set; }
        }
    }
}