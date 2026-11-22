using Adliance.QmDoc.Converter.Docx;

namespace Adliance.QmDoc.Tests.Converter.Docx;

public class DocxThemeTests
{
    [Fact]
    public void Parse_ResolvesVariablesAndLaterDefinitionsWin()
    {
        var theme = DocxTheme.Parse("""
                                    * { --color-text: rgb(2, 30, 69); --color-heading: #8cb918; --color-link: var(--color-heading); --font-family: 'Ubuntu', sans-serif; }
                                    /* --color-text: black; */
                                    * { --color-heading: rgba(0, 0, 0, 0.5); --transform-heading: uppercase; --fontSize: 20px; }
                                    """, 1);

        Assert.Equal("021E45", theme.TextColor);
        Assert.Equal("808080", theme.HeadingColor); // blended with the white page
        Assert.Equal("808080", theme.LinkColor);
        Assert.Equal("Ubuntu", theme.FontFamily);
        Assert.Equal("Ubuntu", theme.HeadingFontFamily); // falls back to the base font
        Assert.True(theme.UppercaseHeadings);
        Assert.Equal(20, theme.FontSizePx);
        Assert.Equal(300, theme.ToTwips(20)); // 20px = 15pt
    }

    [Fact]
    public void Load_ReadsThemeSpecificValues()
    {
        var madx = DocxTheme.Load("madx");
        Assert.Equal("Arial", madx.FontFamily);
        Assert.Equal("000000", madx.TextColor);
        Assert.Equal("878024", madx.HeadingColor);
        Assert.True(madx.UppercaseHeadings);

        var theme2026 = DocxTheme.Load("2026");
        Assert.Equal("Ubuntu", theme2026.FontFamily);
        Assert.Equal("Lora", theme2026.HeadingFontFamily);
        Assert.Equal("8CB918", theme2026.HeadingColor);
        Assert.False(theme2026.UppercaseHeadings);
    }

    [Theory]
    [InlineData("FFFFFF", 40, "FFFFFF")]
    [InlineData("000000", 100, "000000")]
    [InlineData("F47D09", 0, "FFFFFF")]
    public void MixWithWhite(string color, double percentage, string expected)
    {
        Assert.Equal(expected, DocxTheme.MixWithWhite(color, percentage));
    }

    [Fact]
    public void MixWithWhite_KeepsTheHue()
    {
        var mixed = DocxTheme.MixWithWhite("F47D09", 40);
        var r = Convert.ToInt32(mixed[..2], 16);
        var g = Convert.ToInt32(mixed[2..4], 16);
        var b = Convert.ToInt32(mixed[4..], 16);
        Assert.True(r > g && g > b, mixed); // still orange, but lighter
        Assert.True(b > 0x09, mixed);
    }
}
