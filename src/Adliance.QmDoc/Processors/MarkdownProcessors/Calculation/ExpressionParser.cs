using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.Processors.MarkdownProcessors.Calculation;

/// <summary>
/// Resolves a reference like [Kosten], [Kosten:3] or [3:Summe netto] to its value.
/// </summary>
/// <param name="column">The part before the colon (or the whole reference if there is no colon).</param>
/// <param name="row">The part after the colon, or null if there is none.</param>
public delegate Quantity ReferenceResolver(string column, string? row);

/// <summary>
/// Resolves a name written without brackets (eg. a frontmatter value like "Stundensatz") to its value.
/// </summary>
public delegate Quantity NameResolver(string name);

/// <summary>
/// Parses and evaluates formulas like "145h * 2€", "[Stunden] * Stundensatz" or "([Kosten:1] + [Kosten:2]) * 20%".
/// Supports + - * / ( ), percentages, units (including compound units like "€/h"), references to table cells
/// in square brackets and names (like frontmatter values) without brackets.
/// Numbers are parsed with the decimal and group separators of the given culture.
/// </summary>
public sealed partial class ExpressionParser
{
    [GeneratedRegex(@"\d[.,']\d")]
    private static partial Regex NumberWithSeparatorRegex();

    private const string MinutesUnit = "min";
    private const string HoursUnit = "h";

    private readonly string _text;
    private readonly CultureInfo _culture;
    private readonly ReferenceResolver? _resolveReference;
    private readonly NameResolver? _resolveName;
    private int _position;

    private ExpressionParser(string text, CultureInfo culture, ReferenceResolver? resolveReference, NameResolver? resolveName)
    {
        _text = text;
        _culture = culture;
        _resolveReference = resolveReference;
        _resolveName = resolveName;
    }

    public static Quantity Evaluate(string formula, CultureInfo culture, ReferenceResolver? resolveReference, NameResolver? resolveName)
    {
        if (formula.Contains("{{")) throw new CalculationException("The formula contains a placeholder that could not be replaced.");

        var parser = new ExpressionParser(formula, culture, resolveReference, resolveName);
        parser.SkipWhitespace();
        if (parser.AtEnd) throw new CalculationException("The formula is empty.");

        var result = parser.ParseExpression();
        parser.SkipWhitespace();
        if (!parser.AtEnd) throw parser.Unexpected();
        return result;
    }

    /// <summary>
    /// Parses a single value like "12h", "-1.080,50 €" or "€ 90" (as found in table cells).
    /// </summary>
    public static Quantity ParseValue(string text, CultureInfo culture)
    {
        var parser = new ExpressionParser(text, culture, null, null);
        try
        {
            parser.SkipWhitespace();
            var negative = parser.TryConsume('-') || parser.TryConsume('−');
            if (!negative) parser.TryConsume('+');
            parser.SkipWhitespace();

            var result = parser.ParseNumberWithUnit();
            parser.SkipWhitespace();
            if (!parser.AtEnd) throw parser.Unexpected();
            return negative ? result.Negate() : result;
        }
        catch (CalculationException)
        {
            throw new CalculationException($"\"{text}\" is not a number{parser.SeparatorHint()}.");
        }
    }

    private bool AtEnd => _position >= _text.Length;
    private char Current => _text[_position];

    private Quantity ParseExpression()
    {
        var result = ParseTerm();
        while (true)
        {
            SkipWhitespace();
            if (TryConsume('+')) result = result.Add(ParseTerm());
            else if (TryConsume('-') || TryConsume('−')) result = result.Subtract(ParseTerm());
            else return result;
        }
    }

    private Quantity ParseTerm()
    {
        var result = ParseUnary();
        while (true)
        {
            SkipWhitespace();
            if (TryConsume('*') || TryConsume('×') || TryConsume('·')) result = result.Multiply(ParseUnary());
            else if (TryConsume('/') || TryConsume('÷')) result = result.Divide(ParseUnary());
            else return result;
        }
    }

    private Quantity ParseUnary()
    {
        SkipWhitespace();
        if (TryConsume('-') || TryConsume('−')) return ParseUnary().Negate();
        if (TryConsume('+')) return ParseUnary();
        return ParsePrimary();
    }

