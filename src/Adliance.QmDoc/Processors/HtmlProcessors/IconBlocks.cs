using System.Text.RegularExpressions;

namespace Adliance.QmDoc.Processors.HtmlProcessors;

public class IconBlocks : IHtmlProcessor
{
    public HtmlProcessorResult Apply(string html)
    {
        html = Regex.Replace(html, @"<p>\{?\{\W*\?\W*\}\}? (.*?)</p>", "<div class=\"block block-question\">$1</div>", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<p>\{?\{\W*!!\W*\}\}? (.*?)</p>", "<div class=\"block block-danger\">$1</div>", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<p>\{?\{\W*!\W*\}\}? (.*?)</p>", "<div class=\"block block-alert\">$1</div>", RegexOptions.IgnoreCase);
        return new HtmlProcessorResult(html);
    }
}
