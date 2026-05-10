using System.Collections.Generic;
using Adliance.AspNetCore.Buddy.Pdf.V2;
using Adliance.QmDoc.Parameters;
using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Themes;

namespace Adliance.QmDoc.Converter;

public class PdfConverter(PdfParameters parameters, Options.Options options) : Converter(TargetExtension.Pdf, parameters, options)
{
    private readonly Options.Options _options = options;

    protected override byte[] Convert(ConverterFile file, string markdown)
    {
        var theme = string.IsNullOrWhiteSpace(parameters.Theme) ? _options.Theme : parameters.Theme;
        var html = LoadHtml(file, markdown);

        var settings = ThemeProvider.GetOptions(theme);
        var pdfOptions = new PdfOptions
        {
            FooterHtml = ApplyCommonPlaceholders(file, ThemeProvider.GetFooter(theme)),
            HeaderHtml = ApplyCommonPlaceholders(file, ThemeProvider.GetHeader(theme)),
            FooterHeight = settings.Pdf.FooterHeight,
            HeaderHeight = settings.Pdf.HeaderHeight
        };
        var pdfer = new AdliancePdfer(new AdliancePdferSettings());
        var pdf = pdfer.HtmlToPdf(html, pdfOptions).GetAwaiter().GetResult();
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
