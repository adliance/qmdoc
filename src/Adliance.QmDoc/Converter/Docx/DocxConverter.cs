using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Adliance.QmDoc.Parameters;
using Adliance.QmDoc.Processors.MarkdownProcessors;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Docx;
using Microsoft.Extensions.Logging.Abstractions;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;

namespace Adliance.QmDoc.Converter.Docx;

public partial class DocxConverter(DocxParameters parameters, Options.Options options) : Converter(TargetExtension.Docx, parameters, options)
{
    private const string DefaultLanguage = "de-DE";

    protected override async Task<byte[]> Convert(ConverterFile file, MarkdownProcessorContext markdownContext)
    {
        var theme = DocxTheme.Load(GetTheme(markdownContext));
        var context = new DocxRenderContext(file.SourceAbsolutePath, theme);

        var document = DocxTemplateHelper.Standard;
        ThemeStylesProcessor.Apply(document, theme, GetLanguage(file, markdownContext));

        var renderer = new DocxDocumentRenderer(document, ThemeStylesProcessor.CreateDocumentStyles(), NullLogger<DocxDocumentRenderer>.Instance);
        IMarkdownObjectRenderer[] ownRenderers =
        [
            // before the default renderers, so that they take precedence
            new PageBreakRenderer(),
            new HeadingRenderer(context),
            new LinkRenderer(context),
            new AlertBlockRenderer(context),
            new FigureRenderer(context),
            new TableRenderer(context),
            new FootnoteLinkRenderer(context),
            new FootnoteGroupRenderer(context)
        ];
        for (var i = 0; i < ownRenderers.Length; i++) renderer.ObjectRenderers.Insert(i, ownRenderers[i]);

        var markdownDocument = Markdown.Parse(markdownContext.Markdown, markdownContext.Pipeline);
        context.CollectHeadings(markdownDocument);
        markdownContext.Pipeline.Setup(renderer);
        renderer.Render(markdownDocument);

        CenterImages(document.MainDocumentPart!.Document.Body!);
        ThemeStylesProcessor.ApplyAfterRendering(document, theme);
        IconBlocksProcessor.Apply(document, theme);
        var authorParagraphs = AuthorLineProcessor.Apply(document, markdownContext.Frontmatter.Author);
        var title = ApplyCommonPlaceholders(file, "{{ TITLE }}", markdownContext);
        if (markdownContext.Frontmatter.EnableDocumentTitle != false) TitleBlockProcessor.Apply(document, title, authorParagraphs);
        FooterProcessor.Apply(document, title, ApplyCommonPlaceholders(file, "{{ GIT_DATE_VERSION }}", markdownContext));

        foreach (var e in context.Errors) Program.WriteLine($"\t\t {e.ErrorMessage}");

        var tempPath = Path.GetTempFileName();
        document.SaveAs(tempPath).Close();

        var bytes = await File.ReadAllBytesAsync(tempPath);
        File.Delete(tempPath);
        return bytes;
    }

    protected override void PrepareAdditionalProcessors(ConverterFile file, IList<IMarkdownProcessor> markdownProcessors)
    {
        markdownProcessors.Add(new LinkToChapters(file.SourceAbsolutePath));
    }

    /// <summary>
    /// The language of the document (for spell checking and hyphenation): the language of the file name (eg. "report.en.md"),
    /// otherwise the culture of the frontmatter, otherwise German, like the HTML/PDF.
    /// </summary>
    internal static string GetLanguage(ConverterFile file, MarkdownProcessorContext markdownContext)
    {
        var languageCode = Path.GetExtension(Path.GetFileNameWithoutExtension(file.SourceAbsolutePath)).TrimStart('.');
        if (LanguageCodeRegex().IsMatch(languageCode) && TryGetSpecificCulture(languageCode, out var fromFileName)) return fromFileName;
        if (!string.IsNullOrWhiteSpace(markdownContext.Frontmatter.Culture) && TryGetSpecificCulture(markdownContext.Frontmatter.Culture, out var fromCulture)) return fromCulture;
        return DefaultLanguage;
    }

    private static bool TryGetSpecificCulture(string name, out string result)
    {
        result = DefaultLanguage;
        try
        {
            var culture = CultureInfo.CreateSpecificCulture(name.Trim());
            if (string.IsNullOrEmpty(culture.Name)) return false;
            result = culture.Name;
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Images on their own are centered, like in the HTML/PDF.
    /// </summary>
    private static void CenterImages(Body body)
    {
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            if (!paragraph.Descendants<DW.Inline>().Any() || paragraph.Descendants<Text>().Any(t => !string.IsNullOrWhiteSpace(t.Text))) continue;
            if (paragraph.ParagraphProperties?.NumberingProperties != null) continue;

            var pPr = paragraph.ParagraphProperties ??= new ParagraphProperties();
            pPr.Justification = new Justification { Val = JustificationValues.Center };
        }
    }

    [GeneratedRegex(@"^[a-z]{2,3}(-[a-z]{2})?$", RegexOptions.IgnoreCase)]
    private static partial Regex LanguageCodeRegex();
}
