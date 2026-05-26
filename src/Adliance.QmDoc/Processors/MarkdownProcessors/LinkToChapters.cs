using System;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class LinkToChapters : IMarkdownProcessor
{
    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        var matches = Regex.Matches(markdownContext.Markdown, @"\[#(.*?)\]");
        foreach (Match? m in matches)
        {
            if (m == null) continue;
            markdownContext.Markdown = markdownContext.Markdown.Replace(m.Value, $"<span class=\"link-to-chapter\"><i></i>[{(m.Groups[1].Value).Trim()}](#{GetChapterId(m.Groups[1].Value)})</span>");
        }

        return markdownContext;
    }

    public static string GetChapterId(string chapterName)
    {
        if (string.IsNullOrWhiteSpace(chapterName)) throw new ArgumentException(null, nameof(chapterName));

        var result = chapterName.Trim();
        result = result.Replace(" ", "-");
        result = result.Replace(",", "");
        result = result.Replace(":", "");
        result = result.ToLower();
        result = result.Replace("ä", "a");
        result = result.Replace("ö", "o");
        result = result.Replace("ü", "u");
        result = result.Replace("\"", "");
        result = result.Replace("%22", "");
        result = result.Replace("ß", "");
        result = result.Replace("---", "-");
        result = result.Replace("--", "-");
        result = result.TrimStart('1', '2', '3', '4', '5', '6', '7', '8', '9', '.', '-');

        return result;
    }
}
