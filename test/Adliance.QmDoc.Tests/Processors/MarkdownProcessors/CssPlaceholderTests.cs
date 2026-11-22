using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Tests.Helpers;

namespace Adliance.QmDoc.Tests.Processors.MarkdownProcessors;

public class CssPlaceholderTests
{
    [Fact]
    public void Apply_ReplacesCssWithThemeStyles()
    {
        var result = new CssPlaceholder("2026").Apply("<head>{{ CSS }}</head>").Markdown;
        Assert.StartsWith("<head><style>", result);
        Assert.EndsWith("</style></head>", result);
        Assert.True(result.Length > 1000);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Apply_WithoutTheme_DoesNothing(string theme)
    {
        Assert.Equal("{{ CSS }}", new CssPlaceholder(theme).Apply("{{ CSS }}").Markdown);
    }
}
