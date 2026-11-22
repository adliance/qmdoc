using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.Processors.HtmlProcessors;

/// <summary>
/// Embeds local images into the HTML, so that the HTML (and the PDF service) doesn't need access to the image files.
/// </summary>
public class EmbedImages(string sourceFilePath) : IHtmlProcessor
{
    public HtmlProcessorResult Apply(string html)
    {
        var result = new HtmlProcessorResult(html);

        result.ResultingHtml = Regex.Replace(html, "<img src=\"(.*?)\"", m =>
        {
            var imageUrl = m.Groups[1].Value;
            if (IsRemote(imageUrl)) return m.Value;

            // Markdig percent-encodes spaces and non-ASCII characters ("my%20image.png") and HTML-encodes "&"
            var imagePath = Uri.UnescapeDataString(WebUtility.HtmlDecode(imageUrl));
            var fullFilePath = Path.Combine(Path.GetDirectoryName(sourceFilePath) ?? "", imagePath);
            if (!File.Exists(fullFilePath))
            {
                result.Errors.Add(new ProcessorError(sourceFilePath, $"Unable to find image '{imagePath}'.", false));
                return m.Value;
            }

            var imageBase64 = Convert.ToBase64String(File.ReadAllBytes(fullFilePath));
            return $"<img src=\"data:{GetMimeType(fullFilePath)};base64,{imageBase64}\"";
        }, RegexOptions.IgnoreCase);

        return result;
    }

    /// <summary>
    /// Images from the web (or already embedded ones) are left alone.
    /// </summary>
    private static bool IsRemote(string imageUrl) =>
        imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        || imageUrl.StartsWith("//", StringComparison.Ordinal)
        || imageUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase);

    private static string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase)) return "image/png";
        if (extension.Equals(".svg", StringComparison.OrdinalIgnoreCase)) return "image/svg+xml";
        return "image/jpeg";
    }
}
