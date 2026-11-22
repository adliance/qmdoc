using System.Collections.Generic;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Renderers.Docx;
using Markdig.Renderers.Docx.Blocks;
using Markdig.Syntax;

namespace Adliance.QmDoc.Converter.Docx;

/// <summary>
/// Renders headings like the default renderer, but adds a bookmark, so that chapter links can link to the heading.
/// </summary>
internal sealed class HeadingRenderer(DocxRenderContext context) : LeafBlockParagraphRendererBase<HeadingBlock>
{
    protected override void WriteObject(DocxDocumentRenderer renderer, HeadingBlock obj)
    {
        var styleId = renderer.Styles.Headings.GetValueOrDefault(obj.Level, renderer.Styles.UndefinedHeading);
        var paragraph = WriteAsParagraph(renderer, obj, styleId);

        var bookmarkName = DocxRenderContext.GetBookmarkNameOfHeading(obj);
        if (bookmarkName == null) return;

        var id = context.NextBookmarkId().ToXml();
        var start = new BookmarkStart { Name = bookmarkName, Id = id };
        if (paragraph.ParagraphProperties != null) paragraph.InsertAfter(start, paragraph.ParagraphProperties);
        else paragraph.PrependChild(start);
        paragraph.AppendChild(new BookmarkEnd { Id = id });
    }
}
