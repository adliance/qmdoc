using Adliance.QmDoc.Processors.MarkdownProcessors.Calculation;

namespace Adliance.QmDoc.Tests.Processors.MarkdownProcessors.Calculation;

public class UnitTests
{
    private static readonly Unit Hours = Unit.Of("h");
    private static readonly Unit Euro = Unit.Of("€");
    private static readonly Unit Pieces = Unit.Of("Stk");

    [Fact]
    public void None_IsEmpty()
    {
        Assert.True(Unit.None.IsNone);
        Assert.Equal("", Unit.None.ToString());
        Assert.False(Unit.None.ContainsCurrency);
        Assert.False(Unit.None.IsSingleCurrency);
    }

    [Fact]
    public void Of_CreatesSimpleUnit()
    {
        Assert.False(Hours.IsNone);
        Assert.Equal("h", Hours.ToString());
    }

    [Theory]
    [InlineData("€", true, true)]
    [InlineData("$", true, true)]
    [InlineData("£", true, true)]
    [InlineData("EUR", true, true)]
    [InlineData("USD", true, true)]
    [InlineData("CHF", true, true)]
    [InlineData("GBP", true, true)]
    [InlineData("h", false, false)]
    [InlineData("eur", false, false)] // currency codes are case-sensitive
    public void Currencies_AreDetected(string symbol, bool containsCurrency, bool isSingleCurrency)
    {
        var unit = Unit.Of(symbol);
        Assert.Equal(containsCurrency, unit.ContainsCurrency);
        Assert.Equal(isSingleCurrency, unit.IsSingleCurrency);
    }

    [Theory]
    [InlineData("€", true)]
    [InlineData("EUR", true)]
    [InlineData("CHF", true)]
    [InlineData("eur", false)]
    [InlineData("h", false)]
    [InlineData("Rabatt", false)]
    [InlineData("", false)]
    public void IsCurrency(string symbol, bool expected)
    {
        Assert.Equal(expected, Unit.IsCurrency(symbol));
    }

    [Fact]
    public void CompoundCurrencyUnit_IsNotSingleCurrency()
    {
        var perHour = Euro.Divide(Hours);
        Assert.True(perHour.ContainsCurrency);
        Assert.False(perHour.IsSingleCurrency);
        Assert.False(Euro.Multiply(Euro).IsSingleCurrency);
    }

    [Fact]
    public void Multiply_AddsExponents()
    {
        Assert.Equal("h²", Hours.Multiply(Hours).ToString());
        Assert.Equal("h³", Hours.Multiply(Hours).Multiply(Hours).ToString());
    }

    [Fact]
    public void Multiply_WithNone_KeepsUnit()
    {
        Assert.Equal(Hours, Hours.Multiply(Unit.None));
        Assert.Equal(Hours, Unit.None.Multiply(Hours));
    }

    [Fact]
    public void Divide_CreatesCompoundUnit()
    {
        Assert.Equal("€/h", Euro.Divide(Hours).ToString());
    }

    [Fact]
    public void Divide_BySameUnit_Cancels()
    {
        Assert.True(Hours.Divide(Hours).IsNone);
        Assert.Equal(Euro, Euro.Divide(Hours).Multiply(Hours));
    }

    [Fact]
    public void ToString_WithoutNumerator_Uses1()
    {
        Assert.Equal("1/h", Unit.None.Divide(Hours).ToString());
    }

    [Fact]
    public void ToString_WithMultipleDenominators_UsesBrackets()
    {
        Assert.Equal("€/(Stk·h)", Euro.Divide(Hours).Divide(Pieces).ToString());
    }

    [Fact]
    public void ToString_PutsCurrenciesFirst()
    {
        Assert.Equal("€·h", Hours.Multiply(Euro).ToString());
        Assert.Equal("€·h", Euro.Multiply(Hours).ToString());
    }

    [Fact]
    public void ToString_SortsOtherSymbolsOrdinally()
    {
        Assert.Equal("Stk·h", Hours.Multiply(Pieces).ToString());
    }

    [Fact]
    public void ToString_UsesSuperscriptsForMultiDigitExponents()
    {
        var unit = Unit.None;
        for (var i = 0; i < 12; i++) unit = unit.Multiply(Hours);
        Assert.Equal("h¹²", unit.ToString());
        Assert.Equal("1/h¹²", Unit.None.Divide(unit).ToString());
    }

    [Fact]
    public void PreferCurrency_DropsOtherNumeratorUnits()
    {
        Assert.Equal(Euro, Hours.Multiply(Euro).PreferCurrency());
        Assert.Equal(Euro, Hours.Multiply(Pieces).Multiply(Euro).PreferCurrency());
    }

    [Fact]
    public void PreferCurrency_KeepsDenominator()
    {
        var unit = Pieces.Multiply(Euro).Divide(Hours).PreferCurrency();
        Assert.Equal("€/h", unit.ToString());
    }

    [Fact]
    public void PreferCurrency_WithoutSingleCurrency_KeepsUnit()
    {
        var twoCurrencies = Euro.Multiply(Unit.Of("$")).Multiply(Hours);
        Assert.Equal(twoCurrencies, twoCurrencies.PreferCurrency());

        var squared = Euro.Multiply(Euro).Multiply(Hours);
        Assert.Equal(squared, squared.PreferCurrency());

        Assert.Equal(Hours.Multiply(Hours), Hours.Multiply(Hours).PreferCurrency());
    }

    [Fact]
    public void Equality_IsStructural()
    {
        var a = Euro.Divide(Hours);
        var b = Unit.None.Divide(Hours).Multiply(Euro);
        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());

        Assert.False(a.Equals(Euro));
        Assert.False(a.Equals(null));
        Assert.False(a.Equals("€/h"));
        Assert.False(Hours.Equals(Unit.Of("H")));
    }
}
