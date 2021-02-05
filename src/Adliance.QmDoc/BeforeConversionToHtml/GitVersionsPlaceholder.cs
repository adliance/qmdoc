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
                var changes = GitService.GetVersions(_sourceFilePath, _ignoreGitCommitsSince).ToList();

                string replacement;
                if (changes.Any())
                {
                    replacement = "| Datum | Person | Version | Änderung" + Environment.NewLine + "|-|-|-|-|";

                    for (var i = 0; i < changes.Count; i++)
                    {
                        var change = changes[i];

                        // if we have the same message multiple times in a row, just use the latest commit
                        if (changes.Count > i + 1 && (changes[i + 1].Message ?? "").Equals(change.Message, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

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