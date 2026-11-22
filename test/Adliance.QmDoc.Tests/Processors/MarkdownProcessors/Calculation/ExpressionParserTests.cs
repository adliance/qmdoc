using System.Globalization;
using Adliance.QmDoc.Processors.MarkdownProcessors.Calculation;
using static Adliance.QmDoc.Tests.Helpers.Md;

namespace Adliance.QmDoc.Tests.Processors.MarkdownProcessors.Calculation;

public class ExpressionParserTests
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

    private static Quantity Eval(string formula, CultureInfo? culture = null, ReferenceResolver? references = null, NameResolver? names = null) =>
        ExpressionParser.Evaluate(formula, culture ?? De, references, names);

    private static string EvalFormatted(string formula, CultureInfo? culture = null) => Eval(formula, culture).Format(culture ?? De);

    private static string EvalError(string formula, CultureInfo? culture = null, ReferenceResolver? references = null, NameResolver? names = null) =>
        Assert.Throws<CalculationException>(() => Eval(formula, culture, references, names)).Message;

    #region Evaluate: arithmetic

    [Theory]
    [InlineData("1 + 2", "3")]
    [InlineData("1+2", "3")]
    [InlineData("  1  +  2  ", "3")]
    [InlineData("5 - 7", "-2")]
    [InlineData("2 + 3 * 4", "14")]
    [InlineData("(2 + 3) * 4", "20")]
    [InlineData("((2))", "2")]
    [InlineData("2 * (3 + (4 - 1)) / 3", "4")]
    [InlineData("10 - 2 - 3", "5")] // left associative
    [InlineData("100 / 10 / 2", "5")] // left associative
    [InlineData("10 / 4", "2,5")]
    [InlineData("10 / 3", "3,33")]
    [InlineData("2 × 3", "6")]
    [InlineData("2 · 3", "6")]
    [InlineData("6 ÷ 2", "3")]
    [InlineData("5 − 2", "3")] // unicode minus
    [InlineData("−5 + 2", "-3")]
    [InlineData("-5h + 8h", "3h")]
    [InlineData("--5", "5")]
    [InlineData("+5", "5")]
    [InlineData("-(2 + 3)", "-5")]
    [InlineData("2 * -3", "-6")]
    public void Evaluate_Arithmetic(string formula, string expected)
    {
        Assert.Equal(expected, EvalFormatted(formula));
    }

    [Theory]
    [InlineData("20%", "0,2")]
    [InlineData("20 %", "0,2")]
    [InlineData("1.200€ * 20%", "240€")]
    [InlineData("200 * 7,5%", "15,0")] // the result keeps the decimals of the input
    [InlineData("100 + 10%", "100,1")] // percent is just a number, not "plus 10 percent"
    public void Evaluate_Percentages(string formula, string expected)
    {
        Assert.Equal(expected, EvalFormatted(formula));
    }

    #endregion

    #region Evaluate: units

    [Theory]
    [InlineData("12h + 4h", "16h")]
    [InlineData("12 h + 4h", $"16{Nbsp}h")] // the style of the first operand is used
    [InlineData("(12h + 4h) * 2", "32h")]
    [InlineData("40h + 90min", "41,5h")] // minutes are converted to hours
    [InlineData("90min + 1h", "2,5h")]
    [InlineData("100min", "1,67h")]
    [InlineData("90 min", $"1,5{Nbsp}h")]
    [InlineData("145h * 2€", "290€")] // "quantity at price"
    [InlineData("3 Stk * 25€", "75€")]
    [InlineData("290€ / 145h", "2€/h")]
    [InlineData("12h * 90€/h", "1.080€")]
    [InlineData("12h * 90 €/h", $"1.080{Nbsp}€")]
    [InlineData("90 €/h * 12h", $"1.080{Nbsp}€")]
    [InlineData("2h * 60€/min", "7.200€")] // "€/min" is converted to "€/h"
    [InlineData("10€/2", "5€")] // a number after "/" is a division, not a unit
    [InlineData("5h * 2h", "10h²")]
    [InlineData("10h / 2h", "5")]
    [InlineData("10km / 2h", "5km/h")]
    [InlineData("€ 90 * 3", $"€{Nbsp}270")]
    [InlineData("€90 * 3", "€270")]
    [InlineData("$ 90 * 3", $"${Nbsp}270")]
    [InlineData("£ 10 + £ 5", $"£{Nbsp}15")]
    [InlineData("100 EUR + 50 EUR", $"150{Nbsp}EUR")]
    [InlineData("10 CHF * 3h", $"30{Nbsp}CHF")]
    [InlineData("€ 135,- * 150h", $"€{Nbsp}20.250,-")]
    [InlineData("€135,- * 2", "€270,-")]
    [InlineData("€135,- * 1,5", "€202,50")]
    [InlineData("0,01€ / 2", "0,01€")]
    [InlineData("1.080,50 € * 2", $"2.161,00{Nbsp}€")]
    [InlineData("1.000.000€ * 2", "2.000.000€")]
    public void Evaluate_Units(string formula, string expected)
    {
        Assert.Equal(expected, EvalFormatted(formula));
    }

    [Fact]
    public void Evaluate_KeepsDecimalsOfMostPreciseInput()
    {
        Assert.Equal("3,000", EvalFormatted("1,000 + 2"));
        Assert.Equal("3,50", EvalFormatted("1,25 + 2,25"));
        Assert.Equal(3, Eval("1,125h + 1,5h").Decimals);
    }

    [Theory]
    [InlineData("12h + 5€", "Cannot add \"h\" and \"€\".")]
    [InlineData("12h - 5€", "Cannot subtract \"h\" and \"€\".")]
    [InlineData("5 + 2h", "Cannot add a number without unit and \"h\".")]
    [InlineData("1h + 1Stk", "Cannot add \"h\" and \"Stk\".")]
    [InlineData("1h + 1H", "Cannot add \"h\" and \"H\".")] // units are case-sensitive
    [InlineData("10€ + 10 EUR", "Cannot add \"€\" and \"EUR\".")] // currencies are not converted
    public void Evaluate_IncompatibleUnits(string formula, string expected)
    {
        Assert.Equal(expected, EvalError(formula));
    }

    [Theory]
    [InlineData("10 / 2h", "5/h")]
    [InlineData("10 / 2 h", "5/h")]
    [InlineData("1 / 4h", "0,25/h")]
    [InlineData("10 / 2h / 1Stk", "5/(Stk·h)")]
    public void Evaluate_UnitWithoutNumerator(string formula, string expected)
    {
        Assert.Equal(expected, EvalFormatted(formula));
    }

    [Theory]
    [InlineData("135,- € * 2", $"270,-{Nbsp}€")]
    [InlineData("135,-€ * 2", "270,-€")]
    [InlineData("135,- EUR * 2", $"270,-{Nbsp}EUR")]
    [InlineData("135,- CHF * 1,5", $"202,50{Nbsp}CHF")]
    [InlineData("(135,- € + 15,- €) * 2", $"300,-{Nbsp}€")]
    [InlineData("135,- $", $"135,-{Nbsp}$")]
    public void Evaluate_DashForNoCents_WithCurrencyAfterIt(string formula, string expected)
    {
        Assert.Equal(expected, EvalFormatted(formula));
    }

    #endregion

    #region Evaluate: cultures

    [Theory]
    [InlineData("1,234.50€ * 2", "2,469.00€")]
    [InlineData("1.5h + 1h", "2.5h")]
    [InlineData("1,000,000 / 1000", "1,000")]
    [InlineData("€135.- * 2", "€270.-")]
    public void Evaluate_EnglishCulture(string formula, string expected)
    {
        Assert.Equal(expected, EvalFormatted(formula, En));
    }

    [Fact]
    public void Evaluate_AustrianCulture_AcceptsSpacesAsThousandsSeparator()
    {
        var at = CultureInfo.GetCultureInfo("de-AT");
        Assert.Equal(2469m, Eval("1 234,50 € * 2", at).Value);
        Assert.Equal(2469m, Eval($"1{Nbsp}234,50 € * 2", at).Value);
        Assert.Equal(2469m, Eval("1 234,50 € * 2", at).Value);
    }

    [Fact]
    public void Evaluate_SwissCulture_AcceptsApostrophesAsThousandsSeparator()
    {
        var ch = CultureInfo.GetCultureInfo("de-CH");
        Assert.Equal(2469m, Eval("1'234.50 CHF * 2", ch).Value);
        Assert.Equal(2469m, Eval("1’234.50 CHF * 2", ch).Value);
    }

    [Fact]
    public void Evaluate_GermanCulture_DoesNotAcceptSpacesAsThousandsSeparator()
    {
        Assert.StartsWith("Unexpected \"2\" at position 3", EvalError("1 234 * 2"));
    }

    [Fact]
    public void Evaluate_ThousandsSeparator_RequiresExactlyThreeDigits()
    {
        Assert.Equal("Unexpected \".\" at position 3 (the culture is \"de-DE\", so the decimal separator is \",\" and the thousands separator is \".\").", EvalError("12.5h * 2"));
        Assert.StartsWith("Unexpected \".\" at position 2", EvalError("1.0800"));
        Assert.StartsWith("Unexpected \".\" at position 2", EvalError("1.08"));
    }

    [Fact]
    public void Evaluate_WrongDecimalSeparator_HintsAtCulture()
    {
        Assert.Equal("Unexpected \",\" at position 2 (the culture is \"en-US\", so the decimal separator is \".\" and the thousands separator is \",\").", EvalError("1,5h", En));
    }

    [Theory]
    [InlineData("5,-3")]
    [InlineData("5,- 3")]
    [InlineData("5,-Rabatt")]
    [InlineData("5,-(2)")]
    [InlineData("5,- [B]")]
    public void Evaluate_DashAfterDecimalSeparatorFollowedByOperand_IsNotDashForNoCents(string formula)
    {
        // "5,-3" is not "5,-" followed by "3", the error points to the "," instead
        var error = EvalError(formula, references: (_, _) => throw new InvalidOperationException(), names: _ => throw new InvalidOperationException());
        Assert.StartsWith("Unexpected \",\" at position 2", error);
    }

    #endregion

    #region Evaluate: references and names

    [Fact]
    public void Evaluate_References_AreSplitIntoColumnAndRow()
    {
        var calls = new List<(string Column, string? Row)>();
        var result = Eval("[Kosten] + [ Kosten : 3 ] + [4:Summe netto]", references: (column, row) =>
        {
            calls.Add((column, row));
            return new Quantity(1, Unit.None, 0, default);
        });

        Assert.Equal(3m, result.Value);
        Assert.Equal(new List<(string, string?)> { ("Kosten", null), ("Kosten", "3"), ("4", "Summe netto") }, calls);
    }

    [Fact]
    public void Evaluate_Reference_OnlySplitsAtFirstColon()
    {
        (string, string?) call = default;
        Eval("[Zeit:10:30]", references: (column, row) =>
        {
            call = (column, row);
            return new Quantity(1, Unit.None, 0, default);
        });
        Assert.Equal(("Zeit", "10:30"), call);
    }

    [Theory]
    [InlineData("[]", "\"[]\" is not a valid reference.")]
    [InlineData("[ ]", "\"[ ]\" is not a valid reference.")]
    [InlineData("[Kosten:]", "\"[Kosten:]\" is not a valid reference.")]
    [InlineData("[:3]", "\"[:3]\" is not a valid reference.")]
    [InlineData("[Kosten", "A closing bracket \"]\" is missing.")]
    public void Evaluate_InvalidReferences(string formula, string expected)
    {
        Assert.Equal(expected, EvalError(formula, references: (_, _) => throw new InvalidOperationException("should not be called")));
    }

    [Fact]
    public void Evaluate_ReferenceWithoutResolver_Throws()
    {
        Assert.Equal("References like \"[Kosten]\" only work inside tables.", EvalError("[Kosten] * 2"));
    }

    [Fact]
    public void Evaluate_Names_AreResolved()
    {
        var names = new List<string>();
        var result = Eval("Stunden * Stundensatz_2026 + _x", names: name =>
        {
            names.Add(name);
            return new Quantity(2, Unit.None, 0, default);
        });

        Assert.Equal(6m, result.Value);
        Assert.Equal(new List<string> { "Stunden", "Stundensatz_2026", "_x" }, names);
    }

    [Fact]
    public void Evaluate_NameWithoutResolver_Throws()
    {
        Assert.Equal("Names like \"Stundensatz\" cannot be used here.", EvalError("2 * Stundensatz"));
    }

    [Fact]
    public void Evaluate_UnitAfterNonNumber_IsAName()
    {
        // units are only allowed directly after a number, so "€ / h" is a division by a name "h"
        Assert.Equal("Names like \"h\" cannot be used here.", EvalError("10€ / h"));
    }

    #endregion

    #region Evaluate: syntax errors

    [Theory]
    [InlineData("", "The formula is empty.")]
    [InlineData("   ", "The formula is empty.")]
    [InlineData("2 *", "The formula ends unexpectedly.")]
    [InlineData("-", "The formula ends unexpectedly.")]
    [InlineData("2 * (3 + 4", "A closing bracket \")\" is missing.")]
    [InlineData("2 * (3 + 4 5)", "Unexpected \"5\" at position 12.")]
    [InlineData("2 + 3)", "Unexpected \")\" at position 6.")]
    [InlineData("5 & 3", "Unexpected \"&\" at position 3.")]
    [InlineData("2 3", "Unexpected \"3\" at position 3.")]
    [InlineData("()", "Unexpected \")\" at position 2.")]
    [InlineData("€", "The formula ends unexpectedly.")]
    [InlineData("€ h", "Unexpected \"h\" at position 3.")]
    [InlineData("2 * {{ Faktor }}", "The formula contains a placeholder that could not be replaced.")]
    public void Evaluate_SyntaxErrors(string formula, string expected)
    {
        Assert.Equal(expected, EvalError(formula));
    }

    [Fact]
    public void Evaluate_Overflow()
    {
        Assert.Equal("The result is too large.", EvalError("79228162514264337593543950335 * 2"));
    }

    #endregion

    #region ParseValue

    [Theory]
    [InlineData("12", "12", "", 0)]
    [InlineData("12h", "12", "h", 0)]
    [InlineData("12,5 h", "12.5", "h", 1)]
    [InlineData("-1.080,50 €", "-1080.50", "€", 2)]
    [InlineData("−5h", "-5", "h", 0)]
    [InlineData("+5h", "5", "h", 0)]
    [InlineData("  € 90  ", "90", "€", 0)]
    [InlineData("- € 90", "-90", "€", 0)]
    [InlineData("90 €/h", "90", "€/h", 0)]
    [InlineData("90min", "1.5", "h", 0)]
    [InlineData("135,-", "135", "", 0)]
    [InlineData("20%", "0.2", "", 0)]
    [InlineData("135,- €", "135", "€", 0)]
    [InlineData("135,-EUR", "135", "EUR", 0)]
    public void ParseValue_ValidValues(string text, string value, string unit, int decimals)
    {
        var result = ExpressionParser.ParseValue(text, De);
        Assert.Equal(decimal.Parse(value, CultureInfo.InvariantCulture), result.Value);
        Assert.Equal(unit, result.Unit.ToString());
        Assert.Equal(decimals, result.Decimals);
    }

    [Fact]
    public void ParseValue_RemembersStyle()
    {
        Assert.Equal(new UnitStyle(false, false), ExpressionParser.ParseValue("12h", De).Style);
        Assert.Equal(new UnitStyle(true, false), ExpressionParser.ParseValue("12 h", De).Style);
        Assert.Equal(new UnitStyle(true, true), ExpressionParser.ParseValue("€ 12", De).Style);
        Assert.Equal(new UnitStyle(false, true, true), ExpressionParser.ParseValue("€12,-", De).Style);
        Assert.Equal(default, ExpressionParser.ParseValue("12", De).Style);
    }

    [Theory]
    [InlineData("abc", "\"abc\" is not a number.")]
    [InlineData("12h + 3h", "\"12h + 3h\" is not a number.")] // only a single value, no formula
    [InlineData("--12", "\"--12\" is not a number.")]
    [InlineData("(12)", "\"(12)\" is not a number.")]
    [InlineData("[Kosten]", "\"[Kosten]\" is not a number.")]
    [InlineData("12.5h", "\"12.5h\" is not a number (the culture is \"de-DE\", so the decimal separator is \",\" and the thousands separator is \".\").")]
    public void ParseValue_InvalidValues(string text, string expected)
    {
        Assert.Equal(expected, Assert.Throws<CalculationException>(() => ExpressionParser.ParseValue(text, De)).Message);
    }

    #endregion
}
