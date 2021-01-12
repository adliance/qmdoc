using System;
using System.Text.RegularExpressions;
using Adliance.QmDoc.BeforeConversionToHtml;

namespace Adliance.QmDoc.AfterConversionToHtml
{
    public class GitVersionPlaceholder : IAfterConversionToHtmlStep
    {
        private readonly string _sourceFilePath;
        private readonly DateTime? _ignoreGitCommitsSince;

        public GitVersionPlaceholder(string sourceFilePath, DateTime? ignoreGitCommitsSince)
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
                result = Regex.Replace(result, @"\{{1,2}\W*GIT_VERSION\W*\}{1,2}", latestVersion.ShaShort, RegexOptions.IgnoreCase);
            }
            else
            {
                result = Regex.Replace(result, @"\{{1,2}\W*GIT_VERSION\W*\}{1,2}", "", RegexOptions.IgnoreCase);
            }

            return new Result(result);
        }
    }
}