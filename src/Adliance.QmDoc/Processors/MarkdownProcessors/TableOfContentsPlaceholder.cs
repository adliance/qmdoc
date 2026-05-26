using System.Globalization;
using System.Linq;
using System.Text;
using Adliance.AspNetCore.Buddy.Pdf;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class TableOfContentsPlaceholder : IMarkdownProcessor
{
    public static bool ContainsTocPlaceholder(MarkdownProcessorContext markdownContext)
    {
        return markdownContext.ContainsPlaceholderInSource("TOC");
    }

    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        markdownContext.ReplacePlaceholder("TOC", BuildToc(markdownContext));
        return markdownContext;
    }

    private static string BuildToc(MarkdownProcessorContext context)
    {
        var document = Markdown.Parse(context.Markdown, context.Pipeline);
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

            var indentText = "";
            if (heading.Level > 1) indentText = string.Concat(Enumerable.Repeat("&nbsp;", (heading.Level - 1) * 5));
            var pageText = "";
            var page = GetPageNumber(text, context.PdfMetadata);
            if (page.HasValue) pageText = page.Value.ToString(CultureInfo.InvariantCulture);
            sb.AppendLine(CultureInfo.InvariantCulture, $"| {indentText}[{text}](#{LinkToChapters.GetChapterId(text)}) | {pageText} |");
        }

        sb.AppendLine("");
        sb.AppendLine("</div>");

        return sb.ToString();
    }

    private static int? GetPageNumber(string chapterTitle, PdfMetadata? pdfMetadata)
    {
        if (pdfMetadata == null) return null;

        foreach (var o in pdfMetadata.Outline)
        {
            var page = GetPageNumber(chapterTitle, o);
            if (page != null) return page;
        }

        return null;
    }

    private static int? GetPageNumber(string chapterTitle, PdfMetadata.OutlineData outline)
    {
        if (outline.Title == chapterTitle) return outline.Page;
        foreach (var o in outline.Children)
        {
            var page = GetPageNumber(chapterTitle, o);
            if (page != null) return page;
        }

        return null;
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
