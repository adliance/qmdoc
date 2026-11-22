using Adliance.AspNetCore.Buddy.Pdf.V2;
using Adliance.QmDoc.Converter;
using Adliance.QmDoc.Parameters;
using Adliance.QmDoc.Tests.Helpers;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Actions;
using UglyToad.PdfPig.Annotations;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Outline;
using static Adliance.QmDoc.Tests.Helpers.Md;

namespace Adliance.QmDoc.Tests.Converter;

/// <summary>
/// Integration tests of the Markdown to PDF conversion: the PDFs are rendered by the actual PDF service (https://pdf2.adliance.dev),
/// so these tests need internet access. Run without them with: dotnet test -- --filter-not-trait "Category=Integration"
/// </summary>
[Trait("Category", "Integration")]
public sealed class PdfConverterTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private async Task<PdfDocument> Convert(string markdown, string fileName = "doc.md", Action<PdfParameters>? configure = null)
    {
        _dir.Write("src/" + fileName, markdown);
        await Run(_dir.Combine("src/" + fileName), configure);
        return Open(Path.ChangeExtension(fileName, ".pdf"));
    }

    private async Task Run(string source, Action<PdfParameters>? configure = null)
    {
        var parameters = new PdfParameters { Source = source, Target = _dir.Combine("out") };
        configure?.Invoke(parameters);
        await new PdfConverter(parameters, new Options.Options()).Run();
    }

    private PdfDocument Open(string fileName) => PdfDocument.Open(File.ReadAllBytes(_dir.Combine("out/" + fileName)));

    private static string Text(Page page) => string.Join(" ", page.GetWords().Select(w => w.Text));

    /// <summary>
    /// All characters of the page, also the ones of rotated text (eg. the watermark), which are not grouped into words.
    /// </summary>
    private static string Letters(Page page) => string.Concat(page.Letters.Select(l => l.Value));

    private static double TopOf(Page page, string word) => page.GetWords().First(w => w.Text == word).BoundingBox.Top;

    private static readonly string ThreePages = Lines(
        "---",
        "title: Probe Titel",
        "---",
        "{{ TOC }}",
        "",
        "---",
        "",
        "# Einleitung",
        "",
        "Hallo Welt.",
        "",
        "---",
        "",
        "# Kosten",
        "",
        "## Details",
        "",
        "Siehe [#Einleitung].");

    [Fact]
    public async Task Run_CreatesA4PdfWithTitle()
    {
        using var pdf = await Convert(Lines("---", "title: Kostenschätzung", "---", "# Einleitung"));

        Assert.Equal(1, pdf.NumberOfPages);
        Assert.Equal("Kostenschätzung", pdf.Information.Title);
        var page = pdf.GetPage(1);
        Assert.Equal(595.3, page.Width, 1.0); // A4 in points
        Assert.Equal(841.9, page.Height, 1.0);
    }

    [Fact]
    public async Task Run_ContainsTheProcessedMarkdown()
    {
        using var pdf = await Convert(Lines("---", "title: Angebot", "Kunde: ACME", "---", "# Einleitung", "", "Für {{ Kunde }}: {{ = 6 * 7 }} Stunden."));

        var text = Text(pdf.GetPage(1));
        Assert.Contains("Angebot", text); // the document title
        Assert.Contains("1. Einleitung", text);
        Assert.Contains("Für ACME: 42 Stunden.", text);
        Assert.DoesNotContain("{{", text);
    }

    [Fact]
    public async Task Run_PageBreaksStartNewPages()
    {
        using var pdf = await Convert(ThreePages);

        Assert.Equal(3, pdf.NumberOfPages);
        Assert.Contains("Hallo Welt.", Text(pdf.GetPage(2)));
        Assert.Contains("2.1. Details", Text(pdf.GetPage(3)));
    }

    [Fact]
    public async Task Run_TocShowsThePageNumbersOfTheChapters()
    {
        using var pdf = await Convert(ThreePages);
        Assert.Contains("1. Einleitung 2 2. Kosten 3 2.1. Details 3", Text(pdf.GetPage(1)));
    }

    [Fact]
    public async Task Run_CreatesBookmarksForAllHeadings()
    {
        using var pdf = await Convert(ThreePages);

        Assert.True(pdf.TryGetBookmarks(out var bookmarks));
        var nodes = bookmarks.GetNodes().OfType<DocumentBookmarkNode>().Select(b => (b.Title, b.Level, b.PageNumber)).ToList();
        Assert.Equal([("1. Einleitung", 0, 2), ("2. Kosten", 0, 3), ("2.1. Details", 1, 3)], nodes);
    }

    [Fact]
    public async Task Run_ChapterLinksJumpToTheChapter()
    {
        using var pdf = await Convert(ThreePages);

        var link = Assert.Single(pdf.GetPage(3).GetAnnotations(), a => a.Type == AnnotationType.Link);
        var goTo = Assert.IsAssignableFrom<AbstractGoToAction>(link.Action);
        Assert.Equal(2, goTo.Destination.PageNumber);
    }

    [Fact]
    public async Task Run_LinksToWebPagesStayAsTheyAre()
    {
        using var pdf = await Convert("Siehe [Doku](https://example.com/page.html).");
        Assert.Equal(["https://example.com/page.html"], pdf.GetPage(1).GetHyperlinks().Select(h => h.Uri));
    }

    [Fact]
    public async Task Run_FooterShowsTitleAndPageNumbersOnEveryPage()
    {
        using var pdf = await Convert(ThreePages);

        foreach (var page in pdf.GetPages())
        {
            Assert.EndsWith($"Probe Titel {page.Number} | 3", Text(page));
        }
    }

    [Fact]
    public async Task Run_HeaderShowsTheLogo()
    {
        using var pdf = await Convert("# A");
        Assert.Single(pdf.GetPage(1).GetImages());
    }

    [Fact]
    public async Task Run_EnableHeaderFalse_HidesTheHeader()
    {
        using var pdf = await Convert(Lines("---", "enableHeader: false", "---", "# A"));
        Assert.Empty(pdf.GetPage(1).GetImages());
    }

    [Fact]
    public async Task Run_EnableFooterFalse_HidesTheFooter()
    {
        using var pdf = await Convert(Lines("---", "enableFooter: false", "---", "# A"));
        Assert.DoesNotContain("1 | 1", Text(pdf.GetPage(1)));
    }

    [Fact]
    public async Task Run_StampsTheWatermarkOnEveryPage()
    {
        using var pdf = await Convert(Lines("---", "watermark: Entwurf", "---", "A", "", "---", "", "B"));

        Assert.Equal(2, pdf.NumberOfPages);
        Assert.All(pdf.GetPages(), page => Assert.Contains("Entwurf", Letters(page)));
    }

    [Fact]
    public async Task Run_WithoutWatermark_HasNoWatermark()
    {
        using var pdf = await Convert("Text");
        Assert.DoesNotContain("Entwurf", Letters(pdf.GetPage(1)));
    }

    [Fact]
    public async Task Run_FrontmatterSetsThePageSize()
    {
        // pdfWidth and pdfHeight are given in CSS pixels (96 per inch), the PDF uses points (72 per inch)
        using var pdf = await Convert(Lines("---", "pdfWidth: 800", "pdfHeight: 1200", "---", "Text"));

        var page = pdf.GetPage(1);
        Assert.Equal(600, page.Width, 2.0);
        Assert.Equal(900, page.Height, 2.0);
    }

    [Fact]
    public async Task Run_ThemeParameter_UsesHeaderAndFooterOfTheTheme()
    {
        using var defaultTheme = await Convert("Marker", "default.md");
        using var madx = await Convert("Marker", "madx.md", p => p.Theme = "madx");

        Assert.Contains("MacroArray Diagnostics", Text(madx.GetPage(1)));
        Assert.DoesNotContain("MacroArray Diagnostics", Text(defaultTheme.GetPage(1)));
        Assert.True(TopOf(madx.GetPage(1), "Marker") < TopOf(defaultTheme.GetPage(1), "Marker") - 20, "The header of madx is higher, so the text must start further down.");
    }

    [Fact]
    public async Task Run_FrontmatterTheme_UsesTheHeaderHeightOfThatTheme()
    {
        using var themeParameter = await Convert("Marker", "parameter.md", p => p.Theme = "madx");
        using var frontmatterTheme = await Convert(Lines("---", "theme: madx", "---", "Marker"), "frontmatter.md", p => p.Theme = "2026");

        Assert.Contains("MacroArray Diagnostics", Text(frontmatterTheme.GetPage(1)));
        Assert.Equal(TopOf(themeParameter.GetPage(1), "Marker"), TopOf(frontmatterTheme.GetPage(1), "Marker"), 0.5);
    }

    [Fact]
    public async Task Run_RendersMermaidDiagrams()
    {
        using var pdf = await Convert(Lines("```mermaid", "flowchart LR", "    A[Importieren] --> B[Validieren]", "```"));

        var letters = Letters(pdf.GetPage(1));
        Assert.Contains("Importieren", letters);
        Assert.Contains("Validieren", letters);
        Assert.DoesNotContain("flowchart", letters);
    }

    [Fact]
    public async Task Run_EmbedsLocalImages()
    {
        _dir.Write("src/pixel.png", System.Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="));
        using var pdf = await Convert(Lines("---", "enableHeader: false", "---", "![Pixel](pixel.png)"));
        Assert.Single(pdf.GetPage(1).GetImages());
    }

    [Fact]
    public async Task Run_ConvertsAllFilesOfADirectory()
    {
        _dir.Write("src/a.md", "# A");
        _dir.Write("src/b.md", "# B");
        await Run(_dir.Combine("src"));

        using var a = Open("a.pdf");
        using var b = Open("b.pdf");
        Assert.Equal("a", a.Information.Title);
        Assert.Equal("b", b.Information.Title);
    }

    [Fact]
    public async Task Run_ServiceNotReachable_FailsAndWritesNoFile()
    {
        var source = _dir.Write("src/doc.md", "# A");
        var parameters = new PdfParameters { Source = source, Target = _dir.Combine("out") };
        var converter = new PdfConverter(parameters, new Options.Options(), timeoutSeconds => new AdliancePdfer(new DefaultPdferConfiguration { ServerUrl = "http://127.0.0.1:9", TimeoutSeconds = timeoutSeconds }));

        await Assert.ThrowsAsync<HttpRequestException>(converter.Run);
        Assert.False(_dir.Exists("out/doc.pdf"));
    }

    [Fact]
    public void AdliancePdferSettings_UsesTheAdliancePdfService()
    {
        var settings = new AdliancePdferSettings { TimeoutSeconds = 42 };
        Assert.Equal("https://pdf2.adliance.dev", settings.ServerUrl);
        Assert.Equal(42, settings.TimeoutSeconds);
        Assert.Null(settings.ApiKeyPdf);
    }

    [Fact]
    public void GetWatermarkHtml_ShrinksFontForLongTexts()
    {
        Assert.Contains("font-size=\"40\"", PdfConverter.GetWatermarkHtml("Entwurf"));
        Assert.Contains("font-size=\"15.64\"", PdfConverter.GetWatermarkHtml("Internal Draft - do not publish"));
        Assert.Contains(">Entwurf &lt;intern&gt;</text>", PdfConverter.GetWatermarkHtml("Entwurf <intern>"));
    }
}
