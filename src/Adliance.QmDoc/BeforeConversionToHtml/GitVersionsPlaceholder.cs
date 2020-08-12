using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.BeforeConversionToHtml
{
    public class GitVersionsPlaceholder : IBeforeConversionToHtmlStep
    {
        private readonly string _sourceFilePath;

        public GitVersionsPlaceholder(string sourceFilePath)
        {
            _sourceFilePath = sourceFilePath;
        }

        public Result Apply(string markdown, Context context)
        {
            var pattern = @"\{\{\W*GIT_VERSIONS\W*\}\}";
            var result = markdown;

            if (Regex.IsMatch(markdown, pattern, RegexOptions.IgnoreCase))
            {
                var changes = GitService.GetVersions(_sourceFilePath);
                var replacement = "";
                if (changes.Any())
                {
                    replacement = "| Datum | Person | Version | Änderung" + Environment.NewLine + "|-|-|-|-|";

                    foreach (var change in changes)
                    {
                        replacement += Environment.NewLine + $"| {change.Date.ToString("dd. MM. yyyy", new CultureInfo("de-DE"))} | {change.Author} | {change.ShaShort} | {change.MessageShort}";
                    }
                }

                result = Regex.Replace(result, pattern, replacement, RegexOptions.IgnoreCase);
            }

            return new Result(result, context);
        }
    }
}
