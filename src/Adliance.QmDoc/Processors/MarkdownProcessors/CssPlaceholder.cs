using Adliance.QmDoc.Themes;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class CssPlaceholder(string theme) : IMarkdownProcessor
{
    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        if (string.IsNullOrWhiteSpace(theme)) return markdownContext;
        markdownContext.ReplacePlaceholder("CSS", "<style>" + ThemeProvider.GetCss(theme) + "</style>");
        return markdownContext;
    }
}
