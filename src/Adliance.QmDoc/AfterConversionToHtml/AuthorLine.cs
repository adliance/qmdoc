using System.Text.RegularExpressions;

namespace Adliance.QmDoc.AfterConversionToHtml
{
    public class AuthorLine : IAfterConversionToHtmlStep
    {
        public Result Apply(string html)
        {
            var result = html;

            foreach (Match? match in Regex.Matches(html, @"<h4.*?>(.*?) \| (.*?) \| (.*?)</h4>", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            {
                if (match == null) continue;
                
                var authorSnippet = $"<div class=\"document-author\">" +
                                    $"<span class=\"name\">{match.Groups[1].Value}</span>, <span class=\"company\">{match.Groups[2].Value}</span><br />" +
                                    $"<span class=\"email\"><a href=\"mailto:{match.Groups[3].Value}\">{match.Groups[3].Value}</a></span>" +
                                    "</div>";
                result = result.Replace(match.Value, authorSnippet);
            }

            return new Result(result);
        }
    }
}
