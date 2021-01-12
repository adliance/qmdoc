using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.BeforeConversionToHtml
{
    public class GitVersionsPlaceholder : IBeforeConversionToHtmlStep
    {
        private readonly string _sourceFilePath;
        private readonly DateTime? _ignoreGitCommitsSince;

        public GitVersionsPlaceholder(string sourceFilePath, DateTime? ignoreGitCommitsSince)
        {
            _sourceFilePath = sourceFilePath;
            _ignoreGitCommitsSince = ignoreGitCommitsSince;
        }

        public Result Apply(string markdown, Context context)
        {
            var pattern = @"\{\{\W*GIT_VERSIONS\W*\}\}";
            var result = markdown;

            if (Regex.IsMatch(markdown, pattern, RegexOptions.IgnoreCase))
            {
                var changes = GitService.GetVersions(_sourceFilePath, _ignoreGitCommitsSince);

                string replacement;
                if (changes.Any())
                {
                    replacement = "| Datum | Person | Version | Änderung" + Environment.NewLine + "|-|-|-|-|";

                    foreach (var change in changes)
                    {
                        replacement += Environment.NewLine + $"| {change.Date.ToString("dd. MM. yyyy", new CultureInfo("de-DE")).Replace(" ", "&nbsp;")} |" +
                                       $" {change.Author.Replace(" ", "&nbsp;")} |" +
                                       $" {change.ShaShort} |" +
                                       $" {change.MessageShort} {(string.IsNullOrWhiteSpace(change.Message) ? "" : $"<span class=\"git-version-details\"><br />{change.Message.Substring(change.MessageShort.Length).Replace("\r", "").Replace("\n", "").Trim()}")}</span>";
                    }
                }
                else
                {
                    replacement = "{{!}} Dieses Dokument befindet sich nicht in Versionskontrolle.";
                }

                result = Regex.Replace(result, pattern, replacement, RegexOptions.IgnoreCase);
            }

            return new Result(result, context);
        }
    }
}