using Adliance.QmDoc.Extensions;

namespace Adliance.QmDoc.Tests.Extensions;

public class LenientMatchingTests
{
    private static readonly string[] Names = ["Projektmeldung (Forschungsregister)", "Andere Projektmeldung", "Konzept A", "Konzept B", "Summe netto"];

    [Fact]
    public void FindPartial_PrefersStartsWith()
    {
        Assert.Equal([0], LenientMatching.FindPartial(Names, "Projektmeldung"));
    }

    [Fact]
    public void FindPartial_FallsBackToContains()
    {
        Assert.Equal([4], LenientMatching.FindPartial(Names, "netto"));
    }

    [Fact]
    public void FindPartial_IgnoresCaseAndSurroundingWhitespace()
    {
        Assert.Equal([4], LenientMatching.FindPartial(Names, "  SUMME  "));
    }

    [Fact]
    public void FindPartial_ReturnsAllMatches()
    {
        Assert.Equal([2, 3], LenientMatching.FindPartial(Names, "konzept"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("gibts nicht")]
    public void FindPartial_NoMatch(string search)
    {
        Assert.Empty(LenientMatching.FindPartial(Names, search));
    }
}
