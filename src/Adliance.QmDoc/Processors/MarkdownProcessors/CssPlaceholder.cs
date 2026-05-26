using System.Text.RegularExpressions;
using Adliance.QmDoc.Themes;
using DocumentFormat.OpenXml.Office.Word;

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

    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        if (string.IsNullOrWhiteSpace(theme)) return markdownContext;
        markdownContext.ReplacePlaceholder("CSS", "<style>" + GetTranspiledCss() + "</style>");
        return markdownContext;
    }
}
