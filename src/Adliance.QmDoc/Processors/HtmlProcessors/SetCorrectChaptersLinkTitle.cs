using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Adliance.QmDoc.Extensions;
using Markdig.Helpers;

namespace Adliance.QmDoc.Processors.HtmlProcessors;

public class SetCorrectChaptersLinkTitle(string filePath) : IHtmlProcessor
{
    public HtmlProcessorResult Apply(string html)
    {
        html = RewriteLegacyAsciiAnchors(html);
        var resultingHtml = html;

        var hMatches = Regex.Matches(html, "<h\\d id=\"(.*?)\">(.*?)</");
        foreach (Match? m in hMatches)
        {
            if (m == null) continue;

            var id = m.Groups[1].Value;
            var title = m.Groups[2].Value;

            // Markdig percent-encodes non-ASCII characters in links, but not in ids
            var href = Uri.EscapeDataString(id);
            resultingHtml = Regex.Replace(resultingHtml, $"<a href=\"#(?:{Regex.Escape(id)}|{Regex.Escape(href)})\">.*?</a>", $"<a href=\"#{href}\">{(title ?? "").RemoveMultipleNbsp().Trim()}</a>");
        }

        var result = new HtmlProcessorResult(resultingHtml);

        var hrefMatches = Regex.Matches(html, "href=\"#(.*?)\"", RegexOptions.IgnoreCase);
        // also check for links to non-existing chapters
        foreach (Match? m in hrefMatches)
        {
            if (m == null) continue;

            var id = Uri.UnescapeDataString(m.Groups[1].Value);
            var hasMatchingId = Regex.IsMatch(html, $" id=\"{Regex.Escape(id)}\"", RegexOptions.IgnoreCase);

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

                result.Errors.Add(new ProcessorError(filePath, errorMessage));
            }
        }

        return result;
    }

    /// <summary>
    /// Heading ids used to be ASCII-only, so existing documents contain links like "#elimination-des-dechets".
    /// Rewrites such links to the actual id (e.g. "#élimination-des-déchets"), so they keep working.
    /// For duplicate headings, the first one is used, just like the ASCII-only ids did.
    /// </summary>
    private static string RewriteLegacyAsciiAnchors(string html)
    {
        var headings = Regex.Matches(html, "<h\\d id=\"(.*?)\">(.*?)</h\\d>")
            .Select(m => (Id: m.Groups[1].Value, Text: WebUtility.HtmlDecode(Regex.Replace(m.Groups[2].Value, "<.*?>", ""))))
            .ToList();
        var ids = headings.Select(h => h.Id).ToHashSet();
        var legacyIds = headings
            .Where(h => !string.IsNullOrWhiteSpace(h.Text))
            .GroupBy(h => LinkHelper.Urilize(h.Text.Trim(), allowOnlyAscii: true))
            .Where(g => g.Key != "")
            .ToDictionary(g => g.Key, g => g.First().Id);

        return Regex.Replace(html, "href=\"#(.*?)\"", m =>
        {
            var id = Uri.UnescapeDataString(m.Groups[1].Value);
            if (ids.Contains(id) || !legacyIds.TryGetValue(id, out var newId)) return m.Value;
            return $"href=\"#{Uri.EscapeDataString(newId)}\"";
        });
    }
}