    private Quantity ParsePrimary()
    {
        SkipWhitespace();
        if (AtEnd) throw Unexpected();

        if (TryConsume('('))
        {
            var result = ParseExpression();
            SkipWhitespace();
            if (!TryConsume(')')) throw AtEnd ? new CalculationException("A closing bracket \")\" is missing.") : Unexpected();
            return result;
        }

        if (Current == '[') return ParseReference();
        if (char.IsDigit(Current) || Unit.CurrencySymbolChars.Contains(Current)) return ParseNumberWithUnit();
        if (char.IsLetter(Current) || Current == '_')
        {
            // units are only allowed directly after a number, so a name here is a reference to eg. a frontmatter value
            var start = _position;
            while (!AtEnd && (char.IsLetterOrDigit(Current) || Current == '_')) _position++;
            var name = _text[start.._position];
            if (_resolveName == null) throw new CalculationException($"Names like \"{name}\" cannot be used here.");
            return _resolveName(name);
        }

        throw Unexpected();
    }

    private Quantity ParseReference()
    {
        _position++; // [
        var end = _text.IndexOf(']', _position);
        if (end < 0) throw new CalculationException("A closing bracket \"]\" is missing.");

        var reference = _text[_position..end];
        _position = end + 1;

        var colon = reference.IndexOf(':', StringComparison.Ordinal);
        var column = (colon < 0 ? reference : reference[..colon]).Trim();
        var row = colon < 0 ? null : reference[(colon + 1)..].Trim();
        if (column.Length == 0 || row is { Length: 0 }) throw new CalculationException($"\"[{reference}]\" is not a valid reference.");
        if (_resolveReference == null) throw new CalculationException($"References like \"[{reference}]\" only work inside tables.");
        return _resolveReference(column, row);
    }

    /// <summary>
    /// Parses "12", "12h", "12,5 h", "90€/h", "20%" or "€ 90".
    /// </summary>
    private Quantity ParseNumberWithUnit()
    {
        if (Unit.CurrencySymbolChars.Contains(Current))
        {
            var symbol = Current.ToString();
            _position++;
            var whitespaceStart = _position;
            SkipWhitespace();
            var space = _position > whitespaceStart;
            if (AtEnd || !char.IsDigit(Current)) throw Unexpected();
            var (prefixedValue, prefixedDecimals, prefixedDash) = ParseNumber();
            return new Quantity(prefixedValue, Unit.Of(symbol), prefixedDecimals, new UnitStyle(space, true, prefixedDash));
        }

        var (value, decimals, dashForNoCents) = ParseNumber();
        var beforeUnit = _position;
        SkipWhitespace();
        var spaceBetween = _position > beforeUnit;

        if (TryConsume('%')) return new Quantity(value / 100, Unit.None, decimals, default);

        var numerator = TryParseUnitSymbol();
        if (numerator == null)
        {
            _position = beforeUnit;
            return new Quantity(value, Unit.None, decimals, default);
        }

        // "€/h" (without whitespace) is a compound unit, while "€ / h" or "€/2" are divisions
        string? denominator = null;
        if (!AtEnd && Current == '/' && _position + 1 < _text.Length && IsUnitStart(_text[_position + 1]))
        {
            _position++;
            denominator = TryParseUnitSymbol();
        }

        var unit = Unit.Of(NormalizeUnitSymbol(numerator, ref value));
        if (denominator != null)
        {
            var factor = 1m;
            unit = unit.Divide(Unit.Of(NormalizeUnitSymbol(denominator, ref factor)));
            value /= factor;
        }

        return new Quantity(value, unit, decimals, new UnitStyle(spaceBetween, false, dashForNoCents));
    }

    /// <summary>
    /// Minutes are always converted to hours, so that they can be combined with each other.
    /// </summary>
    private static string NormalizeUnitSymbol(string symbol, ref decimal value)
    {
        if (!symbol.Equals(MinutesUnit, StringComparison.Ordinal)) return symbol;
        value /= 60;
        return HoursUnit;
    }

    private static bool IsUnitStart(char c) => char.IsLetter(c) || Unit.CurrencySymbolChars.Contains(c);

