using Adliance.QmDoc.Converter.Docx;
using Adliance.QmDoc.Parameters;
using Adliance.QmDoc.Tests.Helpers;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using static Adliance.QmDoc.Tests.Helpers.Md;

namespace Adliance.QmDoc.Tests.Converter.Docx;

/// <summary>
/// End-to-end tests of the Markdown to DOCX conversion.
/// </summary>
public sealed class DocxConverterTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private async Task<WordprocessingDocument> Convert(string markdown, string fileName = "doc.md")
    {
        var source = _dir.Write("src/" + fileName, markdown);
        await new DocxConverter(new DocxParameters { Source = source, Target = _dir.Combine("out") }, new Options.Options()).Run();
        return Open(Path.ChangeExtension(fileName, ".docx"));
    }

    private WordprocessingDocument Open(string fileName) => WordprocessingDocument.Open(new MemoryStream(File.ReadAllBytes(_dir.Combine("out/" + fileName))), false);

    private static Body Body(WordprocessingDocument document) => document.MainDocumentPart!.Document!.Body!;

    private static List<string> Paragraphs(WordprocessingDocument document) =>
        Body(document).Elements<Paragraph>().Select(p => string.Concat(p.Descendants<Text>().Select(t => t.Text))).ToList();

    private static Paragraph ParagraphWithText(WordprocessingDocument document, string text) =>
        Body(document).Descendants<Paragraph>().First(p => string.Concat(p.Descendants<Text>().Select(t => t.Text)) == text);

    [Fact]
    public async Task Run_StartsWithTitleBlock()
    {
        using var document = await Convert(Lines("---", "title: Mein Dokument", "---", "# A"));
        Assert.Equal("Mein Dokument", Paragraphs(document)[0]);
    }

    [Fact]
    public async Task Run_TitleFromFileName()
    {
        using var document = await Convert("Text", "My Document.md");
        Assert.Equal("My Document", Paragraphs(document)[0]);
    }

    [Fact]
    public async Task Run_RunsMarkdownProcessors()
    {
        using var document = await Convert(Lines("---", "title: T", "Satz: 10€/h", "---", "# Einleitung", "", "{{ TITLE }}: {{ = 3h * Satz }}"));
        var paragraphs = Paragraphs(document);
        Assert.Contains("1. Einleitung", paragraphs);
        Assert.Contains("T: 30€", paragraphs);
    }

    [Fact]
    public async Task Run_RendersTables()
    {
        using var document = await Convert(Lines("| Position | Stunden |", "|---|---|", "| **Konzept** | 10h |", "| Summe | {{ = SUM }} |"));
        var table = Assert.Single(Body(document).Elements<Table>());
        var rows = table.Elements<TableRow>().ToList();
        Assert.Equal(3, rows.Count);
        Assert.NotNull(rows[0].TableRowProperties?.GetFirstChild<TableHeader>());

        var cells = rows.Select(r => r.Elements<TableCell>().Select(c => c.InnerText).ToArray()).ToList();
        Assert.Equal(["Position", "Stunden"], cells[0]);
        Assert.Equal(["Konzept", "10h"], cells[1]);
        Assert.Equal(["Summe", "10h"], cells[2]);

        Assert.NotNull(rows[1].Descendants<Run>().First().RunProperties?.Bold);
        Assert.Null(rows[1].Elements<TableCell>().Last().Descendants<Run>().First().RunProperties?.Bold);
    }

    private static readonly DocxTheme DefaultTheme = DocxTheme.Load("2026");
    private static string Fill(string color) => DocxTheme.MixWithWhite(color, 40);

    public static TheoryData<string, string> Callouts => new()
    {
        { "{!}", Fill(DefaultTheme.WarningColor) },
        { "{!!}", Fill(DefaultTheme.DangerColor) },
        { "{?}", Fill(DefaultTheme.InfoColor) },
        { "{{!}}", Fill(DefaultTheme.WarningColor) },
        { "{{!!}}", Fill(DefaultTheme.DangerColor) },
        { "{{?}}", Fill(DefaultTheme.InfoColor) },
        { "{{ ! }}", Fill(DefaultTheme.WarningColor) },
        { "{{!!}}   ", Fill(DefaultTheme.DangerColor) }
    };

    [Theory]
    [MemberData(nameof(Callouts))]
    public async Task Run_RendersCallouts(string placeholder, string fill)
    {
        using var document = await Convert($"{placeholder} Callout text");
        Assert.Contains("Callout text", Paragraphs(document));
        var paragraph = ParagraphWithText(document, "Callout text");
        Assert.Equal(fill, paragraph.ParagraphProperties?.Shading?.Fill?.Value);
        Assert.NotNull(paragraph.ParagraphProperties?.ParagraphBorders?.TopBorder);
    }

    [Fact]
    public void CalloutColors_MatchTheme()
    {
        // CSS: {{!}} uses the warning color, {{!!}} the danger color and {{?}} the info color, all mixed with 60% white
        Assert.Equal("F47D09", DefaultTheme.WarningColor);
        Assert.Equal("ED4622", DefaultTheme.DangerColor);
        Assert.Equal("10A8E5", DefaultTheme.InfoColor);
    }

    [Fact]
    public async Task Run_RendersCalloutWithFormatting()
    {
        using var document = await Convert("{{!}} **Wichtig:** Text");
        var paragraph = ParagraphWithText(document, "Wichtig: Text");
        Assert.Equal(Fill(DefaultTheme.WarningColor), paragraph.ParagraphProperties?.Shading?.Fill?.Value);
    }

    [Fact]
    public async Task Run_CalloutsInLists_KeepTheList()
    {
        using var document = await Convert(Lines("- {{!}} Erster Punkt", "- Zweiter Punkt"));
        var paragraph = ParagraphWithText(document, "\u25CF Erster Punkt");
        Assert.NotNull(paragraph.ParagraphProperties?.NumberingProperties);
        Assert.Null(paragraph.ParagraphProperties?.Shading);
    }

    [Theory]
    [InlineData("NOTE", nameof(DocxTheme.InfoColor))]
    [InlineData("TIP", nameof(DocxTheme.InfoColor))]
    [InlineData("IMPORTANT", nameof(DocxTheme.DangerColor))]
    [InlineData("WARNING", nameof(DocxTheme.WarningColor))]
    [InlineData("CAUTION", nameof(DocxTheme.WarningColor))]
    public async Task Run_RendersGitHubAlertsAsCallouts(string kind, string colorProperty)
    {
        using var document = await Convert(Lines($"> [!{kind}]", "> Erster Absatz", ">", "> Zweiter Absatz"));
        var color = (string)typeof(DocxTheme).GetProperty(colorProperty)!.GetValue(DefaultTheme)!;

        foreach (var text in new[] { "Erster Absatz", "Zweiter Absatz" })
        {
            var paragraph = ParagraphWithText(document, text);
            Assert.Equal(Fill(color), paragraph.ParagraphProperties?.Shading?.Fill?.Value);
            Assert.Equal(color, paragraph.ParagraphProperties?.ParagraphBorders?.LeftBorder?.Color?.Value);
        }
    }

    [Theory]
    [InlineData("Text {{!}} in the middle")]
    [InlineData("{{!}}Text without space")]
    [InlineData("{{x}} Text")]
    public async Task Run_OtherTexts_AreNoCallouts(string markdown)
    {
        using var document = await Convert(markdown);
        var paragraph = ParagraphWithText(document, markdown);
        Assert.Null(paragraph.ParagraphProperties?.Shading);
    }

    [Fact]
    public async Task Run_RendersAuthorLine()
    {
        using var document = await Convert("#### Max Muster | ACME GmbH | max@example.com");
        Assert.Contains("Max Muster, ACME GmbHmax@example.com", Paragraphs(document)); // name and company, line break, email
        var paragraph = ParagraphWithText(document, "Max Muster, ACME GmbHmax@example.com");
        Assert.Equal(ThemeStylesProcessor.SubtitleStyle, paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value);
        Assert.Single(paragraph.Descendants<Hyperlink>());
    }

    [Fact]
    public async Task Run_RendersAuthorFromFrontmatter()
    {
        using var document = await Convert(Lines("---", "title: Mein Dokument", "author: Max Muster", "---", "Text"));
        var paragraphs = Paragraphs(document);
        Assert.Equal("Mein Dokument", paragraphs[0]);
        Assert.Equal("Max Muster, Adliance GmbH", paragraphs[1]);
    }

    [Theory]
    [InlineData("Max Muster | ACME GmbH | max@example.com")]
    [InlineData("```\n| A | B | C |\n```")]
    public async Task Run_OtherTextsWithPipes_AreNoAuthorLines(string markdown)
    {
        using var document = await Convert(markdown.Replace("\\n", "\n"));
        Assert.Contains(Paragraphs(document), p => p.Contains(" | ", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Run_CodeBlocksKeepTheirStyle()
    {
        using var document = await Convert(Lines("```", "| A | B | C |", "zweite Zeile", "```"));
        var paragraph = Body(document).Elements<Paragraph>().Single(p => p.InnerText.StartsWith("| A", StringComparison.Ordinal));
        Assert.Equal(ThemeStylesProcessor.CodeBlockStyle, paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value);
    }

    [Fact]
    public async Task Run_WithoutDocumentTitle()
    {
        using var document = await Convert(Lines("---", "title: Mein Dokument", "enableDocumentTitle: false", "---", "Text"));
        Assert.DoesNotContain("Mein Dokument", Paragraphs(document));
    }

    [Fact]
    public async Task Run_TitleIsCentered()
    {
        using var document = await Convert(Lines("---", "title: Mein Dokument", "---", "Text"));
        var paragraph = ParagraphWithText(document, "Mein Dokument");
        Assert.Equal(ThemeStylesProcessor.TitleStyle, paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value);
        var style = document.MainDocumentPart!.StyleDefinitionsPart!.Styles!.Elements<Style>().Single(s => s.StyleId?.Value == ThemeStylesProcessor.TitleStyle);
        Assert.Equal(JustificationValues.Center, style.StyleParagraphProperties?.Justification?.Val?.Value);
        Assert.Null(style.StyleParagraphProperties?.ParagraphBorders);
    }

    [Fact]
    public async Task Run_InlineCodeUsesExistingStyle()
    {
        using var document = await Convert("Text mit `Code`");
        var run = Body(document).Descendants<Run>().Single(r => r.InnerText == "Code");
        var styleId = run.RunProperties?.RunStyle?.Val?.Value;
        Assert.NotNull(styleId);
        var style = Assert.Single(document.MainDocumentPart!.StyleDefinitionsPart!.Styles!.Elements<Style>(), s => s.StyleId?.Value == styleId);
        Assert.Equal(DefaultTheme.CodeFontFamily, style.StyleRunProperties?.RunFonts?.Ascii?.Value);
    }

    [Theory]
    [InlineData("doc.md", "", "de-DE")]
    [InlineData("doc.en.md", "", "en-US")]
    [InlineData("doc.md", "culture: de-AT", "de-AT")]
    [InlineData("doc.en.md", "culture: de-AT", "en-US")]
    public async Task Run_SetsLanguage(string fileName, string frontmatter, string language)
    {
        using var document = await Convert(Lines("---", frontmatter, "---", "Text"), fileName);
        var styles = document.MainDocumentPart!.StyleDefinitionsPart!.Styles!;
        Assert.Equal(language, styles.DocDefaults?.RunPropertiesDefault?.RunPropertiesBaseStyle?.Languages?.Val?.Value);
        Assert.Single(styles.Descendants<Languages>()); // the template's "ru-RU" and "en-US" are gone
    }

    [Fact]
    public async Task Run_UsesThemeForHeadingsAndText()
    {
        using var document = await Convert("# Kapitel");
        var styles = document.MainDocumentPart!.StyleDefinitionsPart!.Styles!;
        var heading = styles.Elements<Style>().Single(s => s.StyleId?.Value == "Heading1");
        Assert.Equal("heading 1", heading.StyleName?.Val?.Value); // the built-in heading style of Word
        Assert.Equal("Heading1", ParagraphWithText(document, "1. Kapitel").ParagraphProperties?.ParagraphStyleId?.Val?.Value);
        Assert.Equal(DefaultTheme.HeadingColor, heading.StyleRunProperties?.Color?.Val?.Value);
        Assert.Equal(DefaultTheme.HeadingFontFamily, heading.StyleRunProperties?.RunFonts?.Ascii?.Value);
        Assert.NotNull(heading.StyleParagraphProperties?.KeepNext);
        Assert.Equal(DefaultTheme.TextColor, styles.DocDefaults?.RunPropertiesDefault?.RunPropertiesBaseStyle?.Color?.Val?.Value);

        var body = styles.Elements<Style>().Single(s => s.StyleId?.Value == "Normal");
        Assert.Equal(JustificationValues.Both, body.StyleParagraphProperties?.Justification?.Val?.Value);
        Assert.NotNull(document.MainDocumentPart.DocumentSettingsPart!.Settings!.GetFirstChild<AutoHyphenation>());

        var link = styles.Elements<Style>().Single(s => s.StyleId?.Value == "Hyperlink");
        Assert.Equal(DefaultTheme.LinkColor, link.StyleRunProperties?.Color?.Val?.Value);
        Assert.Null(link.StyleRunProperties?.Underline);
    }

    [Fact]
    public async Task Run_UsesOnlyBuiltInStyles()
    {
        using var document = await Convert(Lines("# A", "Text `Code` [Link](https://example.com)[^1]", "", "- Liste", "", "> Zitat", "", "```", "Code", "```", "", "[^1]: Fußnote"));
        var styles = document.MainDocumentPart!.StyleDefinitionsPart!.Styles!.Elements<Style>().ToList();
        Assert.DoesNotContain(styles, s => s.CustomStyle?.Value == true);
        Assert.DoesNotContain(styles, s => s.StyleId!.Value!.StartsWith("MD", StringComparison.Ordinal));
        Assert.Contains(styles, s => s.StyleId?.Value == "Normal" && s.Default?.Value == true);

        // all used styles are defined
        var ids = styles.Select(s => s.StyleId!.Value).ToHashSet();
        var used = Body(document).Descendants<ParagraphStyleId>().Select(s => s.Val!.Value)
            .Concat(Body(document).Descendants<RunStyle>().Select(s => s.Val!.Value))
            .Concat(document.MainDocumentPart.EndnotesPart!.Endnotes!.Descendants<ParagraphStyleId>().Select(s => s.Val!.Value))
            .Concat(document.MainDocumentPart.NumberingDefinitionsPart!.Numbering!.Descendants<ParagraphStyleIdInLevel>().Select(s => s.Val!.Value));
        Assert.All(used, id => Assert.Contains(id, ids));
        Assert.Empty(document.MainDocumentPart.NumberingDefinitionsPart.Numbering.Descendants<StyleLink>());
    }

    [Fact]
    public async Task Run_TableOfContentsHasNoLines()
    {
        using var document = await Convert(Lines("{{ TOC }}", "", "# Kapitel", "## Unterkapitel"));
        var table = Assert.Single(Body(document).Elements<Table>());
        var rows = table.Elements<TableRow>().ToList();
        Assert.Equal(["1. Kapitel", "1.1. Unterkapitel"], rows.Select(r => r.Elements<TableCell>().First().InnerText.Trim().Replace("\u00A0", "", StringComparison.Ordinal)));

        var borders = table.GetFirstChild<TableProperties>()!.TableBorders!;
        Assert.Equal(BorderValues.Nil, borders.InsideHorizontalBorder!.Val!.Value);
        Assert.Equal(BorderValues.Nil, borders.BottomBorder!.Val!.Value);
        Assert.DoesNotContain(table.Descendants<TableCellBorders>(), _ => true);

        var margins = table.GetFirstChild<TableProperties>()!.TableCellMarginDefault!;
        Assert.Equal(DefaultTheme.ToTwips(5).ToString(System.Globalization.CultureInfo.InvariantCulture), margins.TopMargin!.Width!.Value);
        Assert.All(table.Descendants<Hyperlink>().SelectMany(h => h.Descendants<Run>()), r => Assert.Equal(DefaultTheme.TextColor, r.RunProperties?.Color?.Val?.Value));
    }

    [Fact]
    public async Task Run_UsesA4()
    {
        using var document = await Convert("Text");
        var sectPr = Assert.IsType<SectionProperties>(Body(document).LastChild);
        Assert.Equal(11906U, sectPr.GetFirstChild<PageSize>()?.Width?.Value);
        Assert.Equal(16838U, sectPr.GetFirstChild<PageSize>()?.Height?.Value);
    }

    [Fact]
    public async Task Run_RendersFootnotesAsEndnotes()
    {
        using var document = await Convert(Lines("Text[^1] und mehr[^note].", "", "[^1]: Erste Fußnote.", "[^note]: Zweite Fußnote."));
        Assert.Equal("Text und mehr.", ParagraphWithText(document, "Text und mehr.").InnerText);
        Assert.Equal([1L, 2L], Body(document).Descendants<EndnoteReference>().Select(r => r.Id!.Value));
        Assert.DoesNotContain(Paragraphs(document), p => p.Contains("Fußnote", StringComparison.Ordinal));

        var endnotes = document.MainDocumentPart!.EndnotesPart!.Endnotes!.Elements<Endnote>().Where(e => e.Id!.Value > 0).ToList();
        Assert.Equal(["Erste Fußnote.", "Zweite Fußnote."], endnotes.Select(e => e.InnerText.Trim()));
        Assert.All(endnotes, e => Assert.Single(e.Descendants<EndnoteReferenceMark>()));
    }

    [Fact]
    public async Task Run_RendersChapterLinksAsLinksToBookmarks()
    {
        using var document = await Convert(Lines("# Einleitung", "Siehe [#Details].", "# Details", "Text"));
        var link = Assert.Single(Body(document).Descendants<Hyperlink>());
        Assert.Equal("2. Details", link.InnerText); // like in the HTML/PDF, the title of the heading with its numbering
        var heading = ParagraphWithText(document, "2. Details");
        Assert.Equal(link.Anchor?.Value, heading.Elements<BookmarkStart>().Single().Name?.Value);
        Assert.DoesNotContain(Paragraphs(document), p => p.Contains("[#", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Run_RendersTableColumnsAndCellContent()
    {
        using var document = await Convert(Lines("| Name | Betrag |", "|---|---:|", "| [Link](https://example.com) | `10` |", "| B | 20 |"));
        var table = Assert.Single(Body(document).Elements<Table>());
        Assert.Equal(TableWidthUnitValues.Pct, table.GetFirstChild<TableProperties>()?.TableWidth?.Type?.Value);

        var rows = table.Elements<TableRow>().ToList();
        Assert.Single(rows[1].Descendants<Hyperlink>());
        Assert.Equal(JustificationValues.Right, rows[1].Elements<TableCell>().Last().Descendants<Paragraph>().Single().ParagraphProperties?.Justification?.Val?.Value);
        Assert.Equal(JustificationValues.Left, rows[1].Elements<TableCell>().First().Descendants<Paragraph>().Single().ParagraphProperties?.Justification?.Val?.Value);

        // header with a thicker border, alternating row colors
        Assert.NotNull(rows[0].Elements<TableCell>().First().TableCellProperties?.TableCellBorders?.BottomBorder);
        Assert.Equal(DefaultTheme.TableBackgroundColor, rows[1].Elements<TableCell>().First().TableCellProperties?.Shading?.Fill?.Value);
        Assert.Equal(DefaultTheme.TableAlternateBackgroundColor, rows[2].Elements<TableCell>().First().TableCellProperties?.Shading?.Fill?.Value);
    }

    [Fact]
    public async Task Run_EmbedsImages()
    {
        _dir.Write("src/small.png", Png(100, 50));
        _dir.Write("src/wide.png", Png(5000, 1000));
        using var document = await Convert(Lines("![Klein](small.png)", "", "![Breit](wide.png)", "", "![Nochmal](small.png)", "", "![Fehlt](missing.png)"));

        Assert.Equal(2, document.MainDocumentPart!.ImageParts.Count()); // the same image is only embedded once
        var extents = Body(document).Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent>().ToList();
        Assert.Equal(3, extents.Count);
        Assert.Equal(DefaultTheme.ToEmus(100), extents[0].Cx!.Value); // small images keep their size
        Assert.InRange(extents[1].Cx!.Value, ThemeStylesProcessor.TextWidth * 635L - 5, ThemeStylesProcessor.TextWidth * 635L); // wide images are resized to fit the page
        Assert.InRange(extents[1].Cy!.Value, extents[1].Cx!.Value / 5 - 5, extents[1].Cx!.Value / 5 + 5);

        var paragraph = Body(document).Descendants<Paragraph>().First(p => p.Descendants<Drawing>().Any());
        Assert.Equal(JustificationValues.Center, paragraph.ParagraphProperties?.Justification?.Val?.Value);
        Assert.Contains("Fehlt", Paragraphs(document)); // missing images are rendered as their alternative text
    }

    [Fact]
    public async Task Run_EmptyFigure_DoesNotCrash()
    {
        using var document = await Convert(Lines("^^^", "^^^", "", "Text"));
        Assert.Contains("Text", Paragraphs(document));
    }

    [Fact]
    public async Task Run_RendersFiguresAndFloatingImages()
    {
        _dir.Write("src/image.png", Png(100, 50));
        using var document = await Convert(Lines("^^^", "![Bild](image.png)", "^^^ Eine Bildunterschrift", "", "![Bild](image.png){.left} Text daneben.", "",
            "^^^", "![Bild](image.png)", "^^^ Rechts {.right}", "Text"));

        var figures = Body(document).Elements<Table>().ToList();
        Assert.Equal(2, figures.Count);
        Assert.Equal("Eine Bildunterschrift", figures[0].Descendants<Paragraph>().Last().InnerText);
        Assert.Equal(ThemeStylesProcessor.CaptionStyle, figures[0].Descendants<Paragraph>().Last().ParagraphProperties?.ParagraphStyleId?.Val?.Value);
        Assert.Equal(DefaultTheme.FigureBackgroundColor, figures[0].Descendants<TableCell>().Single().TableCellProperties?.Shading?.Fill?.Value);
        Assert.Null(figures[0].GetFirstChild<TableProperties>()?.TablePositionProperties);

        Assert.Equal("Rechts", figures[1].Descendants<Paragraph>().Last().InnerText);
        Assert.Equal(HorizontalAlignmentValues.Right, figures[1].GetFirstChild<TableProperties>()?.TablePositionProperties?.TablePositionXAlignment?.Value);

        var anchor = Assert.Single(Body(document).Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor>());
        Assert.Equal("left", anchor.HorizontalPosition?.HorizontalAlignment?.Text);
    }

    [Fact]
    public async Task Run_CreatesValidDocument()
    {
        _dir.Write("src/image.png", Png(100, 50));
        using var document = await Convert(Lines(
            "---", "title: Alles", "author: Max Muster", "---",
            "{{ TOC }}", "",
            "# Kapitel", "## Unterkapitel", "Text mit **fett**, `Code`, [Link](https://example.com), [#Kapitel] und Fußnote[^1].", "",
            "- {{!}} Liste", "- Zweiter Punkt", "",
            "{{?}} Frage", "", "> [!NOTE]", "> Hinweis", "", "> Zitat", "",
            "| A | B |", "|---|--:|", "| 1 | [Link](https://example.com) |", "",
            "| C |", "|---|", "| 2 |", "",
            "![Bild](image.png)", "", "^^^", "![Bild](image.png)", "^^^ Unterschrift {.right}", "Text", "",
            "```", "Code", "```", "", "---", "", "Ende", "",
            "[^1]: Fußnote mit [Link](https://example.com)."));

        var errors = new DocumentFormat.OpenXml.Validation.OpenXmlValidator().Validate(document, TestContext.Current.CancellationToken)
            .Select(e => $"{e.Part?.Uri}: {e.Path?.XPath}: {e.Description}").ToList();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }

    /// <summary>
    /// A PNG with just the header, which is enough to read its size.
    /// </summary>
    private static byte[] Png(int width, int height)
    {
        var bytes = new byte[33];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, (byte)'I', (byte)'H', (byte)'D', (byte)'R' }.CopyTo(bytes, 0);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(16), width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(20), height);
        bytes[24] = 8;
        bytes[25] = 2;
        return bytes;
    }

    [Fact]
    public async Task Run_RendersPageBreaks()
    {
        using var document = await Convert(Lines("A", "", "---", "", "B"));
        Assert.Contains(Body(document).Descendants<Break>(), b => b.Type?.Value == BreakValues.Page);
    }

    [Fact]
    public async Task Run_AddsFooter()
    {
        using var document = await Convert(Lines("---", "title: Mein Dokument", "---", "Text"));
        var footer = Assert.Single(document.MainDocumentPart!.FooterParts);
        Assert.StartsWith("Mein Dokument", footer.Footer!.InnerText);
        Assert.Contains(footer.Footer.Descendants<FieldCode>(), f => f.Text.Trim() == "PAGE");
        Assert.Contains(footer.Footer.Descendants<FieldCode>(), f => f.Text.Trim() == "NUMPAGES");
        Assert.NotNull(Body(document).Descendants<FooterReference>().SingleOrDefault());
    }

    [Fact]
    public async Task Run_FooterTitleUsesWholeWidthWithoutVersionLine()
    {
        using var document = await Convert(Lines("---", "title: Ein sehr langer Dokumenttitel, der fast die ganze Zeile braucht", "---", "Text"));
        var footer = Assert.Single(document.MainDocumentPart!.FooterParts).Footer!;

        var cells = Assert.Single(footer.Descendants<TableRow>()).Elements<TableCell>().ToList();
        Assert.Equal(2, cells.Count);
        Assert.Equal("Ein sehr langer Dokumenttitel, der fast die ganze Zeile braucht", Assert.Single(cells[0].Elements<Paragraph>()).InnerText); // no Git version, no second line
        Assert.Contains(cells[1].Descendants<FieldCode>(), f => f.Text.Trim() == "PAGE");
        Assert.True(int.Parse(cells[0].TableCellProperties!.TableCellWidth!.Width!.Value!, System.Globalization.CultureInfo.InvariantCulture) >
                    int.Parse(cells[1].TableCellProperties!.TableCellWidth!.Width!.Value!, System.Globalization.CultureInfo.InvariantCulture) * 4);

        Assert.Empty(new DocumentFormat.OpenXml.Validation.OpenXmlValidator().Validate(document.MainDocumentPart.FooterParts.Single(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Run_MultipleFiles_DoNotShareContent()
    {
        _dir.Write("src/a.md", "Content of A");
        _dir.Write("src/b.md", "Content of B");
        await new DocxConverter(new DocxParameters { Source = _dir.Combine("src"), Target = _dir.Combine("out") }, new Options.Options()).Run();

        using var b = Open("b.docx");
        var paragraphs = Paragraphs(b);
        Assert.Contains("Content of B", paragraphs);
        Assert.DoesNotContain("Content of A", paragraphs);
        Assert.Single(b.MainDocumentPart!.FooterParts);
    }
}
