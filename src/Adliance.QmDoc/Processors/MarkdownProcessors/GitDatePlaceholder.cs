using System.Globalization;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class GitDatePlaceholder(GitService.Change? latestVersion) : IMarkdownProcessor
{
    public const string Placeholder = "GIT_DATE";

    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        markdownContext.ReplacePlaceholder(Placeholder,
            latestVersion != null
                ? latestVersion.Date.ToString("dd. MMMM yyyy", new CultureInfo("de-DE"))
                : "");

        return markdownContext;
    }
}
