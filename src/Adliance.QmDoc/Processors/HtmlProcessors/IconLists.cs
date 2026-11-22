using System.Text.RegularExpressions;

namespace Adliance.QmDoc.Processors.HtmlProcessors;

/// <summary>
/// The optional data-line attribute comes from --source-lines (see <see cref="Converter.SourceLines"/>) and is kept.
/// </summary>
public class IconLists : IHtmlProcessor
{
    public HtmlProcessorResult Apply(string html)
    {
        html = Regex.Replace(html, @"<li( data-line=""\d+"")?> *\{?\{\W*\?\W*\}\}? *", "<li$1><i class=\"fad fa-question-circle\"></i>", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<li( data-line=""\d+"")?> *\{?\{\W*!!\W*\}\}? *", "<li$1><i class=\"fad fa-exclamation-circle\"></i>", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<li( data-line=""\d+"")?> *\{?\{\W*!\W*\}\}? *", "<li$1><i class=\"fad fa-info-circle\"></i>", RegexOptions.IgnoreCase);
        return new HtmlProcessorResult(html);
    }
}
