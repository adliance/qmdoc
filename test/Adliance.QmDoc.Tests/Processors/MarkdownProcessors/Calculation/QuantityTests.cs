using System.Globalization;
using Adliance.QmDoc.Processors.MarkdownProcessors.Calculation;
using static Adliance.QmDoc.Tests.Helpers.Md;

namespace Adliance.QmDoc.Tests.Processors.MarkdownProcessors.Calculation;

public class QuantityTests
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");
    private static readonly Unit Hours = Unit.Of("h");
    private static readonly Unit Euro = Unit.Of("€");

    private static Quantity Q(decimal value, Unit? unit = null, int decimals = 0, UnitStyle style = default) => new(value, unit ?? Unit.None, decimals, style);

    [Fact]
    public void Add_SameUnit()
    {
        var result = Q(12, Hours).Add(Q(1.5m, Hours, 1));
        Assert.Equal(13.5m, result.Value);
        Assert.Equal(Hours, result.Unit);
        Assert.Equal(1, result.Decimals);
    }

    [Fact]
    public void Subtract_SameUnit()
    {
        var result = Q(12, Hours).Subtract(Q(20, Hours));
        Assert.Equal(-8m, result.Value);
    }

    [Fact]
    public void Add_DifferentUnits_Throws()
    {
        var ex = Assert.Throws<CalculationException>(() => Q(12, Hours).Add(Q(5, Euro)));
        Assert.Equal("Cannot add \"h\" and \"€\".", ex.Message);
    }

    [Fact]
    public void Subtract_NumberAndUnit_Throws()
    {
        var ex = Assert.Throws<CalculationException>(() => Q(12).Subtract(Q(5, Euro)));
        Assert.Equal("Cannot subtract a number without unit and \"€\".", ex.Message);
    }

    [Fact]
    public void Multiply_CombinesUnitsAndPrefersCurrency()
    {
        var result = Q(145, Hours).Multiply(Q(2, Euro));
        Assert.Equal(290m, result.Value);
        Assert.Equal(Euro, result.Unit);
    }

    [Fact]
    public void Divide_CombinesUnits()
    {
        var result = Q(290, Euro).Divide(Q(145, Hours));
        Assert.Equal(2m, result.Value);
        Assert.Equal("€/h", result.Unit.ToString());
    }

    [Fact]
    public void Divide_ByZero_Throws()
    {
        var ex = Assert.Throws<CalculationException>(() => Q(1).Divide(Q(0, Hours)));
        Assert.Equal("Division by zero.", ex.Message);
    }

    [Fact]
    public void Overflow_Throws()
    {
        Assert.Equal("The result is too large.", Assert.Throws<CalculationException>(() => Q(decimal.MaxValue).Multiply(Q(2))).Message);
        Assert.Equal("The result is too large.", Assert.Throws<CalculationException>(() => Q(decimal.MaxValue).Add(Q(decimal.MaxValue))).Message);
        Assert.Equal("The result is too large.", Assert.Throws<CalculationException>(() => Q(decimal.MaxValue).Divide(Q(0.5m, null, 1))).Message);
    }

    [Fact]
    public void Negate()
    {
        Assert.Equal(-5m, Q(5, Hours).Negate().Value);
        Assert.Equal(Hours, Q(5, Hours).Negate().Unit);
    }

    [Theory]
    [InlineData(2, 0, 0)] // whole number
    [InlineData(2.5, 0, 1)] // up to two more decimals if needed
    [InlineData(1.6666666, 0, 2)] // but not more than two
    [InlineData(2, 1, 1)] // the decimals of the input are kept
    [InlineData(2.12345, 3, 3)]
    [InlineData(0.001, 0, 0)] // rounds to zero
    public void DisplayDecimals_ForNumbers(double value, int decimals, int expected)
    {
        Assert.Equal(expected, Q((decimal)value, Hours, decimals).DisplayDecimals);
    }

    [Theory]
    [InlineData(290, 0, 0)] // whole amounts without cents
    [InlineData(290, 2, 2)] // cents in the input
    [InlineData(362.5, 0, 2)] // cents in the result are always shown with two decimals
    [InlineData(0.005, 0, 2)]
    [InlineData(1.2345, 3, 3)] // more precise input keeps its decimals
    public void DisplayDecimals_ForMoney(double value, int decimals, int expected)
    {
        Assert.Equal(expected, Q((decimal)value, Euro, decimals).DisplayDecimals);
    }

    [Fact]
    public void Format_PlainNumber_UsesCulture()
    {
        Assert.Equal("1.234,5", Q(1234.5m).Format(De));
        Assert.Equal("1,234.5", Q(1234.5m).Format(En));
    }

    [Fact]
    public void Format_RoundsHalfAwayFromZero()
    {
        Assert.Equal("0,13", Q(0.125m, null, 2).Format(De));
        Assert.Equal("-0,13", Q(-0.125m, null, 2).Format(De));
        Assert.Equal("0,01€", Q(0.005m, Euro, 2).Format(De));
    }

    [Fact]
    public void Format_AvoidsNegativeZero()
    {
        Assert.Equal("0", Q(-0.001m).Format(De));
        Assert.Equal("0€", Q(-0.001m, Euro).Format(De));
    }

    [Fact]
    public void Format_UnitWithoutNumerator()
    {
        var perHour = Unit.None.Divide(Hours);
        Assert.Equal("5/h", Q(5, perHour).Format(De));
        Assert.Equal("5/h", Q(5, perHour, 0, new UnitStyle(true, false)).Format(De));
        Assert.Equal("1.000,5/h", Q(1000.5m, perHour, 1).Format(De));
        Assert.Equal("5/(€·h)", Q(5, perHour.Divide(Euro)).Format(De));
    }

    [Fact]
    public void Format_UnitWithoutSpace()
    {
        Assert.Equal("12h", Q(12, Hours).Format(De));
    }

    [Fact]
    public void Format_UnitWithSpace_UsesNonBreakingSpace()
    {
        Assert.Equal($"12{Nbsp}h", Q(12, Hours, 0, new UnitStyle(true, false)).Format(De));
    }

    [Fact]
    public void Format_PrefixCurrency()
    {
        Assert.Equal("€90", Q(90, Euro, 0, new UnitStyle(false, true)).Format(De));
        Assert.Equal($"€{Nbsp}90", Q(90, Euro, 0, new UnitStyle(true, true)).Format(De));
    }

    [Fact]
    public void Format_PrefixIsIgnoredForCompoundUnits()
    {
        Assert.Equal($"90{Nbsp}€/h", Q(90, Euro.Divide(Hours), 0, new UnitStyle(true, true)).Format(De));
    }

    [Fact]
    public void Format_DashForNoCents()
    {
        Assert.Equal($"€{Nbsp}20.250,-", Q(20250, Euro, 0, new UnitStyle(true, true, true)).Format(De));
        Assert.Equal("1,234.-€", Q(1234, Euro, 0, new UnitStyle(false, false, true)).Format(En));
    }

    [Fact]
    public void Format_DashForNoCents_OnlyForWholeAmountsOfMoney()
    {
        Assert.Equal("20,50€", Q(20.5m, Euro, 0, new UnitStyle(false, false, true)).Format(De));
        Assert.Equal("20h", Q(20, Hours, 0, new UnitStyle(false, false, true)).Format(De));
    }

    [Fact]
    public void Format_DashForNoCents_IsKeptWhenMinimumDecimalsAreRequested()
    {
        // "135,-" already means "no cents", so it stays like that even if other amounts in the column have cents
        Assert.Equal("135,-€", Q(135, Euro, 0, new UnitStyle(false, false, true)).Format(De));
        Assert.Equal("135,-€", Q(135, Euro, 0, new UnitStyle(false, false, true)).Format(De, 2));
    }

    [Fact]
    public void Format_MinimumDecimals()
    {
        Assert.Equal("10,00€", Q(10, Euro).Format(De, 2));
        Assert.Equal("10,125€", Q(10.125m, Euro, 3).Format(De, 2));
    }
}
