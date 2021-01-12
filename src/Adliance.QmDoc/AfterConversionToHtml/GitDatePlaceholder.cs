using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Adliance.QmDoc.BeforeConversionToHtml;

namespace Adliance.QmDoc.AfterConversionToHtml
{
    public class GitDatePlaceholder : IAfterConversionToHtmlStep
    {
        private readonly string _sourceFilePath;
        private readonly DateTime? _ignoreGitCommitsSince;

        public GitDatePlaceholder(string sourceFilePath, DateTime? ignoreGitCommitsSince)
        {
            _sourceFilePath = sourceFilePath;
            _ignoreGitCommitsSince = ignoreGitCommitsSince;
        }

        public Result Apply(string html)
        {
            var latestVersion = GitService.GetLatestVersion(_sourceFilePath, _ignoreGitCommitsSince);
            var result = html;
            if (latestVersion != null)
            {
                result = Regex.Replace(result, @"\{{1,2}\W*GIT_DATE\W*\}{1,2}", latestVersion.Date.ToString("dd. MMMM yyyy", new CultureInfo("de-DE")), RegexOptions.IgnoreCase);
            }
            else
            {
                result = Regex.Replace(result, @"\{{1,2}\W*GIT_DATE\W*\}{1,2}", "", RegexOptions.IgnoreCase);
            }

            return new Result(result);
        }
    }
}