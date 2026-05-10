using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class GitVersionPlaceholder(string sourceFilePath, DateTime? ignoreGitCommitsSince, IList<string> ignoreCommits, IList<string> ignoreCommitsWithout)
    : IMarkdownProcessor
{
    public MarkdownProcessorResult Apply(string markdown, MarkdownProcessorContext markdownProcessorContext)
    {
        var latestVersion = GitService.GetLatestVersion(sourceFilePath, ignoreGitCommitsSince, ignoreCommits, ignoreCommitsWithout);
        var result = markdown;
        if (latestVersion != null)
        {
            result = Regex.Replace(result, @"\{{1,2}\W*GIT_VERSION\W*\}{1,2}", latestVersion.ShaShort, RegexOptions.IgnoreCase);
        }
        else
        {
            result = Regex.Replace(result, @"\{{1,2}\W*GIT_VERSION\W*\}{1,2}", "", RegexOptions.IgnoreCase);
        }

        return new MarkdownProcessorResult(result, markdownProcessorContext);
    }
}
