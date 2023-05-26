using System.Text.RegularExpressions;
using Adliance.QmDoc.Themes;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class CssPlaceholder : IMarkdownProcessor
{
    private readonly string? _theme;

    public CssPlaceholder(string? theme)
    {
        _theme = theme;
    }


    private string GetTranspiledCss()
    {
        var scss = ThemeProvider.GetScss(_theme ?? "");
        var css = SharpScss.Scss.ConvertToCss(scss);
        return css.Css;
    }

    public MarkdownProcessorResult Apply(string markdown, MarkdownProcessorContext markdownProcessorContext)
    {
        if (string.IsNullOrWhiteSpace(_theme)) return new MarkdownProcessorResult(markdown, markdownProcessorContext);
        var result = Regex.Replace(markdown, @"\{\{\W*CSS\W*\}\}", "<style>" + GetTranspiledCss() + "</style>", RegexOptions.IgnoreCase);
        return new MarkdownProcessorResult(result, markdownProcessorContext);
    }
}