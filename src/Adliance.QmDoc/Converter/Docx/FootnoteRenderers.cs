using System.Globalization;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Extensions.Footnotes;
using Markdig.Renderers;
using Markdig.Renderers.Docx;
using MdFootnote = Markdig.Extensions.Footnotes.Footnote;

namespace Adliance.QmDoc.Converter.Docx;

/// <summary>
/// Renders the references to footnotes ("[^1]") as references to Word endnotes. Endnotes are shown at the end of the document,
/// just like the footnotes in the HTML/PDF.
/// </summary>
internal sealed class FootnoteLinkRenderer(DocxRenderContext context) : DocxObjectRenderer<FootnoteLink>
{
    protected override void WriteObject(DocxDocumentRenderer renderer, FootnoteLink link)
    {
        if (link.IsBackLink) return;

        var runProperties = new RunProperties(new RunStyle { Val = ThemeStylesProcessor.EndnoteReferenceStyle });
        var order = link.Footnote.Order;

        // Word allows only one reference per endnote, further references just show the number
        var run = context.ReferencedFootnotes.Add(order)
            ? new Run(runProperties, new EndnoteReference { Id = order })
            : new Run(runProperties, new Text(order.ToString(CultureInfo.InvariantCulture)));
        renderer.Cursor.Write(run);
    }
}

/// <summary>
/// Renders the footnotes (collected by Markdig at the end of the document) as Word endnotes.
/// </summary>
internal sealed class FootnoteGroupRenderer(DocxRenderContext context) : MarkdownObjectRenderer<DocxDocumentRenderer, FootnoteGroup>
{
    protected override void Write(DocxDocumentRenderer renderer, FootnoteGroup group)
    {
        renderer.ForceCloseParagraph();

        var mainPart = renderer.Document.MainDocumentPart!;
        foreach (var footnote in group.OfType<MdFootnote>().OrderBy(f => f.Order))
        {
            if (!context.ReferencedFootnotes.Contains(footnote.Order)) continue;

            var endnotesPart = GetOrCreateEndnotesPart(renderer.Document);
            var endnote = new Endnote { Id = footnote.Order };
            endnotesPart.Endnotes!.AppendChild(endnote);

            DocxRenderContext.RenderInto(renderer, endnote, () => renderer.WriteChildren(footnote));

            if (!endnote.Elements<Paragraph>().Any()) endnote.AppendChild(new Paragraph());
            foreach (var paragraph in endnote.Elements<Paragraph>())
            {
                var pPr = paragraph.ParagraphProperties ??= new ParagraphProperties();
                pPr.ParagraphStyleId = new ParagraphStyleId { Val = ThemeStylesProcessor.EndnoteTextStyle };
            }

            var first = endnote.Elements<Paragraph>().First();
            var referenceMark = new Run(new RunProperties(new RunStyle { Val = ThemeStylesProcessor.EndnoteReferenceStyle }), new EndnoteReferenceMark());
            first.InsertAfter(referenceMark, first.ParagraphProperties);
            first.InsertAfter(new Run(new Text(" ") { Space = SpaceProcessingModeValues.Preserve }), referenceMark);

            MoveHyperlinkRelationships(mainPart, endnotesPart, endnote);
        }
    }

    private static EndnotesPart GetOrCreateEndnotesPart(WordprocessingDocument document)
    {
        var mainPart = document.MainDocumentPart!;
        if (mainPart.EndnotesPart != null) return mainPart.EndnotesPart;

        var part = mainPart.AddNewPart<EndnotesPart>();
        part.Endnotes = new Endnotes(
            new Endnote(SeparatorParagraph(new SeparatorMark())) { Type = FootnoteEndnoteValues.Separator, Id = -1 },
            new Endnote(SeparatorParagraph(new ContinuationSeparatorMark())) { Type = FootnoteEndnoteValues.ContinuationSeparator, Id = 0 });

        // endnotes are numbered with roman numbers by default
        var settings = mainPart.DocumentSettingsPart?.Settings;
        if (settings != null && settings.GetFirstChild<EndnoteDocumentWideProperties>() == null)
        {
            var properties = new EndnoteDocumentWideProperties(
                new NumberingFormat { Val = NumberFormatValues.Decimal },
                new EndnoteSpecialReference { Id = -1 },
                new EndnoteSpecialReference { Id = 0 });

            // the endnote properties must come before the compatibility settings
            var next = settings.ChildElements.FirstOrDefault(e => e is Compatibility or DocumentVariables or Rsids or AttachedSchema or ThemeFontLanguages
                or ColorSchemeMapping or DoNotIncludeSubdocsInStats or DoNotAutoCompressPictures or ForceUpgrade or Captions or ReadModeInkLockDown
                or DecimalSymbol or ListSeparator || e.NamespaceUri != settings.NamespaceUri);
            if (next != null) settings.InsertBefore(properties, next);
            else settings.AppendChild(properties);
        }

        return part;
    }

    private static Paragraph SeparatorParagraph(OpenXmlElement mark) =>
        new(new ParagraphProperties(new SpacingBetweenLines { After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto }), new Run(mark));

    /// <summary>
    /// Hyperlinks are rendered with a relationship of the main document part, but the endnotes are in a separate part.
    /// </summary>
    private static void MoveHyperlinkRelationships(MainDocumentPart mainPart, EndnotesPart endnotesPart, Endnote endnote)
    {
        foreach (var hyperlink in endnote.Descendants<Hyperlink>().Where(h => h.Id?.Value != null))
        {
            var relationship = mainPart.HyperlinkRelationships.FirstOrDefault(r => r.Id == hyperlink.Id!.Value);
            if (relationship == null) continue;

            var moved = endnotesPart.AddHyperlinkRelationship(relationship.Uri, relationship.IsExternal);
            mainPart.DeleteReferenceRelationship(relationship);
            hyperlink.Id = moved.Id;
        }
    }
}
