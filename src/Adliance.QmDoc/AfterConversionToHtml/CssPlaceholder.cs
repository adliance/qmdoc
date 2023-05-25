using System.Text.RegularExpressions;
using Adliance.QmDoc.Themes;

namespace Adliance.QmDoc.AfterConversionToHtml;

public class CssPlaceholder : IAfterConversionToHtmlStep
{
    private readonly string _theme;

    public CssPlaceholder(string theme)
    {
        _theme = theme;
    }

    public Result Apply(string html)
    {
        var result = Regex.Replace(html, @"\{\{\W*CSS\W*\}\}", "<style>" + GetTranspiledCss() + "</style>", RegexOptions.IgnoreCase);
        return new Result(result);
    }

    private string GetTranspiledCss()
    {
        var scss = ThemeProvider.GetScss(_theme);
        var css = SharpScss.Scss.ConvertToCss(scss);
        return css.Css;
    }
}