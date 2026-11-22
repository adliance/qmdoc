using Adliance.QmDoc.Themes;

namespace Adliance.QmDoc.Tests.Themes;

/// <summary>
/// Themes in "~/.qmdoc/themes" override the embedded themes, so these tests only check what all themes have in common.
/// </summary>
public class ThemeProviderTests
{
    [Theory]
    [InlineData("2026")]
    [InlineData("madx")]
    [InlineData("unknown-theme")]
    public void GetContent_ContainsPlaceholders(string theme)
    {
        var content = ThemeProvider.GetContent(theme);
        Assert.Contains("{{body}}", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("{{css}}", content.Replace(" ", ""), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("2026")]
    [InlineData("madx")]
    public void GetHeaderAndFooter_AreHtml(string theme)
    {
        Assert.Contains("<html", ThemeProvider.GetHeader(theme));
        Assert.Contains("<html", ThemeProvider.GetFooter(theme));
    }

    [Fact]
    public void GetCss_CombinesSharedAndThemeCss()
    {
        var shared = ThemeProvider.GetCss("unknown-theme");
        var theme = ThemeProvider.GetCss("2026");
        Assert.NotNull(shared);
        Assert.NotNull(theme);
        Assert.StartsWith(shared, theme);
        Assert.True(theme.Length > shared.Length);
    }

    [Fact]
    public void GetOptions_ReadsPdfSettings()
    {
        var options = ThemeProvider.GetOptions("2026");
        Assert.True(options.Pdf.HeaderHeight > 0);
        Assert.True(options.Pdf.FooterHeight > 0);
        Assert.True(options.Pdf.TimeoutSeconds > 0);
    }

    [Fact]
    public void ThemeOptions_Defaults()
    {
        var options = new ThemeOptions();
        Assert.Equal(60, options.Pdf.TimeoutSeconds);
        Assert.Null(options.Pdf.Scale);
    }
}
