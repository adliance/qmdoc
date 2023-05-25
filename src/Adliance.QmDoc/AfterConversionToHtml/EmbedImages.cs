using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.AfterConversionToHtml;

public class EmbedImages : IAfterConversionToHtmlStep
{
    private readonly string _sourceFilePath;


    public EmbedImages(string sourceFilePath)
    {
        _sourceFilePath = sourceFilePath;

    }

    public Result Apply(string html)
    {
        var resultingHtml = html;
        var result = new Result(resultingHtml);

        foreach (Match? match in Regex.Matches(html, "<img src=\"(.*?)\"", RegexOptions.IgnoreCase))
        {
            if (match==null) continue;
                
            var imageUrl = match.Groups[1].Value;
            imageUrl = imageUrl.Replace("%20", " ");

            var fullFilePath = Path.Combine(Path.GetDirectoryName(_sourceFilePath) ?? "", imageUrl);
            if (File.Exists(fullFilePath))
            {
                // we have a local file
                var imageBytes = File.ReadAllBytes(fullFilePath);
                var imageBase64 = Convert.ToBase64String(imageBytes);

                var mimeType = "image/jpeg";
                if (Path.GetExtension(fullFilePath).Equals(".png", StringComparison.OrdinalIgnoreCase))
                {
                    mimeType = "image/png";
                }
                else if (Path.GetExtension(fullFilePath).Equals(".svg", StringComparison.OrdinalIgnoreCase))
                {
                    mimeType = "image/svg+xml";
                }

                resultingHtml = resultingHtml.Replace($"<img src=\"{imageUrl}\"", $"<img src=\"data:{mimeType};base64,{imageBase64}\"");
            }
            else
            {
                result.Errors.Add(new ProcessorError(_sourceFilePath, $"Unable to find image '{imageUrl}'.", false));
            }
        }

        result.ResultingHtml = resultingHtml;
        return result;
    }

}