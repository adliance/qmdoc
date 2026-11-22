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

    [Theory]
    [InlineData("{!}", "D6E4F0")]
    [InlineData("{!!}", "FCE4D6")]
    [InlineData("{?}", "FFF2CC")]
    public async Task Run_RendersSingleBraceCallouts(string placeholder, string fill)
    {
        using var document = await Convert($"{placeholder} Callout text");
        var paragraph = ParagraphWithText(document, "Callout text");
        Assert.Equal(fill, paragraph.ParagraphProperties?.Shading?.Fill?.Value);
    }

    [Theory(Skip = "Bug: the documented callouts \"{{!}}\", \"{{!!}}\" and \"{{?}}\" are not rendered in DOCX, only \"{!}\" etc. with single braces")]
    [InlineData("{{!}}", "D6E4F0")]
    [InlineData("{{!!}}", "FCE4D6")]
    [InlineData("{{?}}", "FFF2CC")]
    public async Task Run_RendersCallouts(string placeholder, string fill)
    {
        using var document = await Convert($"{placeholder} Callout text");
        Assert.Contains("Callout text", Paragraphs(document));
        var paragraph = ParagraphWithText(document, "Callout text");
        Assert.Equal(fill, paragraph.ParagraphProperties?.Shading?.Fill?.Value);
    }

    [Fact]
    public async Task Run_RendersAuthorLine()
    {
        using var document = await Convert("Max Muster | ACME GmbH | max@example.com");
        Assert.Contains("Max Muster, ACME GmbHmax@example.com", Paragraphs(document)); // name and company, line break, email
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
