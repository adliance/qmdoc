using Adliance.QmDoc.Processors.HtmlProcessors;

namespace Adliance.QmDoc.Tests.Processors.HtmlProcessors;

public class IconListsTests
{
    [Theory]
    [InlineData("<li>{{?}} Frage</li>", "<li><i class=\"fad fa-question-circle\"></i>Frage</li>")]
    [InlineData("<li>{{!}} Hinweis</li>", "<li><i class=\"fad fa-info-circle\"></i>Hinweis</li>")]
    [InlineData("<li>{{!!}} Gefahr</li>", "<li><i class=\"fad fa-exclamation-circle\"></i>Gefahr</li>")]
    [InlineData("<li>  {{!}}   Hinweis</li>", "<li><i class=\"fad fa-info-circle\"></i>Hinweis</li>")]
    public void Apply_AddsIconsToListItems(string html, string expected)
    {
        Assert.Equal(expected, new IconLists().Apply(html).ResultingHtml);
    }

    [Theory]
    [InlineData("<li>Text {{!}}</li>")]
    [InlineData("<p>{{!}} Text</p>")]
    public void Apply_IgnoresOtherPositions(string html)
    {
        Assert.Equal(html, new IconLists().Apply(html).ResultingHtml);
    }
}
