using System;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Renderers.Docx;

namespace Adliance.QmDoc.Converter.Docx;

/// <summary>
/// Replaces the styles of the DOCX template with the built-in styles of Word (Normal, Heading 1, Quote, ...), formatted with the colors,
/// fonts and sizes of the theme, so that the DOCX looks like the PDF. Also sets the page size, language and hyphenation.
/// </summary>
internal static class ThemeStylesProcessor
{
    public const string NormalStyle = "Normal";
    public const string TitleStyle = "Title";
    public const string SubtitleStyle = "Subtitle";
    public const string HeadingStylePrefix = "Heading";
    public const string ListParagraphStyle = "ListParagraph";
    public const string QuoteStyle = "Quote";
    public const string CodeBlockStyle = "HTMLPreformatted";
    public const string CodeInlineStyle = "HTMLCode";
    public const string HyperlinkStyle = "Hyperlink";
    public const string CaptionStyle = "Caption";
    public const string EndnoteTextStyle = "EndnoteText";
    public const string EndnoteReferenceStyle = "EndnoteReference";

    private const string DefaultParagraphFontStyle = "DefaultParagraphFont";

    // the numbering styles of the template, which the list renderer uses to find the numbering definitions of bullet and ordered lists
    private const string TemplateBulletListStyle = "MDListBullet";
    private const string TemplateOrderedListStyle = "MDListNumber";

    // A4 with margins similar to the PDF
    private const int PageWidth = 11906;
    private const int PageHeight = 16838;
    private const int PageMarginLeftRight = 1134; // 2 cm
    private const int PageMarginTopBottom = 1417; // 2.5 cm
    public const int TextWidth = PageWidth - 2 * PageMarginLeftRight;
    public const int TextHeight = PageHeight - 2 * PageMarginTopBottom;

    /// <summary>
    /// The styles the Markdown renderer uses for the different Markdown elements.
    /// </summary>
    public static DocumentStyles CreateDocumentStyles()
    {
        var styles = new DocumentStyles
        {
            UndefinedHeading = HeadingStylePrefix + 6,
            Paragraph = NormalStyle,
            CodeBlock = CodeBlockStyle,
            UnknownFormatting = NormalStyle,
            Hyperlink = HyperlinkStyle,
            ListOrdered = TemplateOrderedListStyle,
            ListOrderedItem = ListParagraphStyle,
            ListBullet = TemplateBulletListStyle,
            ListBulletItem = ListParagraphStyle,
            Quote = QuoteStyle,
            HorizontalLine = null,
            CodeInline = CodeInlineStyle
        };

        styles.Headings[0] = TitleStyle;
        for (var level = 1; level <= 6; level++) styles.Headings[level] = HeadingStylePrefix + level;
        return styles;
    }

    /// <summary>
    /// Is applied before the Markdown is rendered.
    /// </summary>
    public static void Apply(WordprocessingDocument document, DocxTheme theme, string language)
    {
        var mainPart = document.MainDocumentPart!;
        mainPart.StyleDefinitionsPart!.Styles = CreateStyles(theme, language);
        AddFonts(mainPart.FontTablePart?.Fonts, theme);
        ApplySettings(mainPart.DocumentSettingsPart?.Settings);
    }

    /// <summary>
    /// Is applied after the Markdown is rendered, because the lists are only created while rendering, and the section properties
    /// must be the last element of the body.
    /// </summary>
    public static void ApplyAfterRendering(WordprocessingDocument document, DocxTheme theme)
    {
        ApplyPageSize(document.MainDocumentPart!.Document.Body!);

        var numbering = document.MainDocumentPart.NumberingDefinitionsPart?.Numbering;
        if (numbering != null)
        {
            foreach (var abstractNum in numbering.Elements<AbstractNum>())
            {
                // the numbering styles of the template are not part of the document anymore
                abstractNum.StyleLink = null;
                abstractNum.NumberingStyleLink = null;
            }

            foreach (var level in numbering.Descendants<Level>())
            {
                level.ParagraphStyleIdInLevel = null;
                level.NumberingSymbolRunProperties ??= new NumberingSymbolRunProperties();
                level.NumberingSymbolRunProperties.Color = new Color { Val = theme.ListColor };
            }
        }

        ApplyContextualSpacing(document.MainDocumentPart.Document.Body!, theme);
    }

