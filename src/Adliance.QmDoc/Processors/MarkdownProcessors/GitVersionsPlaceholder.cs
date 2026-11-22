using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class GitVersionsPlaceholder(IList<GitService.Change> changes): IMarkdownProcessor
{
    public const string Placeholder = "GIT_VERSIONS";

    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        string replacement;
        if (changes.Any())
        {
            replacement = "| Datum | Person | Version | Änderung | \n|-|-|-|-|";

            foreach (var change in changes)
            {
                var details = GetDetails(change);
                replacement += $"\n| {change.Date.ToString("dd. MM. yyyy", new CultureInfo("de-DE")).Replace(" ", "&nbsp;")} |" +
                               $" {change.Author.Replace(" ", "&nbsp;")} |" +
                               $" {change.ShaShort} |" +
                               $" {change.MessageShort}{(details.Length == 0 ? "" : $" <span class=\"git-version-details\"> {details}</span>")} |";
            }
        }
        else
        {
            replacement = "{{!}} Dieses Dokument befindet sich nicht in Versionskontrolle.";
        }

        markdownContext.ReplacePlaceholder(Placeholder, replacement);
        return markdownContext;
    }

    /// <summary>
    /// The rest of the commit message after the first line, in a single line (the table cell can't contain line breaks).
    /// </summary>
    private static string GetDetails(GitService.Change change)
    {
        if (change.Message.Length <= change.MessageShort.Length || !change.Message.StartsWith(change.MessageShort, StringComparison.Ordinal)) return "";
        return Regex.Replace(change.Message[change.MessageShort.Length..], @"\s+", " ").Trim();
    }
}
