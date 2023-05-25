using System.Text.RegularExpressions;

namespace Adliance.QmDoc.BeforeConversionToHtml;

public class PageBreak : IBeforeConversionToHtmlStep
{
    public Result Apply(string markdown, Context context)
    {
        var pageBreakHtml = "<div style=\"page-break-after: always;\"></div>";

        markdown = Regex.Replace(markdown, "^---", pageBreakHtml, RegexOptions.Multiline);
        return new Result(markdown, context);
    }
}