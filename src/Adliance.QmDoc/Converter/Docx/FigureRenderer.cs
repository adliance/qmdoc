using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Extensions.Figures;
using Markdig.Renderers.Docx;
using Markdig.Renderers.Html;

namespace Adliance.QmDoc.Converter.Docx;

/// <summary>
/// Renders figures ("^^^") like the CSS of the theme: a box with a background and border around the image, with the caption below it.
/// The box is as wide as its content and centered, or floating on the left/right side if the caption has the class ".left"/".right".
/// </summary>
internal sealed class FigureRenderer(DocxRenderContext context) : DocxObjectRenderer<Figure>
{
    protected override void WriteObject(DocxDocumentRenderer renderer, Figure figure)
    {
        renderer.ForceCloseParagraph();

        var theme = context.Theme;
        var classes = figure.OfType<FigureCaption>().SelectMany(c => c.GetAttributes().Classes ?? []).Concat(figure.GetAttributes().Classes ?? []).ToList();
        var floating = classes.Contains("left") ? "left" : classes.Contains("right") ? "right" : null;

        var padding = theme.ToTwips(16); // CSS: 1rem
        var gap = theme.ToTwips(8); // CSS: .5rem margin of floating figures
        var borderSize = (uint)System.Math.Max(2, System.Math.Round(theme.ToPoints(1) * 8));

        var tableProperties = new TableProperties();
        if (floating != null)
        {
            tableProperties.AppendChild(new TablePositionProperties
            {
                LeftFromText = (short)(floating == "right" ? gap : 0),
                RightFromText = (short)(floating == "left" ? gap : 0),
                BottomFromText = (short)theme.ToTwips(2),
                VerticalAnchor = VerticalAnchorValues.Text,
                HorizontalAnchor = HorizontalAnchorValues.Margin,
                TablePositionXAlignment = floating == "left" ? HorizontalAlignmentValues.Left : HorizontalAlignmentValues.Right,
                TablePositionY = 0
            });
        }

        tableProperties.AppendChild(new TableWidth { Width = "0", Type = TableWidthUnitValues.Auto });
        if (floating == null) tableProperties.AppendChild(new TableJustification { Val = TableRowAlignmentValues.Center });
        tableProperties.AppendChild(new TableBorders(
            new TopBorder { Val = BorderValues.Single, Size = borderSize, Space = 0, Color = theme.FigureBorderColor },
            new LeftBorder { Val = BorderValues.Single, Size = borderSize, Space = 0, Color = theme.FigureBorderColor },
            new BottomBorder { Val = BorderValues.Single, Size = borderSize, Space = 0, Color = theme.FigureBorderColor },
            new RightBorder { Val = BorderValues.Single, Size = borderSize, Space = 0, Color = theme.FigureBorderColor }));
        tableProperties.AppendChild(new TableLayout { Type = TableLayoutValues.Autofit });
        tableProperties.AppendChild(new TableCellMarginDefault(
            new TopMargin { Width = padding.ToXml(), Type = TableWidthUnitValues.Dxa },
            new TableCellLeftMargin { Width = (short)padding, Type = TableWidthValues.Dxa },
            new BottomMargin { Width = padding.ToXml(), Type = TableWidthUnitValues.Dxa },
            new TableCellRightMargin { Width = (short)padding, Type = TableWidthValues.Dxa }));

        var cell = new TableCell(new TableCellProperties(
            new TableCellWidth { Width = "0", Type = TableWidthUnitValues.Auto },
            new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = theme.FigureBackgroundColor }));
        var table = new Table(tableProperties, new TableGrid(new GridColumn()), new TableRow(new TableRowProperties(new CantSplit()), cell));
        DocxRenderContext.WriteBlock(renderer, table);

        // floating figures have a max. width of 40%, like in the CSS
        var maxWidth = (floating == null ? ThemeStylesProcessor.TextWidth : ThemeStylesProcessor.TextWidth * 4 / 10) - 2 * padding - 40;
        context.PushMaxImageWidth(maxWidth);
        DocxRenderContext.RenderInto(renderer, cell, () =>
        {
            foreach (var block in figure)
            {
                if (block is FigureCaption caption) WriteCaption(renderer, caption);
                else renderer.Write(block);
            }
        });
        context.PopMaxImageWidth();

        foreach (var paragraph in cell.Elements<Paragraph>())
        {
            var pPr = paragraph.ParagraphProperties ??= new ParagraphProperties();
            pPr.KeepNext = new KeepNext();
            if (pPr.ParagraphStyleId?.Val?.Value == ThemeStylesProcessor.CaptionStyle) continue;
            pPr.SpacingBetweenLines = new SpacingBetweenLines { Before = "0", After = "0" };
            pPr.Justification = new Justification { Val = JustificationValues.Center };
        }

        if (!cell.Elements<Paragraph>().Any()) cell.AppendChild(new Paragraph());
        var lastParagraph = cell.Elements<Paragraph>().Last();
        if (lastParagraph.ParagraphProperties != null) lastParagraph.ParagraphProperties.KeepNext = null; // null for an empty figure

        if (floating == null) DocxRenderContext.WriteBlock(renderer, TableRenderer.SpacerParagraph(theme.ToTwips(theme.MarginBasePx * 2)));
    }

    private void WriteCaption(DocxDocumentRenderer renderer, FigureCaption caption)
    {
        var paragraph = new Paragraph(new ParagraphProperties(new ParagraphStyleId { Val = ThemeStylesProcessor.CaptionStyle }));
        DocxRenderContext.WriteBlock(renderer, paragraph);
        DocxRenderContext.RenderInto(renderer, paragraph, () => WriteLeafInline(renderer, caption));
        TrimTrailingWhitespace(paragraph);
    }

    /// <summary>
    /// The generic attributes ("{.right}") are removed from the caption, but the space before them is still there.
    /// </summary>
    private static void TrimTrailingWhitespace(OpenXmlElement paragraph)
    {
        var last = paragraph.Descendants<Text>().LastOrDefault();
        if (last != null) last.Text = last.Text.TrimEnd();
    }
}
