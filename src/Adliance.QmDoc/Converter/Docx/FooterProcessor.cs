using System.Globalization;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Adliance.QmDoc.Converter.Docx;

/// <summary>
/// Adds a footer with the document title (using the whole width except the page number), the Git version in a smaller line below the
/// title (if there is one) and the page number on the right, like the footer of the PDF.
/// </summary>
internal static class FooterProcessor
{
    private const int DefaultTextWidth = 9360; // twips, Letter/A4 with 1" margins (approx.)
    private const int PageNumberWidth = 1200; // twips, enough for "999 | 999"

    internal static void Apply(WordprocessingDocument document, string title, string version)
    {
        var body = document.MainDocumentPart!.Document.Body!;
        var sectPr = body.Descendants<SectionProperties>().LastOrDefault();
        if (sectPr == null)
        {
            sectPr = new SectionProperties();
            body.AppendChild(sectPr);
        }

        var footerPart = document.MainDocumentPart.AddNewPart<FooterPart>();
        footerPart.Footer = new Footer(
            BuildFooterTable(title, version, GetTextWidth(sectPr)),
            // Word expects a paragraph after a table, keep it as small as possible
            new Paragraph(new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "0", Line = "20", LineRule = LineSpacingRuleValues.Exact }),
                new Run(new RunProperties(new FontSize { Val = "2" }))));

        foreach (var existing in sectPr.Elements<FooterReference>().ToList())
            existing.Remove();

        // the footer reference must be the first child of the section properties
        sectPr.InsertAt(new FooterReference
        {
            Type = HeaderFooterValues.Default,
            Id = document.MainDocumentPart.GetIdOfPart(footerPart)
        }, 0);
    }

    private static int GetTextWidth(SectionProperties sectPr)
    {
        var pageWidth = (int?)sectPr.GetFirstChild<PageSize>()?.Width?.Value;
        var margin = sectPr.GetFirstChild<PageMargin>();
        if (pageWidth == null || margin?.Left?.Value == null || margin.Right?.Value == null) return DefaultTextWidth;

        var textWidth = pageWidth.Value - (int)margin.Left.Value - (int)margin.Right.Value;
        return textWidth > PageNumberWidth * 2 ? textWidth : DefaultTextWidth;
    }

    private static Table BuildFooterTable(string title, string version, int textWidth)
    {
        var none = BorderValues.None;
        var table = new Table(
            new TableProperties(
                new TableWidth { Width = textWidth.ToString(CultureInfo.InvariantCulture), Type = TableWidthUnitValues.Dxa },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Color = "134094", Size = 6, Space = 0 },
                    new LeftBorder { Val = none },
                    new BottomBorder { Val = none },
                    new RightBorder { Val = none },
                    new InsideHorizontalBorder { Val = none },
                    new InsideVerticalBorder { Val = none }),
                new TableLayout { Type = TableLayoutValues.Fixed }, // order matters, must come after the borders
                // no cell margins, so that the footer starts and ends at the same position as the text
                new TableCellMarginDefault(
                    new TableCellLeftMargin { Width = 0, Type = TableWidthValues.Dxa },
                    new TableCellRightMargin { Width = 0, Type = TableWidthValues.Dxa })),
            new TableGrid(
                new GridColumn { Width = (textWidth - PageNumberWidth).ToString(CultureInfo.InvariantCulture) },
                new GridColumn { Width = PageNumberWidth.ToString(CultureInfo.InvariantCulture) }));

        // Left: document title, with the Git version in a smaller line below
        var titleCell = Cell(textWidth - PageNumberWidth);
        titleCell.AppendChild(Paragraph(JustificationValues.Left, spacingBefore: 60, TextRun(title, 18)));
        if (!string.IsNullOrWhiteSpace(version))
            titleCell.AppendChild(Paragraph(JustificationValues.Left, spacingBefore: 0, TextRun(version, 15)));

        // Right: <PAGE> | <NUMPAGES>
        var pageParagraph = Paragraph(JustificationValues.Right, spacingBefore: 60);
        AppendField(pageParagraph, " PAGE ");
        pageParagraph.AppendChild(TextRun(" | ", 18));
        AppendField(pageParagraph, " NUMPAGES ");
        var pageCell = Cell(PageNumberWidth);
        pageCell.AppendChild(pageParagraph);

        table.AppendChild(new TableRow(titleCell, pageCell));
        return table;
    }

    private static TableCell Cell(int width) =>
        new(new TableCellProperties(new TableCellWidth { Width = width.ToString(CultureInfo.InvariantCulture), Type = TableWidthUnitValues.Dxa }));

    private static Paragraph Paragraph(JustificationValues justification, int spacingBefore, params OpenXmlElement[] runs)
    {
        var paragraph = new Paragraph(new ParagraphProperties(
            new SpacingBetweenLines { Before = spacingBefore.ToString(CultureInfo.InvariantCulture), After = "0" },
            new Justification { Val = justification }));
        paragraph.Append(runs);
        return paragraph;
    }

    private static void AppendField(Paragraph para, string instruction)
    {
        para.AppendChild(new Run(Rpr(18), new FieldChar { FieldCharType = FieldCharValues.Begin }));
        para.AppendChild(new Run(Rpr(18), new FieldCode(instruction)));
        para.AppendChild(new Run(Rpr(18), new FieldChar { FieldCharType = FieldCharValues.End }));
    }

    private static Run TextRun(string text, int halfPoints) =>
        new(Rpr(halfPoints), new Text(text) { Space = SpaceProcessingModeValues.Preserve });

    private static RunProperties Rpr(int halfPoints) =>
        new(new FontSize { Val = halfPoints.ToString(CultureInfo.InvariantCulture) }, new FontSizeComplexScript { Val = halfPoints.ToString(CultureInfo.InvariantCulture) });
}
