using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Tests.Helpers;

namespace Adliance.QmDoc.Tests.Processors.MarkdownProcessors;

public class GitDateAndVersionPlaceholderTests
{
    [Fact]
    public void Apply_ReplacesVersionAndDate()
    {
        Assert.Equal("Version 0123456, 05. März 2026", new GitDateAndVersionPlaceholder(GitDatePlaceholderTests.Change).Apply("{{ GIT_DATE_VERSION }}").Markdown);
    }

    [Fact]
    public void Apply_WithoutChange_RemovesPlaceholder()
    {
        Assert.Equal("", new GitDateAndVersionPlaceholder(null).Apply("{{ GIT_DATE_VERSION }}").Markdown);
    }
}
