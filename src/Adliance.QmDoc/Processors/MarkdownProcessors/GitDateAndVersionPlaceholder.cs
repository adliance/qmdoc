using System;
using System.Collections.Generic;
using System.Globalization;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class GitDateAndVersionPlaceholder(string sourceFilePath, DateTime? ignoreGitCommitsSince, IList<string> ignoreCommits, IList<string> ignoreCommitsWithout)
    : IMarkdownProcessor
{
    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        var latestVersion = GitService.GetLatestVersion(sourceFilePath, ignoreGitCommitsSince, ignoreCommits, ignoreCommitsWithout);

        markdownContext.ReplacePlaceholder("GIT_DATE_VERSION",
            latestVersion != null
                ? $"Version {latestVersion.ShaShort}, {latestVersion.Date.ToString("dd. MMMM yyyy", new CultureInfo("de-DE"))}"
                : "");

        return markdownContext;
    }
}
