namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class GitVersionPlaceholder(GitService.Change? latestVersion) : IMarkdownProcessor
{
    public const string Placeholder = "GIT_VERSION";

    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        markdownContext.ReplacePlaceholder(Placeholder,
            latestVersion != null
                ? latestVersion.ShaShort
                : "");

        return markdownContext;
    }
}
