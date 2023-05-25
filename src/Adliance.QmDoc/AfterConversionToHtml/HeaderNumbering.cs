using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.AfterConversionToHtml;

public class HeaderNumbering : IAfterConversionToHtmlStep
{
    private readonly bool _enable;

    public HeaderNumbering(bool enable)
    {
        _enable = enable;
    }

    public Result Apply(string html)
    {
        if (!_enable)
        {
            return new Result(html);
        }

        var h1 = 0;
        var h2 = 0;
        var h3 = 0;
        var h4 = 0;

        var titlesToReplace = new List<Tuple<string, string>>();
        var sb = new StringBuilder();
        foreach (var line in html.Split('\n'))
        {
            var lineClosure = line;


            var h1Match = Regex.Match(line, "<h1(.*?)>(.*?)</h1>", RegexOptions.IgnoreCase);
            var h2Match = Regex.Match(line, "<h2(.*?)>(.*?)</h2>", RegexOptions.IgnoreCase);
            var h3Match = Regex.Match(line, "<h3(.*?)>(.*?)</h3>", RegexOptions.IgnoreCase);
            var h4Match = Regex.Match(line, "<h4(.*?)>(.*?)</h4>", RegexOptions.IgnoreCase);

            if (h1Match.Success)
            {
                var newTitle = $"{++h1}&nbsp;&nbsp;&nbsp;{h1Match.Groups[2].Value}";
                titlesToReplace.Add(new Tuple<string, string>(h1Match.Groups[2].Value, newTitle));

                lineClosure = line.Replace(h1Match.Value, $"<h1{h1Match.Groups[1].Value}>{newTitle}</h1>");
                h2 = 0;
                h3 = 0;
                h4 = 0;
            }
            else if (h2Match.Success)
            {
                var newTitle = $"{h1}.{++h2}&nbsp;&nbsp;&nbsp;{h2Match.Groups[2].Value}";
                titlesToReplace.Add(new Tuple<string, string>(h2Match.Groups[2].Value, newTitle));

                lineClosure = line.Replace(h2Match.Value, $"<h2{h2Match.Groups[1].Value}>{newTitle}</h2>");
                h3 = 0;
                h4 = 0;
            }
            else if (h3Match.Success)
            {
                var newTitle = $"{h1}.{h2}.{++h3}&nbsp;&nbsp;&nbsp;{h3Match.Groups[2].Value}";
                titlesToReplace.Add(new Tuple<string, string>(h3Match.Groups[2].Value, newTitle));

                lineClosure = line.Replace(h3Match.Value, $"<h3{h3Match.Groups[1].Value}>{newTitle}</h3>");
                h4 = 0;
            }
            else if (h4Match.Success)
            {
                var newTitle = $"{h1}.{h2}.{h3}.{++h4}&nbsp;&nbsp;&nbsp;{h4Match.Groups[2].Value}";
                titlesToReplace.Add(new Tuple<string, string>(h4Match.Groups[2].Value, newTitle));

                lineClosure = line.Replace(h4Match.Value, $"<h4{h4Match.Groups[1].Value}>{newTitle}</h4>");
            }

            sb.AppendLine(lineClosure);
        }

        var result = sb.ToString();

        foreach (var title in titlesToReplace)
        {
            result = Regex.Replace(result, $"(<a.*?)>{title.Item1}</a>", $"$1>{title.Item2.Replace("&nbsp;&nbsp;&nbsp;", " ")}</a>");
        }

        return new Result(result);
    }

}