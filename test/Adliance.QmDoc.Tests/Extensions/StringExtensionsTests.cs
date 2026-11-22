using Adliance.QmDoc.Extensions;

namespace Adliance.QmDoc.Tests.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("a&nbsp;&nbsp;b", "a&nbsp;b")]
    [InlineData("a&nbsp;&nbsp;&nbsp;b", "a&nbsp;b")]
    [InlineData("a&NBSP;&nbsp;b", "a&nbsp;b")]
    [InlineData("a&nbsp;b", "a&nbsp;b")]
    [InlineData("", "")]
    [InlineData("  ", "  ")]
    public void RemoveMultipleNbsp(string input, string expected)
    {
        Assert.Equal(expected, input.RemoveMultipleNbsp());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" , ,")]
    public void SplitCleanOrder_Empty(string? input)
    {
        Assert.Empty(input.SplitCleanOrder());
    }

    [Fact]
    public void SplitCleanOrder_TrimsDeduplicatesAndSorts()
    {
        Assert.Equal(["a", "b", "c d"], " c d , b,a,, b ".SplitCleanOrder());
    }
}
