using System.Text.RegularExpressions;

namespace Adliance.QmDoc.AfterConversionToHtml
{
    public class ListIcons : IAfterConversionToHtmlStep
    {
        public Result Apply(string html)
        {
            html = Regex.Replace(html, @"<li>\W*\{?\{\W*\?\W*\}\}?\W*", "<li><i class=\"fad fa-question-circle\"></i>", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<li>\W*\{?\{\W*!!\W*\}\}?\W*", "<li><i class=\"fad fa-exclamation-circle\"></i>", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<li>\W*\{?\{\W*!\W*\}\}?\W*", "<li><i class=\"fad fa-info-circle\"></i>", RegexOptions.IgnoreCase);
            return new Result(html);
        }
    }
}
