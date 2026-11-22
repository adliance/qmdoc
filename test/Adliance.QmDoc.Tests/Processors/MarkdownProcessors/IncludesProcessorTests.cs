using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Tests.Helpers;
using static Adliance.QmDoc.Tests.Helpers.Md;

namespace Adliance.QmDoc.Tests.Processors.MarkdownProcessors;

public sealed class IncludesProcessorTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private MarkdownProcessorContext Process(string fileName, string markdown)
    {
        var path = _dir.Write(fileName, markdown);
        return new IncludesProcessor(path, "2026").Apply(Md.Context(markdown));
    }

    [Theory]
    [InlineData("{{ include partial.md }}")]
    [InlineData("{{include partial.md}}")]
    [InlineData("{{ INCLUDE ./partial.md }}")]
    [InlineData("{{   include    partial.md   }}")]
    public void Apply_IncludesFile(string placeholder)
    {
        _dir.Write("partial.md", "Included");
        var context = Process("doc.md", $"Before\n\n{placeholder}\n\nAfter");
        Assert.Equal("Before\n\nIncluded\n\nAfter", context.Markdown);
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void Apply_IncludesFileInSubdirectory()
    {
        _dir.Write("includes/partial.md", "Included");
        Assert.Equal("Included", Process("doc.md", "{{ include ./includes/partial.md }}").Markdown);
    }

    [Fact]
    public void Apply_IncludesFileWithAbsolutePath()
    {
        var path = _dir.Write("elsewhere/partial.md", "Included");
        Assert.Equal("Included", Process("doc.md", $"{{{{ include {path} }}}}").Markdown);
    }

    [Fact]
    public void Apply_IncludesFileWithSpacesInPath()
    {
        _dir.Write("my includes/my partial.md", "Included");
        Assert.Equal("Included", Process("doc.md", "{{ include my includes/my partial.md }}").Markdown);
    }

    [Fact]
    public void Apply_NestedIncludes_AreRelativeToTheIncludingFile()
    {
        _dir.Write("includes/a.md", "A {{ include nested/b.md }}");
        _dir.Write("includes/nested/b.md", "B {{ include ../c.md }}");
        _dir.Write("includes/c.md", "C");
        Assert.Equal("A B C", Process("doc.md", "{{ include includes/a.md }}").Markdown);
    }

    [Fact]
    public void Apply_SameFileCanBeIncludedMultipleTimes()
    {
        _dir.Write("partial.md", "X");
        var context = Process("doc.md", "{{ include partial.md }} {{ include partial.md }}");
        Assert.Equal("X X", context.Markdown);
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void Apply_NormalizesLineEndings()
    {
        _dir.Write("partial.md", "Line 1\r\nLine 2");
        Assert.Equal("Line 1\nLine 2", Process("doc.md", "{{ include partial.md }}").Markdown);
    }

    [Fact]
    public void Apply_MissingInclude_KeepsPlaceholderAndReportsError()
    {
        var context = Process("doc.md", "{{ include missing.md }}");
        Assert.Equal("{{ include missing.md }}", context.Markdown);
        var error = Assert.Single(context.Errors);
        Assert.Equal("Unable to find an include file \"missing.md\", referenced from \"doc.md\".", error.ErrorMessage);
        Assert.Equal(_dir.Combine("doc.md"), error.FilePath);
    }

    [Fact]
    public void Apply_MissingNestedInclude_ReportsIncludingFile()
    {
        _dir.Write("a.md", "{{ include missing.md }}");
        var error = Assert.Single(Process("doc.md", "{{ include a.md }}").Errors);
        Assert.Equal("Unable to find an include file \"missing.md\", referenced from \"a.md\".", error.ErrorMessage);
        Assert.Equal(_dir.Combine("a.md"), error.FilePath);
    }

    [Fact]
    public void Apply_SelfInclude_ReportsError()
    {
        var context = Process("doc.md", "A {{ include doc.md }}");
        Assert.Equal("A ", context.Markdown);
        Assert.Equal("Circular include detected for \"doc.md\", referenced from \"doc.md\".", Assert.Single(context.Errors).ErrorMessage);
    }

    [Fact]
    public void Apply_CircularInclude_ReportsError()
    {
        _dir.Write("a.md", "A {{ include b.md }}");
        _dir.Write("b.md", "B {{ include a.md }}");
        var context = Process("doc.md", "{{ include a.md }}");
        Assert.Equal("A B ", context.Markdown);
        Assert.Equal("Circular include detected for \"a.md\", referenced from \"b.md\".", Assert.Single(context.Errors).ErrorMessage);
    }

    [Fact]
    public void Apply_LanguageDocument_PrefersLanguageSpecificInclude()
    {
        _dir.Write("partial.md", "English");
        _dir.Write("partial.de.md", "Deutsch");
        Assert.Equal("Deutsch", Process("report.de.md", "{{ include partial.md }}").Markdown);
        Assert.Equal("English", Process("report.md", "{{ include partial.md }}").Markdown);
    }

    [Fact]
    public void Apply_LanguageDocument_FallsBackToDefaultInclude()
    {
        _dir.Write("partial.md", "English");
        Assert.Equal("English", Process("report.fr.md", "{{ include partial.md }}").Markdown);
    }

    [Fact]
    public void Apply_LanguageDocument_WithRegionAndSubdirectory()
    {
        _dir.Write("includes/partial.md", "Deutsch");
        _dir.Write("includes/partial.de-at.md", "Österreichisch");
        Assert.Equal("Österreichisch", Process("report.de-at.md", "{{ include includes/partial.md }}").Markdown);
    }

    [Fact]
    public void Apply_LanguageDocument_AppliesLanguageToNestedIncludes()
    {
        _dir.Write("a.md", "{{ include b.md }}");
        _dir.Write("b.md", "English");
        _dir.Write("b.de.md", "Deutsch");
        Assert.Equal("Deutsch", Process("report.de.md", "{{ include a.md }}").Markdown);
    }

    [Theory]
    [InlineData("my.document.md")] // "document" is too long to be a language code
    [InlineData("report.d.md")]
    [InlineData("report.de_AT.md")]
    public void Apply_FileNameWithoutLanguageCode_UsesDefaultInclude(string fileName)
    {
        _dir.Write("partial.md", "Default");
        _dir.Write("partial.document.md", "Wrong");
        _dir.Write("partial.d.md", "Wrong");
        _dir.Write("partial.de_AT.md", "Wrong");
        Assert.Equal("Default", Process(fileName, "{{ include partial.md }}").Markdown);
    }

    [Fact]
    public void Apply_IgnoresIncludesInInlineCode()
    {
        _dir.Write("partial.md", "Included");
        var context = Process("doc.md", "`{{ include partial.md }}` {{ include partial.md }}");
        Assert.Equal("`{{ include partial.md }}` Included", context.Markdown);
    }

    [Fact]
    public void Apply_IgnoresIncludesInCodeBlocks()
    {
        var markdown = Lines("```markdown", "{{ include missing.md }}", "```");
        var context = Process("doc.md", markdown);
        Assert.Equal(markdown, context.Markdown);
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void Apply_IgnoresIncludesInCodeOfIncludedFiles()
    {
        _dir.Write("partial.md", "`{{ include other.md }}`");
        _dir.Write("other.md", "Other");
        Assert.Equal("`{{ include other.md }}`", Process("doc.md", "{{ include partial.md }}").Markdown);
    }
}
