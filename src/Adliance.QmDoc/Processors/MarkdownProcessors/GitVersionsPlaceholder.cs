using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class GitVersionsPlaceholder(string sourceFilePath, DateTime? ignoreGitCommitsSince, IList<string> ignoreCommits, IList<string> ignoreCommitsWithout)
    : IMarkdownProcessor
{
    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        var changes = GitService.GetVersions(sourceFilePath, ignoreGitCommitsSince, ignoreCommits, ignoreCommitsWithout).ToList();

        string replacement;
        if (changes.Any())
        {
            replacement = "| Datum | Person | Version | Änderung | \n|-|-|-|-|";

            for (var i = 0; i < changes.Count; i++)
            {
                var change = changes[i];
                // if we have the same message multiple times in a row, just use the latest commit
                /*if (i > 0 && (changes[i - 1].Message ?? "").Equals(change.Message, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }*/

                replacement += $"\n| {change.Date.ToString("dd. MM. yyyy", new CultureInfo("de-DE")).Replace(" ", "&nbsp;")} |" +
                               $" {change.Author.Replace(" ", "&nbsp;")} |" +
                               $" {change.ShaShort} |" +
                               $" {change.MessageShort} {(string.IsNullOrWhiteSpace(change.Message) ? "" : $"<span class=\"git-version-details\"> {change.Message.Substring(change.MessageShort.Length).Replace("\r", "").Replace("\n", "").Trim()}")}</span> |";
            }
        }
        else
        {
            replacement = "{{!}} Dieses Dokument befindet sich nicht in Versionskontrolle.";
        }

        markdownContext.ReplacePlaceholder("GIT_VERSIONS", replacement);


        return markdownContext;
    }
}
