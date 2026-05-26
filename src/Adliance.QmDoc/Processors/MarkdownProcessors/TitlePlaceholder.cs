namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class TitlePlaceholder(string title) : IMarkdownProcessor
{
    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        markdownContext.ReplacePlaceholder("TITLE", title);
        return markdownContext;
    }
}
