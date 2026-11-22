using System.Text.RegularExpressions;

namespace Adliance.QmDoc.Processors.HtmlProcessors;

public class BodyPlaceholder(string body) : IHtmlProcessor
{
    public HtmlProcessorResult Apply(string html)
    {
        // use a MatchEvaluator, so that "$0" or "$$" in the document are inserted as they are and not interpreted as substitutions
        var result = Regex.Replace(html, @"\{\{\W*BODY\W*\}\}", _ => body, RegexOptions.IgnoreCase);
        return new HtmlProcessorResult(result);
    }
}
