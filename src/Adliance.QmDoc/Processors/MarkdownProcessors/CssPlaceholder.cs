using System.Text.RegularExpressions;
using Adliance.QmDoc.Themes;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class CssPlaceholder(string? theme) : IMarkdownProcessor
{
    private string GetTranspiledCss()
    {
        var css = ThemeProvider.GetCss(theme ?? "");
        if (css == null)
        {
            var scss = ThemeProvider.GetScss(theme ?? "") ?? "";
            css = SharpScss.Scss.ConvertToCss(scss).Css;
        }

        return css;
    }

    public MarkdownProcessorResult Apply(string markdown, MarkdownProcessorContext markdownProcessorContext)
    {
        if (string.IsNullOrWhiteSpace(theme)) return new MarkdownProcessorResult(markdown, markdownProcessorContext);
        var result = Regex.Replace(markdown, @"\{\{\W*CSS\W*\}\}", "<style>" + GetTranspiledCss() + "</style>", RegexOptions.IgnoreCase);
        return new MarkdownProcessorResult(result, markdownProcessorContext);
    }
}
