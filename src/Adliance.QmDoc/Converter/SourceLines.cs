using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Adliance.QmDoc.Processors.MarkdownProcessors;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Adliance.QmDoc.Converter;

/// <summary>
/// Adds "data-line" attributes with the (zero-based) line in the source file to the HTML elements of all blocks (headings, paragraphs, list items, tables, ...),
/// so that editor previews can scroll to the same position as the editor (--source-lines).
/// The Markdown processors add, remove and change lines (frontmatter, includes, table of contents, heading numbers, ...), so the lines of the
/// processed Markdown are mapped back to the source with a line diff. Changed lines map to the source lines they replace, eg. all lines of an
/// include map to the line of the include placeholder.
/// </summary>
public static class SourceLines
{
    public const string Attribute = "data-line";

    public static string ToHtml(MarkdownProcessorContext context)
    {
        // the frontmatter is removed before all other processors, the diff is more reliable without it
        var withoutFrontmatter = context.Frontmatter.MarkdownWithoutFrontmatter;
        var frontmatterLines = 0;
        if (withoutFrontmatter.Length < context.SourceMarkdown.Length && context.SourceMarkdown.EndsWith(withoutFrontmatter, StringComparison.Ordinal))
        {
            frontmatterLines = context.SourceMarkdown[..^withoutFrontmatter.Length].Count(c => c == '\n');
        }
        else
        {
            withoutFrontmatter = context.SourceMarkdown;
        }

        var map = MapLines(withoutFrontmatter, context.Markdown).Select(l => l + frontmatterLines).ToArray();
        return ToHtml(map, context.Markdown, context.Pipeline);
    }

    internal static string ToHtml(int[] map, string processedMarkdown, MarkdownPipeline pipeline)
    {
        var document = Markdown.Parse(processedMarkdown, pipeline);
        var lines = new Dictionary<Block, int>();
        foreach (var block in document.Descendants<Block>()) // parents come before their children
        {
            if (block.Line < 0 || block.Line >= map.Length) continue;

            // a block never starts before its parent (eg. the header row of a table created by a placeholder has a bogus line)
            var line = map[block.Line];
            if (block.Parent != null && lines.TryGetValue(block.Parent, out var parentLine)) line = Math.Max(line, parentLine);
            lines[block] = line;
            block.GetAttributes().AddPropertyIfNotExist(Attribute, line.ToString(CultureInfo.InvariantCulture));
        }

        return document.ToHtml(pipeline);
    }

    /// <summary>
    /// Returns the zero-based source line for each zero-based line of the processed Markdown.
    /// </summary>
    internal static int[] MapLines(string sourceMarkdown, string processedMarkdown)
    {
        var sourceLines = LineDiff.SplitLines(sourceMarkdown);
        var processedLines = LineDiff.SplitLines(processedMarkdown);
        var map = new int[processedLines.Length];
        int sourceLine = 0, processedLine = 0;

        foreach (var block in LineDiff.Diff(sourceLines, processedLines))
        {
            // unchanged lines before the block
            while (processedLine < block.InsertStartB) map[processedLine++] = sourceLine++;

            for (var i = 0; i < block.InsertCountB; i++)
            {
                map[block.InsertStartB + i] = block.DeleteCountA == block.InsertCountB
                    ? block.DeleteStartA + i // changed lines, eg. "# Title" -> "# 1. Title"
                    : PlaceholderLine(sourceLines, block.DeleteStartA, block.DeleteCountA);
            }

            sourceLine = block.DeleteStartA + block.DeleteCountA;
            processedLine = block.InsertStartB + block.InsertCountB;
        }

        while (processedLine < map.Length) map[processedLine++] = sourceLine++;

        for (var i = 0; i < map.Length; i++) map[i] = Math.Clamp(map[i], 0, Math.Max(sourceLines.Length - 1, 0));
        return map;
    }

    /// <summary>
    /// When lines are added or removed (includes, table of contents, Git versions, the frontmatter, ...), the new lines belong to the placeholder
    /// that created them, which is the last non-empty line of the replaced lines (eg. the frontmatter is removed and "{{ TOC }}" is replaced in one block).
    /// Lines that are only inserted belong to the next non-empty line: the diff often keeps the empty line before an include
    /// and treats the included lines as inserted before the include placeholder.
    /// </summary>
    private static int PlaceholderLine(IReadOnlyList<string> sourceLines, int start, int count)
    {
        for (var line = start + count - 1; line >= start; line--)
        {
            if (!string.IsNullOrWhiteSpace(sourceLines[line])) return line;
        }

        var next = start + count;
        while (next < sourceLines.Count - 1 && string.IsNullOrWhiteSpace(sourceLines[next])) next++;
        return next;
    }
}
