using System;
using System.Linq;
using System.Text;
using Markdig;
using Markdig.Syntax;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

/// <summary>
/// Replaces horizontal rules written as "---" with page breaks.
/// Only real horizontal rules are replaced, not "---" in code blocks, tables ("---|---") or underlines of headings ("Title\n---").
/// </summary>
public class PageBreak : IMarkdownProcessor
{
    private const string PageBreakHtml = "<div style=\"page-break-after: always;\"></div>";

    /// <summary>
    /// Without the frontmatter extension, because the frontmatter is already removed. Otherwise two page breaks at the beginning
    /// of the document could be mistaken for a frontmatter.
    /// </summary>
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        var markdown = markdownContext.Markdown;
        if (!markdown.Contains("---", StringComparison.Ordinal)) return markdownContext;

        var pageBreaks = Markdown.Parse(markdown, Pipeline)
            .Descendants<ThematicBreakBlock>()
            .Where(b => markdown.AsSpan(b.Span.Start, b.Span.Length).Trim().StartsWith("---", StringComparison.Ordinal)) // "***" and "___" stay horizontal rules
            .OrderByDescending(b => b.Span.Start);

        var result = new StringBuilder(markdown);
        foreach (var pageBreak in pageBreaks)
        {
            // an empty line after the <div> ends the HTML block, otherwise a line right after the page break would be part of it (eg. "# Heading" as literal text)
            var lineEnd = markdown.IndexOf('\n', pageBreak.Span.Start);
            var textFollows = pageBreak.Parent is MarkdownDocument && lineEnd >= 0 && !IsBlankLine(markdown, lineEnd + 1);
            result.Remove(pageBreak.Span.Start, pageBreak.Span.Length).Insert(pageBreak.Span.Start, textFollows ? PageBreakHtml + "\n" : PageBreakHtml);
        }

        markdownContext.Markdown = result.ToString();
        return markdownContext;
    }

    private static bool IsBlankLine(string markdown, int start)
    {
        if (start >= markdown.Length) return true;
        var end = markdown.IndexOf('\n', start);
        return markdown.AsSpan(start, (end < 0 ? markdown.Length : end) - start).IsWhiteSpace();
    }
}
