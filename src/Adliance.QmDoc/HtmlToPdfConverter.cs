using Adliance.AspNetCore.Buddy.Pdf;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Adliance.QmDoc.AfterConversionToHtml;
using Adliance.QmDoc.Themes;

namespace Adliance.QmDoc
{
    public static class HtmlToPdfConverter
    {
        public static async Task ConvertHtmlTPdf(string theme, string html, string targetFilePath, string title)
        {
            var settings = ThemeProvider.GetOptions(theme);

            var pdfOptions = new PdfOptions
            {
                FooterHtml = ReplacePlaceholders(theme, title, ThemeProvider.GetFooter(theme)),
                HeaderHtml = ReplacePlaceholders(theme, title, ThemeProvider.GetHeader(theme)),
                MarginBottom = settings.Pdf.MarginBottom,
                MarginTop = settings.Pdf.MarginTop,
                MarginLeft = settings.Pdf.MarginLeft,
                MarginRight = settings.Pdf.MarginRight,
                HeaderSpacing = settings.Pdf.HeaderSpacing,
                FooterSpacing = settings.Pdf.FooterSpacing
            };

            var pdfer = new AdliancePdfer(new AdliancePdferSettings());
            var pdf = await pdfer.HtmlToPdf(html, pdfOptions);
            await File.WriteAllBytesAsync(targetFilePath, pdf);
        }

        private static string ReplacePlaceholders(string theme, string title, string html)
        {
            IAfterConversionToHtmlStep[] steps =
            {
                new TitlePlaceholder(title),
                new DatePlaceholder(),
                new CssPlaceholder(theme)
            };

            var errors = new List<ProcessorError>();
            var result = html;
            foreach (var step in steps)
            {
                var stepResult = step.Apply(result);
                errors.AddRange(stepResult.Errors);
                result = stepResult.ResultingHtml;
            }

            return result;
        }
    }

    public class AdliancePdferSettings : IAdliancePdferSettings
    {
        public string PdfServerUrl => "https://adliance-pdf-on-linux.azurewebsites.net/html-to-pdf";
    }
}