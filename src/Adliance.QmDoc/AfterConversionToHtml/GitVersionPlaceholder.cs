using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.AfterConversionToHtml;

public class GitVersionPlaceholder : IAfterConversionToHtmlStep
{
    private readonly string _sourceFilePath;
    private readonly DateTime? _ignoreGitCommitsSince;
    private readonly IList<string> _ignoreCommits;
    private readonly IList<string> _ignoreCommitsWithout;

    public GitVersionPlaceholder(string sourceFilePath, DateTime? ignoreGitCommitsSince, IList<string> ignoreCommits, IList<string> ignoreCommitsWithout)
    {
        _sourceFilePath = sourceFilePath;
        _ignoreGitCommitsSince = ignoreGitCommitsSince;
        _ignoreCommits = ignoreCommits;
        _ignoreCommitsWithout = ignoreCommitsWithout;
    }
        
    public Result Apply(string html)
    {
        var latestVersion = GitService.GetLatestVersion(_sourceFilePath, _ignoreGitCommitsSince, _ignoreCommits, _ignoreCommitsWithout);
        var result = html;
        if (latestVersion != null)
        {
            result = Regex.Replace(result, @"\{{1,2}\W*GIT_VERSION\W*\}{1,2}", latestVersion.ShaShort, RegexOptions.IgnoreCase);
        }
        else
        {
            result = Regex.Replace(result, @"\{{1,2}\W*GIT_VERSION\W*\}{1,2}", "", RegexOptions.IgnoreCase);
        }

        return new Result(result);
    }
}