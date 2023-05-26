using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using Adliance.AspNetCore.Buddy.Pdf.V2;
using Adliance.QmDoc.Extensions;
using Adliance.QmDoc.Parameters;
using Adliance.QmDoc.Processors.HtmlProcessors;
using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Themes;
using Markdig;

namespace Adliance.QmDoc.Converter;

public class PdfConverter : Converter
{
    private readonly PdfParameters _parameters;
    private readonly Options.Options _options;

    public PdfConverter(PdfParameters parameters, Options.Options options) : base(TargetExtension.Pdf, parameters, options)
    {
        _parameters = parameters;
        _options = options;
    }

    protected override byte[] Convert(ConverterFile file, string markdown)
    {
        var theme = string.IsNullOrWhiteSpace(_parameters.Theme) ? _options.Theme : _parameters.Theme;
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
        markdownProcessors.Add(new LinkedDocumentsPlaceholder(file.SourceAbsolutePath)); // add after the "LinkToDocuments" step, because that one fills the context with the linked documents 
    }
}

public class AdliancePdferSettings : IPdferConfiguration
{
    public string ServerUrl => "https://pdf2.adliance.dev";
}