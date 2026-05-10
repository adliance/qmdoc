using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Adliance.QmDoc.Converter.Docx;

internal static class AuthorLineProcessor
{
    private static readonly Regex Pattern = new(@"^(.+?) \| (.+?) \| (.+)$", RegexOptions.Compiled);

    internal static void Apply(WordprocessingDocument document)
    {
        var body = document.MainDocumentPart!.Document.Body!;

        foreach (var para in body.Descendants<Paragraph>().ToList())
        {
            var text = string.Concat(para.Descendants<Text>().Select(t => t.Text));
            var match = Pattern.Match(text);
            if (!match.Success) continue;

            var name = match.Groups[1].Value;
            var company = match.Groups[2].Value;
            var email = match.Groups[3].Value;

            // Replace paragraph content with formatted author block
            foreach (var child in para.ChildElements.OfType<Run>().ToList())
                child.Remove();

            // Reuse or create ParagraphProperties with author styling
            var pPr = para.ParagraphProperties;
            if (pPr == null)
            {
                pPr = new ParagraphProperties();
                para.PrependChild(pPr);
            }

            pPr.ParagraphStyleId = null; // clear any heading style
            pPr.SpacingBetweenLines = new SpacingBetweenLines { Before = "0", After = "240" };

            var gray = new Color { Val = "7F7F7F" };

            // "Name, Company" run
            para.AppendChild(new Run(
                new RunProperties(new Bold(), gray.CloneNode(true) as Color ?? new Color { Val = "7F7F7F" }),
                new Text($"{name}, {company}") { Space = SpaceProcessingModeValues.Preserve }
            ));

            // Line break
            para.AppendChild(new Run(new Break()));

            // Email run (italic, slightly smaller)
            para.AppendChild(new Run(
                new RunProperties(
                    new Italic(),
                    gray.CloneNode(true) as Color ?? new Color { Val = "7F7F7F" },
                    new FontSize { Val = "18" },
                    new FontSizeComplexScript { Val = "18" }
                ),
                new Text(email) { Space = SpaceProcessingModeValues.Preserve }
            ));
        }
    }
}
