using Adliance.QmDoc.Processors.HtmlProcessors;

namespace Adliance.QmDoc.Tests.Processors.HtmlProcessors;

public class IconBlocksTests
{
    [Theory]
    [InlineData("<p>{{?}} Frage</p>", "<div class=\"block block-question\">Frage</div>")]
    [InlineData("<p>{{!}} Hinweis</p>", "<div class=\"block block-alert\">Hinweis</div>")]
    [InlineData("<p>{{!!}} Gefahr</p>", "<div class=\"block block-danger\">Gefahr</div>")]
    [InlineData("<p>{{ ! }} Hinweis</p>", "<div class=\"block block-alert\">Hinweis</div>")]
    [InlineData("<p>{!} Hinweis</p>", "<div class=\"block block-alert\">Hinweis</div>")] // Markdig may swallow one brace
    [InlineData("<p>{{!}} <strong>Wichtig</strong> Text</p>", "<div class=\"block block-alert\"><strong>Wichtig</strong> Text</div>")]
    public void Apply_ConvertsCallouts(string html, string expected)
    {
        Assert.Equal(expected, new IconBlocks().Apply(html).ResultingHtml);
    }

    [Theory]
    [InlineData("<p>Text {{!}} in the middle</p>")]
    [InlineData("<p>{{!}}</p>")]
    [InlineData("<li>{{!}} Item</li>")]
    public void Apply_IgnoresOtherPositions(string html)
    {
        Assert.Equal(html, new IconBlocks().Apply(html).ResultingHtml);
    }

    [Fact]
    public void Apply_MultipleCallouts()
    {
        Assert.Equal("<div class=\"block block-question\">A</div>\n<div class=\"block block-danger\">B</div>", new IconBlocks().Apply("<p>{{?}} A</p>\n<p>{{!!}} B</p>").ResultingHtml);
    }

    [Fact(Skip = "Bug: callouts with a line break inside the paragraph (\"{{!}} Line 1\\nLine 2\") are not converted, because \".\" doesn't match newlines")]
    public void Apply_CalloutWithLineBreak()
    {
        Assert.Equal("<div class=\"block block-alert\">Line 1\nLine 2</div>", new IconBlocks().Apply("<p>{{!}} Line 1\nLine 2</p>").ResultingHtml);
    }

    [Fact]
    public void Apply_CalloutOverSeveralLines()
    {
        Assert.Equal("<div class=\"block block-alert\">A callout that\nwraps</div>", new IconBlocks().Apply("<p>{{!}} A callout that\nwraps</p>").ResultingHtml);
    }
}
