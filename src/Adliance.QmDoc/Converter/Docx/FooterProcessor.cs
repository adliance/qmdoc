using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Adliance.QmDoc.Converter.Docx;

internal static class FooterProcessor
{
    internal static void Apply(WordprocessingDocument document, string title, string centerText)
    {
        var footerPart = document.MainDocumentPart!.AddNewPart<FooterPart>();
        footerPart.Footer = new Footer(BuildFooterParagraph(title, centerText));

        var body = document.MainDocumentPart.Document.Body!;
        var sectPr = body.Descendants<SectionProperties>().LastOrDefault();
        if (sectPr == null)
        {
            sectPr = new SectionProperties();
            body.AppendChild(sectPr);
        }

        foreach (var existing in sectPr.Elements<FooterReference>().ToList())
            existing.Remove();

        sectPr.AppendChild(new FooterReference
        {
            Type = HeaderFooterValues.Default,
            Id = document.MainDocumentPart.GetIdOfPart(footerPart)
        });
    }

    private static Paragraph BuildFooterParagraph(string title, string centerText)
    {
        var pPr = new ParagraphProperties(
            new ParagraphBorders(
                new TopBorder { Val = BorderValues.Single, Color = "134094", Size = 6, Space = 4 }
            ),
            new Tabs(
                new TabStop { Val = TabStopValues.Center, Position = 4680 },
                new TabStop { Val = TabStopValues.Right, Position = 9360 }
            ),
            new SpacingBetweenLines { Before = "60", After = "0" }
        );

        var para = new Paragraph(pPr);

        // Left: document title
        para.AppendChild(TextRun(title));

        // Center: git date / version
        para.AppendChild(TabRun());
        para.AppendChild(TextRun(centerText));

        // Right: <PAGE> | <NUMPAGES>
        para.AppendChild(TabRun());
        para.AppendChild(TextRun(" "));
        AppendField(para, " PAGE ");
        para.AppendChild(TextRun(" | "));
        AppendField(para, " NUMPAGES ");

        return para;
    }

    private static void AppendField(Paragraph para, string instruction)
    {
        para.AppendChild(new Run(Rpr(), new FieldChar { FieldCharType = FieldCharValues.Begin }));
        para.AppendChild(new Run(Rpr(), new FieldCode(instruction)));
        para.AppendChild(new Run(Rpr(), new FieldChar { FieldCharType = FieldCharValues.End }));
    }

    private static Run TextRun(string text) =>
        new(Rpr(), new Text(text) { Space = SpaceProcessingModeValues.Preserve });

    private static Run TabRun() => new(Rpr(), new TabChar());

    private static RunProperties Rpr() =>
        new(new FontSize { Val = "18" }, new FontSizeComplexScript { Val = "18" });
}
