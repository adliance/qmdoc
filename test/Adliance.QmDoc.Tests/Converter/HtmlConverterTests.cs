using Adliance.QmDoc.Parameters;
using Adliance.QmDoc.Tests.Helpers;
using LibGit2Sharp;
using static Adliance.QmDoc.Tests.Helpers.Md;

namespace Adliance.QmDoc.Tests.Converter;

/// <summary>
/// End-to-end tests of the Markdown to HTML conversion (which runs the same Markdown processors as the PDF conversion).
/// </summary>
public sealed class HtmlConverterTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private async Task<(string Html, CapturingHtmlConverter Converter)> Convert(string markdown, string fileName = "doc.md", Action<HtmlParameters>? configure = null, bool saveMarkdown = false)
    {
        var source = _dir.Write("src/" + fileName, markdown);
        var parameters = new HtmlParameters { Source = source, Target = _dir.Combine("out") };
        configure?.Invoke(parameters);
        var converter = new CapturingHtmlConverter(parameters, saveMarkdown);
        await converter.Run();
        return (_dir.Read("out/" + Path.ChangeExtension(fileName, ".html")), converter);
    }

    private static readonly string FullDocument = Lines(
        "---",
        "title: Kostenschätzung",
        "author: Hannes Sachsenhofer",
        "Stundensatz: 100 €/h",
        "clientName: ACME",
        "---",
        "",
        "{{ TOC }}",
        "",
        "# Einleitung",
        "",
        "Für {{ clientName }}, siehe [#Kosten]. Dokument: {{ TITLE }}.",
        "",
        "{{!}} Ein Hinweis.",
        "",
        "- {{?}} Eine Frage",
        "",
        "{{ include partials/legal.md }}",
        "",
        "# Kosten",
        "",
        "| Position | Stunden | Kosten |",
        "|---|--:|--:|",
        "| Konzept | 10h | {{ = [Stunden] * Stundensatz }} |",
        "| Umsetzung | 90min | {{ = [Stunden] * Stundensatz }} |",
        "| **Summe** | {{ = SUM }} | **{{ = SUM }}** |",
        "",
        "Kunde: {{ CUSTOMER }}",
        "",
        "![Logo](logo.png)",
        "",
        "Siehe auch [other.md].",
        "",
        "Code: `{{ = 1 + 1 }}`",
        "",
        "---",
        "",
        "# Anhang");

    private async Task<(string Html, CapturingHtmlConverter Converter)> ConvertFullDocument()
    {
        _dir.Write("src/partials/legal.md", "## Rechtliches\n\nText");
        _dir.Write("src/placeholders.json", """{ "CUSTOMER": "Medizinische Universität Wien" }""");
        _dir.Write("src/other.md", "# Other");
        _dir.Write("src/logo.png", [1, 2, 3]);
        return await Convert(FullDocument, configure: p => p.PlaceholdersFile = "placeholders.json");
    }

    [Fact]
    public async Task Run_FullDocument_HasNoErrors()
    {
        var (_, converter) = await ConvertFullDocument();
        Assert.Empty(converter.SingleContext.Errors.Select(e => e.ErrorMessage));
    }

    [Fact]
    public async Task Run_FullDocument_ReplacesAllPlaceholders()
    {
        var (html, _) = await ConvertFullDocument();
        Assert.DoesNotContain("{{", html.Replace("<code>{{ = 1 + 1 }}</code>", ""));
        Assert.Contains("<code>{{ = 1 + 1 }}</code>", html);
        Assert.Contains("Für ACME", html);
        Assert.Contains("Dokument: Kostenschätzung.", html);
        Assert.Contains("Kunde: Medizinische Universität Wien", html);
    }

    [Fact]
    public async Task Run_FullDocument_UsesThemeLayout()
    {
        var (html, converter) = await ConvertFullDocument();
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<title>Kostenschätzung</title>", html);
        Assert.Contains("<div class=\"document-title\">Kostenschätzung</div>", html);
        Assert.Contains("<style>", html);
        Assert.Contains("startOnLoad: true", html);
        Assert.DoesNotContain("data-placeholder-for", html);
        Assert.Equal("2026", converter.SingleContext.Theme);
    }

    [Fact]
    public async Task Run_FullDocument_AddsAuthorLine()
    {
        var (html, _) = await ConvertFullDocument();
        Assert.Contains("<div class=\"document-author\"><span class=\"name\">Hannes Sachsenhofer</span>, Adliance GmbH</div>", html);
    }

    [Fact]
    public async Task Run_FullDocument_NumbersHeadingsIncludingIncludes()
    {
        var (html, _) = await ConvertFullDocument();
        Assert.Contains(">1. Einleitung</h1>", html);
        Assert.Contains(">1.1. Rechtliches</h2>", html);
        Assert.Contains(">2. Kosten</h1>", html);
        Assert.Contains(">3. Anhang</h1>", html);
    }

    [Fact]
    public async Task Run_FullDocument_RendersTocAndChapterLinks()
    {
        var (html, _) = await ConvertFullDocument();
        Assert.Contains("<div class=\"toc\">", html);
        Assert.Contains("<a href=\"#einleitung\">1. Einleitung</a>", html);
        Assert.Contains("<a href=\"#rechtliches\">1.1. Rechtliches</a>", html);
        Assert.Contains("<span class=\"link-to-chapter\"><i></i><a href=\"#kosten\">2. Kosten</a></span>", html);
    }

    [Fact]
    public async Task Run_FullDocument_CalculatesTable()
    {
        var (html, _) = await ConvertFullDocument();
        Assert.Contains($"<td style=\"text-align: right;\">1.000{Nbsp}€</td>", html);
        Assert.Contains($"<td style=\"text-align: right;\">150{Nbsp}€</td>", html);
        Assert.Contains("<td style=\"text-align: right;\">11,5h</td>", html);
        Assert.Contains($"<td style=\"text-align: right;\"><strong>1.150{Nbsp}€</strong></td>", html);
    }

    [Fact]
    public async Task Run_FullDocument_RendersCalloutsImagesLinksAndPageBreaks()
    {
        var (html, _) = await ConvertFullDocument();
        Assert.Contains("<div class=\"block block-alert\">Ein Hinweis.</div>", html);
        Assert.Contains("<li><i class=\"fad fa-question-circle\"></i>Eine Frage</li>", html);
        Assert.Contains($"<img src=\"data:image/png;base64,{System.Convert.ToBase64String([1, 2, 3])}\"", html);
        Assert.Contains("<span class=\"link-to-document\"><i></i><a href=\"other.pdf\">other</a></span>", html); // links to other documents always point to the PDF
        Assert.Contains("<div style=\"page-break-after: always;\"></div>", html);
    }

    [Fact]
    public async Task Run_SaveMarkdown_WritesProcessedMarkdown()
    {
        var (_, converter) = await Convert(Lines("---", "title: T", "---", "# A", "", "{{ = 2h * 3 }} {{ TITLE }}"), saveMarkdown: true);
        var markdown = _dir.Read("out/doc.md");
        Assert.Equal(converter.SingleContext.Markdown, markdown);
        Assert.Contains("# 1. A", markdown);
        Assert.Contains("6h T", markdown);
    }

    [Fact]
    public async Task Run_SaveMarkdownIntoSourceDirectory_Throws()
    {
        var source = _dir.Write("src/doc.md", "# A");
        var converter = new CapturingHtmlConverter(new HtmlParameters { Source = source, Target = _dir.Combine("src") }, saveMarkdown: true);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(converter.Run);
        Assert.Contains("would overwrite the source file", ex.Message);
        Assert.Equal("# A", _dir.Read("src/doc.md"));
    }

    [Fact]
    public async Task Run_TitleFromFileName()
    {
        var (html, _) = await Convert("{{ TITLE }}", "My Document.md");
        Assert.Contains("<title>My Document</title>", html);
        Assert.Contains("<p>My Document</p>", html);
    }

    [Fact]
    public async Task Run_TitleParameterOverridesFrontmatter()
    {
        var (html, _) = await Convert(Lines("---", "title: Frontmatter", "---", "{{ TITLE }}"), configure: p => p.Title = "Parameter");
        Assert.Contains("<title>Parameter</title>", html);
        Assert.Contains("<p>Parameter</p>", html);
    }

    [Fact]
    public async Task Run_DisabledDocumentTitleAndHeaderNumbering()
    {
        var (html, _) = await Convert(Lines("---", "enableDocumentTitle: false", "enableHeaderNumbering: false", "---", "# A"));
        Assert.DoesNotContain("<div class=\"document-title\">", html);
        Assert.Contains(">A</h1>", html);
    }

    [Theory]
    [InlineData("theme: madx", "madx")]
    [InlineData("theme: _shared", "2026")] // internal themes can't be used
    [InlineData("title: no theme", "2026")]
    public async Task Run_ThemeFromFrontmatter(string frontmatter, string expectedTheme)
    {
        var (_, converter) = await Convert(Lines("---", frontmatter, "---", "Text"));
        Assert.Equal(expectedTheme, converter.SingleContext.Theme);
    }

    [Fact]
    public async Task Run_ThemeFromParameter()
    {
        var (_, converter) = await Convert("Text", configure: p => p.Theme = "madx");
        Assert.Equal("madx", converter.SingleContext.Theme);
    }

    [Fact]
    public async Task Run_FrontmatterThemeOverridesParameter()
    {
        var (_, converter) = await Convert(Lines("---", "theme: madx", "---", "Text"), configure: p => p.Theme = "2026");
        Assert.Equal("madx", converter.SingleContext.Theme);
    }

    [Fact]
    public async Task Run_FrontmatterValueInsideCalculation_IsReplacedBeforeCalculating()
    {
        var (html, converter) = await Convert(Lines("---", "Faktor: 3", "---", "{{ = 2 * {{ Faktor }} }} and {{ = 2 * Faktor }}"));
        Assert.Contains("<p>6 and 6</p>", html);
        Assert.Empty(converter.SingleContext.Errors);
    }

    [Fact]
    public async Task Run_FrontmatterValueInTableCell_CanBeReferenced()
    {
        var markdown = Lines("---", "Stundensatz: 135 €/h", "---", "| Position | Stunden | Satz | Kosten |", "|---|---|---|---|", "| a | 2h | {{ Stundensatz }} | {{ = [Stunden] * [Satz] }} |");
        var (html, converter) = await Convert(markdown);
        Assert.Contains($"<td>270{Nbsp}€</td>", html);
        Assert.Empty(converter.SingleContext.Errors);
    }

    [Fact]
    public async Task Run_CalculationInIncludedFile_UsesFrontmatterOfDocument()
    {
        _dir.Write("src/partial.md", "{{ = 2h * Stundensatz }}");
        var (html, _) = await Convert(Lines("---", "Stundensatz: 10€/h", "---", "{{ include partial.md }}"));
        Assert.Contains("<p>20€</p>", html);
    }

    [Fact]
    public async Task Run_TableInBlockquote_IsCalculated()
    {
        var (html, converter) = await Convert(Lines("> | A | B |", "> |---|---|", "> | x | 2 |", "> | y | {{ = [B:x] * 2 }} |"));
        Assert.Contains("<td>4</td>", html);
        Assert.Empty(converter.SingleContext.Errors);
    }

    [Fact]
    public async Task Run_TocWithFrontmatterPlaceholderInHeading()
    {
        var (html, _) = await Convert(Lines("---", "clientName: ACME", "---", "{{ TOC }}", "", "# Angebot für {{ clientName }}"));
        Assert.Contains("id=\"angebot-für-acme\"", html);
        Assert.Contains("<a href=\"#angebot-f%C3%BCr-acme\">1. Angebot für ACME</a>", html);
    }

    [Fact]
    public async Task Run_TocWithCalculationInHeading()
    {
        var (html, _) = await Convert(Lines("{{ TOC }}", "", "# Kosten {{ = 2 * 3 }}"));
        Assert.Contains("id=\"kosten-6\"", html);
        Assert.Contains("<a href=\"#kosten-6\">1. Kosten 6</a>", html);
    }

    [Fact]
    public async Task Run_ChapterLinkToHeadingWithPlaceholderAndCalculation()
    {
        var (html, converter) = await Convert(Lines("---", "clientName: ACME", "---", "# Kosten für {{ clientName }}: {{ = 2 * 3 }}€", "", "[#Kosten für ACME]"));
        Assert.Contains("id=\"kosten-für-acme-6\"", html);
        Assert.Contains("<span class=\"link-to-chapter\"><i></i><a href=\"#kosten-f%C3%BCr-acme-6\">1. Kosten für ACME: 6€</a></span>", html);
        Assert.Empty(converter.SingleContext.Errors);
    }

    [Fact]
    public async Task Run_PageBreaksAroundTextAtBeginningOfDocument()
    {
        var (html, converter) = await Convert(Lines("---", "", "Deckblatt", "", "---", "", "# A"));
        Assert.Equal(2, html.Split("<div style=\"page-break-after: always;\"></div>").Length - 1);
        Assert.Contains("<p>Deckblatt</p>", html);
        Assert.Contains(">1. A</h1>", html);
        Assert.Empty(converter.SingleContext.Errors);
    }

    [Fact]
    public async Task Run_EmptyFrontmatter_HasNoPageBreaks()
    {
        var (html, _) = await Convert(Lines("---", "---", "# A"));
        Assert.DoesNotContain("<div style=\"page-break-after: always;\">", html);
        Assert.Contains(">1. A</h1>", html);
    }

    [Fact]
    public async Task Run_CodeExamples_AreLeftAlone()
    {
        var markdown = Lines(
            "# A",
            "",
            "```markdown",
            "---",
            "{{ include missing.md }}",
            "[#A] [missing.md] [XYZ-999] {{ = 1 + 1 }}",
            "```",
            "",
            "`[#A]` `{{ include missing.md }}`");
        var (html, converter) = await Convert(markdown);
        Assert.Contains("<code class=\"language-markdown\">---\n{{ include missing.md }}\n[#A] [missing.md] [XYZ-999] {{ = 1 + 1 }}\n</code>", html);
        Assert.Contains("<code>[#A]</code> <code>{{ include missing.md }}</code>", html);
        Assert.DoesNotContain("<div style=\"page-break-after: always;\">", html);
        Assert.Empty(converter.SingleContext.Errors);
    }

    [Fact]
    public async Task Run_DollarSignsInDocument_StayAsTheyAre()
    {
        var (html, _) = await Convert(Lines("---", "Preis: $0 / $$", "---", "{{ Preis }}", "", "`echo $$ $0 $&`"));
        Assert.Contains("<p>$0 / $$</p>", html);
        Assert.Contains("<code>echo $$ $0 $&amp;</code>", html);
    }

    [Fact]
    public async Task Run_ImageLinkWithFileNameAsText_IsEmbedded()
    {
        _dir.Write("src/my chart.png", [1, 2, 3]);
        _dir.Write("src/other.md", "x");
        var (html, converter) = await Convert("![my chart.png](<my chart.png>) [other.md](other.md)");
        Assert.Contains($"<img src=\"data:image/png;base64,{System.Convert.ToBase64String([1, 2, 3])}\" alt=\"my chart.png\" />", html);
        Assert.Contains("<a href=\"other.md\">other.md</a>", html); // regular links stay regular links
        Assert.Empty(converter.SingleContext.Errors);
        Assert.Empty(converter.SingleContext.LinkedDocuments);
    }

    [Fact]
    public async Task Run_CalculationErrors_AreReportedAndFormulaStays()
    {
        var (html, converter) = await Convert("{{ = 1h + 1€ }}");
        Assert.Contains("<p>{{ = 1h + 1€ }}</p>", html);
        Assert.Equal("Calculation \"{{ = 1h + 1€ }}\": Cannot add \"h\" and \"€\".", Assert.Single(converter.SingleContext.Errors).ErrorMessage);
    }

    [Fact]
    public async Task Run_GitPlaceholders_OutsideOfRepository()
    {
        var (html, _) = await Convert("[{{ GIT_VERSION }}] [{{ GIT_DATE }}] [{{ GIT_DATE_VERSION }}]\n\n{{ GIT_VERSIONS }}");
        Assert.Contains("<p>[] [] []</p>", html);
        Assert.Contains("<div class=\"block block-alert\">Dieses Dokument befindet sich nicht in Versionskontrolle.</div>", html);
    }

    [Fact]
    public async Task Run_GitPlaceholders_InsideOfRepository()
    {
        using var repo = new Repository(Repository.Init(_dir.Path));
        var signature = new Signature("Hannes", "test@example.com", new DateTimeOffset(2026, 3, 5, 12, 0, 0, TimeSpan.Zero));
        _dir.Write("README.md", "x");
        Commands.Stage(repo, "README.md");
        repo.Commit("Initial", signature, signature);
        _dir.Write("src/doc.md", "{{ GIT_VERSION }}, {{ GIT_DATE }}");
        Commands.Stage(repo, "src/doc.md");
        var commit = repo.Commit("Add document", signature, signature);

        var (html, _) = await Convert("{{ GIT_VERSION }}, {{ GIT_DATE }}");

        Assert.Contains($"<p>{commit.Sha[..7]}, 05. März 2026</p>", html);
    }

    [Fact]
    public async Task Run_Directory_ConvertsAllMarkdownFiles()
    {
        _dir.Write("src/a.md", "# A");
        _dir.Write("src/b.md", "# B");
        _dir.Write("src/sub/c.md", "# C");
        _dir.Write("src/ignored.txt", "x");

        var converter = new CapturingHtmlConverter(new HtmlParameters { Source = _dir.Combine("src"), Target = _dir.Combine("out") });
        await converter.Run();

        Assert.Equal(["a.md", "b.md"], converter.Results.Select(r => r.File.SourceRelativePath));
        Assert.True(_dir.Exists("out/a.html"));
        Assert.True(_dir.Exists("out/b.html"));
        Assert.False(_dir.Exists("out/sub/c.html"));
    }

    [Fact]
    public async Task Run_DirectoryWithSubdirectories_KeepsStructure()
    {
        _dir.Write("src/a.md", "# A");
        _dir.Write("src/sub/c.md", "# C");

        var converter = new CapturingHtmlConverter(new HtmlParameters { Source = _dir.Combine("src") + Path.DirectorySeparatorChar, Target = _dir.Combine("out"), IncludeSubdirectories = true });
        await converter.Run();

        Assert.Equal(["a.md", Path.Combine("sub", "c.md")], converter.Results.Select(r => r.File.SourceRelativePath));
        Assert.Contains(">1. C</h1>", _dir.Read("out/sub/c.html"));
    }

    [Fact]
    public async Task Run_MultipleFiles_DoNotShareState()
    {
        _dir.Write("src/a.md", "---\nStundensatz: 10€/h\n---\n{{ = 1h * Stundensatz }}");
        _dir.Write("src/b.md", "{{ = 1h * Stundensatz }}");

        var converter = new CapturingHtmlConverter(new HtmlParameters { Source = _dir.Combine("src"), Target = _dir.Combine("out") });
        await converter.Run();

        Assert.Contains("<p>10€</p>", _dir.Read("out/a.html"));
        Assert.Contains("<p>{{ = 1h * Stundensatz }}</p>", _dir.Read("out/b.html"));
        Assert.Empty(converter.Results[0].Context.Errors);
        Assert.Single(converter.Results[1].Context.Errors);
    }

    [Fact]
    public async Task Run_ConvertsExamples()
    {
        var examples = Path.Combine(FindRepositoryRoot(), "examples", "input");
        foreach (var file in Directory.GetFiles(examples, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(examples, file);
            _dir.Write("src/" + relative, File.ReadAllBytes(file));
        }

        var converter = new CapturingHtmlConverter(new HtmlParameters { Source = _dir.Combine("src"), Target = _dir.Combine("out") }, saveMarkdown: true);
        await converter.Run();

        Assert.Equal(3, converter.Results.Count);
        foreach (var (file, context) in converter.Results)
        {
            Assert.True(_dir.Exists("out/" + Path.ChangeExtension(file.SourceRelativePath, ".html")));
            Assert.DoesNotContain("{{ include ./", context.Markdown);
            Assert.Empty(context.Errors.Select(e => e.ErrorMessage));
        }

        Assert.Contains("Dies ist die deutsche Übersetzung", _dir.Read("out/multi-language-example.de.md"));
        Assert.DoesNotContain("Dies ist die deutsche Übersetzung", _dir.Read("out/multi-language-example.md"));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "QmDoc.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Unable to find the repository root.");
    }
}