    private static Styles CreateStyles(DocxTheme theme, string language)
    {
        var paragraphAfter = theme.ToTwips(theme.MarginBasePx * 2);
        var line = ((int)Math.Round(240 * theme.LineHeight)).ToXml();
        var styles = new Styles();

        var fontSize = theme.ToHalfPoints(theme.FontSizePx);
        styles.AppendChild(new DocDefaults(
            new RunPropertiesDefault(new RunPropertiesBaseStyle(
                Fonts(theme.FontFamily),
                new Color { Val = theme.TextColor },
                new FontSize { Val = fontSize },
                new FontSizeComplexScript { Val = fontSize },
                new Languages { Val = language, EastAsia = language, Bidi = language })),
            new ParagraphPropertiesDefault(new ParagraphPropertiesBaseStyle(
                new SpacingBetweenLines { Before = "0", After = "0", Line = line, LineRule = LineSpacingRuleValues.Auto }))));

        styles.AppendChild(ParagraphStyle(NormalStyle, "Normal", null, (ppr, _) =>
        {
            ppr.SpacingBetweenLines = new SpacingBetweenLines { Before = "0", After = paragraphAfter.ToXml() };
            ppr.Justification = new Justification { Val = JustificationValues.Both };
        }, isDefault: true));

        styles.AppendChild(new Style(new StyleName { Val = "Default Paragraph Font" }, new UIPriority { Val = 1 }, new SemiHidden(), new UnhideWhenUsed())
        {
            Type = StyleValues.Character, StyleId = DefaultParagraphFontStyle, Default = true
        });

        styles.AppendChild(new Style(new StyleName { Val = "Normal Table" }, new UIPriority { Val = 99 }, new SemiHidden(), new UnhideWhenUsed(),
            new StyleTableProperties(
                new TableIndentation { Width = 0, Type = TableWidthUnitValues.Dxa },
                new TableCellMarginDefault(
                    new TopMargin { Width = "0", Type = TableWidthUnitValues.Dxa },
                    new TableCellLeftMargin { Width = 108, Type = TableWidthValues.Dxa },
                    new BottomMargin { Width = "0", Type = TableWidthUnitValues.Dxa },
                    new TableCellRightMargin { Width = 108, Type = TableWidthValues.Dxa })))
        {
            Type = StyleValues.Table, StyleId = "TableNormal", Default = true
        });

        styles.AppendChild(new Style(new StyleName { Val = "No List" }, new UIPriority { Val = 99 }, new SemiHidden(), new UnhideWhenUsed())
        {
            Type = StyleValues.Numbering, StyleId = "NoList", Default = true
        });

        AddHeadingStyles(styles, theme);

        styles.AppendChild(ParagraphStyle(TitleStyle, "Title", 10, (ppr, rpr) =>
        {
            ppr.KeepNext = new KeepNext();
            ppr.SpacingBetweenLines = new SpacingBetweenLines
            {
                Before = theme.ToTwips(theme.MarginBasePx).ToXml(),
                After = theme.ToTwips(theme.MarginBasePx * 9).ToXml(),
                Line = "336", // line-height: 1.4em
                LineRule = LineSpacingRuleValues.Auto
            };
            ppr.Justification = new Justification { Val = JustificationValues.Center };
            rpr.RunFonts = Fonts(theme.HeadingFontFamily);
            rpr.Bold = new Bold();
            rpr.Color = new Color { Val = theme.DocumentTitleColor };
            SetFontSize(rpr, theme.ToHalfPoints(theme.FontSizePx * 3));
        }));

        styles.AppendChild(ParagraphStyle(SubtitleStyle, "Subtitle", 11, (ppr, rpr) =>
        {
            ppr.SpacingBetweenLines = new SpacingBetweenLines { Before = "0", After = theme.ToTwips(theme.MarginBasePx * 9).ToXml() };
            ppr.Justification = new Justification { Val = JustificationValues.Center };
            rpr.Color = new Color { Val = theme.DocumentTitleAuthorColor };
        }));

        styles.AppendChild(ParagraphStyle(QuoteStyle, "Quote", 29, (ppr, rpr) =>
        {
            // CSS: 10px border on the left, 2rem padding (including the border)
            var borderWidth = (uint)Math.Min(96, Math.Round(theme.ToPoints(10) * 8));
            ppr.ParagraphBorders = new ParagraphBorders(
                new LeftBorder { Val = BorderValues.Single, Size = borderWidth, Space = (uint)Math.Round(theme.ToPoints(22)), Color = theme.PreBackgroundColor });
            ppr.Indentation = new Indentation { Left = theme.ToTwips(32).ToXml() };
            rpr.Color = new Color { Val = theme.BlockquoteColor };
            SetFontSize(rpr, theme.ToHalfPoints(theme.FontSizeBlockquotePx));
        }));

        styles.AppendChild(ParagraphStyle(ListParagraphStyle, "List Paragraph", 34, (ppr, _) =>
        {
            ppr.SpacingBetweenLines = new SpacingBetweenLines { Before = theme.ToTwips(theme.MarginBasePx).ToXml(), After = "0" };
        }));

        var codeSize = theme.ToHalfPoints(theme.FontSizePx * 0.85);
        styles.AppendChild(ParagraphStyle(CodeBlockStyle, "HTML Preformatted", 99, (ppr, rpr) =>
        {
            // the border has the same color as the background, so it works like the padding in CSS
            var padding = (uint)Math.Min(31, Math.Round(theme.ToPoints(16)));
            ppr.ParagraphBorders = new ParagraphBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4, Space = padding, Color = theme.PreBackgroundColor },
                new LeftBorder { Val = BorderValues.Single, Size = 4, Space = padding, Color = theme.PreBackgroundColor },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Space = padding, Color = theme.PreBackgroundColor },
                new RightBorder { Val = BorderValues.Single, Size = 4, Space = padding, Color = theme.PreBackgroundColor });
            ppr.Shading = new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = theme.PreBackgroundColor };
            ppr.SpacingBetweenLines = new SpacingBetweenLines
            {
                Before = (padding * 20).ToXml(),
                After = (paragraphAfter + padding * 20).ToXml(),
                Line = "240",
                LineRule = LineSpacingRuleValues.Auto
            };
            ppr.Indentation = new Indentation { Left = (padding * 20).ToXml(), Right = (padding * 20).ToXml() };
            ppr.Justification = new Justification { Val = JustificationValues.Left };
            rpr.RunFonts = Fonts(theme.CodeFontFamily);
            rpr.Color = new Color { Val = theme.PreColor };
            SetFontSize(rpr, codeSize);
        }, unhideWhenUsed: true));

        styles.AppendChild(ParagraphStyle(CaptionStyle, "caption", 35, (ppr, rpr) =>
        {
            ppr.SpacingBetweenLines = new SpacingBetweenLines { Before = theme.ToTwips(3).ToXml(), After = "0" };
            ppr.Justification = new Justification { Val = JustificationValues.Center };
            SetFontSize(rpr, theme.ToHalfPoints(theme.FontSizeFigureCaptionPx));
        }, unhideWhenUsed: true));

        styles.AppendChild(ParagraphStyle(EndnoteTextStyle, "endnote text", 99, (ppr, rpr) =>
        {
            ppr.SpacingBetweenLines = new SpacingBetweenLines { Before = "0", After = "0" };
            ppr.Justification = new Justification { Val = JustificationValues.Left };
            SetFontSize(rpr, theme.ToHalfPoints(theme.FontSizePx * 0.85));
        }, unhideWhenUsed: true));

        styles.AppendChild(CharacterStyle(CodeInlineStyle, "HTML Code", rpr =>
        {
            rpr.RunFonts = Fonts(theme.CodeFontFamily);
            SetFontSize(rpr, codeSize);
        }));

        styles.AppendChild(CharacterStyle(HyperlinkStyle, "Hyperlink", rpr => rpr.Color = new Color { Val = theme.LinkColor }));

        styles.AppendChild(CharacterStyle(EndnoteReferenceStyle, "endnote reference", rpr =>
        {
            rpr.Color = new Color { Val = theme.FootnoteColor };
            rpr.VerticalTextAlignment = new VerticalTextAlignment { Val = VerticalPositionValues.Superscript };
        }));

        return styles;
    }

    private static void AddHeadingStyles(Styles styles, DocxTheme theme)
    {
        // CSS: font size relative to the base font size, margin-top when following a paragraph (minus the margin of the paragraph, which is
        // added in DOCX, while it collapses in CSS) and margin-bottom
        (double Size, double Before, double After)[] levels =
        [
            (1.75, 4, 2),
            (1.4, 2, 1.5),
            (1.25, 1, 1.3),
            (1, 0, 1),
            (1, 0, 1),
            (1, 0, 1)
        ];

        for (var i = 0; i < levels.Length; i++)
        {
            var (size, before, after) = levels[i];
            var level = i;
            styles.AppendChild(ParagraphStyle(HeadingStylePrefix + (i + 1), "heading " + (i + 1), 9, (ppr, rpr) =>
            {
                ppr.KeepNext = new KeepNext();
                ppr.KeepLines = new KeepLines();
                ppr.SpacingBetweenLines = new SpacingBetweenLines
                {
                    Before = theme.ToTwips(theme.MarginBasePx * before).ToXml(),
                    After = theme.ToTwips(theme.MarginBasePx * after).ToXml()
                };
                ppr.Justification = new Justification { Val = JustificationValues.Left };
                ppr.OutlineLevel = new OutlineLevel { Val = level };

                rpr.RunFonts = Fonts(theme.HeadingFontFamily);
                rpr.Bold = new Bold();
                rpr.Caps = theme.UppercaseHeadings ? new Caps() : null;
                rpr.Color = new Color { Val = theme.HeadingColor };
                SetFontSize(rpr, theme.ToHalfPoints(theme.FontSizePx * size));
            }));
        }
    }

    /// <summary>
    /// Mimics the spacing rules of the CSS that depend on the following element: a heading directly followed by a heading of the next
    /// level has no space in between, and lists have some space after their last item.
    /// </summary>
    private static void ApplyContextualSpacing(Body body, DocxTheme theme)
    {
        foreach (var paragraph in body.Elements<Paragraph>().ToList())
        {
            var style = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
            var next = paragraph.NextSibling() as Paragraph;
            var nextStyle = next?.ParagraphProperties?.ParagraphStyleId?.Val?.Value;

            var level = GetHeadingLevel(style);
            var nextLevel = GetHeadingLevel(nextStyle);
            if (level != null && nextLevel != null && (nextLevel == level + 1 || (level >= 4 && nextLevel > level)))
            {
                SetSpacing(paragraph, after: "0");
                SetSpacing(next!, before: "0");
            }

            if (style == ListParagraphStyle && nextStyle != ListParagraphStyle)
            {
                SetSpacing(paragraph, after: theme.ToTwips(theme.MarginBasePx * 2).ToXml());
            }
        }
    }

    private static int? GetHeadingLevel(string? style) =>
        style != null && style.StartsWith(HeadingStylePrefix, StringComparison.Ordinal) && int.TryParse(style[HeadingStylePrefix.Length..], out var level) ? level : null;

    private static void SetSpacing(Paragraph paragraph, string? before = null, string? after = null)
    {
        var ppr = paragraph.ParagraphProperties ??= new ParagraphProperties();
        var spacing = ppr.SpacingBetweenLines ??= new SpacingBetweenLines();
        if (before != null) spacing.Before = before;
        if (after != null) spacing.After = after;
    }

    private static void AddFonts(Fonts? fonts, DocxTheme theme)
    {
        if (fonts == null) return;

        foreach (var (name, family, pitch) in new[]
                 {
                     (theme.FontFamily, FontFamilyValues.Swiss, FontPitchValues.Variable),
                     (theme.HeadingFontFamily, FontFamilyValues.Swiss, FontPitchValues.Variable),
                     (theme.CodeFontFamily, FontFamilyValues.Modern, FontPitchValues.Fixed)
                 })
        {
            if (fonts.Elements<Font>().Any(f => string.Equals(f.Name?.Value, name, StringComparison.OrdinalIgnoreCase))) continue;
            fonts.AppendChild(new Font(new FontFamily { Val = family }, new Pitch { Val = pitch }) { Name = name });
        }
    }

    private static void ApplySettings(Settings? settings)
    {
        if (settings == null) return;

        // the template claims that spelling and grammar have already been checked
        settings.Elements<ProofState>().ToList().ForEach(x => x.Remove());

        if (settings.GetFirstChild<AutoHyphenation>() == null)
        {
            var autoHyphenation = new AutoHyphenation();
            var defaultTabStop = settings.GetFirstChild<DefaultTabStop>();
            if (defaultTabStop != null) settings.InsertAfter(autoHyphenation, defaultTabStop);
            else settings.PrependChild(autoHyphenation);
        }
    }

    private static void ApplyPageSize(Body body)
    {
        var sectPr = body.Elements<SectionProperties>().LastOrDefault();
        if (sectPr == null)
        {
            sectPr = new SectionProperties();
            body.AppendChild(sectPr);
        }

        sectPr.RemoveAllChildren<PageSize>();
        sectPr.RemoveAllChildren<PageMargin>();

        // the page size must follow the header/footer references, footnote/endnote properties and section type
        var previous = sectPr.ChildElements.LastOrDefault(e => e is HeaderReference or FooterReference or FootnoteProperties or EndnoteProperties or SectionType);
        var pageSize = new PageSize { Width = PageWidth, Height = PageHeight };
        if (previous == null) sectPr.PrependChild(pageSize);
        else sectPr.InsertAfter(pageSize, previous);

        sectPr.InsertAfter(new PageMargin
        {
            Top = PageMarginTopBottom,
            Bottom = PageMarginTopBottom,
            Left = PageMarginLeftRight,
            Right = PageMarginLeftRight,
            Header = 709,
            Footer = 567,
            Gutter = 0
        }, pageSize);
    }

    private static RunFonts Fonts(string font) => new() { Ascii = font, HighAnsi = font, ComplexScript = font, EastAsia = font };

    private static void SetFontSize(StyleRunProperties rpr, string halfPoints)
    {
        rpr.FontSize = new FontSize { Val = halfPoints };
        rpr.FontSizeComplexScript = new FontSizeComplexScript { Val = halfPoints };
    }

    /// <summary>
    /// A built-in paragraph style of Word, identified by its (English) name.
    /// </summary>
    private static Style ParagraphStyle(string id, string name, int? uiPriority, Action<StyleParagraphProperties, StyleRunProperties> configure,
        bool isDefault = false, bool unhideWhenUsed = false)
    {
        var style = new Style(new StyleName { Val = name }) { Type = StyleValues.Paragraph, StyleId = id };
        if (isDefault) style.Default = true;
        if (!isDefault) style.AppendChild(new BasedOn { Val = NormalStyle });
        if (!isDefault) style.AppendChild(new NextParagraphStyle { Val = NormalStyle });
        if (uiPriority != null) style.AppendChild(new UIPriority { Val = uiPriority });
        if (unhideWhenUsed)
        {
            style.AppendChild(new SemiHidden());
            style.AppendChild(new UnhideWhenUsed());
        }

        style.AppendChild(new PrimaryStyle());

        var ppr = new StyleParagraphProperties();
        var rpr = new StyleRunProperties();
        configure(ppr, rpr);
        if (ppr.HasChildren) style.StyleParagraphProperties = ppr;
        if (rpr.HasChildren) style.StyleRunProperties = rpr;
        return style;
    }

    /// <summary>
    /// A built-in character style of Word, identified by its (English) name.
    /// </summary>
    private static Style CharacterStyle(string id, string name, Action<StyleRunProperties> configure)
    {
        var style = new Style(new StyleName { Val = name }, new BasedOn { Val = DefaultParagraphFontStyle }, new UIPriority { Val = 99 }, new UnhideWhenUsed())
        {
            Type = StyleValues.Character, StyleId = id
        };
        var rpr = new StyleRunProperties();
        configure(rpr);
        style.StyleRunProperties = rpr;
        return style;
    }
}
