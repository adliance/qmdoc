using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.AfterConversionToHtml;

public class GitDateAndVersionPlaceholder : IAfterConversionToHtmlStep
{
    private readonly string _sourceFilePath;
    private readonly DateTime? _ignoreGitCommitsSince;
    private readonly IList<string> _ignoreCommits;
    private readonly IList<string> _ignoreCommitsWithout;

    public GitDateAndVersionPlaceholder(string sourceFilePath, DateTime? ignoreGitCommitsSince, IList<string> ignoreCommits, IList<string> ignoreCommitsWithout)
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
            result = Regex.Replace(result, @"\{{1,2}\W*GIT_DATE_VERSION\W*\}{1,2}", $"Version {latestVersion.ShaShort}, {latestVersion.Date.ToString("dd. MMMM yyyy", new CultureInfo("de-DE"))}", RegexOptions.IgnoreCase);
        }
        else
        {
            result = Regex.Replace(result, @"\{{1,2}\W*GIT_DATE_VERSION\W*\}{1,2}", "", RegexOptions.IgnoreCase);
        }

        return new Result(result);
    }
}