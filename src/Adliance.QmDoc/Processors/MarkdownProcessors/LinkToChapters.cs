using System;
using System.Text.RegularExpressions;
using Markdig.Helpers;

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

    /// <summary>
    /// Mirrors the id Markdig's AutoIdentifierExtension assigns to headings (via .UseAdvancedExtensions()),
    /// so that chapter links and the TOC point to the same anchor Markdig actually renders.
    /// </summary>
    public static string GetChapterId(string chapterName)
    {
        if (string.IsNullOrWhiteSpace(chapterName)) throw new ArgumentException(null, nameof(chapterName));

        return LinkHelper.Urilize(chapterName.Trim(), allowOnlyAscii: true);
    }
}
