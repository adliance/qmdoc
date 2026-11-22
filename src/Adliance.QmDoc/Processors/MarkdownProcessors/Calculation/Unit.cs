using System;
using System.Collections.Generic;
using System.Linq;

namespace Adliance.QmDoc.Processors.MarkdownProcessors.Calculation;

/// <summary>
/// A (compound) unit like "h", "€" or "€/h", stored as unit symbols with their exponents.
/// </summary>
public sealed class Unit : IEquatable<Unit>
{
    private static readonly HashSet<string> CurrencySymbols = new(StringComparer.Ordinal) { "€", "$", "£", "EUR", "USD", "CHF", "GBP" };

    /// <summary>
    /// Currency symbols that are written without letters and may therefore also be placed in front of the number ("€ 90").
    /// </summary>
    public static readonly IReadOnlySet<char> CurrencySymbolChars = new HashSet<char> { '€', '$', '£' };

    private readonly SortedDictionary<string, int> _exponents;

    private Unit(SortedDictionary<string, int> exponents)
    {
        _exponents = exponents;
    }

    public static Unit None { get; } = new(new SortedDictionary<string, int>(StringComparer.Ordinal));

    public static Unit Of(string symbol) => new(new SortedDictionary<string, int>(StringComparer.Ordinal) { [symbol] = 1 });

    public bool IsNone => _exponents.Count == 0;

    public bool ContainsCurrency => _exponents.Keys.Any(CurrencySymbols.Contains);

    /// <summary>
    /// True for a plain currency like "€" (but not for "€/h").
    /// </summary>
    public bool IsSingleCurrency => _exponents.Count == 1 && _exponents.All(x => CurrencySymbols.Contains(x.Key) && x.Value == 1);

    public Unit Multiply(Unit other) => Combine(other, 1);

    public Unit Divide(Unit other) => Combine(other, -1);

    private Unit Combine(Unit other, int sign)
    {
        var result = new SortedDictionary<string, int>(_exponents, StringComparer.Ordinal);
        foreach (var (symbol, exponent) in other._exponents)
        {
            var newExponent = result.GetValueOrDefault(symbol) + sign * exponent;
            if (newExponent == 0) result.Remove(symbol);
            else result[symbol] = newExponent;
        }

        return new Unit(result);
    }

    /// <summary>
    /// Multiplying a quantity with an amount of money is read as "quantity at that price", e.g. "145h * 2€" = "290€".
    /// So if the result contains exactly one currency, all other units in the numerator are dropped ("h·€" becomes "€").
    /// </summary>
    public Unit PreferCurrency()
    {
        var currencies = _exponents.Where(x => CurrencySymbols.Contains(x.Key)).ToList();
        if (currencies.Count != 1 || currencies[0].Value != 1) return this;

        var result = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var (symbol, exponent) in _exponents)
        {
            if (CurrencySymbols.Contains(symbol) || exponent < 0) result[symbol] = exponent;
        }

        return new Unit(result);
    }

    public override string ToString()
    {
        // currencies first, so that it's "€·h" and not "h·€"
        var ordered = _exponents.OrderBy(x => CurrencySymbols.Contains(x.Key) ? 0 : 1).ThenBy(x => x.Key, StringComparer.Ordinal).ToList();
        var numerator = ordered.Where(x => x.Value > 0).Select(x => FormatSymbol(x.Key, x.Value)).ToList();
        var denominator = ordered.Where(x => x.Value < 0).Select(x => FormatSymbol(x.Key, -x.Value)).ToList();

        var result = numerator.Count > 0 ? string.Join("·", numerator) : (denominator.Count > 0 ? "1" : "");
        if (denominator.Count == 1) result += "/" + denominator[0];
        else if (denominator.Count > 1) result += "/(" + string.Join("·", denominator) + ")";
        return result;
    }

    private static string FormatSymbol(string symbol, int exponent)
    {
        if (exponent == 1) return symbol;
        const string superscripts = "⁰¹²³⁴⁵⁶⁷⁸⁹";
        return symbol + string.Concat(exponent.ToString(System.Globalization.CultureInfo.InvariantCulture).Select(c => superscripts[c - '0']));
    }

    public bool Equals(Unit? other)
    {
        if (other is null) return false;
        return _exponents.Count == other._exponents.Count && _exponents.All(x => other._exponents.TryGetValue(x.Key, out var e) && e == x.Value);
    }

    public override bool Equals(object? obj) => obj is Unit other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var (symbol, exponent) in _exponents)
        {
            hash.Add(symbol, StringComparer.Ordinal);
            hash.Add(exponent);
        }

        return hash.ToHashCode();
    }
}
