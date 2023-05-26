using System.Text.RegularExpressions;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class TitlePlaceholder : IMarkdownProcessor
{
    private readonly string _title;

    public TitlePlaceholder(string title)
    {
        _title = title;
    }

    public MarkdownProcessorResult Apply(string markdown, MarkdownProcessorContext markdownProcessorContext)
    {
        var result = Regex.Replace(markdown, @"\{\{\W*TITLE\W*\}\}", _title, RegexOptions.IgnoreCase);
        return new MarkdownProcessorResult(result, markdownProcessorContext);
    }
}