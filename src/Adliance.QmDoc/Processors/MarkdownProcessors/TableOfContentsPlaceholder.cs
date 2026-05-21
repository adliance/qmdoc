using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class TableOfContentsPlaceholder : IMarkdownProcessor
{
    public MarkdownProcessorResult Apply(string markdown, MarkdownProcessorContext context)
    {
        const string placeholderPattern = @"\{{1,2}\W*TOC\W*\}{1,2}";

        if (!Regex.IsMatch(markdown, placeholderPattern, RegexOptions.IgnoreCase)) return new MarkdownProcessorResult(markdown, context);

        var toc = BuildToc(markdown, context);
        var result = Regex.Replace(markdown, placeholderPattern, toc, RegexOptions.IgnoreCase);

        return new MarkdownProcessorResult(result, context);
    }

    private static string BuildToc(string markdown, MarkdownProcessorContext context)
    {
        var document = Markdown.Parse(markdown, context.Pipeline);
        var sb = new StringBuilder();

        sb.AppendLine("<div class=\"toc\">");
        sb.AppendLine("");
        sb.AppendLine("| | |");
        sb.AppendLine("|-|-:|");
        foreach (var heading in document.Descendants<HeadingBlock>())
        {
            if (heading.Level > 5) continue;

            var text = ExtractText(heading);
            if (string.IsNullOrWhiteSpace(text)) continue;

            var indent = "";
            if (heading.Level > 1) indent = string.Concat(Enumerable.Repeat("&nbsp;", (heading.Level - 1) * 5));
            sb.AppendLine(CultureInfo.InvariantCulture, $"| {indent}[{text}](#{LinkToChapters.GetChapterId(text)}) |  |");
        }
        sb.AppendLine("");
        sb.AppendLine("</div>");

        return sb.ToString();
    }

    private static string ExtractText(HeadingBlock heading)
    {
        var sb = new StringBuilder();
        if (heading.Inline == null) return string.Empty;

        foreach (var inline in heading.Inline.Descendants<LiteralInline>())
            sb.Append(inline.Content.ToString());

        return sb.ToString().Trim();
    }
}
