using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Tests.Helpers;

namespace Adliance.QmDoc.Tests.Processors.MarkdownProcessors;

public class TitlePlaceholderTests
{
    [Fact]
    public void Apply_ReplacesTitle()
    {
        Assert.Equal("# My Document\n\nMy Document", new TitlePlaceholder("My Document").Apply("# {{ TITLE }}\n\n{{title}}").Markdown);
    }

    [Fact]
    public void Apply_WithoutPlaceholder_DoesNothing()
    {
        Assert.Equal("Text", new TitlePlaceholder("My Document").Apply("Text").Markdown);
    }
}
