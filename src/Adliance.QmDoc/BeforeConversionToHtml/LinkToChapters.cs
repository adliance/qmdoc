using System;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.BeforeConversionToHtml;

public class LinkToChapters : IBeforeConversionToHtmlStep
{
    public Result Apply(string markdown, Context context)
    {
        var result = markdown;

        var matches = Regex.Matches(markdown, @"\[#(.*?)\]");
        foreach (Match? m in matches)
        {
            if (m == null) continue;
            result = result.Replace(m.Value, $"<span class=\"link-to-chapter\"><i></i>[{(m.Groups[1].Value ?? "").Trim()}](#{GetChapterId(m.Groups[1].Value)})</span>");
        }

        return new Result(result, context);
    }

    private string GetChapterId(string chapterName)
    {
        if (string.IsNullOrWhiteSpace(chapterName))
        {
            throw new ArgumentException(nameof(chapterName));
        }

        var result = chapterName.Trim();
        result = result.Replace(" ", "-");
        result = result.Replace(",", "");
        result = result.ToLower();
        result = result.Replace("ä", "a");
        result = result.Replace("ö", "o");
        result = result.Replace("ü", "u");
        result = result.Replace("\"", "");
        result = result.Replace("%22", "");
        result = result.Replace("ß", "");
        result = result.Replace("---", "-");
        result = result.Replace("--", "-");
            
        return result;
    }
}