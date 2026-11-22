using System.Collections.Generic;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Adliance.QmDoc.Converter.Docx;

/// <summary>
/// Adds the document title at the beginning of the document, like the "document-title" of the HTML/PDF layout.
/// </summary>
internal static class TitleBlockProcessor
{
    internal static void Apply(WordprocessingDocument document, string title, ICollection<Paragraph> authorParagraphs)
    {
        var body = document.MainDocumentPart!.Document.Body!;
        var properties = new ParagraphProperties(new ParagraphStyleId { Val = ThemeStylesProcessor.TitleStyle });

        // CSS: no space between the title and the author line directly below it
        if (body.FirstChild is Paragraph first && authorParagraphs.Contains(first)) properties.SpacingBetweenLines = new SpacingBetweenLines { After = "0" };

        body.PrependChild(new Paragraph(properties, new Run(new Text(title) { Space = SpaceProcessingModeValues.Preserve })));
    }
}
