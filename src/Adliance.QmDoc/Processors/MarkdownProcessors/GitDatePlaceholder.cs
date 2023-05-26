using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Adliance.QmDoc.Processors.HtmlProcessors;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class GitDatePlaceholder : IMarkdownProcessor
{
    private readonly string _sourceFilePath;
    private readonly DateTime? _ignoreGitCommitsSince;
    private readonly IList<string> _ignoreCommits;
    private readonly IList<string> _ignoreCommitsWithout;

    public GitDatePlaceholder(string sourceFilePath, DateTime? ignoreGitCommitsSince, IList<string> ignoreCommits, IList<string> ignoreCommitsWithout)
    {
        _sourceFilePath = sourceFilePath;
        _ignoreGitCommitsSince = ignoreGitCommitsSince;
        _ignoreCommits = ignoreCommits;
        _ignoreCommitsWithout = ignoreCommitsWithout;
    }
    
    public MarkdownProcessorResult Apply(string markdown, MarkdownProcessorContext markdownProcessorContext)
    {
        var latestVersion = GitService.GetLatestVersion(_sourceFilePath, _ignoreGitCommitsSince, _ignoreCommits, _ignoreCommitsWithout);
        var result = markdown;
        if (latestVersion != null)
        {
            result = Regex.Replace(result, @"\{{1,2}\W*GIT_DATE\W*\}{1,2}", latestVersion.Date.ToString("dd. MMMM yyyy", new CultureInfo("de-DE")), RegexOptions.IgnoreCase);
        }
        else
        {
            result = Regex.Replace(result, @"\{{1,2}\W*GIT_DATE\W*\}{1,2}", "", RegexOptions.IgnoreCase);
        }

        return new MarkdownProcessorResult(result, markdownProcessorContext);
    }
}