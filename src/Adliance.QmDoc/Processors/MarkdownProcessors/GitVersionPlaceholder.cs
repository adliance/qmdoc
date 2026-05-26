using System;
using System.Collections.Generic;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class GitVersionPlaceholder(string sourceFilePath, DateTime? ignoreGitCommitsSince, IList<string> ignoreCommits, IList<string> ignoreCommitsWithout)
    : IMarkdownProcessor
{
    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        var latestVersion = GitService.GetLatestVersion(sourceFilePath, ignoreGitCommitsSince, ignoreCommits, ignoreCommitsWithout);

        markdownContext.ReplacePlaceholder("GIT_VERSION",
            latestVersion != null
                ? latestVersion.ShaShort
                : "");

        return markdownContext;
    }
}
