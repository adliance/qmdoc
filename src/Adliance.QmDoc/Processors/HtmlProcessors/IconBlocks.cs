using System.Text.RegularExpressions;

namespace Adliance.QmDoc.Processors.HtmlProcessors;

/// <summary>
/// The optional data-line attribute comes from --source-lines (see <see cref="Converter.SourceLines"/>) and is kept.
/// </summary>
public class IconBlocks : IHtmlProcessor
{
    public HtmlProcessorResult Apply(string html)
    {
        // Singleline, because a paragraph can be written over several lines
        html = Regex.Replace(html, @"<p( data-line=""\d+"")?>\{?\{\W*\?\W*\}\}? (.*?)</p>", "<div class=\"block block-question\"$1>$2</div>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        html = Regex.Replace(html, @"<p( data-line=""\d+"")?>\{?\{\W*!!\W*\}\}? (.*?)</p>", "<div class=\"block block-danger\"$1>$2</div>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        html = Regex.Replace(html, @"<p( data-line=""\d+"")?>\{?\{\W*!\W*\}\}? (.*?)</p>", "<div class=\"block block-alert\"$1>$2</div>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return new HtmlProcessorResult(html);
    }
}
