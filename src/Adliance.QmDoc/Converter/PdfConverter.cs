using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Adliance.AspNetCore.Buddy.Pdf.V2;
using Adliance.QmDoc.Parameters;
using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Themes;

namespace Adliance.QmDoc.Converter;

public class PdfConverter(ThemedConversionParameters parameters, Options.Options options) : Converter(TargetExtension.Pdf, parameters, options)
{
    private readonly Options.Options _options = options;

    protected override async Task<byte[]> Convert(ConverterFile file, MarkdownProcessorContext markdownContext)
    {
        var theme = string.IsNullOrWhiteSpace(parameters.Theme) ? _options.Theme : parameters.Theme;
        var settings = ThemeProvider.GetOptions(theme);

        var pdfOptions = new PdfOptions
        {
            FooterHtml = markdownContext.Frontmatter.EnableFooter == false ? null : ApplyCommonPlaceholders(file, ThemeProvider.GetFooter(GetTheme(markdownContext)), markdownContext),
            HeaderHtml =  markdownContext.Frontmatter.EnableHeader == false ? null : ApplyCommonPlaceholders(file, ThemeProvider.GetHeader(GetTheme(markdownContext)), markdownContext),
            FooterHeight = settings.Pdf.FooterHeight,
            HeaderHeight = settings.Pdf.HeaderHeight,
            Outline = true,
            PaperHeight = markdownContext.Frontmatter.PdfHeight,
            PaperWidth = markdownContext.Frontmatter.PdfWidth,
            Scale = markdownContext.Frontmatter.PdfScale ?? settings.Pdf.Scale,
            Watermarks = string.IsNullOrWhiteSpace(markdownContext.Frontmatter.Watermark) ? null : [new WatermarkOptions { Html = GetWatermarkHtml(markdownContext.Frontmatter.Watermark) }]
        };

        var pdfer = new AdliancePdfer(new AdliancePdferSettings
        {
            TimeoutSeconds = markdownContext.Frontmatter.PdfTimeoutSeconds ?? settings.Pdf.TimeoutSeconds
        });

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

    private static string GetWatermarkHtml(string text)
    {
        // the SVG uses an A4-shaped coordinate system (in mm) and is scaled to the page, so the text runs from corner to corner;
        // the font size shrinks for longer texts so that it always fits onto the diagonal
        const double width = 210;
        const double height = 297;
        var angle = -Math.Atan(height / width) * 180 / Math.PI;
        var fontSize = Math.Min(40, Math.Sqrt(width * width + height * height) * 0.8 / (text.Trim().Length * 0.6));

        return string.Create(CultureInfo.InvariantCulture, $"""
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}" preserveAspectRatio="xMidYMid meet" style="display: block; width: 100%; height: 100%;">
                    <text x="{width / 2}" y="{height / 2}" transform="rotate({angle:0.##} {width / 2} {height / 2})" text-anchor="middle" dominant-baseline="central"
                          font-family="Arial, Helvetica, sans-serif" font-weight="bold" font-size="{fontSize:0.##}" fill="#c00000" fill-opacity="0.2">{WebUtility.HtmlEncode(text.Trim())}</text>
                </svg>
                """);
    }

    protected override void PrepareAdditionalProcessors(ConverterFile file, IList<IMarkdownProcessor> markdownProcessors)
    {
        markdownProcessors.Add(new LinkToChapters(file.SourceAbsolutePath));
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
    public required int TimeoutSeconds { get; init; }
}
