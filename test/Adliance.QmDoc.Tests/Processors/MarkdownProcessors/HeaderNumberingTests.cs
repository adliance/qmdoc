using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Tests.Helpers;
using static Adliance.QmDoc.Tests.Helpers.Md;

namespace Adliance.QmDoc.Tests.Processors.MarkdownProcessors;

public class HeaderNumberingTests
{
    private static string Number(string markdown) => new HeaderNumbering(true).Apply(markdown).Markdown;

    [Fact]
    public void Apply_NumbersHeadingsHierarchically()
    {
        var markdown = Lines("# A", "## B", "## C", "### D", "#### E", "#### F", "### G", "# H", "## I", "### J");
        Assert.Equal(Lines("# 1. A", "## 1.1. B", "## 1.2. C", "### 1.2.1. D", "#### 1.2.1.1. E", "#### 1.2.1.2. F", "### 1.2.2. G", "# 2. H", "## 2.1. I", "### 2.1.1. J"), Number(markdown));
    }

    [Fact]
    public void Apply_DoesNotNumberLevel5And6()
    {
        var markdown = Lines("# A", "##### B", "###### C");
        Assert.Equal(Lines("# 1. A", "##### B", "###### C"), Number(markdown));
    }

    [Fact]
    public void Apply_SkippedLevels_AreNumberedWithZero()
    {
        Assert.Equal(Lines("# 1. A", "### 1.0.1. B"), Number(Lines("# A", "### B")));
        Assert.Equal(Lines("## 0.1. A"), Number(Lines("## A")));
    }

    [Fact]
    public void Apply_KeepsTextBetweenHeadings()
    {
        var markdown = Lines("Intro", "", "# A", "", "Text with # hash", "", "## B", "", "- list");
        Assert.Equal(Lines("Intro", "", "# 1. A", "", "Text with # hash", "", "## 1.1. B", "", "- list"), Number(markdown));
    }

    [Fact]
    public void Apply_NormalizesWhitespaceAfterHashes()
    {
        Assert.Equal("# 1. A", Number("#    A   "));
    }

    [Fact]
    public void Apply_KeepsInlineFormatting()
    {
        Assert.Equal("# 1. **Bold** and `code`", Number("# **Bold** and `code`"));
    }

    [Fact]
    public void Apply_IgnoresHeadingsInCodeBlocks()
    {
        var markdown = Lines("# A", "", "```", "# Not a heading", "```", "", "# B");
        Assert.Equal(Lines("# 1. A", "", "```", "# Not a heading", "```", "", "# 2. B"), Number(markdown));
    }

    [Fact]
    public void Apply_LeavesAuthorLineInFirstLineUnnumbered()
    {
        var markdown = Lines("#### Hannes Sachsenhofer | Adliance GmbH | hannes@example.com", "", "# A", "#### B");
        Assert.Equal(Lines("#### Hannes Sachsenhofer | Adliance GmbH | hannes@example.com", "", "# 1. A", "#### 1.0.0.1. B"), Number(markdown));
    }

    [Fact]
    public void Apply_NumbersLevel4HeadingInFirstLineIfItIsNotTheFirstLine()
    {
        Assert.Equal(Lines("", "#### 0.0.0.1. A"), Number(Lines("", "#### A")));
    }

    [Fact]
    public void Apply_UpdatesLinkTextsOfHeadings()
    {
        var markdown = Lines("# Einleitung", "", "See <a href=\"#einleitung\">Einleitung</a>.");
        Assert.Equal(Lines("# 1. Einleitung", "", "See <a href=\"#einleitung\">1. Einleitung</a>."), Number(markdown));
    }

    [Fact]
    public void Apply_Disabled_DoesNothing()
    {
        var markdown = Lines("# A", "## B");
        Assert.Equal(markdown, new HeaderNumbering(false).Apply(markdown).Markdown);
    }

    [Fact]
    public void Apply_DuplicateHeadings_AreNumberedIndividually()
    {
        Assert.Equal(Lines("# 1. A", "## 1.1. X", "# 2. B", "## 2.1. X"), Number(Lines("# A", "## X", "# B", "## X")));
    }

    [Fact(Skip = "Bug: setext headings (\"Title\\n===\") are turned into \"# 1. Title\\n===\", so the underline becomes a paragraph (or a page break for \"---\")")]
    public void Apply_SetextHeadings()
    {
        Assert.Equal(Lines("# 1. A", "", "## 1.1. B"), Number(Lines("A", "===", "", "B", "---")));
    }

    [Fact]
    public void Apply_UpdatesLinkTextsOfHeadingsWithDollarSigns()
    {
        var markdown = Lines("# Preis $0 und $$", "", "<a href=\"#preis\">Preis $0 und $$</a>");
        Assert.Equal(Lines("# 1. Preis $0 und $$", "", "<a href=\"#preis\">1. Preis $0 und $$</a>"), Number(markdown));
    }
}
