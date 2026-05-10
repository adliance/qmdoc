using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Renderers;
using Markdig.Renderers.Docx;
using Markdig.Syntax;

namespace Adliance.QmDoc.Converter.Docx;

public class PageBreakRenderer : MarkdownObjectRenderer<DocxDocumentRenderer, ThematicBreakBlock>
{
    protected override void Write(DocxDocumentRenderer renderer, ThematicBreakBlock obj)
    {
        renderer.ForceCloseParagraph();

        var para = new Paragraph(new Run(new Break { Type = BreakValues.Page }));
        renderer.Cursor.Write(para);
        renderer.Cursor.SetAfter(para);
    }
}
