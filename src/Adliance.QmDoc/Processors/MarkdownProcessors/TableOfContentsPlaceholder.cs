using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Adliance.AspNetCore.Buddy.Pdf;
using Markdig;
using Markdig.Renderers.Html;
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
        var outline = FlattenOutline(context.PdfMetadata);
        var occurrences = new Dictionary<string, int>();
        var sb = new StringBuilder();

        sb.AppendLine("<div class=\"toc\">");
        sb.AppendLine("");
        sb.AppendLine("| | |");
        sb.AppendLine("|-|-:|");
        foreach (var heading in document.Descendants<HeadingBlock>())
        {
            var text = ExtractText(heading);
            if (string.IsNullOrWhiteSpace(text)) continue;

            // count all headings (also the ones not shown in the TOC), because all of them are part of the PDF outline
            var normalizedTitle = NormalizeTitle(text);
            var occurrence = occurrences.GetValueOrDefault(normalizedTitle);
            occurrences[normalizedTitle] = occurrence + 1;

            if (heading.Level > 5) continue;

            var indentText = "";
            if (heading.Level > 1) indentText = string.Concat(Enumerable.Repeat("&nbsp;", (heading.Level - 1) * 5));
            var pageText = "";
            var page = GetPageNumber(normalizedTitle, occurrence, outline);
            if (page.HasValue) pageText = page.Value.ToString(CultureInfo.InvariantCulture);

            // use the id Markdig actually assigned, as it differs from the title for duplicate headings (e.g. "open-questions-1")
            var id = heading.GetAttributes().Id ?? LinkToChapters.GetChapterId(text);
            sb.AppendLine(CultureInfo.InvariantCulture, $"| {indentText}[{text}](#{id}) | {pageText} |");
        }

        sb.AppendLine("");
        sb.AppendLine("</div>");

        return sb.ToString();
    }

    /// <summary>
    /// Returns the page of the n-th outline entry (zero-based <paramref name="occurrence"/>) with the given title,
    /// so that multiple headings with the same title get their own page number.
    /// </summary>
    private static int? GetPageNumber(string normalizedTitle, int occurrence, List<PdfMetadata.OutlineData> outline)
    {
        return outline
            .Where(o => NormalizeTitle(o.Title).Equals(normalizedTitle, StringComparison.OrdinalIgnoreCase))
            .Skip(occurrence)
            .Select(o => (int?)o.Page)
            .FirstOrDefault();
    }

    /// <summary>
    /// Flattens the outline in document order.
    /// </summary>
    private static List<PdfMetadata.OutlineData> FlattenOutline(PdfMetadata? pdfMetadata)
    {
        var result = new List<PdfMetadata.OutlineData>();
        if (pdfMetadata == null) return result;

        foreach (var o in pdfMetadata.Outline) AddOutline(o, result);
        return result;

        static void AddOutline(PdfMetadata.OutlineData outline, List<PdfMetadata.OutlineData> result)
        {
            result.Add(outline);
            foreach (var o in outline.Children) AddOutline(o, result);
        }
    }

    private static string NormalizeTitle(string title)
    {
        return title.Trim().Replace(" ", "").Replace("‐", "");
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
