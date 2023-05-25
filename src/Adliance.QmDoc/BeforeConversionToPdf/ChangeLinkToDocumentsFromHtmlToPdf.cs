using System.Text.RegularExpressions;

namespace Adliance.QmDoc.BeforeConversionToPdf;

public class ChangeLinkToDocumentsFromHtmlToPdf : IBeforeConversionToPdfStep
{
    private readonly string _filePath;

    public ChangeLinkToDocumentsFromHtmlToPdf(string filePath)
    {
        _filePath = filePath;
    }

    public Result Apply(string html)
    {
        var resultingHtml = Regex.Replace(html, " href=\"(.*?)\\.html\"", " href=\"$1.pdf\"", RegexOptions.IgnoreCase);
        var result = new Result(resultingHtml);
        return result;
    }
}