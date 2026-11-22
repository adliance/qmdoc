using System;
using System.Globalization;

namespace Adliance.QmDoc.Processors.MarkdownProcessors.Calculation;

public class CalculationException(string message) : Exception(message);

/// <summary>
/// How the value was written in the source ("12h" vs. "12 h", "90€" vs. "€ 90", "135,-" vs. "135"), so that results look the same.
/// </summary>
public readonly record struct UnitStyle(bool SpaceBetween, bool Prefix, bool DashForNoCents = false);

/// <summary>
/// A number with an (optional) unit.
/// </summary>
/// <param name="Value">The exact value.</param>
/// <param name="Unit">The unit, <see cref="Calculation.Unit.None"/> for plain numbers.</param>
/// <param name="Decimals">The highest number of decimals of all numbers this value was calculated from, used to format the result.</param>
/// <param name="Style">How the unit is written.</param>
public sealed record Quantity(decimal Value, Unit Unit, int Decimals, UnitStyle Style)
{
    public Quantity Negate() => this with { Value = -Value };

    public Quantity Add(Quantity other) => AddOrSubtract(other, 1);

    public Quantity Subtract(Quantity other) => AddOrSubtract(other, -1);

    private Quantity AddOrSubtract(Quantity other, int sign)
    {
        if (!Unit.Equals(other.Unit))
        {
            throw new CalculationException($"Cannot {(sign > 0 ? "add" : "subtract")} {DescribeUnit(Unit)} and {DescribeUnit(other.Unit)}.");
        }

        return new Quantity(Checked(() => Value + sign * other.Value), Unit, Math.Max(Decimals, other.Decimals), PickStyle(this, other, Unit));
    }

    public Quantity Multiply(Quantity other)
    {
        var unit = Unit.Multiply(other.Unit).PreferCurrency();
        return new Quantity(Checked(() => Value * other.Value), unit, Math.Max(Decimals, other.Decimals), PickStyle(this, other, unit));
    }

    public Quantity Divide(Quantity other)
    {
        if (other.Value == 0) throw new CalculationException("Division by zero.");
        var unit = Unit.Divide(other.Unit);
        return new Quantity(Checked(() => Value / other.Value), unit, Math.Max(Decimals, other.Decimals), PickStyle(this, other, unit));
    }

    /// <summary>
    /// The number of decimals the value is shown with.
    /// Money is either shown without cents (if everything is whole numbers) or with (at least) two decimals.
    /// Everything else uses the same number of decimals as the input, but up to two more if the result needs them (eg. "90min" = "1,5h").
    /// </summary>
    public int DisplayDecimals
    {
        get
        {
            var maxDecimals = Math.Max(Decimals, 2);
            var needed = NeededDecimals(Math.Round(Value, maxDecimals, MidpointRounding.AwayFromZero));
            if (Unit.ContainsCurrency) return Decimals == 0 && needed == 0 ? 0 : maxDecimals;
            return Math.Max(Decimals, needed);
        }
    }

    /// <param name="culture">The culture used to format the number.</param>
    /// <param name="minimumDecimals">Show at least this many decimals (used to show all amounts of a table column with cents, if one of them has cents).</param>
    public string Format(CultureInfo culture, int minimumDecimals = 0)
    {
        var naturalDecimals = DisplayDecimals;
        var decimals = Math.Max(naturalDecimals, minimumDecimals);
        var rounded = Math.Round(Value, decimals, MidpointRounding.AwayFromZero);
        if (rounded == 0) rounded = 0; // avoid "-0"

        string number;
        if (naturalDecimals == 0 && Style.DashForNoCents && Unit.ContainsCurrency)
        {
            number = rounded.ToString("N0", culture) + culture.NumberFormat.NumberDecimalSeparator + "-"; // "135,-"
        }
        else
        {
            number = rounded.ToString("N" + decimals.ToString(CultureInfo.InvariantCulture), culture);
        }
        if (Unit.IsNone) return number;

        // non-breaking space, so that number and unit are never split across lines
        var separator = Style.SpaceBetween ? "\u00A0" : "";
        return Style.Prefix && Unit.IsSingleCurrency ? Unit + separator + number : number + separator + Unit;
    }

    private static int NeededDecimals(decimal value)
    {
        var decimals = 0;
        while (decimals < 28 && Math.Round(value, decimals) != value) decimals++;
        return decimals;
    }

    /// <summary>
    /// The result is written like the operand that "brought" the unit, preferably the one with the currency (eg. "12h * 90 €/h" = "1.080 €").
    /// </summary>
    private static UnitStyle PickStyle(Quantity a, Quantity b, Unit resultUnit)
    {
        if (resultUnit.IsNone) return default;

        Quantity? source = null;
        if (resultUnit.ContainsCurrency) source = a.Unit.ContainsCurrency ? a : b.Unit.ContainsCurrency ? b : null;
        source ??= a.Unit.IsNone ? b : a;
        return source.Style;
    }

    private static string DescribeUnit(Unit unit) => unit.IsNone ? "a number without unit" : $"\"{unit}\"";

    private static decimal Checked(Func<decimal> calculation)
    {
        try
        {
            return calculation();
        }
        catch (OverflowException)
        {
            throw new CalculationException("The result is too large.");
        }
    }
}
