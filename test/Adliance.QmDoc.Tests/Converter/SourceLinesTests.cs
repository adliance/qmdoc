using Adliance.QmDoc.Converter;
using static Adliance.QmDoc.Tests.Helpers.Md;

namespace Adliance.QmDoc.Tests.Converter;

public class SourceLinesTests
{
    [Fact]
    public void MapLines_UnchangedMarkdown_MapsEachLineToItself()
    {
        var markdown = Lines("# A", "", "Text", "", "# B");
        Assert.Equal([0, 1, 2, 3, 4], SourceLines.MapLines(markdown, markdown));
    }

    [Fact]
    public void MapLines_ChangedLines_MapToTheLinesTheyReplace()
    {
        var source = Lines("# A", "", "Text", "", "# B");
        var processed = Lines("# 1. A", "", "Text", "", "# 2. B");
        Assert.Equal([0, 1, 2, 3, 4], SourceLines.MapLines(source, processed));
    }

    [Fact]
    public void MapLines_RemovedFrontmatter_IsSkipped()
    {
        var source = Lines("---", "title: X", "---", "# A", "Text");
        var processed = Lines("# A", "Text");
        Assert.Equal([3, 4], SourceLines.MapLines(source, processed));
    }

    [Fact]
    public void MapLines_IncludedLines_MapToIncludePlaceholder()
    {
        var source = Lines("# A", "", "{{ include b.md }}", "", "# C");
        var processed = Lines("# A", "", "## B", "", "Text of B", "", "# C");
        var map = SourceLines.MapLines(source, processed);
        Assert.Equal(0, map[0]);
        Assert.Equal(2, map[2]);
        Assert.Equal(2, map[4]);
        Assert.Equal(4, map[6]);
    }

    [Fact]
    public void ToHtml_AddsSourceLineToBlocks()
    {
        var context = Context(Lines("---", "title: X", "---", "# A", "", "- one", "- two", "", "| X |", "|---|", "| 1 |"));
        var html = SourceLines.ToHtml(context);

        Assert.Contains("<h1 id=\"a\" data-line=\"3\">", html);
        Assert.Contains("<li data-line=\"5\">one</li>", html);
        Assert.Contains("<li data-line=\"6\">two</li>", html);
        Assert.Contains("<table data-line=\"8\">", html);
        Assert.Contains("<tr data-line=\"10\">", html);
    }

    [Fact]
    public void ToHtml_FrontmatterAndExpandedPlaceholder_MapToPlaceholder()
    {
        var context = Context(Lines("---", "title: X", "---", "", "{{ TOC }}", "", "# A"));
        context.Markdown = Lines("- [A](#a)", "- [B](#b)", "", "# A");
        var html = SourceLines.ToHtml(context);

        Assert.Contains("<ul data-line=\"4\">", html);
        Assert.Contains("<h1 id=\"a\" data-line=\"6\">", html);
    }
}
