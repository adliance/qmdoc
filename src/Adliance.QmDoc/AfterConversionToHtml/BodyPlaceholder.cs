using System.Text.RegularExpressions;

namespace Adliance.QmDoc.AfterConversionToHtml
{
    public class BodyPlaceholder : IAfterConversionToHtmlStep
    {
        private readonly string _body;

        public BodyPlaceholder(string body)
        {
            _body = body;
        }

        public Result Apply(string html)
        {
            var result = Regex.Replace(html, @"\{\{\W*BODY\W*\}\}", _body, RegexOptions.IgnoreCase);
            return new Result(result);
        }
    }
}
