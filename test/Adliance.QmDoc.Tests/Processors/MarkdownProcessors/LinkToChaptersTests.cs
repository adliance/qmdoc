using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Tests.Helpers;
using static Adliance.QmDoc.Tests.Helpers.Md;

namespace Adliance.QmDoc.Tests.Processors.MarkdownProcessors;

public class LinkToChaptersTests
{
    private const string FilePath = "/docs/doc.md";

    private static MarkdownProcessorContext Link(string markdown) => new LinkToChapters(FilePath).Apply(markdown);

    private static string ChapterLink(string title, string id) => $"<span class=\"link-to-chapter\"><i></i>[{title}](#{id})</span>";

    [Fact]
    public void Apply_LinksToHeading()
    {
        var context = Link(Lines("# Einleitung", "", "See [#Einleitung]."));
        Assert.Equal(Lines("# Einleitung", "", $"See {ChapterLink("Einleitung", "einleitung")}."), context.Markdown);
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void Apply_MatchIsCaseInsensitive()
    {
        Assert.Contains("(#einleitung)", Link(Lines("# Einleitung", "[#einleitung]")).Markdown);
    }

    [Fact]
    public void Apply_LinksToNumberedHeading()
    {
        var context = Link(Lines("# 1. Einleitung", "## 1.1. Ziele", "", "[#Ziele]"));
        Assert.EndsWith(ChapterLink("Ziele", "ziele"), context.Markdown);
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void Apply_LinksToNonAsciiHeading()
    {
        var context = Link(Lines("# Élimination des déchets", "", "[#Élimination des déchets]"));
        Assert.EndsWith(ChapterLink("Élimination des déchets", "élimination-des-déchets"), context.Markdown);
    }

    [Fact]
    public void Apply_LinksToHeadingWithFormatting()
    {
        var context = Link(Lines("# The **important** part", "", "[#The important part]"));
        Assert.EndsWith(ChapterLink("The important part", "the-important-part"), context.Markdown);
    }

    [Fact]
    public void Apply_PartialTitle_StartsWith()
    {
        var context = Link(Lines("# Projektmeldung (Forschungsregister)", "# Andere Meldung", "", "[#Projektmeldung]"));
        Assert.EndsWith(ChapterLink("Projektmeldung", "projektmeldung-forschungsregister"), context.Markdown);
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void Apply_PartialTitle_Contains()
    {
        var context = Link(Lines("# Funktionsbeschreibung Projektmeldung", "", "[#Projektmeldung]"));
        Assert.EndsWith(ChapterLink("Projektmeldung", "funktionsbeschreibung-projektmeldung"), context.Markdown);
    }

    [Fact]
    public void Apply_ExactTitle_IsPreferredOverPartialTitle()
    {
        var context = Link(Lines("# Konzept Details", "# Konzept", "", "[#Konzept]"));
        Assert.EndsWith(ChapterLink("Konzept", "konzept"), context.Markdown);
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void Apply_AmbiguousPartialTitle_ReportsErrorAndLinksToFirst()
    {
        var context = Link(Lines("# Konzept A", "# Konzept B", "", "[#Konzept]"));
        Assert.EndsWith(ChapterLink("Konzept", "konzept-a"), context.Markdown);
        Assert.StartsWith("The chapter link \"[#Konzept]\" is ambiguous", Assert.Single(context.Errors).ErrorMessage);
    }

    private static readonly string DuplicateHeadings = Lines("# Risk Analysis", "## Open Questions", "# Implementation", "### Details", "#### Open Questions", "");

    [Fact]
    public void Apply_DuplicateHeadings_ReportsAmbiguousLinkWithSuggestions()
    {
        var context = Link(DuplicateHeadings + "\n[#Open Questions]");
        Assert.EndsWith(ChapterLink("Open Questions", "open-questions"), context.Markdown);

        var nl = Environment.NewLine;
        Assert.Equal(
            $"The chapter link \"[#Open Questions]\" is ambiguous, linking to the first match. Add parent chapters to the link to make it unique, e.g.:{nl}  - [#Risk Analysis > Open Questions]{nl}  - [#Implementation > Details > Open Questions]",
            Assert.Single(context.Errors).ErrorMessage);
    }

    [Theory]
    [InlineData("[#Implementation > Open Questions]", "open-questions-1")] // levels in between can be omitted
    [InlineData("[#Implementation > Details > Open Questions]", "open-questions-1")]
    [InlineData("[#Implement > Open Questions]", "open-questions-1")] // parents can be partial
    [InlineData("[#implementation>open questions]", "open-questions-1")]
    [InlineData("[#Risk Analysis > Open Questions]", "open-questions")]
    [InlineData("[#Risk Analysis > Open]", "open-questions")] // partial title below a parent
    public void Apply_DuplicateHeadings_CanBeQualifiedWithParents(string link, string expectedId)
    {
        var context = Link(DuplicateHeadings + "\n" + link);
        Assert.EndsWith($"(#{expectedId})</span>", context.Markdown);
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void Apply_ParentsInWrongOrder_ReportsError()
    {
        var context = Link(DuplicateHeadings + "\n[#Details > Implementation > Open Questions]");
        Assert.Equal("Unable to find a chapter matching \"[#Details > Implementation > Open Questions]\".", Assert.Single(context.Errors).ErrorMessage);
    }

    [Fact]
    public void Apply_UnknownChapterWithPath_ReportsErrorAndUsesSlug()
    {
        var context = Link(Lines("# A", "[#Foo > Bar Baz]"));
        Assert.EndsWith(ChapterLink("Bar Baz", "bar-baz"), context.Markdown);
        Assert.Equal("Unable to find a chapter matching \"[#Foo > Bar Baz]\".", Assert.Single(context.Errors).ErrorMessage);
    }

    [Fact]
    public void Apply_UnknownChapterWithoutPath_IsReportedLater()
    {
        // reported by SetCorrectChaptersLinkTitle, which knows all ids in the final HTML
        var context = Link(Lines("# A", "[#Gibt es nicht]"));
        Assert.EndsWith(ChapterLink("Gibt es nicht", "gibt-es-nicht"), context.Markdown);
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void Apply_MultipleLinksInOneLine()
    {
        var context = Link(Lines("# A", "# B", "[#A] and [#B]"));
        Assert.EndsWith($"{ChapterLink("A", "a")} and {ChapterLink("B", "b")}", context.Markdown);
    }

    [Fact]
    public void Apply_DoesNotTouchOtherLinks()
    {
        const string markdown = "[Text](#anchor) [^1] [other.md] [ ] [x]";
        Assert.Equal(markdown, Link(markdown).Markdown);
    }

    [Theory]
    [InlineData("Ticket [#] and C[#]")]
    [InlineData("[# ]")]
    public void Apply_EmptyChapterLink_StaysWithoutError(string markdown)
    {
        var context = Link(Lines("# A", markdown));
        Assert.Equal(Lines("# A", markdown), context.Markdown);
        Assert.Empty(context.Errors);
    }

    [Theory]
    [InlineData("[#A > ]")]
    [InlineData("[# > A]")]
    [InlineData("[#A >  > B]")]
    public void Apply_ChapterLinkWithEmptySegment_ReportsError(string link)
    {
        var context = Link(Lines("# A", link));
        Assert.Equal(Lines("# A", link), context.Markdown);
        Assert.Equal($"The chapter link \"{link}\" contains an empty chapter title.", Assert.Single(context.Errors).ErrorMessage);
    }

    [Fact]
    public void Apply_IgnoresInlineCode()
    {
        var context = Link(Lines("# A", "", "`[#A]` and [#A]"));
        Assert.Equal(Lines("# A", "", $"`[#A]` and {ChapterLink("A", "a")}"), context.Markdown);
    }

    [Fact]
    public void Apply_IgnoresCodeBlocks()
    {
        var markdown = Lines("# A", "", "```", "[#A]", "[#Gibt es nicht > B]", "```");
        var context = Link(markdown);
        Assert.Equal(markdown, context.Markdown);
        Assert.Empty(context.Errors);
    }

    [Theory]
    [InlineData("Einleitung", "einleitung")]
    [InlineData("  Open Questions  ", "open-questions")]
    [InlineData("Élimination des déchets", "élimination-des-déchets")]
    [InlineData("1. Einleitung", "einleitung")]
    [InlineData("Ä, Ö & Ü?", "ä-ö-ü")]
    public void GetChapterId(string title, string expected)
    {
        Assert.Equal(expected, LinkToChapters.GetChapterId(title));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void GetChapterId_EmptyTitle_Throws(string title)
    {
        Assert.Throws<ArgumentException>(() => LinkToChapters.GetChapterId(title));
    }

    [Theory]
    [InlineData("[#123](https://example.com/issues/123)")]
    [InlineData("[#Einleitung][ref]")]
    [InlineData("![#Einleitung](image.png)")]
    public void Apply_RegularLinks_AreNoChapterLinks(string link)
    {
        var markdown = Lines("# Einleitung", "", link);
        Assert.Equal(markdown, Link(markdown).Markdown);
    }

    [Fact]
    public void Apply_HeadingWithInlineCode_IsFound()
    {
        var context = Link(Lines("# Using `qmdoc` on the CLI", "", "[#Using qmdoc]"));
        Assert.Contains("(#using-qmdoc-on-the-cli)", context.Markdown);
        Assert.Empty(context.Errors);
    }
}
