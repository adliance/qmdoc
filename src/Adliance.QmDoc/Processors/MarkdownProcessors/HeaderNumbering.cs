using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Adliance.QmDoc.Processors.HtmlProcessors;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class HeaderNumbering : IMarkdownProcessor
{
    private readonly bool _enable;

    public HeaderNumbering(bool enable)
    {
        _enable = enable;
    }


    public MarkdownProcessorResult Apply(string markdown, MarkdownProcessorContext markdownProcessorContext)
    {
        if (!_enable) return new MarkdownProcessorResult(markdown, markdownProcessorContext);

        var h1 = 0;
        var h2 = 0;
        var h3 = 0;
        var h4 = 0;

        var titlesToReplace = new List<Tuple<string, string>>();
        var sb = new StringBuilder();
        var lineNumber = 0;
        foreach (var line in markdown.Split('\n'))
        {
            var lineClosure = line;
            lineNumber++;

            var h1Match = Regex.Match(line, "^# (.*)$", RegexOptions.IgnoreCase);
            var h2Match = Regex.Match(line, "^## (.*)$", RegexOptions.IgnoreCase);
            var h3Match = Regex.Match(line, "^### (.*)$", RegexOptions.IgnoreCase);
            var h4Match = Regex.Match(line, "^#### (.*)$", RegexOptions.IgnoreCase);

            if (lineNumber == 1 && h4Match.Success)
            {
                sb.AppendLine(lineClosure);
                continue;
            }

            if (h1Match.Success)
            {
                var newTitle = $"{++h1} {h1Match.Groups[1].Value}";
                titlesToReplace.Add(new Tuple<string, string>(h1Match.Groups[1].Value, newTitle));

                lineClosure = line.Replace(h1Match.Value, $"# {newTitle}");
                h2 = 0;
                h3 = 0;
                h4 = 0;
            }
            else if (h2Match.Success)
            {
                var newTitle = $"{h1}.{++h2} {h2Match.Groups[1].Value}";
                titlesToReplace.Add(new Tuple<string, string>(h2Match.Groups[1].Value, newTitle));

                lineClosure = line.Replace(h2Match.Value, $"## {newTitle}");
                h3 = 0;
                h4 = 0;
            }
            else if (h3Match.Success)
            {
                var newTitle = $"{h1}.{h2}.{++h3} {h3Match.Groups[1].Value}";
                titlesToReplace.Add(new Tuple<string, string>(h3Match.Groups[1].Value, newTitle));

                lineClosure = line.Replace(h3Match.Value, $"### {newTitle}");
                h4 = 0;
            }
            else if (h4Match.Success)
            {
                var newTitle = $"{h1}.{h2}.{h3}.{++h4} {h4Match.Groups[1].Value}";
                titlesToReplace.Add(new Tuple<string, string>(h4Match.Groups[1].Value, newTitle));

                lineClosure = line.Replace(h4Match.Value, $"#### {newTitle}");
            }

            sb.AppendLine(lineClosure);
        }

        var result = sb.ToString();

        foreach (var title in titlesToReplace)
        {
            result = Regex.Replace(result, $"(<a.*?)>{title.Item1}</a>", $"$1>{title.Item2.Replace("   ", " ")}</a>");
        }

        return new MarkdownProcessorResult(result, markdownProcessorContext);
    }
}