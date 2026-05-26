using System.Text.RegularExpressions;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class PageBreak : IMarkdownProcessor
{
    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        const string pageBreakHtml = "<div style=\"page-break-after: always;\"></div>";
        markdownContext.Markdown = Regex.Replace(markdownContext.Markdown, "^---", pageBreakHtml, RegexOptions.Multiline);
        return markdownContext;
    }
}
