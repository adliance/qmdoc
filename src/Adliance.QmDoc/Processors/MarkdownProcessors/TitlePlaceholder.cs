using System.Text.RegularExpressions;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class TitlePlaceholder(string title) : IMarkdownProcessor
{
    public MarkdownProcessorResult Apply(string markdown, MarkdownProcessorContext markdownProcessorContext)
    {
        var result = Regex.Replace(markdown, @"\{\{\W*TITLE\W*\}\}", title, RegexOptions.IgnoreCase);
        return new MarkdownProcessorResult(result, markdownProcessorContext);
    }
}
