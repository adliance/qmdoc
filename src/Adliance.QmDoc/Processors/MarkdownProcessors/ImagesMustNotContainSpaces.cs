using System.Text.RegularExpressions;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class ImagesMustNotContainSpaces(string sourceFilePath) : IMarkdownProcessor
{
    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        foreach (Match? m in Regex.Matches(markdownContext.Markdown, "\\!\\[.*?\\]\\((.* .*)\\)", RegexOptions.IgnoreCase))
        {
            if (m == null) continue;
            markdownContext.Errors.Add(new ProcessorError(sourceFilePath, $"'{m.Groups[1].Value}' contains spaces, but image URLs must not contain spaces."));
        }

        return markdownContext;
    }
}
