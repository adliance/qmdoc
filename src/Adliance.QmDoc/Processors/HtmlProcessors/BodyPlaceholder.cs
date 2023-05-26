using System.Text.RegularExpressions;

namespace Adliance.QmDoc.Processors.HtmlProcessors;

public class BodyPlaceholder : IHtmlProcessor
{
    private readonly string _body;

    public BodyPlaceholder(string body)
    {
        _body = body;
    }

    public HtmlProcessorResult Apply(string html)
    {
        var result = Regex.Replace(html, @"\{\{\W*BODY\W*\}\}", _body, RegexOptions.IgnoreCase);
        return new HtmlProcessorResult(result);
    }
}