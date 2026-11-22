using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Tests.Helpers;
using static Adliance.QmDoc.Tests.Helpers.Md;

namespace Adliance.QmDoc.Tests.Processors.MarkdownProcessors;

public class CodeSpansTests
{
    private static CodeSpans Find(string markdown) => CodeSpans.Find(markdown, Md.Context("").Pipeline);

    [Fact]
    public void Contains_InlineCode()
    {
        const string markdown = "Text `code` text";
        var spans = Find(markdown);
        Assert.False(spans.Contains(markdown.IndexOf("Text", StringComparison.Ordinal)));
        Assert.True(spans.Contains(markdown.IndexOf('`')));
        Assert.True(spans.Contains(markdown.IndexOf("code", StringComparison.Ordinal)));
        Assert.True(spans.Contains(markdown.LastIndexOf('`')));
        Assert.False(spans.Contains(markdown.LastIndexOf("text", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("```\ncode\n```")]
    [InlineData("~~~yaml\ncode\n~~~")]
    [InlineData("Text\n\n    code")]
    [InlineData("- List\n\n  ```\n  code\n  ```")]
    [InlineData("> ```\n> code\n> ```")]
    public void Contains_CodeBlocks(string markdown)
    {
        Assert.True(Find(markdown).Contains(markdown.IndexOf("code", StringComparison.Ordinal)));
    }

    [Fact]
    public void Contains_TextAfterCodeBlock_IsNoCode()
    {
        var markdown = Lines("```", "code", "```", "", "text");
        Assert.False(Find(markdown).Contains(markdown.IndexOf("text", StringComparison.Ordinal)));
    }

    [Fact]
    public void Contains_YamlBlockAtBeginning_IsNoCode()
    {
        // the frontmatter is already removed, so this is text between two page breaks
        var markdown = Lines("---", "text", "---");
        Assert.False(Find(markdown).Contains(markdown.IndexOf("text", StringComparison.Ordinal)));
    }

    [Fact]
    public void Contains_WithoutCode()
    {
        Assert.False(Find("Text").Contains(0));
        Assert.False(Find("").Contains(0));
    }
}
