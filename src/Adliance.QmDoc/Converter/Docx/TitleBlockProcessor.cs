using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Adliance.QmDoc.Converter.Docx;

internal static class TitleBlockProcessor
{
    internal static void Apply(WordprocessingDocument document, string title)
    {
        var body = document.MainDocumentPart!.Document.Body!;

        var para = new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "480" },
                new ParagraphBorders(
                    new BottomBorder { Val = BorderValues.Single, Color = "134094", Size = 12, Space = 6 }
                )
            ),
            new Run(
                new RunProperties(
                    new FontSize { Val = "72" },
                    new FontSizeComplexScript { Val = "72" },
                    new Bold(),
                    new Color { Val = "134094" }
                ),
                new Text(title)
            )
        );

        body.PrependChild(para);
    }
}
