using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using Markdig.Helpers;
using Markdig.Renderers.Docx;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Adliance.QmDoc.Converter.Docx;

/// <summary>
/// State shared by the QmDoc specific DOCX renderers of one document.
/// </summary>
internal sealed partial class DocxRenderContext(string sourceFilePath, DocxTheme theme)
{
    private readonly Dictionary<string, Heading> _headingsById = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Heading> _headingsByLegacyId = new(StringComparer.Ordinal);
    private readonly Stack<int> _maxImageWidths = new();
    private int _drawingId;
    private int _bookmarkId;

    public string SourceFilePath { get; } = sourceFilePath;
    public DocxTheme Theme { get; } = theme;
    public List<ProcessorError> Errors { get; } = [];

    /// <summary>
    /// The footnotes that are already referenced in the text (each footnote can only be referenced once in DOCX).
    /// </summary>
    public HashSet<int> ReferencedFootnotes { get; } = [];

    /// <summary>
    /// The relationship ids of the already embedded images (by their full path), so that images used multiple times are only embedded once.
    /// </summary>
    public Dictionary<string, string> EmbeddedImages { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// The width (in twips) available for images at the current position, eg. smaller inside of figures.
    /// </summary>
    public int MaxImageWidth => _maxImageWidths.Count > 0 ? _maxImageWidths.Peek() : ThemeStylesProcessor.TextWidth;

    public void PushMaxImageWidth(int twips) => _maxImageWidths.Push(Math.Min(twips, MaxImageWidth));
    public void PopMaxImageWidth() => _maxImageWidths.Pop();

    public uint NextDrawingId() => (uint)++_drawingId;
    public int NextBookmarkId() => ++_bookmarkId;

    /// <summary>
    /// Collects the headings of the document, so that chapter links can show the title of the heading (including the numbering),
    /// even if the link comes before the heading.
    /// </summary>
    public void CollectHeadings(MarkdownDocument document)
    {
        foreach (var block in document.Descendants<HeadingBlock>())
        {
            var id = block.GetAttributes().Id;
            if (string.IsNullOrWhiteSpace(id) || _headingsById.ContainsKey(id)) continue;

            var text = GetText(block).Trim();
            var heading = new Heading(text, GetBookmarkName(id));
            _headingsById[id] = heading;

            // heading ids used to be ASCII-only, so existing documents may still contain links like "#elimination-des-dechets"
            var legacyId = LinkHelper.Urilize(text, allowOnlyAscii: true);
            if (!string.IsNullOrEmpty(legacyId)) _headingsByLegacyId.TryAdd(legacyId, heading);
        }
    }

    public Heading? FindHeading(string id) =>
        _headingsById.GetValueOrDefault(id) ?? _headingsByLegacyId.GetValueOrDefault(id);

    public static string? GetBookmarkNameOfHeading(HeadingBlock block)
    {
        var id = block.GetAttributes().Id;
        return string.IsNullOrWhiteSpace(id) ? null : GetBookmarkName(id);
    }

    public void AddError(string message, bool isWarningOnly = false) => Errors.Add(new ProcessorError(SourceFilePath, message, isWarningOnly));

    /// <summary>
    /// Bookmark names in DOCX may only contain letters, digits and underscores, and have at most 40 characters.
    /// The leading underscore hides them in the bookmark dialog of Word, like the bookmarks of Word's own table of contents.
    /// </summary>
    public static string GetBookmarkName(string id)
    {
        var name = "_" + InvalidBookmarkCharacters().Replace(id, "_");
        if (name.Length <= 40) return name;

        var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(id)))[..8];
        return name[..31] + "_" + hash;
    }

    private static string GetText(ContainerInline? inline)
    {
        if (inline == null) return "";
        var sb = new StringBuilder();
        foreach (var child in inline.Descendants())
        {
            switch (child)
            {
                case LiteralInline literal:
                    sb.Append(literal.Content.ToString());
                    break;
                case CodeInline code:
                    sb.Append(code.Content);
                    break;
            }
        }

        return sb.ToString();
    }

    private static string GetText(LeafBlock block) => GetText(block.Inline);

    /// <summary>
    /// Renders blocks into another element (eg. a table cell or an endnote) and continues at the previous position afterwards.
    /// </summary>
    public static void RenderInto(DocxDocumentRenderer renderer, OpenXmlCompositeElement target, Action render)
    {
        var insertAfter = renderer.Cursor.InsertAfter;
        renderer.Cursor.GoInto(target);
        render();
        renderer.Cursor.PopAndAdvanceAfter(null);
        if (insertAfter != null) renderer.Cursor.SetAfter(insertAfter);
    }

    /// <summary>
    /// Writes a block level element (eg. a table) at the current position and continues after it.
    /// </summary>
    public static void WriteBlock(DocxDocumentRenderer renderer, OpenXmlElement element)
    {
        renderer.Cursor.Write(element);
        renderer.Cursor.SetAfter(element);
    }

    public sealed record Heading(string Text, string BookmarkName);

    [GeneratedRegex(@"[^A-Za-z0-9_]")]
    private static partial Regex InvalidBookmarkCharacters();
}
