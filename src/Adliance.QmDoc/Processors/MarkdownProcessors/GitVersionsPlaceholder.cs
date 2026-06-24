using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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

        markdownContext.ReplacePlaceholder(Placeholder, replacement);


        return markdownContext;
    }
}
