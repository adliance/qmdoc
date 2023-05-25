using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.BeforeConversionToHtml;

public class GitVersionsPlaceholder : IBeforeConversionToHtmlStep
{
    private readonly string _sourceFilePath;
    private readonly DateTime? _ignoreGitCommitsSince;
    private readonly IList<string> _ignoreCommits;
    private readonly IList<string> _ignoreCommitsWithout;

    public GitVersionsPlaceholder(string sourceFilePath, DateTime? ignoreGitCommitsSince, IList<string> ignoreCommits, IList<string> ignoreCommitsWithout)
    {
        _sourceFilePath = sourceFilePath;
        _ignoreGitCommitsSince = ignoreGitCommitsSince;
        _ignoreCommits = ignoreCommits;
        _ignoreCommitsWithout = ignoreCommitsWithout;
    }

    public Result Apply(string markdown, Context context)
    {
        var pattern = @"\{\{\W*GIT_VERSIONS\W*\}\}";
        var result = markdown;

        if (Regex.IsMatch(markdown, pattern, RegexOptions.IgnoreCase))
        {
            var changes = GitService.GetVersions(_sourceFilePath, _ignoreGitCommitsSince, _ignoreCommits, _ignoreCommitsWithout).ToList();

            string replacement;
            if (changes.Any())
            {
                replacement = "| Datum | Person | Version | Änderung" + Environment.NewLine + "|-|-|-|-|";

                for (var i = 0; i < changes.Count; i++)
                {
                    var change = changes[i];
                    // if we have the same message multiple times in a row, just use the latest commit
                    if (i > 0 && (changes[i - 1].Message ?? "").Equals(change.Message, StringComparison.OrdinalIgnoreCase))
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