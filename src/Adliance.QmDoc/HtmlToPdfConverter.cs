using System;
using Adliance.AspNetCore.Buddy.Pdf;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Adliance.AspNetCore.Buddy.Pdf.V1;
using Adliance.QmDoc.AfterConversionToHtml;
using Adliance.QmDoc.Themes;
using DatePlaceholder = Adliance.QmDoc.AfterConversionToHtml.DatePlaceholder;
using TitlePlaceholder = Adliance.QmDoc.AfterConversionToHtml.TitlePlaceholder;

namespace Adliance.QmDoc
{
    public static class HtmlToPdfConverter
    {
        public static async Task ConvertHtmlTPdf(string theme, string html, string baseDirectory, string sourceFilePath, string targetFilePath, string title, DateTime? ignoreGitCommitsSince)
        {
            var settings = ThemeProvider.GetOptions(theme);

            var pdfOptions = new PdfOptions
            {
                FooterHtml = ReplacePlaceholders(theme, ThemeProvider.GetFooter(theme), sourceFilePath, title, ignoreGitCommitsSince),
                HeaderHtml = ReplacePlaceholders(theme, ThemeProvider.GetHeader(theme), sourceFilePath, title, ignoreGitCommitsSince),
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

        private static string ReplacePlaceholders(string theme, string html, string sourceFilePath, string title, DateTime? ignoreGitCommitsSince)
        {
            IAfterConversionToHtmlStep[] steps =
            {
                new TitlePlaceholder(title),
                new DatePlaceholder(),
                new CssPlaceholder(theme),
                new GitVersionPlaceholder(sourceFilePath, ignoreGitCommitsSince),
                new GitDatePlaceholder(sourceFilePath, ignoreGitCommitsSince),
                new GitDateAndVersionPlaceholder(sourceFilePath, ignoreGitCommitsSince)
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

    public class AdliancePdferSettings : IPdferConfiguration
    {
        public string ServerUrl => "https://adliance-pdf-on-linux.azurewebsites.net/html-to-pdf";
    }
}