using System.Collections.Generic;
using System.Threading.Tasks;
using Adliance.AspNetCore.Buddy.Pdf.V2;
using Adliance.QmDoc.Parameters;
using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Themes;

namespace Adliance.QmDoc.Converter;

public class PdfConverter(PdfParameters parameters, Options.Options options) : Converter(TargetExtension.Pdf, parameters, options)
{
    private readonly Options.Options _options = options;

    protected override async Task<byte[]> Convert(ConverterFile file, MarkdownProcessorContext markdownContext)
    {
        var theme = string.IsNullOrWhiteSpace(parameters.Theme) ? _options.Theme : parameters.Theme;
        var settings = ThemeProvider.GetOptions(theme);
        var pdfOptions = new PdfOptions
        {
            FooterHtml = ApplyCommonPlaceholders(file, ThemeProvider.GetFooter(GetTheme(markdownContext)), markdownContext),
            HeaderHtml = ApplyCommonPlaceholders(file, ThemeProvider.GetHeader(GetTheme(markdownContext)), markdownContext),
            FooterHeight = settings.Pdf.FooterHeight,
            HeaderHeight = settings.Pdf.HeaderHeight,
            Outline = true
        };

        var pdfer = new AdliancePdfer(new AdliancePdferSettings());

        if (TableOfContentsPlaceholder.ContainsTocPlaceholder(markdownContext))
        {
            Program.WriteLine("\tTOC detected, second pass required");
            var firstPassHtml = RunHtmlProcessors(file, markdownContext);
            var firstPassPdf = await pdfer.HtmlToPdf(firstPassHtml, pdfOptions);
            var firstPassMetadata = await pdfer.GetPdfMetadata(firstPassPdf);
            markdownContext.ResetForSecondPass(firstPassMetadata);
            markdownContext = RunMarkdownProcessors(file, markdownContext);
        }

        var html = RunHtmlProcessors(file, markdownContext);
        var pdf = await pdfer.HtmlToPdf(html, pdfOptions);
        return pdf;
    }

    protected override void PrepareAdditionalProcessors(ConverterFile file, IList<IMarkdownProcessor> markdownProcessors)
    {
        markdownProcessors.Add(new LinkToChapters());
        markdownProcessors.Add(new PageBreak());
        markdownProcessors.Add(new LinkToDocuments(file.SourceBaseDirectory, file.SourceAbsolutePath));
        markdownProcessors.Add(new LinkedDocumentsPlaceholder()); // add after the "LinkToDocuments" step, because that one fills the context with the linked documents
    }
}

public class AdliancePdferSettings : IPdferConfiguration
{
    public string ServerUrl => "https://pdf2.adliance.dev";
    public string? ApiKeyPdf => null;
    public string? ApiKeyTemplate => null;
}
