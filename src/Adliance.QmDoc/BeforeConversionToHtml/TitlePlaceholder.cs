using System.Text.RegularExpressions;

namespace Adliance.QmDoc.BeforeConversionToHtml
{
    public class TitlePlaceholder : IBeforeConversionToHtmlStep
    {
        private readonly string _title;

        public TitlePlaceholder(string title)
        {
            _title = title;
        }

        public Result Apply(string markdown, Context context)
        {
            var result = Regex.Replace(markdown, @"\{\{\W*TITLE\W*\}\}", _title, RegexOptions.IgnoreCase);
            return new Result(result, context);
        }
    }
}
