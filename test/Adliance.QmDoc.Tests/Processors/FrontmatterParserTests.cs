using Adliance.QmDoc.Tests.Helpers;
using static Adliance.QmDoc.Tests.Helpers.Md;

namespace Adliance.QmDoc.Tests.Processors;

public class FrontmatterParserTests
{
    [Fact]
    public void Parse_WithoutFrontmatter_ReturnsDefaults()
    {
        var context = Md.Context("# Title\n\nText");
        var frontmatter = context.Frontmatter;

        Assert.Null(frontmatter.Title);
        Assert.Null(frontmatter.Theme);
        Assert.Null(frontmatter.Author);
        Assert.Null(frontmatter.Culture);
        Assert.Null(frontmatter.Watermark);
        Assert.Null(frontmatter.EnableHeaderNumbering);
        Assert.Empty(frontmatter.Custom);
        Assert.Equal("# Title\n\nText", frontmatter.MarkdownWithoutFrontmatter);
        Assert.Equal("# Title\n\nText", context.Markdown);
    }

    [Fact]
    public void Parse_ReadsTypedValues()
    {
        var frontmatter = Md.Context(Lines(
            "---",
            "title: Konzept Projektmeldungen",
            "theme: madx",
            "author: Hannes Sachsenhofer",
            "pdfWidth: 210",
            "pdfHeight: 297",
            "pdfScale: 0.75",
            "pdfTimeoutSeconds: 120",
            "enableHeader: false",
            "enableFooter: true",
            "enableHeaderNumbering: false",
            "enableDocumentTitle: false",
            "watermark: Entwurf",
            "culture: de-AT",
            "---",
            "Text")).Frontmatter;

        Assert.Equal("Konzept Projektmeldungen", frontmatter.Title);
        Assert.Equal("madx", frontmatter.Theme);
        Assert.Equal("Hannes Sachsenhofer", frontmatter.Author);
        Assert.Equal(210, frontmatter.PdfWidth);
        Assert.Equal(297, frontmatter.PdfHeight);
        Assert.Equal(0.75, frontmatter.PdfScale);
        Assert.Equal(120, frontmatter.PdfTimeoutSeconds);
        Assert.False(frontmatter.EnableHeader);
        Assert.True(frontmatter.EnableFooter);
        Assert.False(frontmatter.EnableHeaderNumbering);
        Assert.False(frontmatter.EnableDocumentTitle);
        Assert.Equal("Entwurf", frontmatter.Watermark);
        Assert.Equal("de-AT", frontmatter.Culture);
    }

    [Theory]
    [InlineData("---\ntitle: Test\n---   \nText")]
    [InlineData("---\ntitle: Test\n---\t\n\nText")]
    [InlineData("---\ntitle: Test\n---")]
    public void Parse_RemovesWhitespaceAfterFrontmatter(string markdown)
    {
        var context = Md.Context(markdown);
        Assert.Equal("Test", context.Frontmatter.Title);
        Assert.Equal(markdown.EndsWith("Text") ? "Text" : "", context.Markdown);
    }

    [Fact]
    public void Parse_RemovesFrontmatterFromMarkdown()
    {
        var context = Md.Context(Lines("---", "title: Test", "---", "", "", "# Heading", "", "Text"));
        Assert.Equal(Lines("# Heading", "", "Text"), context.Markdown);
        Assert.Equal(Lines("# Heading", "", "Text"), context.Frontmatter.MarkdownWithoutFrontmatter);
        Assert.StartsWith("---", context.SourceMarkdown);
    }

    [Fact]
    public void Parse_CollectsAllScalarValuesAsCustomValues()
    {
        var custom = Md.Context(Lines(
            "---",
            "title: Test",
            "Stundensatz: 135 €/h",
            "clientName: ACME",
            "Anzahl: 3",
            "Aktiv: true",
            "Leer:",
            "Liste:",
            "  - a",
            "  - b",
            "Objekt:",
            "  key: value",
            "---",
            "Text")).Frontmatter.Custom;

        Assert.Equal("Test", custom["title"]);
        Assert.Equal("135 €/h", custom["Stundensatz"]);
        Assert.Equal("ACME", custom["clientName"]);
        Assert.Equal("3", custom["Anzahl"]);
        Assert.Equal("true", custom["Aktiv"]);
        Assert.Equal("", custom["Leer"]);
        Assert.False(custom.ContainsKey("Liste"));
        Assert.False(custom.ContainsKey("Objekt"));
    }

    [Fact]
    public void Parse_CustomValues_AreCaseInsensitive()
    {
        var custom = Md.Context(Lines("---", "ClientName: ACME", "---")).Frontmatter.Custom;
        Assert.Equal("ACME", custom["clientname"]);
        Assert.Equal("ACME", custom["CLIENTNAME"]);
    }

    [Fact]
    public void Parse_UnknownTypedPropertiesAreIgnored()
    {
        var frontmatter = Md.Context(Lines("---", "somethingElse: 1", "---", "Text")).Frontmatter;
        Assert.Null(frontmatter.Title);
        Assert.Equal("1", frontmatter.Custom["somethingElse"]);
    }

    [Theory]
    [InlineData("---\n---\nText", "Text")]
    [InlineData("---\n---\n\nText", "Text")]
    [InlineData("---\n\n---\nText", "Text")]
    [InlineData("---  \n  \n---\t\nText", "Text")]
    [InlineData("---\r\n---\r\nText", "Text")]
    [InlineData("---\n---", "")]
    [InlineData("---\n# just a comment\n---\nText", "Text")]
    public void Parse_EmptyFrontmatter(string markdown, string expected)
    {
        var context = Md.Context(markdown);
        Assert.Empty(context.Frontmatter.Custom);
        Assert.Equal(expected, context.Markdown.Replace("\r\n", "\n"));
    }

    [Theory]
    [InlineData("---\nText\n---")]
    [InlineData("Text\n---\n---")]
    [InlineData("----\n----\nText")]
    [InlineData("---\n- a\n- b\n---\nText")]
    public void Parse_HorizontalRules_AreNoEmptyFrontmatter(string markdown)
    {
        Assert.Equal(markdown, Md.Context(markdown).Markdown);
    }

    [Fact]
    public void Parse_HorizontalRuleLaterInDocument_IsNoFrontmatter()
    {
        var markdown = Lines("Text", "", "---", "title: Test", "---");
        var context = Md.Context(markdown);
        Assert.Null(context.Frontmatter.Title);
        Assert.Equal(markdown, context.Markdown);
    }

    [Fact]
    public void Parse_QuotedValues()
    {
        var frontmatter = Md.Context(Lines("---", "title: \"Test: with colon\"", "Preis: '€ 135,-'", "---")).Frontmatter;
        Assert.Equal("Test: with colon", frontmatter.Title);
        Assert.Equal("€ 135,-", frontmatter.Custom["Preis"]);
    }
}
