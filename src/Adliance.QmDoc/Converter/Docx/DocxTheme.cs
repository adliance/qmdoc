using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Adliance.QmDoc.Themes;

namespace Adliance.QmDoc.Converter.Docx;

/// <summary>
/// The colors, fonts and sizes of a theme, read from the CSS variables of the theme (eg. "--color-heading"), so that the DOCX looks like the PDF.
/// Sizes are converted from CSS pixels to points, including the scale of the PDF.
/// </summary>
internal sealed partial class DocxTheme
{
    private readonly Dictionary<string, string> _variables;

    private DocxTheme(Dictionary<string, string> variables, double scale)
    {
        _variables = variables;
        Scale = scale;
    }

    public static DocxTheme Load(string theme)
    {
        var scale = ThemeProvider.GetOptions(theme).Pdf.Scale ?? 0.85;
        return Parse(ThemeProvider.GetCss(theme) ?? "", scale);
    }

    /// <summary>
    /// Reads all CSS variables, later definitions override earlier ones (the theme CSS comes after the shared CSS).
    /// </summary>
    public static DocxTheme Parse(string css, double scale = 0.85)
    {
        css = CommentRegex().Replace(css, "");
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in VariableRegex().Matches(css))
        {
            variables[m.Groups["name"].Value] = m.Groups["value"].Value.Trim();
        }

