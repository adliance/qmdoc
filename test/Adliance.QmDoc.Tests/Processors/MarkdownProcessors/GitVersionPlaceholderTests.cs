using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Tests.Helpers;

namespace Adliance.QmDoc.Tests.Processors.MarkdownProcessors;

public class GitVersionPlaceholderTests
{
    [Fact]
    public void Apply_ReplacesShortSha()
    {
        Assert.Equal("Version 0123456", new GitVersionPlaceholder(GitDatePlaceholderTests.Change).Apply("Version {{ GIT_VERSION }}").Markdown);
    }

    [Fact]
    public void Apply_WithoutChange_RemovesPlaceholder()
    {
        Assert.Equal("Version ", new GitVersionPlaceholder(null).Apply("Version {{ GIT_VERSION }}").Markdown);
    }

    [Fact]
    public void Apply_DoesNotReplaceGitVersions()
    {
        Assert.Equal("{{ GIT_VERSIONS }}", new GitVersionPlaceholder(GitDatePlaceholderTests.Change).Apply("{{ GIT_VERSIONS }}").Markdown);
    }
}
