using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.BeforeConversionToHtml
{
    public class LinkedDocumentsPlaceholder : IBeforeConversionToHtmlStep
    {
        private readonly string _sourceFilePath;

        public LinkedDocumentsPlaceholder(string sourceFilePath)
        {
            _sourceFilePath = sourceFilePath;
        }

        public Result Apply(string markdown, Context context)
        {
            var pattern = @"\{\{\W*LINKED_DOCUMENTS\W*\}\}";
            var result = markdown;

            if (Regex.IsMatch(markdown, pattern, RegexOptions.IgnoreCase))
            {
                var replacement = context.LinkedDocuments.Distinct().OrderBy(x => x.NiceName).Aggregate("", (current, d) => current + $"{Environment.NewLine}* [{d.NiceName}]({d.FileName})");
                result = Regex.Replace(result, pattern, replacement.Trim(), RegexOptions.IgnoreCase);
            }

            return new Result(result, context);
        }
    }
}
