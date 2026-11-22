using Adliance.QmDoc.Processors.HtmlProcessors;

namespace Adliance.QmDoc.Tests.Processors.HtmlProcessors;

public class AuthorLineTests
{
    [Fact]
    public void Apply_WithAuthor_PrependsAuthorLine()
    {
        var result = new AuthorLine("Hannes Sachsenhofer").Apply("<h1>A</h1>");
        Assert.Equal("<div class=\"document-author\"><span class=\"name\">Hannes Sachsenhofer</span>, Adliance GmbH</div><h1>A</h1>", result.ResultingHtml);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Apply_WithAuthor_IgnoresAuthorHeading()
    {
        const string heading = "<h4 id=\"x\">Max | ACME | max@example.com</h4>";
        Assert.EndsWith(heading, new AuthorLine("Hannes").Apply(heading).ResultingHtml);
    }

    [Fact]
    public void Apply_WithoutAuthor_ConvertsAuthorHeading()
    {
        var result = new AuthorLine(null).Apply("<h4 id=\"max-acme\">Max Muster | ACME GmbH | max@example.com</h4><p>Text</p>");
        Assert.Equal(
            "<div class=\"document-author\"><span class=\"name\">Max Muster</span>, <span class=\"company\">ACME GmbH</span><br /><span class=\"email\"><a href=\"mailto:max@example.com\">max@example.com</a></span></div><p>Text</p>",
            result.ResultingHtml);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Apply_WithoutAuthorAndHeading_DoesNothing(string? author)
    {
        const string html = "<h4>Just a heading</h4><h3>A | B | C</h3>";
        Assert.Equal(html, new AuthorLine(author).Apply(html).ResultingHtml);
    }
}
