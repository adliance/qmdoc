using System;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Extensions.Tables;
using Markdig.Renderers;
using Markdig.Renderers.Docx;
using Markdig.Syntax;
using MdTable = Markdig.Extensions.Tables.Table;
using MdTableCell = Markdig.Extensions.Tables.TableCell;
using MdTableRow = Markdig.Extensions.Tables.TableRow;
using OxTable = DocumentFormat.OpenXml.Wordprocessing.Table;
using OxTableCell = DocumentFormat.OpenXml.Wordprocessing.TableCell;
using OxTableRow = DocumentFormat.OpenXml.Wordprocessing.TableRow;

namespace Adliance.QmDoc.Converter.Docx;

/// <summary>
/// Renders tables like the CSS of the theme: full width, horizontal lines only, a thicker line below the header, alternating row colors
/// and the column alignment of the Markdown. The content of the cells is rendered like any other Markdown (links, code, ...).
/// The table of contents has no lines and less padding.
/// </summary>
internal sealed class TableRenderer(DocxRenderContext context) : MarkdownObjectRenderer<DocxDocumentRenderer, MdTable>
{
    protected override void Write(DocxDocumentRenderer renderer, MdTable table)
    {
        renderer.ForceCloseParagraph();

        var theme = context.Theme;
        var isToc = IsTableOfContents(table);
        var rows = table.OfType<MdTableRow>().ToList();

        // the table of contents has an empty header row
        if (isToc) rows = rows.Where(r => !r.IsHeader || r.OfType<MdTableCell>().Any(c => c.Any(b => b is LeafBlock { Inline.FirstChild: not null }))).ToList();

        var columnCount = table.ColumnDefinitions.Count;
        foreach (var row in rows) columnCount = Math.Max(columnCount, row.OfType<MdTableCell>().Sum(c => Math.Max(1, c.ColumnSpan)));
        if (columnCount == 0 || rows.Count == 0) return;

        // CSS: 10px padding, or "5px 10px" in the table of contents
        var verticalPadding = theme.ToTwips(isToc ? theme.MarginBasePx / 2 : theme.MarginBasePx).ToXml();
        var horizontalPadding = (short)theme.ToTwips(theme.MarginBasePx);
        var border = isToc ? BorderValues.Nil : BorderValues.Single;
        var oxTable = new OxTable(
            new TableProperties(
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Nil },
                    new LeftBorder { Val = BorderValues.Nil },
                    new BottomBorder { Val = border, Size = 4, Space = 0, Color = theme.TableBorderColor },
                    new RightBorder { Val = BorderValues.Nil },
                    new InsideHorizontalBorder { Val = border, Size = 4, Space = 0, Color = theme.TableBorderColor },
                    new InsideVerticalBorder { Val = BorderValues.Nil }),
                new TableLayout { Type = TableLayoutValues.Autofit },
                new TableCellMarginDefault(
                    new TopMargin { Width = verticalPadding, Type = TableWidthUnitValues.Dxa },
                    new TableCellLeftMargin { Width = horizontalPadding, Type = TableWidthValues.Dxa },
                    new BottomMargin { Width = verticalPadding, Type = TableWidthUnitValues.Dxa },
                    new TableCellRightMargin { Width = horizontalPadding, Type = TableWidthValues.Dxa })),
            new TableGrid(Enumerable.Range(0, columnCount).Select(_ => new GridColumn { Width = (ThemeStylesProcessor.TextWidth / columnCount).ToXml() })));

        DocxRenderContext.WriteBlock(renderer, oxTable);

        var bodyRowIndex = 0;
        foreach (var row in rows)
        {
            var oxRow = new OxTableRow();
            if (row.IsHeader) oxRow.AppendChild(new TableRowProperties(new CantSplit(), new TableHeader()));
            else oxRow.AppendChild(new TableRowProperties(new CantSplit()));

            // CSS: tr:nth-of-type(even) in the body has the alternate background
            var background = row.IsHeader ? null : bodyRowIndex++ % 2 == 1 ? theme.TableAlternateBackgroundColor : theme.TableBackgroundColor;

            var columnIndex = 0;
            foreach (var cell in row.OfType<MdTableCell>())
            {
                var span = Math.Max(1, cell.ColumnSpan);
                var definitionIndex = cell.ColumnIndex >= 0 ? cell.ColumnIndex : columnIndex;
                var alignment = definitionIndex < table.ColumnDefinitions.Count ? table.ColumnDefinitions[definitionIndex].Alignment : null;

                var oxCell = new OxTableCell(CellProperties(row.IsHeader && !isToc, background, span));
                DocxRenderContext.RenderInto(renderer, oxCell, () => renderer.WriteChildren(cell));
                FormatParagraphs(oxCell, row.IsHeader, alignment, isToc);
                oxRow.AppendChild(oxCell);
                columnIndex += span;
            }

            // Markdown rows may have fewer cells than the header
            for (; columnIndex < columnCount; columnIndex++)
            {
                var oxCell = new OxTableCell(CellProperties(row.IsHeader && !isToc, background, 1));
                FormatParagraphs(oxCell, row.IsHeader, null, isToc);
                oxRow.AppendChild(oxCell);
            }

            oxTable.AppendChild(oxRow);
        }

        // a paragraph is required after a table, otherwise consecutive tables are merged into one; it's as small as possible and
        // only adds the margin below the table
        DocxRenderContext.WriteBlock(renderer, SpacerParagraph(theme.ToTwips(theme.MarginBasePx * (isToc ? 4 : 2))));
    }

    internal static Paragraph SpacerParagraph(int after) => new(new ParagraphProperties(
        new SpacingBetweenLines { Before = "0", After = after.ToXml(), Line = "20", LineRule = LineSpacingRuleValues.Exact }),
        new Run(new RunProperties(new FontSize { Val = "2" }, new FontSizeComplexScript { Val = "2" })));

    /// <summary>
    /// The table of contents is a table directly after a "&lt;div class="toc"&gt;" (see TableOfContentsPlaceholder).
    /// </summary>
    private static bool IsTableOfContents(MdTable table)
    {
        if (table.Parent is not { } parent) return false;
        var index = parent.IndexOf(table);
        return index > 0 && parent[index - 1] is HtmlBlock html && html.Lines.ToString().Contains("class=\"toc\"", StringComparison.OrdinalIgnoreCase);
    }

    private TableCellProperties CellProperties(bool withHeaderBorder, string? background, int span)
    {
        var theme = context.Theme;
        var properties = new TableCellProperties(new TableCellWidth { Width = "0", Type = TableWidthUnitValues.Auto });
        if (span > 1) properties.AppendChild(new GridSpan { Val = span });

        if (withHeaderBorder)
        {
            // CSS: 2px border below the header
            properties.AppendChild(new TableCellBorders(new BottomBorder
            {
                Val = BorderValues.Single, Size = (uint)Math.Round(theme.ToPoints(2) * 8), Space = 0, Color = theme.TableBorderColor
            }));
        }

        if (background != null) properties.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = background });
        properties.AppendChild(new TableCellVerticalAlignment { Val = withHeaderBorder ? TableVerticalAlignmentValues.Bottom : TableVerticalAlignmentValues.Top });
        return properties;
    }

    /// <summary>
    /// CSS: smaller text in the cells, the header in the heading font and bold, links in the table of contents in the text color.
    /// </summary>
    private void FormatParagraphs(OxTableCell cell, bool isHeader, TableColumnAlign? alignment, bool isToc)
    {
        var theme = context.Theme;
        var sizePx = isHeader ? theme.FontSizeTableHeaderPx : theme.FontSizeTableCellPx;
        var fontSize = theme.ToHalfPoints(sizePx);
        var codeFontSize = theme.ToHalfPoints(sizePx * 0.85);

        if (!cell.Elements<Paragraph>().Any()) cell.AppendChild(new Paragraph());

        foreach (var paragraph in cell.Elements<Paragraph>())
        {
            var pPr = paragraph.ParagraphProperties ??= new ParagraphProperties();
            if (pPr.ParagraphStyleId?.Val?.Value is not (null or ThemeStylesProcessor.NormalStyle)) continue; // eg. lists

            if (isHeader) pPr.KeepNext = new KeepNext();
            pPr.SpacingBetweenLines = new SpacingBetweenLines { Before = "0", After = "0" };
            pPr.Justification = new Justification
            {
                Val = alignment switch
                {
                    TableColumnAlign.Center => JustificationValues.Center,
                    TableColumnAlign.Right => JustificationValues.Right,
                    _ => JustificationValues.Left
                }
            };
            pPr.ParagraphMarkRunProperties = new ParagraphMarkRunProperties(new FontSize { Val = fontSize }, new FontSizeComplexScript { Val = fontSize });

            foreach (var run in paragraph.Descendants<Run>())
            {
                var rPr = run.RunProperties ??= new RunProperties();
                var size = rPr.RunStyle?.Val?.Value == ThemeStylesProcessor.CodeInlineStyle ? codeFontSize : fontSize;
                rPr.FontSize = new FontSize { Val = size };
                rPr.FontSizeComplexScript = new FontSizeComplexScript { Val = size };

                if (isHeader)
                {
                    if (size != codeFontSize) rPr.RunFonts ??= new RunFonts { Ascii = theme.HeadingFontFamily, HighAnsi = theme.HeadingFontFamily, ComplexScript = theme.HeadingFontFamily };
                    rPr.Bold = new Bold();
                }

                if (isToc && run.Ancestors<Hyperlink>().Any()) rPr.Color = new Color { Val = theme.TextColor };
            }
        }
    }
}
