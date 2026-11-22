using Adliance.QmDoc.Processors.MarkdownProcessors;

namespace Adliance.QmDoc.Tests.Helpers;

public static class Md
{
    public const string Nbsp = " ";

    public static MarkdownProcessorContext Context(string markdown) => new(markdown.Replace("\r\n", "\n"));

    public static MarkdownProcessorContext Apply(this IMarkdownProcessor processor, string markdown) => processor.Apply(Context(markdown));

    /// <summary>
    /// Joins lines with "\n" (the converter normalizes line endings to "\n" as well).
    /// </summary>
    public static string Lines(params string[] lines) => string.Join("\n", lines);

    /// <summary>
    /// Returns the trimmed cells of the first table row whose first cell is <paramref name="firstCell"/>.
    /// </summary>
    public static string[] Row(string markdown, string firstCell)
    {
        foreach (var line in markdown.Split('\n'))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith('|')) continue;
            var cells = trimmed.Trim('|').Split('|').Select(c => c.Trim()).ToArray();
            if (cells.Length > 0 && cells[0] == firstCell) return cells;
        }

        throw new InvalidOperationException($"No table row \"{firstCell}\" in:\n{markdown}");
    }
}
