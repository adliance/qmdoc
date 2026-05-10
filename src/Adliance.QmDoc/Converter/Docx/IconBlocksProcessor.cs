using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Adliance.QmDoc.Converter.Docx;

internal static class IconBlocksProcessor
{
    private sealed record IconBlockStyle(string Prefix, string Fill, string BorderColor);

    private static readonly IconBlockStyle[] Styles =
    [
        new("{!!}", "FCE4D6", "C55A11"), // danger  – light red,    orange-red border
        new("{!}",  "D6E4F0", "2E74B5"), // alert   – light blue,   blue border
        new("{?}",  "FFF2CC", "D6B656"), // question– light yellow,  amber border
    ];

    internal static void Apply(WordprocessingDocument document)
    {
        var body = document.MainDocumentPart!.Document.Body!;

        foreach (var para in body.Descendants<Paragraph>().ToList())
        {
            var fullText = string.Concat(para.Descendants<Text>().Select(t => t.Text));

            var style = Styles.FirstOrDefault(s => fullText.StartsWith(s.Prefix + " ") || fullText == s.Prefix);
            if (style == null) continue;

            var stripLen = style.Prefix.Length + (fullText.Length > style.Prefix.Length ? 1 : 0); // +1 for the space
            StripLeadingText(para, stripLen);

            var pPr = para.ParagraphProperties;
            if (pPr == null)
            {
                pPr = new ParagraphProperties();
                para.PrependChild(pPr);
            }

            pPr.Shading = new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = style.Fill };
            pPr.ParagraphBorders = new ParagraphBorders(
                new LeftBorder { Val = BorderValues.Single, Color = style.BorderColor, Size = 24, Space = 4 }
            );
            pPr.Indentation = new Indentation { Left = "180" };
        }
    }

    private static void StripLeadingText(Paragraph para, int charCount)
    {
        var remaining = charCount;
        foreach (var text in para.Descendants<Text>())
        {
            if (remaining <= 0) break;
            if (text.Text.Length <= remaining)
            {
                remaining -= text.Text.Length;
                text.Text = string.Empty;
            }
            else
            {
                text.Text = text.Text[remaining..];
                remaining = 0;
            }
        }
    }
}
