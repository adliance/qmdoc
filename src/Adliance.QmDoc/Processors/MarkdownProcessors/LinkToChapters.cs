using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Adliance.QmDoc.Extensions;
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
/// If no title matches exactly, a chapter whose title starts with (or else contains) the link text is used, as long as there is only one,
/// e.g. [#Projektmeldung] links to "Projektmeldung (Forschungsregister)".
/// </summary>
public class LinkToChapters(string filePath) : IMarkdownProcessor
{
    private const char PathSeparator = '>';

    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        var headings = GetHeadings(markdownContext);
        var codeSpans = CodeSpans.Find(markdownContext.Markdown, markdownContext.Pipeline);

        markdownContext.Markdown = Regex.Replace(markdownContext.Markdown, @"(?<![!\]])\[#([^\[\]]*)\](?![(\[])", m =>
        {
            if (codeSpans.Contains(m.Index)) return m.Value;

            var segments = m.Groups[1].Value.Split(PathSeparator).Select(x => x.Trim()).ToArray();
            if (segments.Any(string.IsNullOrEmpty))
            {
                // "[#]" is probably not meant as a chapter link at all, but "[#Risk Analysis > ]" is
                if (!string.IsNullOrWhiteSpace(m.Groups[1].Value)) markdownContext.Errors.Add(new ProcessorError(filePath, $"The chapter link \"{m.Value}\" contains an empty chapter title."));
                return m.Value;
            }

            var title = segments[^1];
            var id = ResolveChapterId(m.Value, segments, headings, markdownContext);
            return $"<span class=\"link-to-chapter\"><i></i>[{title}](#{id})</span>";
        });

        return markdownContext;
    }

    private string ResolveChapterId(string link, string[] segments, List<Heading> headings, MarkdownProcessorContext markdownContext)
    {
        var slugs = segments.Select(GetChapterId).ToArray();
        var candidates = headings.Where(h => h.Slug == slugs[^1] && MatchesParents(h, segments[..^1])).ToList();

        if (candidates.Count == 0)
        {
            var possible = headings.Where(h => MatchesParents(h, segments[..^1])).ToList();
            candidates = LenientMatching.FindPartial(possible.Select(h => h.Title).ToList(), segments[^1]).Select(i => possible[i]).ToList();
        }

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
    /// A parent segment matches if it is the parent's full title, or a part of it.
    /// </summary>
    private static bool MatchesParents(Heading heading, string[] parentSegments)
    {
        var i = 0;
        foreach (var parent in heading.Parents)
        {
            if (i < parentSegments.Length && (parent.Slug == GetChapterId(parentSegments[i]) || parent.Title.Contains(parentSegments[i], StringComparison.OrdinalIgnoreCase))) i++;
        }

        return i == parentSegments.Length;
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

    /// <summary>
    /// The plain text of a heading, including inline code (eg. "Using `qmdoc`" is "Using qmdoc").
    /// </summary>
    public static string ExtractText(HeadingBlock heading)
    {
        if (heading.Inline == null) return string.Empty;

        var sb = new StringBuilder();
        foreach (var inline in heading.Inline.Descendants<Inline>())
        {
            if (inline is LiteralInline literal) sb.Append(literal.Content.ToString());
            else if (inline is CodeInline code) sb.Append(code.Content);
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Mirrors the id Markdig's AutoIdentifierExtension assigns to headings (see MarkdownProcessorContext.Pipeline),
    /// so that chapter links and the TOC point to the same anchor Markdig actually renders.
    /// Note that Markdig appends "-1", "-2", ... for duplicate headings, which is not reflected here.
    /// </summary>
    public static string GetChapterId(string chapterName)
    {
        if (string.IsNullOrWhiteSpace(chapterName)) throw new ArgumentException(null, nameof(chapterName));

        return LinkHelper.Urilize(chapterName.Trim(), allowOnlyAscii: false);
    }

    private sealed record Heading(int Level, string Text, string Slug, string Id, List<Heading> Parents)
    {
        /// <summary>
        /// The title without the numbering added by HeaderNumbering.
        /// </summary>
        public string Title => Regex.Replace(Text, @"^(\d+\.)+\s*", "");

        /// <summary>
        /// The titles of the heading and its parents, without the numbering added by HeaderNumbering.
        /// </summary>
        public IEnumerable<string> Path => Parents.Append(this).Select(h => h.Title);
    }
}