        return new DocxTheme(variables, scale);
    }

    public double Scale { get; }

    public string TextColor => Color("color-text", "021E45");
    public string HeadingColor => Color("color-heading", TextColor);
    public string LinkColor => Color("color-link", HeadingColor);
    public string ListColor => Color("color-li", TextColor);
    public string PreColor => Color("color-pre", TextColor);
    public string PreBackgroundColor => Color("color-pre-background", "FAFAFA");
    public string TableBackgroundColor => Color("color-table-background", "FFFFFF");
    public string TableAlternateBackgroundColor => Color("color-table-background-alternate", PreBackgroundColor);
    public string TableBorderColor => Color("color-table-border", TextColor);
    public string InfoColor => Color("color-info", "10A8E5");
    public string WarningColor => Color("color-warning", "F47D09");
    public string DangerColor => Color("color-danger", "ED4622");
    public string FootnoteColor => Color("color-footnote", TableBorderColor);
    public string DocumentTitleColor => Color("color-documentTitle", TableBorderColor);
    public string DocumentTitleAuthorColor => Color("color-documentTitle-author", DocumentTitleColor);
    public string FigureBackgroundColor => Color("color-figure-background", PreBackgroundColor);
    public string FigureBorderColor => Color("color-figure-border", "F0F0F0");
    public string BlockquoteColor => Color("color-blockquote", "777777");

    public string FontFamily => Font("font-family", "Calibri");
    public string HeadingFontFamily => Font("font-family-heading", FontFamily);
    public string CodeFontFamily => Font("font-family-code", "Courier New");

    public bool UppercaseHeadings => string.Equals(Resolve("transform-heading"), "uppercase", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The base font size in CSS pixels (the other sizes in the CSS are relative to it).
    /// </summary>
    public double FontSizePx => Pixels("fontSize", 16);

    public double FontSizeTableHeaderPx => Pixels("fontSize-th", 13);
    public double FontSizeTableCellPx => Pixels("fontSize-td", 14);
    public double FontSizeFigureCaptionPx => Pixels("fontSize-figcaption", 14);
    public double FontSizeBlockquotePx => Pixels("fontSize-blockquote", 14);

    public double MarginBasePx => Pixels("margin-base", 10);

    public double LineHeight => double.TryParse(Resolve("line-height"), NumberStyles.Float, CultureInfo.InvariantCulture, out var lineHeight) ? lineHeight : 1.3;

    /// <summary>
    /// CSS pixels (as rendered in the PDF) to points.
    /// </summary>
    public double ToPoints(double px) => px * Scale * 0.75;

    /// <summary>
    /// CSS pixels to twentieths of a point, the unit of most sizes in DOCX.
    /// </summary>
    public int ToTwips(double px) => (int)Math.Round(ToPoints(px) * 20);

    /// <summary>
    /// CSS pixels to half-points, the unit of font sizes in DOCX.
    /// </summary>
    public string ToHalfPoints(double px) => ((int)Math.Round(ToPoints(px) * 2)).ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// CSS pixels to EMUs, the unit of image sizes in DOCX.
    /// </summary>
    public long ToEmus(double px) => (long)Math.Round(ToPoints(px) * 12700);

    private string? Resolve(string name, int depth = 0)
    {
        if (depth > 10 || !_variables.TryGetValue(name, out var value)) return null;

        var reference = VarRegex().Match(value);
        return reference.Success ? Resolve(reference.Groups["name"].Value, depth + 1) ?? (string.IsNullOrWhiteSpace(reference.Groups["fallback"].Value) ? null : reference.Groups["fallback"].Value.Trim()) : value;
    }

    private string Color(string name, string fallback) => ParseColor(Resolve(name)) ?? fallback;

    private string Font(string name, string fallback)
    {
        var value = Resolve(name);
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        return value.Split(',')[0].Trim().Trim('\'', '"');
    }

    private double Pixels(string name, double fallback)
    {
        var value = Resolve(name);
        if (value == null) return fallback;
        var match = PixelRegex().Match(value);
        return match.Success ? double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : fallback;
    }

    /// <summary>
    /// Parses "#abc", "#aabbcc", "rgb(1, 2, 3)", "rgba(1, 2, 3, 0.5)" and a few named colors into "RRGGBB".
    /// Transparent colors are blended with white, like they appear on the white page.
    /// </summary>
    public static string? ParseColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim().ToLowerInvariant();

        switch (value)
        {
            case "white": return "FFFFFF";
            case "black": return "000000";
        }

        if (value.StartsWith('#'))
        {
            var hex = value[1..];
            if (hex.Length == 3) hex = string.Concat(hex.Select(c => $"{c}{c}"));
            return hex.Length == 6 && hex.All(Uri.IsHexDigit) ? hex.ToUpperInvariant() : null;
        }

        var rgb = RgbRegex().Match(value);
        if (!rgb.Success) return null;

        var parts = rgb.Groups[1].Value.Split([',', ' ', '/'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => double.Parse(x, CultureInfo.InvariantCulture)).ToArray();
        if (parts.Length < 3) return null;

        var alpha = parts.Length > 3 ? Math.Clamp(parts[3], 0, 1) : 1;
        return ToHex(parts[0] * alpha + 255 * (1 - alpha), parts[1] * alpha + 255 * (1 - alpha), parts[2] * alpha + 255 * (1 - alpha));
    }

    /// <summary>
    /// Mixes the color with white, like "color-mix(in oklch, color X%, white)" in the CSS of the callouts.
    /// Mixing with white keeps the hue in OKLCH, which is the same as mixing in OKLab.
    /// </summary>
    public static string MixWithWhite(string hex, double percentage)
    {
        var (l, a, b) = ToOkLab(hex);
        var t = percentage / 100;
        return FromOkLab(l * t + 1 * (1 - t), a * t, b * t);
    }

    private static (double L, double A, double B) ToOkLab(string hex)
    {
        var r = ToLinear(System.Convert.ToInt32(hex[..2], 16) / 255.0);
        var g = ToLinear(System.Convert.ToInt32(hex[2..4], 16) / 255.0);
        var b = ToLinear(System.Convert.ToInt32(hex[4..6], 16) / 255.0);

        var l = Math.Cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b);
        var m = Math.Cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b);
        var s = Math.Cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);

        return (0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s,
            1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s,
            0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s);
    }

    private static string FromOkLab(double lightness, double a, double b)
    {
        var l = Math.Pow(lightness + 0.3963377774 * a + 0.2158037573 * b, 3);
        var m = Math.Pow(lightness - 0.1055613458 * a - 0.0638541728 * b, 3);
        var s = Math.Pow(lightness - 0.0894841775 * a - 1.2914855480 * b, 3);

        return ToHex(
            FromLinear(4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s) * 255,
            FromLinear(-1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s) * 255,
            FromLinear(-0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s) * 255);
    }

    private static double ToLinear(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    private static double FromLinear(double c) => c <= 0.0031308 ? 12.92 * c : 1.055 * Math.Pow(c, 1 / 2.4) - 0.055;

    private static string ToHex(double r, double g, double b) =>
        $"{Channel(r):X2}{Channel(g):X2}{Channel(b):X2}";

    private static int Channel(double value) => (int)Math.Round(Math.Clamp(value, 0, 255));

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex CommentRegex();

    [GeneratedRegex(@"--(?<name>[\w-]+)\s*:\s*(?<value>[^;{}]+);")]
    private static partial Regex VariableRegex();

    [GeneratedRegex(@"^var\(\s*--(?<name>[\w-]+)\s*(?:,(?<fallback>.*))?\)$")]
    private static partial Regex VarRegex();

    [GeneratedRegex(@"^([\d.]+)px$")]
    private static partial Regex PixelRegex();

    [GeneratedRegex(@"^rgba?\((.*)\)$")]
    private static partial Regex RgbRegex();
}

internal static class DocxNumberExtensions
{
    /// <summary>
    /// Numbers in DOCX attributes are always formatted without any culture specific formatting.
    /// </summary>
    public static string ToXml(this long value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public static string ToXml(this int value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public static string ToXml(this uint value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
