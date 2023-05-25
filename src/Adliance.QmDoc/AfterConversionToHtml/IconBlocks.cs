using System.Text.RegularExpressions;

namespace Adliance.QmDoc.AfterConversionToHtml;

public class IconBlocks : IAfterConversionToHtmlStep
{
    public Result Apply(string html)
    {
        html = Regex.Replace(html, @"<p>\{?\{\W*\?\W*\}\}? (.*?)</p>", "<p class=\"block-question\"><i class=\"fad fa-question-circle\"></i>$1<i style=\"clear:both; display:block;\"></i></p>", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<p>\{?\{\W*!!\W*\}\}? (.*?)</p>", "<p class=\"block-danger\"><i class=\"fad fa-exclamation-circle\"></i>$1</p>", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<p>\{?\{\W*!\W*\}\}? (.*?)</p>", "<p class=\"block-alert\"><i class=\"fad fa-info-circle\"></i>$1</p>", RegexOptions.IgnoreCase);
        return new Result(html);
    }
}