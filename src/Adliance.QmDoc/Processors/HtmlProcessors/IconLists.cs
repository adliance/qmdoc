using System.Text.RegularExpressions;

namespace Adliance.QmDoc.Processors.HtmlProcessors;

public class IconLists : IHtmlProcessor
{
    public HtmlProcessorResult Apply(string html)
    {
        html = Regex.Replace(html, @"<li> *\{?\{\W*\?\W*\}\}? *", "<li><i class=\"fad fa-question-circle\"></i>", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<li> *\{?\{\W*!!\W*\}\}? *", "<li><i class=\"fad fa-exclamation-circle\"></i>", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<li> *\{?\{\W*!\W*\}\}? *", "<li><i class=\"fad fa-info-circle\"></i>", RegexOptions.IgnoreCase);
        return new HtmlProcessorResult(html);
    }
}