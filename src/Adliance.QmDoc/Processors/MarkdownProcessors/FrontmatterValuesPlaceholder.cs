namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class FrontmatterValuesPlaceholder : IMarkdownProcessor
{
    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        foreach (var (key, value) in markdownContext.Frontmatter.Custom)
        {
            markdownContext.ReplacePlaceholder(key, value);
        }

        return markdownContext;
    }
}
