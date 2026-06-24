using System.Globalization;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class GitDateAndVersionPlaceholder(GitService.Change? latestVersion) : IMarkdownProcessor
{
    public const string Placeholder = "GIT_DATE_VERSION";

    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        markdownContext.ReplacePlaceholder(Placeholder,
            latestVersion != null
                ? $"Version {latestVersion.ShaShort}, {latestVersion.Date.ToString("dd. MMMM yyyy", new CultureInfo("de-DE"))}"
                : "");

        return markdownContext;
    }
}
