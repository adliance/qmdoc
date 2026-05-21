using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Syntax;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class HeaderNumbering(bool enable) : IMarkdownProcessor
{
    public MarkdownProcessorResult Apply(string markdown, MarkdownProcessorContext context)
    {
        if (!enable) return new MarkdownProcessorResult(markdown, context);
        var document = Markdown.Parse(markdown, context.Pipeline);

        var h1 = 0;
        var h2 = 0;
        var h3 = 0;
        var h4 = 0;

        var titlesToReplace = new List<(string OldTitle, string NewTitle)>();
        var spanReplacements = new List<(int Start, int Length, string NewHeading)>();

        foreach (var heading in document.Descendants<HeadingBlock>())
        {
            if (heading.Level > 4) continue;
            if (heading is { Line: 0, Level: 4 }) continue; // document author — leave unnumbered

            var text = markdown[heading.Span.Start..(heading.Span.End + 1)].TrimStart('#').Trim();

            string newTitle;
            switch (heading.Level)
            {
                case 1:
                    newTitle = ++h1 + " " + text;
                    h2 = 0;
                    h3 = 0;
                    h4 = 0;
                    break;
                case 2:
                    newTitle = h1 + "." + ++h2 + " " + text;
                    h3 = 0;
                    h4 = 0;
                    break;
                case 3:
                    newTitle = h1 + "." + h2 + "." + ++h3 + " " + text;
                    h4 = 0;
                    break;
                default:
                    newTitle = h1 + "." + h2 + "." + h3 + "." + ++h4 + " " + text;
                    break;
            }

            titlesToReplace.Add((text, newTitle));
            spanReplacements.Add((heading.Span.Start, heading.Span.Length, new string('#', heading.Level) + " " + newTitle));
        }

        // Apply back-to-front so earlier spans stay valid after each replacement
        spanReplacements.Sort((a, b) => b.Start.CompareTo(a.Start));
        var sb = new StringBuilder(markdown);
        foreach (var (start, length, newHeading) in spanReplacements)
            sb.Remove(start, length).Insert(start, newHeading);

        var result = sb.ToString();

        foreach (var (oldTitle, newTitle) in titlesToReplace)
            result = Regex.Replace(result, "(<a.*?)>" + oldTitle + "</a>", "$1>" + newTitle.Replace("   ", " ") + "</a>");

        return new MarkdownProcessorResult(result, context);
    }
}
