using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Tests.Helpers;
using static Adliance.QmDoc.Tests.Helpers.Md;

namespace Adliance.QmDoc.Tests.Processors.MarkdownProcessors;

public class PageBreakTests
{
    private const string PageBreakHtml = "<div style=\"page-break-after: always;\"></div>";

    [Fact]
    public void Apply_ReplacesHorizontalRuleWithPageBreak()
    {
        Assert.Equal(Lines("A", "", PageBreakHtml, "", "B"), new PageBreak().Apply(Lines("A", "", "---", "", "B")).Markdown);
    }

    [Fact]
    public void Apply_DoesNotTouchFrontmatter()
    {
        // the frontmatter is already removed from the markdown that is processed
        var result = new PageBreak().Apply(Lines("---", "title: X", "---", "A")).Markdown;
        Assert.Equal("A", result);
    }

    [Fact]
    public void Apply_OnlyMatchesAtLineStart()
    {
        Assert.Equal("A --- B", new PageBreak().Apply("A --- B").Markdown);
    }

    [Fact]
    public void Apply_DoesNotTouchTablesWithLeadingPipe()
    {
        var table = Lines("| A | B |", "|---|---|", "| 1 | 2 |");
        Assert.Equal(table, new PageBreak().Apply(table).Markdown);
    }

    [Fact]
    public void Apply_DoesNotTouchTablesWithoutLeadingPipe()
    {
        var table = Lines("A | B", "---|---", "1 | 2");
        Assert.Equal(table, new PageBreak().Apply(table).Markdown);
    }

    [Fact]
    public void Apply_DoesNotTouchCodeBlocks()
    {
        var code = Lines("```yaml", "---", "key: value", "```");
        Assert.Equal(code, new PageBreak().Apply(code).Markdown);
    }

    [Fact]
    public void Apply_DoesNotTouchSetextHeadings()
    {
        var heading = Lines("Title", "---");
        Assert.Equal(heading, new PageBreak().Apply(heading).Markdown);
    }

    [Theory]
    [InlineData("----")]
    [InlineData("---   ")]
    [InlineData("------------")]
    public void Apply_ReplacesLongerOrIndentedRules(string rule)
    {
        Assert.Equal(Lines("A", "", PageBreakHtml, "", "B"), new PageBreak().Apply(Lines("A", "", rule, "", "B")).Markdown);
    }

    [Theory]
    [InlineData("***")]
    [InlineData("___")]
    [InlineData("- - -")]
    public void Apply_OtherHorizontalRulesStayHorizontalRules(string rule)
    {
        var markdown = Lines("A", "", rule, "", "B");
        Assert.Equal(markdown, new PageBreak().Apply(markdown).Markdown);
    }

    [Fact]
    public void Apply_PageBreaksAtBeginningOfDocument_AreNoFrontmatter()
    {
        Assert.Equal(Lines(PageBreakHtml, "", "A", "", PageBreakHtml, "", "B"), new PageBreak().Apply(Lines("---", "", "A", "", "---", "", "B")).Markdown);
    }

    [Fact]
    public void Apply_MultiplePageBreaks()
    {
        Assert.Equal(Lines("A", "", PageBreakHtml, "", "B", "", PageBreakHtml), new PageBreak().Apply(Lines("A", "", "---", "", "B", "", "---")).Markdown);
    }

    [Fact]
    public void Apply_PageBreakInBlockquote()
    {
        Assert.Equal(Lines("> A", ">", $"> {PageBreakHtml}"), new PageBreak().Apply(Lines("> A", ">", "> ---")).Markdown);
    }

    [Fact]
    public void Apply_WithoutPageBreaks_DoesNothing()
    {
        Assert.Equal("A - B", new PageBreak().Apply("A - B").Markdown);
    }
}
