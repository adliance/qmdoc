using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Adliance.QmDoc.Converter.Docx;

/// <summary>
/// Renders the author line like the HTML/PDF: either the author from the frontmatter at the beginning of the document,
/// or headings like "#### Name | Company | Email".
/// </summary>
internal static partial class AuthorLineProcessor
{
    /// <summary>
    /// Returns the paragraphs of the author lines.
    /// </summary>
    internal static List<Paragraph> Apply(WordprocessingDocument document, string? frontmatterAuthor)
    {
        var body = document.MainDocumentPart!.Document.Body!;
        var result = new List<Paragraph>();

        if (!string.IsNullOrWhiteSpace(frontmatterAuthor))
        {
            var para = new Paragraph();
            Format(para);
            para.AppendChild(TextRun(frontmatterAuthor, bold: true));
            para.AppendChild(TextRun(", Adliance GmbH", bold: false));
            body.PrependChild(para);
            result.Add(para);
            return result;
        }

        foreach (var para in body.Elements<Paragraph>().ToList())
        {
            if (para.ParagraphProperties?.ParagraphStyleId?.Val?.Value != ThemeStylesProcessor.HeadingStylePrefix + 4) continue;

            var text = string.Concat(para.Descendants<Text>().Select(t => t.Text));
            var match = Pattern().Match(text);
            if (!match.Success) continue;

            var name = match.Groups[1].Value;
            var company = match.Groups[2].Value;
            var email = match.Groups[3].Value;

            foreach (var child in para.ChildElements.Where(c => c is not ParagraphProperties).ToList())
                child.Remove();

            Format(para);
            para.AppendChild(TextRun(name, bold: true));
            para.AppendChild(TextRun(", ", bold: false));
            para.AppendChild(TextRun(company, bold: true));
            para.AppendChild(new Run(new Break()));
            para.AppendChild(EmailLink(document, email));
            result.Add(para);
        }

        return result;
    }

    private static void Format(Paragraph para)
    {
        para.ParagraphProperties = new ParagraphProperties(new ParagraphStyleId { Val = ThemeStylesProcessor.SubtitleStyle });
    }

    private static Run TextRun(string text, bool bold)
    {
        var run = new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        if (bold) run.RunProperties = new RunProperties(new Bold());
        return run;
    }

    private static OpenXmlElement EmailLink(WordprocessingDocument document, string email)
    {
        var run = TextRun(email, bold: false);
        if (!Uri.TryCreate("mailto:" + email.Trim(), UriKind.Absolute, out var uri)) return run;

        var relationship = document.MainDocumentPart!.AddHyperlinkRelationship(uri, true);
        return new Hyperlink(run) { Id = relationship.Id };
    }

    [GeneratedRegex(@"^(.+?) \| (.+?) \| (.+)$")]
    private static partial Regex Pattern();
}
