using System.Globalization;
using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Tests.Helpers;

namespace Adliance.QmDoc.Tests.Processors.MarkdownProcessors;

public class DatePlaceholderTests
{
    [Fact]
    public void Apply_ReplacesDateInGermanFormat()
    {
        var before = DateTime.Now;
        var result = new DatePlaceholder().Apply("Stand: {{ DATE }}").Markdown;
        var after = DateTime.Now;

        var german = CultureInfo.GetCultureInfo("de-DE");
        Assert.Contains(result, new[] { "Stand: " + before.ToString("dd. MMMM yyyy", german), "Stand: " + after.ToString("dd. MMMM yyyy", german) });
    }

    [Fact]
    public void Apply_DoesNotReplaceGitDate()
    {
        Assert.Equal("{{ GIT_DATE }}", new DatePlaceholder().Apply("{{ GIT_DATE }}").Markdown);
    }
}
