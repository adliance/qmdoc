using System.Collections.Generic;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Renderers;
using Markdig.Renderers.Docx;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using OxTable = DocumentFormat.OpenXml.Wordprocessing.Table;
using OxTableCell = DocumentFormat.OpenXml.Wordprocessing.TableCell;
using OxTableRow = DocumentFormat.OpenXml.Wordprocessing.TableRow;

namespace Adliance.QmDoc.Converter;

public class TableRenderer : MarkdownObjectRenderer<DocxDocumentRenderer, Markdig.Extensions.Tables.Table>
{
    protected override void Write(DocxDocumentRenderer renderer, Markdig.Extensions.Tables.Table table)
    {
        renderer.ForceCloseParagraph();

        var oxTable = BuildTable(table);
        renderer.Cursor.Write(oxTable);
        renderer.Cursor.SetAfter(oxTable);

        // DOCX requires a paragraph after the last table in the body.
        var trailingPara = new Paragraph();
        renderer.Cursor.Write(trailingPara);
        renderer.Cursor.SetAfter(trailingPara);
    }

    private static OxTable BuildTable(Markdig.Extensions.Tables.Table markdigTable)
    {
        var table = new OxTable(
            new TableProperties(
                new TableWidth { Width = "0", Type = TableWidthUnitValues.Auto },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4, Space = 0 },
                    new BottomBorder { Val = BorderValues.Single, Size = 4, Space = 0 },
                    new LeftBorder { Val = BorderValues.Single, Size = 4, Space = 0 },
                    new RightBorder { Val = BorderValues.Single, Size = 4, Space = 0 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Space = 0 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Space = 0 }
                )
            )
        );

        foreach (var block in markdigTable)
        {
            if (block is not Markdig.Extensions.Tables.TableRow row) continue;

            var oxRow = new OxTableRow();
            if (row.IsHeader)
                oxRow.AppendChild(new TableRowProperties(new TableHeader()));

            foreach (var cellBlock in row)
            {
                if (cellBlock is not Markdig.Extensions.Tables.TableCell cell) continue;

                var para = new Paragraph();
                foreach (var run in BuildRuns(cell, row.IsHeader))
                    para.AppendChild(run);

                oxRow.AppendChild(new OxTableCell(para));
            }

            table.AppendChild(oxRow);
        }

        return table;
    }

    private static IEnumerable<Run> BuildRuns(MarkdownObject container, bool forceBold)
    {
        foreach (var inline in container.Descendants<LiteralInline>())
        {
            var bold = forceBold || IsInsideStrong(inline);
            var italic = IsInsideEmphasis(inline);

            var run = new Run();
            if (bold || italic)
            {
                var rpr = new RunProperties();
                if (bold) rpr.AppendChild(new Bold());
                if (italic) rpr.AppendChild(new Italic());
                run.AppendChild(rpr);
            }

            run.AppendChild(new Text(inline.Content.ToString()) { Space = SpaceProcessingModeValues.Preserve });
            yield return run;
        }
    }

    private static bool IsInsideStrong(Inline node)
    {
        var parent = node.Parent;
        while (parent != null)
        {
            if (parent is EmphasisInline { DelimiterCount: 2 }) return true;
            parent = parent.Parent;
        }
        return false;
    }

    private static bool IsInsideEmphasis(Inline node)
    {
        var parent = node.Parent;
        while (parent != null)
        {
            if (parent is EmphasisInline { DelimiterCount: 1 }) return true;
            parent = parent.Parent;
        }
        return false;
    }
}
