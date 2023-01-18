using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Adliance.AspNetCore.Buddy.Pdf.V2;
using Adliance.QmDoc.AfterConversionToHtml;
using Adliance.QmDoc.Themes;
using DatePlaceholder = Adliance.QmDoc.AfterConversionToHtml.DatePlaceholder;
using TitlePlaceholder = Adliance.QmDoc.AfterConversionToHtml.TitlePlaceholder;

namespace Adliance.QmDoc
{
    public static class HtmlToPdfConverter
    {
        public static async Task ConvertHtmlTPdf(
            string theme,
            string html,
            string baseDirectory,
            string sourceFilePath,
            string targetFilePath,
            string title,
            DateTime? ignoreGitCommitsSince,
            IList<string> ignoreCommits,
            IList<string> ignoreCommitsWithout)
        {
            var settings = ThemeProvider.GetOptions(theme);

            var pdfOptions = new PdfOptions
            {
                FooterHtml = ReplacePlaceholders(theme, ThemeProvider.GetFooter(theme), sourceFilePath, title, ignoreGitCommitsSince, ignoreCommits, ignoreCommitsWithout),
                HeaderHtml = ReplacePlaceholders(theme, ThemeProvider.GetHeader(theme), sourceFilePath, title, ignoreGitCommitsSince, ignoreCommits, ignoreCommitsWithout),
                FooterHeight = settings.Pdf.FooterHeight,
                HeaderHeight = settings.Pdf.HeaderHeight
            };

            var pdfer = new AdliancePdfer(new AdliancePdferSettings());
            var pdf = await pdfer.HtmlToPdf(html, pdfOptions);
            await File.WriteAllBytesAsync(targetFilePath, pdf);
        }

        private static string ReplacePlaceholders(
            string theme,
            string html,
            string sourceFilePath,
            string title,
            DateTime? ignoreGitCommitsSince,
            IList<string> ignoreCommits,
            IList<string> ignoreCommitsWithout)
        {
            IAfterConversionToHtmlStep[] steps =
            {
                new TitlePlaceholder(title),
                new DatePlaceholder(),
                new CssPlaceholder(theme),
                new GitVersionPlaceholder(sourceFilePath, ignoreGitCommitsSince, ignoreCommits, ignoreCommitsWithout),
                new GitDatePlaceholder(sourceFilePath, ignoreGitCommitsSince, ignoreCommits, ignoreCommitsWithout),
                new GitDateAndVersionPlaceholder(sourceFilePath, ignoreGitCommitsSince, ignoreCommits, ignoreCommitsWithout)
            };
            
            var result = html;
            foreach (var step in steps)
            {
                var stepResult = step.Apply(result);
                result = stepResult.ResultingHtml;
            }

            return result;
        }
    }

    public class AdliancePdferSettings : IPdferConfiguration
    {
        public string ServerUrl => "https://pdf2.adliance.dev";
    }
}