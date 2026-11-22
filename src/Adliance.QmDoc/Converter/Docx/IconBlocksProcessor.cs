using System;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Adliance.QmDoc.Converter.Docx;

internal enum CalloutKind
{
    Info,
    Warning,
    Danger
}

/// <summary>
/// Formats a paragraph like the callouts of the theme: a box with a colored border, filled with a lighter version of the color.
/// </summary>
internal static class Callout
{
    public static string GetColor(CalloutKind kind, DocxTheme theme) => kind switch
    {
        CalloutKind.Warning => theme.WarningColor,
        CalloutKind.Danger => theme.DangerColor,
        _ => theme.InfoColor
    };

    public static void Apply(Paragraph paragraph, CalloutKind kind, DocxTheme theme)
    {
        var color = GetColor(kind, theme);

        // CSS: 1px border, 2 * margin-base padding, background color-mix(in oklch, color 40%, white)
        var padding = (uint)Math.Min(31, Math.Round(theme.ToPoints(theme.MarginBasePx * 2)));
        var indentation = (padding * 20 + 10).ToXml(); // padding and border, so that the box is aligned with the text

        var pPr = paragraph.ParagraphProperties ??= new ParagraphProperties();
        pPr.KeepLines = new KeepLines();
        pPr.ParagraphBorders = new ParagraphBorders(
            new TopBorder { Val = BorderValues.Single, Color = color, Size = 4, Space = padding },
            new LeftBorder { Val = BorderValues.Single, Color = color, Size = 4, Space = padding },
            new BottomBorder { Val = BorderValues.Single, Color = color, Size = 4, Space = padding },
            new RightBorder { Val = BorderValues.Single, Color = color, Size = 4, Space = padding });
        pPr.Shading = new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = DocxTheme.MixWithWhite(color, 40) };
        pPr.SpacingBetweenLines = new SpacingBetweenLines
        {
            Before = (padding * 20).ToXml(),
            After = (theme.ToTwips(theme.MarginBasePx * 2) + padding * 20).ToXml()
        };
        pPr.Indentation = new Indentation { Left = indentation, Right = indentation };
    }

    /// <summary>
    /// The kinds of GitHub alerts ("> [!NOTE]"), mapped like in the CSS of the theme.
    /// </summary>
    public static CalloutKind FromAlertKind(string kind) => kind.ToUpperInvariant() switch
    {
        "WARNING" or "CAUTION" => CalloutKind.Warning,
        "IMPORTANT" => CalloutKind.Danger,
        _ => CalloutKind.Info
    };
}

/// <summary>
/// Renders paragraphs starting with "{{!}}", "{{!!}}" or "{{?}}" as callouts, like in the HTML/PDF conversion.
/// </summary>
internal static partial class IconBlocksProcessor
{
    internal static void Apply(WordprocessingDocument document, DocxTheme theme)
    {
        var body = document.MainDocumentPart!.Document.Body!;

        foreach (var para in body.Descendants<Paragraph>().ToList())
        {
            var fullText = string.Concat(para.Descendants<Text>().Select(t => t.Text));

            var match = PrefixRegex().Match(fullText);
            if (!match.Success) continue;

            StripLeadingText(para, match.Length);
            var kind = match.Groups["type"].Value switch
            {
                "!!" => CalloutKind.Danger,
                "?" => CalloutKind.Info,
                _ => CalloutKind.Warning
            };

            if (para.ParagraphProperties?.NumberingProperties != null) AddListIcon(para, kind, theme);
            else Callout.Apply(para, kind, theme);
        }
    }

    /// <summary>
    /// In lists, the HTML/PDF only shows a small colored icon at the beginning of the list item instead of a box.
    /// </summary>
    private static void AddListIcon(Paragraph para, CalloutKind kind, DocxTheme theme)
    {
        var icon = new Run(
            new RunProperties(new Color { Val = Callout.GetColor(kind, theme) }),
            new Text("● ") { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve });

        var firstRun = para.Elements<Run>().FirstOrDefault();
        if (firstRun != null) para.InsertBefore(icon, firstRun);
        else para.AppendChild(icon);
    }

    private static void StripLeadingText(Paragraph para, int charCount)
    {
        var remaining = charCount;
        foreach (var text in para.Descendants<Text>())
        {
            if (remaining <= 0) break;
            if (text.Text.Length <= remaining)
            {
                remaining -= text.Text.Length;
                text.Text = string.Empty;
            }
            else
            {
                text.Text = text.Text[remaining..];
                remaining = 0;
            }
        }
    }

    /// <summary>
    /// "{{!}} Text", like in the HTML/PDF conversion (also "{{ ! }}" and "{!}"), followed by a space or the end of the paragraph.
    /// </summary>
    [GeneratedRegex(@"^\{\{?\s*(?<type>!!|!|\?)\s*\}\}?(?:\s+|$)")]
    private static partial Regex PrefixRegex();
}
