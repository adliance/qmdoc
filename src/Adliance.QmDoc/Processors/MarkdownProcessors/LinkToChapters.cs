using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Helpers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

/// <summary>
/// Converts chapter links like [#Open Questions] into links to the heading's anchor.
/// If multiple headings share the same title, the link can be qualified with (some of) its parent headings,
/// separated by "&gt;", e.g. [#Risk Analysis > Open Questions].
/// </summary>
public class LinkToChapters(string filePath) : IMarkdownProcessor
{
    private const char PathSeparator = '>';

    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        var headings = GetHeadings(markdownContext);

        markdownContext.Markdown = Regex.Replace(markdownContext.Markdown, @"\[#(.*?)\]", m =>
        {
            var segments = m.Groups[1].Value.Split(PathSeparator).Select(x => x.Trim()).ToArray();
            var title = segments[^1];
            var id = ResolveChapterId(m.Value, segments, headings, markdownContext);
            return $"<span class=\"link-to-chapter\"><i></i>[{title}](#{id})</span>";
        });

        return markdownContext;
    }

    private string ResolveChapterId(string link, string[] segments, List<Heading> headings, MarkdownProcessorContext markdownContext)
    {
        var slugs = segments.Select(GetChapterId).ToArray();
        var candidates = headings.Where(h => h.Slug == slugs[^1] && MatchesParents(h, slugs[..^1])).ToList();

        if (candidates.Count == 1) return candidates[0].Id;

        if (candidates.Count == 0)
        {
            // links without a path fall through to the check for non-existing chapters in SetCorrectChaptersLinkTitle
            if (segments.Length > 1) markdownContext.Errors.Add(new ProcessorError(filePath, $"Unable to find a chapter matching \"{link}\"."));
            return slugs[^1];
        }

        var errorMessage = $"The chapter link \"{link}\" is ambiguous, linking to the first match. Add parent chapters to the link to make it unique, e.g.:";
        foreach (var c in candidates) errorMessage += $"{Environment.NewLine}  - [#{string.Join($" {PathSeparator} ", c.Path)}]";
        markdownContext.Errors.Add(new ProcessorError(filePath, errorMessage));
        return candidates[0].Id;
    }

    /// <summary>
    /// The parent segments must match the heading's parents in the same order, but levels in between may be omitted.
    /// </summary>
    private static bool MatchesParents(Heading heading, string[] parentSlugs)
    {
        var i = 0;
        foreach (var parent in heading.Parents)
        {
            if (i < parentSlugs.Length && parent.Slug == parentSlugs[i]) i++;
        }

        return i == parentSlugs.Length;
    }

    private static List<Heading> GetHeadings(MarkdownProcessorContext markdownContext)
    {
        var document = Markdown.Parse(markdownContext.Markdown, markdownContext.Pipeline);
        var result = new List<Heading>();
        var stack = new Stack<Heading>();

        foreach (var block in document.Descendants<HeadingBlock>())
        {
            var text = ExtractText(block);
            var id = block.GetAttributes().Id;
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(id)) continue;

            while (stack.Count > 0 && stack.Peek().Level >= block.Level) stack.Pop();
            var heading = new Heading(block.Level, text, GetChapterId(text), id, stack.Reverse().ToList());
            stack.Push(heading);
            result.Add(heading);
        }

        return result;
    }

    private static string ExtractText(HeadingBlock heading)
    {
        if (heading.Inline == null) return string.Empty;

        var sb = new StringBuilder();
        foreach (var inline in heading.Inline.Descendants<LiteralInline>())
            sb.Append(inline.Content.ToString());

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Mirrors the id Markdig's AutoIdentifierExtension assigns to headings (via .UseAdvancedExtensions()),
    /// so that chapter links and the TOC point to the same anchor Markdig actually renders.
    /// Note that Markdig appends "-1", "-2", ... for duplicate headings, which is not reflected here.
    /// </summary>
    public static string GetChapterId(string chapterName)
    {
        if (string.IsNullOrWhiteSpace(chapterName)) throw new ArgumentException(null, nameof(chapterName));

        return LinkHelper.Urilize(chapterName.Trim(), allowOnlyAscii: true);
    }

    private sealed record Heading(int Level, string Text, string Slug, string Id, List<Heading> Parents)
    {
        /// <summary>
        /// The titles of the heading and its parents, without the numbering added by HeaderNumbering.
        /// </summary>
        public IEnumerable<string> Path => Parents.Append(this).Select(h => Regex.Replace(h.Text, @"^(\d+\.)+\s*", ""));
    }
}
