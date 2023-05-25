using System.Text.RegularExpressions;

namespace Adliance.QmDoc.AfterConversionToHtml;

public class TitlePlaceholder : IAfterConversionToHtmlStep
{
    private readonly string _title;

    public TitlePlaceholder(string title)
    {
        _title = title;
    }

    public Result Apply(string html)
    {
        var result = Regex.Replace(html, @"\{\{\W*TITLE\W*\}\}", _title, RegexOptions.IgnoreCase);
        return new Result(result);
    }
}