using System.Text.RegularExpressions;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class PageBreak : IMarkdownProcessor
{
    public MarkdownProcessorResult Apply(string markdown, MarkdownProcessorContext markdownProcessorContext)
    {
        var pageBreakHtml = "<div style=\"page-break-after: always;\"></div>";

        markdown = Regex.Replace(markdown, "^---", pageBreakHtml, RegexOptions.Multiline);
        return new MarkdownProcessorResult(markdown, markdownProcessorContext);
    }
}