using Adliance.QmDoc.Processors.HtmlProcessors;

namespace Adliance.QmDoc.Tests.Processors.HtmlProcessors;

public class SetCorrectChaptersLinkTitleTests
{
    private static HtmlProcessorResult Apply(string html) => new SetCorrectChaptersLinkTitle("/docs/doc.md").Apply(html);

    [Fact]
    public void Apply_SetsLinkTextToNumberedHeading()
    {
        var result = Apply("<h1 id=\"einleitung\">1. Einleitung</h1><p><span class=\"link-to-chapter\"><i></i><a href=\"#einleitung\">Einleitung</a></span></p>");
        Assert.Equal("<h1 id=\"einleitung\">1. Einleitung</h1><p><span class=\"link-to-chapter\"><i></i><a href=\"#einleitung\">1. Einleitung</a></span></p>", result.ResultingHtml);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Apply_RemovesMultipleNonBreakingSpaces()
    {
        var result = Apply("<h2 id=\"a\">1.1.&nbsp;&nbsp;&nbsp;A</h2><span class=\"link-to-chapter\"><i></i><a href=\"#a\">A</a></span>");
        Assert.EndsWith("<span class=\"link-to-chapter\"><i></i><a href=\"#a\">1.1.&nbsp;A</a></span>", result.ResultingHtml);
    }

    [Fact]
    public void Apply_NonAsciiIds_ArePercentEncodedInLinks()
    {
        var result = Apply("<h1 id=\"élimination\">1. Élimination</h1><span class=\"link-to-chapter\"><i></i><a href=\"#%C3%A9limination\">Élimination</a></span>");
        Assert.EndsWith("<span class=\"link-to-chapter\"><i></i><a href=\"#%C3%A9limination\">1. Élimination</a></span>", result.ResultingHtml);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Apply_NonAsciiIds_LinkedWithoutEncoding()
    {
        var result = Apply("<h1 id=\"élimination\">1. Élimination</h1><span class=\"link-to-chapter\"><i></i><a href=\"#élimination\">Élimination</a></span>");
        Assert.EndsWith("<span class=\"link-to-chapter\"><i></i><a href=\"#%C3%A9limination\">1. Élimination</a></span>", result.ResultingHtml);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Apply_RewritesLegacyAsciiAnchors()
    {
        var result = Apply("<h1 id=\"élimination-des-déchets\">Élimination des déchets</h1><span class=\"link-to-chapter\"><i></i><a href=\"#elimination-des-dechets\">x</a></span>");
        Assert.EndsWith("<span class=\"link-to-chapter\"><i></i><a href=\"#%C3%A9limination-des-d%C3%A9chets\">Élimination des déchets</a></span>", result.ResultingHtml);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Apply_LegacyAnchorsOfDuplicateHeadings_UseFirstHeading()
    {
        var result = Apply("<h1 id=\"é\">É</h1><h1 id=\"é-1\">É</h1><span class=\"link-to-chapter\"><i></i><a href=\"#e\">x</a></span>");
        Assert.EndsWith("<span class=\"link-to-chapter\"><i></i><a href=\"#%C3%A9\">É</a></span>", result.ResultingHtml);
    }

    [Fact]
    public void Apply_LinkToMissingChapter_ReportsErrorWithSuggestions()
    {
        var result = Apply("<h1 id=\"a\">A</h1><h2 id=\"b\">B</h2><a href=\"#c\">C</a>");
        var nl = Environment.NewLine;
        var error = Assert.Single(result.Errors);
        Assert.Equal($"Unable to find an element \"c\", but there's a link to it.{nl}Are you looking for one of these?{nl}  - a{nl}  - b", error.ErrorMessage);
        Assert.Equal("doc.md", error.FileName);
    }

    [Fact]
    public void Apply_LinkToMissingChapterWithoutAnyIds_ReportsError()
    {
        var error = Assert.Single(Apply("<a href=\"#c\">C</a>").Errors);
        Assert.Equal("Unable to find an element \"c\", but there's a link to it.", error.ErrorMessage);
    }

    [Fact]
    public void Apply_LinkToOtherElementWithId_IsValid()
    {
        Assert.Empty(Apply("<p id=\"fn1\">Footnote</p><a href=\"#fn1\">1</a>").Errors);
    }

    [Fact]
    public void Apply_ExternalLinks_AreIgnored()
    {
        const string html = "<h1 id=\"a\">1. A</h1><a href=\"https://example.com/#a\">A</a><a href=\"other.html#a\">A</a>";
        var result = Apply(html);
        Assert.Equal(html, result.ResultingHtml);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Apply_HeadingWithDollarSigns()
    {
        var result = Apply("<h1 id=\"preis\">1. Preis $0 und $$</h1><span class=\"link-to-chapter\"><i></i><a href=\"#preis\">Preis</a></span>");
        Assert.EndsWith("<span class=\"link-to-chapter\"><i></i><a href=\"#preis\">1. Preis $0 und $$</a></span>", result.ResultingHtml);
    }

    [Fact]
    public void Apply_LinkWithOwnText_KeepsText()
    {
        const string html = "<h1 id=\"intro\">1. Intro</h1><p><a href=\"#intro\">described above</a></p>";
        Assert.Equal(html, Apply(html).ResultingHtml);
    }

    [Fact]
    public void Apply_TableOfContents_SetsLinkText()
    {
        var result = Apply("<h1 id=\"intro\">1. Intro</h1><div class=\"toc\"><table><tr><td><a href=\"#intro\">Intro</a></td></tr></table></div>");
        Assert.Contains("<a href=\"#intro\">1. Intro</a>", result.ResultingHtml);
    }

    [Fact]
    public void Apply_HeadingWithCode_KeepsWholeHeading()
    {
        var result = Apply("<h1 id=\"using-qmdoc\">1. Using <code>qmdoc</code> | CLI</h1><span class=\"link-to-chapter\"><i></i><a href=\"#using-qmdoc\">Using qmdoc</a></span>");
        Assert.EndsWith("<a href=\"#using-qmdoc\">1. Using <code>qmdoc</code> | CLI</a></span>", result.ResultingHtml);
    }
}
