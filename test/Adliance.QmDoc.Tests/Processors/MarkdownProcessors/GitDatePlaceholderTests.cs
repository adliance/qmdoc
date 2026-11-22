using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Tests.Helpers;

namespace Adliance.QmDoc.Tests.Processors.MarkdownProcessors;

public class GitDatePlaceholderTests
{
    internal static readonly GitService.Change Change = new()
    {
        Author = "Hannes Sachsenhofer",
        Date = new DateTimeOffset(2026, 3, 5, 14, 30, 0, TimeSpan.FromHours(1)),
        Sha = "0123456789abcdef0123456789abcdef01234567",
        Message = "Fix typo\n\nIn chapter 2.",
        MessageShort = "Fix typo"
    };

    [Fact]
    public void Apply_ReplacesDateOfLatestChange()
    {
        Assert.Equal("Stand: 05. März 2026", new GitDatePlaceholder(Change).Apply("Stand: {{ GIT_DATE }}").Markdown);
    }

    [Fact]
    public void Apply_WithoutChange_RemovesPlaceholder()
    {
        Assert.Equal("Stand: ", new GitDatePlaceholder(null).Apply("Stand: {{ GIT_DATE }}").Markdown);
    }

    [Fact]
    public void Apply_DoesNotReplaceGitDateVersion()
    {
        Assert.Equal("{{ GIT_DATE_VERSION }}", new GitDatePlaceholder(Change).Apply("{{ GIT_DATE_VERSION }}").Markdown);
    }
}
