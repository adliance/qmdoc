using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Tests.Helpers;

namespace Adliance.QmDoc.Tests.Processors.MarkdownProcessors;

public sealed class LinkToDocumentsTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private MarkdownProcessorContext Link(string markdown, string fileName = "docs/doc.md", string baseDirectory = "docs")
    {
        var path = _dir.Write(fileName, markdown);
        return new LinkToDocuments(_dir.Combine(baseDirectory), path).Apply(Md.Context(markdown));
    }

    private static string DocumentLink(string name, string fileName) => $"<span class=\"link-to-document\"><i></i>[{name}]({fileName})</span>";

    [Fact]
    public void Apply_LinksToMarkdownFileNextToDocument()
    {
        _dir.Write("docs/other.md", "Other");
        var context = Link("See [other.md].");
        Assert.Equal($"See {DocumentLink("other", "other.html")}.", context.Markdown);
        Assert.Empty(context.Errors);
        var linked = Assert.Single(context.LinkedDocuments);
        Assert.Equal("other.html", linked.FileName);
        Assert.Equal("other", linked.NiceName);
    }

    [Fact]
    public void Apply_LinksToOtherFileTypes()
    {
        _dir.Write("docs/data.xlsx", "x");
        Assert.Equal(DocumentLink("data", "data.xlsx"), Link("[data.xlsx]").Markdown);
    }

    [Fact]
    public void Apply_FileNameWithSpaces()
    {
        _dir.Write("docs/my document.md", "x");
        Assert.Equal(DocumentLink("my document", "my%20document.html"), Link("[my document.md]").Markdown);
    }

    [Fact]
    public void Apply_FileNameWithEncodedSpaces()
    {
        _dir.Write("docs/my document.md", "x");
        Assert.Equal(DocumentLink("my document", "my%20document.html"), Link("[my%20document.md]").Markdown);
    }

    [Fact]
    public void Apply_FileInSubdirectory()
    {
        _dir.Write("docs/sub/other.md", "x");
        Assert.Equal(DocumentLink("other", "sub/other.html"), Link("[sub/other.md]").Markdown);
    }

    [Fact]
    public void Apply_FileFoundInOtherDirectory_LinksToRelativePath()
    {
        _dir.Write("other/target.md", "x");
        Assert.Equal(DocumentLink("target", "../other/target.html"), Link("[target.md]").Markdown);
    }

    [Fact]
    public void Apply_MissingFile_ReportsError()
    {
        var context = Link("[missing.md]");
        Assert.Equal("[missing.md]", context.Markdown);
        Assert.Equal("Unable to find a document \"missing.md\", but there's a reference to it.", Assert.Single(context.Errors).ErrorMessage);
        Assert.Empty(context.LinkedDocuments);
    }

    [Fact]
    public void Apply_EmailAddresses_AreIgnored()
    {
        var context = Link("[hannes@example.com]");
        Assert.Equal("[hannes@example.com]", context.Markdown);
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void Apply_DocumentCode_LinksToDocumentStartingWithCode()
    {
        _dir.Write("docs/qm/ABC-123 Verfahrensanweisung.md", "x");
        var context = Link("See [ABC-123].");
        Assert.Equal($"See {DocumentLink("ABC-123 Verfahrensanweisung", "qm/ABC-123%20Verfahrensanweisung.html")}.", context.Markdown);
        Assert.Empty(context.Errors);
        Assert.Single(context.LinkedDocuments);
    }

    [Fact]
    public void Apply_DocumentCode_IsCaseInsensitive()
    {
        _dir.Write("docs/abc-123 x.md", "x");
        Assert.Contains("(abc-123%20x.html)", Link("[ABC-123]").Markdown);
    }

    [Fact]
    public void Apply_DocumentCode_PrefersMarkdownFiles()
    {
        _dir.Write("docs/ABC-123 a.pdf", "x");
        _dir.Write("docs/ABC-123 b.md", "x");
        Assert.Contains("(ABC-123%20b.html)", Link("[ABC-123]").Markdown);
    }

    [Fact]
    public void Apply_DocumentCode_FallsBackToOtherFiles()
    {
        _dir.Write("docs/ABC-123 a.pdf", "x");
        Assert.Equal(DocumentLink("ABC-123 a", "ABC-123%20a.pdf"), Link("[ABC-123]").Markdown);
    }

    [Fact]
    public void Apply_DocumentCode_RelativeToDocumentInSubdirectory()
    {
        _dir.Write("docs/b/ABC-123 x.md", "x");
        Assert.Contains("(../b/ABC-123%20x.html)", Link("[ABC-123]", "docs/a/doc.md").Markdown);
    }

    [Fact]
    public void Apply_DocumentCode_UsedMultipleTimes()
    {
        _dir.Write("docs/ABC-123 x.md", "x");
        var context = Link("[ABC-123] and [ABC-123]");
        Assert.Equal($"{DocumentLink("ABC-123 x", "ABC-123%20x.html")} and {DocumentLink("ABC-123 x", "ABC-123%20x.html")}", context.Markdown);
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void Apply_UnknownDocumentCode_ReportsError()
    {
        var context = Link("[XYZ-999]");
        Assert.Equal("[XYZ-999]", context.Markdown);
        Assert.Equal("Unable to find a document \"XYZ-999\", but there's a referenced document number to it.", Assert.Single(context.Errors).ErrorMessage);
    }

    [Theory]
    [InlineData("[AB-123]")]
    [InlineData("[ABC-12]")]
    [InlineData("[ABC_123]")]
    public void Apply_NoDocumentCode_IsIgnored(string text)
    {
        var context = Link(text);
        Assert.Equal(text, context.Markdown);
        Assert.Empty(context.Errors);
    }

    [Theory]
    [InlineData("![chart.png](chart.png)")]
    [InlineData("[other.md](other.md)")]
    [InlineData("[see example.com](https://example.com)")]
    [InlineData("[Text][other.md]")]
    [InlineData("[other.md][1]")]
    [InlineData("[other.md]: other.md")]
    [InlineData("   [other.md]: other.md")]
    [InlineData("    [other.md]: in an indented code block")]
    [InlineData("Text\n[other.md]: other.md")]
    [InlineData("[ABC-123](https://example.com)")]
    [InlineData("![ABC-123](abc.png)")]
    public void Apply_DoesNotTouchRegularLinksAndImages(string markdown)
    {
        _dir.Write("docs/chart.png", "x");
        _dir.Write("docs/other.md", "x");
        _dir.Write("docs/ABC-123 x.md", "x");
        var context = Link(markdown);
        Assert.Equal(markdown, context.Markdown);
        Assert.Empty(context.Errors);
        Assert.Empty(context.LinkedDocuments);
    }

    [Theory]
    [InlineData("See [other.md]: it explains everything.")]
    public void Apply_ColonAfterLinkThatIsNoDefinition(string markdown)
    {
        _dir.Write("docs/other.md", "x");
        Assert.Contains(DocumentLink("other", "other.html") + ":", Link(markdown).Markdown);
    }

    [Fact]
    public void Apply_SameFileAsDocumentLinkAndRegularLink()
    {
        _dir.Write("docs/other.md", "x");
        Assert.Equal($"{DocumentLink("other", "other.html")} and [other.md](other.md)", Link("[other.md] and [other.md](other.md)").Markdown);
    }

    [Fact]
    public void Apply_IgnoresCode()
    {
        var markdown = "`[missing.md]` `[XYZ-999]`\n\n```\n[missing.md]\n```";
        var context = Link(markdown);
        Assert.Equal(markdown, context.Markdown);
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void Apply_FileInParentDirectory()
    {
        _dir.Write("other/x.md", "x");
        Assert.Equal(DocumentLink("x", "../other/x.html"), Link("[../other/x.md]").Markdown);
    }

    [Fact(Skip = "Bug: texts in square brackets that end with a dot and 1-4 characters (like \"[v1.2]\") are reported as missing documents")]
    public void Apply_VersionNumbers_AreNotReported()
    {
        Assert.Empty(Link("[v1.2]").Errors);
    }
}
