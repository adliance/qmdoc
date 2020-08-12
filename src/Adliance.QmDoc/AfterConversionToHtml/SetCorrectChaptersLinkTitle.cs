using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.AfterConversionToHtml
{
    public class SetCorrectChaptersLinkTitle : IAfterConversionToHtmlStep
    {
        private readonly string _filePath;

        public SetCorrectChaptersLinkTitle(string filePath)
        {
            _filePath = filePath;
        }

        public Result Apply(string html)
        {
            var resultingHtml = html;

            foreach (Match? m in Regex.Matches(html, "<h\\d id=\"(.*?)\">(.*?)</"))
            {
                if (m==null) continue;
                
                var id = m.Groups[1].Value;
                var title = m.Groups[2].Value;

                resultingHtml = Regex.Replace(resultingHtml, $"<a href=\"#{id}\">{id}</a>", $"<a href=\"#{id}\">{title}</a>");
            }

            var result = new Result(resultingHtml);


            // also check for links to non-existing chapters
            foreach (Match? m in Regex.Matches(html, "href=\"#(.*?)\"", RegexOptions.IgnoreCase))
            {
                if (m==null) continue;
                
                var id = m.Groups[1].Value;
                var hasMatchingId = Regex.IsMatch(html, $" id=\"{id}\"", RegexOptions.IgnoreCase);

                if (!hasMatchingId)
                {
                    var errorMessage = $"Unable to find an element \"{id}\", but there's a link to it.";
                    var allIdMatches = Regex.Matches(html, " id=\"(.*?)\"", RegexOptions.IgnoreCase).Select(x => x.Groups[1]).Distinct().ToList();
                    if (allIdMatches.Any())
                    {
                        errorMessage += $"{Environment.NewLine}Are you looking for one of these?";
                        foreach (var idMatch in allIdMatches)
                        {
                            errorMessage += $"{Environment.NewLine}  - {idMatch}";
                        }
                    }
                    result.Errors.Add(new ProcessorError(_filePath, errorMessage));
                }
            }

            return result;
        }
    }
}
