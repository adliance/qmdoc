using System.Text.RegularExpressions;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class ImagesMustNotContainSpaces(string sourceFilePath) : IMarkdownProcessor
{
    public MarkdownProcessorResult Apply(string markdown, MarkdownProcessorContext markdownProcessorContext)
    {
        var result = new MarkdownProcessorResult(markdown, markdownProcessorContext);
        foreach (Match? m in Regex.Matches(markdown, "\\!\\[.*?\\]\\((.* .*)\\)", RegexOptions.IgnoreCase))
        {
            if (m == null) continue;
            result.Errors.Add(new ProcessorError(sourceFilePath, $"'{m.Groups[1].Value}' contains spaces, but image URLs must not contain spaces."));
        }

        return result;
    }
}