    private string? TryParseUnitSymbol()
    {
        if (AtEnd || !IsUnitStart(Current)) return null;
        if (Unit.CurrencySymbolChars.Contains(Current)) return _text[_position++].ToString();

        var start = _position;
        while (!AtEnd && char.IsLetter(Current)) _position++;
        return _text[start.._position];
    }

    private (decimal Value, int Decimals, bool DashForNoCents) ParseNumber()
    {
        var number = new StringBuilder();
        if (ReadDigits(number) == 0) throw Unexpected();
        while (TryConsumeGroupSeparator()) ReadDigits(number);

        var decimals = 0;
        var dashForNoCents = false;
        var decimalSeparator = _culture.NumberFormat.NumberDecimalSeparator;
        if (_text.AsSpan(_position).StartsWith(decimalSeparator, StringComparison.Ordinal) && _position + decimalSeparator.Length < _text.Length)
        {
            var afterSeparator = _text[_position + decimalSeparator.Length];
            if (char.IsDigit(afterSeparator))
            {
                _position += decimalSeparator.Length;
                number.Append('.');
                decimals = ReadDigits(number);
            }
            else if (afterSeparator is '-' or '–' or '—' && !IsFollowedByOperand(_position + decimalSeparator.Length + 1))
            {
                // "135,-" is a common way to write whole amounts of money
                _position += decimalSeparator.Length + 1;
                dashForNoCents = true;
            }
        }

        return (decimal.Parse(number.ToString(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture), decimals, dashForNoCents);
    }

    /// <summary>
    /// True if a number, bracket or reference follows (ignoring whitespace), so that "5,-3" stays a subtraction in cultures where "," is not the decimal separator.
    /// </summary>
    private bool IsFollowedByOperand(int position)
    {
        while (position < _text.Length && char.IsWhiteSpace(_text[position])) position++;
        return position < _text.Length && (char.IsLetterOrDigit(_text[position]) || _text[position] is '(' or '[' || Unit.CurrencySymbolChars.Contains(_text[position]));
    }

    private int ReadDigits(StringBuilder number)
    {
        var count = 0;
        while (!AtEnd && char.IsDigit(Current))
        {
            number.Append(Current);
            _position++;
            count++;
        }

        return count;
    }

    /// <summary>
    /// A group separator ("1.080" in German) only counts if it is followed by exactly three digits.
    /// </summary>
    private bool TryConsumeGroupSeparator()
    {
        if (AtEnd || !IsGroupSeparator(Current)) return false;

        var digitsStart = _position + 1;
        var digitsEnd = digitsStart + 3;
        if (digitsEnd > _text.Length) return false;
        for (var i = digitsStart; i < digitsEnd; i++)
        {
            if (!char.IsDigit(_text[i])) return false;
        }

        if (digitsEnd < _text.Length && char.IsDigit(_text[digitsEnd])) return false;

        _position++;
        return true;
    }

    private bool IsGroupSeparator(char c)
    {
        var separator = _culture.NumberFormat.NumberGroupSeparator;
        if (separator.Length != 1) return false;

        var s = separator[0];
        if (char.IsWhiteSpace(s)) return c is ' ' or '\u00A0' or '\u202F'; // eg. de-AT and fr-FR use (non-breaking) spaces
        if (s is '\'' or '’') return c is '\'' or '’'; // de-CH
        return c == s;
    }

    private bool TryConsume(char c)
    {
        if (AtEnd || Current != c) return false;
        _position++;
        return true;
    }

    private void SkipWhitespace()
    {
        while (!AtEnd && char.IsWhiteSpace(Current)) _position++;
    }

    private CalculationException Unexpected()
    {
        if (AtEnd) return new CalculationException("The formula ends unexpectedly.");
        return new CalculationException($"Unexpected \"{Current}\" at position {_position + 1}{SeparatorHint()}.");
    }

    /// <summary>
    /// Mixing up "." and "," is the most likely mistake, so mention the separators of the culture in use.
    /// </summary>
    private string SeparatorHint()
    {
        var format = _culture.NumberFormat;
        if (!NumberWithSeparatorRegex().IsMatch(_text)) return "";
        return $" (the culture is \"{_culture.Name}\", so the decimal separator is \"{format.NumberDecimalSeparator}\" and the thousands separator is \"{format.NumberGroupSeparator}\")";
    }
}
