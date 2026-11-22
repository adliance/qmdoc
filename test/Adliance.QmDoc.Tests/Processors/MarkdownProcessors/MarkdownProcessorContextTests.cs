using Adliance.AspNetCore.Buddy.Pdf;
using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Tests.Helpers;

namespace Adliance.QmDoc.Tests.Processors.MarkdownProcessors;

public class MarkdownProcessorContextTests
{
    [Theory]
    [InlineData("{{TITLE}}")]
    [InlineData("{{ TITLE }}")]
    [InlineData("{{ title }}")]
    [InlineData("{{   Title}}")]
    [InlineData("{{\tTITLE\t}}")]
    public void ReplacePlaceholder_IgnoresCaseAndWhitespace(string placeholder)
    {
        var context = Md.Context($"Title: {placeholder}.");
        context.ReplacePlaceholder("TITLE", "My Document");
        Assert.Equal("Title: My Document.", context.Markdown);
    }

    [Fact]
    public void ReplacePlaceholder_ReplacesAllOccurrences()
    {
        var context = Md.Context("{{ TITLE }} and {{title}}");
        context.ReplacePlaceholder("TITLE", "X");
        Assert.Equal("X and X", context.Markdown);
    }

    [Theory]
    [InlineData("{{ SUBTITLE }}")]
    [InlineData("{{ TITLE_2 }}")]
    [InlineData("{{ TITLES }}")]
    [InlineData("{ TITLE }")]
    [InlineData("TITLE")]
    public void ReplacePlaceholder_DoesNotReplaceOtherPlaceholders(string text)
    {
        var context = Md.Context(text);
        context.ReplacePlaceholder("TITLE", "X");
        Assert.Equal(text, context.Markdown);
    }

    [Fact]
    public void ReplacePlaceholder_DoesNotReplaceCalculationOfSameName()
    {
        var context = Md.Context("{{ = Faktor }} {{=Faktor}}");
        context.ReplacePlaceholder("Faktor", "3");
        Assert.Equal("{{ = Faktor }} {{=Faktor}}", context.Markdown);
    }

    [Fact]
    public void ReplacePlaceholder_InsideCalculation_OnlyReplacesInnerPlaceholder()
    {
        var context = Md.Context("{{ = 2 * {{ Faktor }} }}");
        context.ReplacePlaceholder("Faktor", "3");
        Assert.Equal("{{ = 2 * 3 }}", context.Markdown);
    }

    [Fact]
    public void ReplacePlaceholder_EscapesPlaceholderName()
    {
        var context = Md.Context("{{ a.b }} {{ axb }}");
        context.ReplacePlaceholder("a.b", "X");
        Assert.Equal("X {{ axb }}", context.Markdown);
    }

    [Fact]
    public void ReplacePlaceholder_ValueWithDollarSign_IsInsertedLiterally()
    {
        var context = Md.Context("Preis: {{ Preis }}");
        context.ReplacePlaceholder("Preis", "$0 / $& / $1");
        Assert.Equal("Preis: $0 / $& / $1", context.Markdown);
    }

    [Fact]
    public void ContainsPlaceholderInSource_ChecksSourceIncludingFrontmatter()
    {
        var context = Md.Context("---\ntitle: '{{ TOC }}'\n---\nText");
        Assert.True(context.ContainsPlaceholderInSource("TOC"));
        Assert.False(context.ContainsPlaceholderInSource("DATE"));
    }

    [Fact]
    public void ContainsPlaceholderInSource_IgnoresChangesToMarkdown()
    {
        var context = Md.Context("{{ TOC }}");
        context.Markdown = "";
        Assert.True(context.ContainsPlaceholderInSource("TOC"));
    }

    [Fact]
    public void ContainsPlaceholderInSource_WithMultiplePlaceholders()
    {
        var context = Md.Context("{{ git_date }}");
        Assert.True(context.ContainsPlaceholderInSource("GIT_VERSION", "GIT_DATE"));
        Assert.False(context.ContainsPlaceholderInSource("GIT_VERSION", "GIT_VERSIONS"));
        Assert.False(context.ContainsPlaceholderInSource());
    }

    [Fact]
    public void ResetForSecondPass_RestoresMarkdownAndClearsErrors()
    {
        var context = Md.Context("---\ntitle: X\n---\nOriginal");
        context.Markdown = "Changed";
        context.Errors.Add(new ProcessorError("a.md", "error"));
        var metadata = new PdfMetadata();

        context.ResetForSecondPass(metadata);

        Assert.Equal("Original", context.Markdown);
        Assert.Empty(context.Errors);
        Assert.Same(metadata, context.PdfMetadata);
    }

    [Fact]
    public void Pipeline_KeepsNonAsciiLettersInHeadingIds()
    {
        var context = Md.Context("# Élimination des déchets\n\n# изхвърляне");
        var html = Markdig.Markdown.ToHtml(context.Markdown, context.Pipeline);
        Assert.Contains("id=\"élimination-des-déchets\"", html);
        Assert.Contains("id=\"изхвърляне\"", html);
    }

    [Fact]
    public void LinkedDocument_EqualityIgnoresCase()
    {
        var a = new LinkedDocument("doc.html", "Doc");
        var b = new LinkedDocument("DOC.html", "doc");
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, new LinkedDocument("other.html", "Doc"));
        Assert.False(a.Equals("doc.html"));
    }
}
