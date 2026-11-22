using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Adliance.AspNetCore.Buddy.Pdf.V2;
using Adliance.QmDoc.Parameters;
using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Themes;

namespace Adliance.QmDoc.Converter;

/// <summary>
/// Converts Markdown to PDF by rendering the HTML with the external PDF service.
/// </summary>
/// <param name="createPdfer">Creates the client of the PDF service for the given timeout (in seconds), uses the Adliance PDF service by default.</param>
public class PdfConverter(ThemedConversionParameters parameters, Options.Options options, Func<int, IPdfer>? createPdfer = null) : Converter(TargetExtension.Pdf, parameters, options)
{
    private readonly Func<int, IPdfer> _createPdfer = createPdfer ?? (timeoutSeconds => new AdliancePdfer(new AdliancePdferSettings { TimeoutSeconds = timeoutSeconds }));

    protected override async Task<byte[]> Convert(ConverterFile file, MarkdownProcessorContext markdownContext)
    {
        var theme = GetTheme(markdownContext);
        var settings = ThemeProvider.GetOptions(theme);
        var frontmatter = markdownContext.Frontmatter;

        var pdfOptions = new PdfOptions
        {
            FooterHtml = frontmatter.EnableFooter == false ? null : ApplyCommonPlaceholders(file, ThemeProvider.GetFooter(theme), markdownContext),
            HeaderHtml = frontmatter.EnableHeader == false ? null : ApplyCommonPlaceholders(file, ThemeProvider.GetHeader(theme), markdownContext),
            FooterHeight = settings.Pdf.FooterHeight,
            HeaderHeight = settings.Pdf.HeaderHeight,
            Outline = true,
            PaperHeight = frontmatter.PdfHeight,
            PaperWidth = frontmatter.PdfWidth,
            Scale = frontmatter.PdfScale ?? settings.Pdf.Scale,
            Watermarks = string.IsNullOrWhiteSpace(frontmatter.Watermark) ? null : [new WatermarkOptions { Html = GetWatermarkHtml(frontmatter.Watermark) }]
        };

        var pdfer = _createPdfer(frontmatter.PdfTimeoutSeconds ?? settings.Pdf.TimeoutSeconds);

        if (TableOfContentsPlaceholder.ContainsTocPlaceholder(markdownContext))
        {
            // the page numbers of the TOC are only known after rendering the PDF once;
            // errors are reported after the second pass only, otherwise they would show up twice
            Program.WriteLine("\tTOC detected, second pass required");
            var firstPassHtml = RunHtmlProcessors(file, markdownContext, reportErrors: false);
            var firstPassPdf = await pdfer.HtmlToPdf(firstPassHtml, pdfOptions);
            var firstPassMetadata = await pdfer.GetPdfMetadata(firstPassPdf);
            markdownContext.ResetForSecondPass(firstPassMetadata);
            markdownContext = RunMarkdownProcessors(file, markdownContext, reportErrors: false);
        }

        var html = RunHtmlProcessors(file, markdownContext);
        return await pdfer.HtmlToPdf(html, pdfOptions);
    }

    internal static string GetWatermarkHtml(string text)
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
}

public class AdliancePdferSettings : IPdferConfiguration
{
    public string ServerUrl => "https://pdf2.adliance.dev";
    public string? ApiKeyPdf => null;
    public string? ApiKeyTemplate => null;
    public required int TimeoutSeconds { get; init; }
}
