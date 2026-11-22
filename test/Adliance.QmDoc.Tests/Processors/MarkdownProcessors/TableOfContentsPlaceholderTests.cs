using Adliance.AspNetCore.Buddy.Pdf;
using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Tests.Helpers;
using static Adliance.QmDoc.Tests.Helpers.Md;

namespace Adliance.QmDoc.Tests.Processors.MarkdownProcessors;

public class TableOfContentsPlaceholderTests
{
    private static string[] TocRows(string markdown) => TocRows(Md.Context(markdown));

    private static string[] TocRows(MarkdownProcessorContext context)
    {
        var result = new TableOfContentsPlaceholder().Apply(context).Markdown;
        return result.Split('\n').Where(l => l.StartsWith("| [") || l.StartsWith("| &nbsp;")).ToArray();
    }

    private static PdfMetadata.OutlineData Outline(string title, int page, params PdfMetadata.OutlineData[] children) => new() { Title = title, Page = page, Children = children };

    [Fact]
    public void Apply_RendersTableInsideTocDiv()
    {
        var result = new TableOfContentsPlaceholder().Apply(Lines("{{ TOC }}", "", "# A")).Markdown;
        Assert.Equal(Lines("<div class=\"toc\">", "", "| | |", "|-|-:|", "| [A](#a) |  |", "", "</div>", "", "", "# A"), result);
    }

    [Fact]
    public void Apply_IndentsByLevelAndSkipsLevel6()
    {
        var rows = TocRows(Lines("{{ TOC }}", "# A", "## B", "### C", "#### D", "##### E", "###### F"));
        Assert.Equal(new[]
        {
            "| [A](#a) |  |",
            "| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[B](#b) |  |",
            "| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[C](#c) |  |",
            "| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[D](#d) |  |",
            "| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[E](#e) |  |"
        }, rows);
    }

    [Fact]
    public void Apply_UsesIdsAssignedByMarkdig()
    {
        var rows = TocRows(Lines("{{ TOC }}", "# 1. Einleitung", "# Open Questions", "# Open Questions", "# Élimination"));
        Assert.Equal(new[]
        {
            "| [1. Einleitung](#einleitung) |  |",
            "| [Open Questions](#open-questions) |  |",
            "| [Open Questions](#open-questions-1) |  |",
            "| [Élimination](#élimination) |  |"
        }, rows);
    }

    [Fact]
    public void Apply_SkipsEmptyHeadings()
    {
        Assert.Equal(new[] { "| [A](#a) |  |" }, TocRows(Lines("{{ TOC }}", "#", "# A")));
    }

    [Fact]
    public void Apply_UsesPageNumbersFromPdfOutline()
    {
        var context = Md.Context(Lines("{{ TOC }}", "# A", "## Open Questions", "# B", "## Open Questions", "# Not in PDF"));
        context.ResetForSecondPass(new PdfMetadata
        {
            Outline =
            [
                Outline("A", 2, Outline("Open Questions", 3)),
                Outline("B", 4, Outline("Open Questions", 6))
            ]
        });

        Assert.Equal(new[]
        {
            "| [A](#a) | 2 |",
            "| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[Open Questions](#open-questions) | 3 |",
            "| [B](#b) | 4 |",
            "| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[Open Questions](#open-questions-1) | 6 |",
            "| [Not in PDF](#not-in-pdf) |  |"
        }, TocRows(context));
    }

    [Fact]
    public void Apply_PageNumberMatchingIgnoresSpacesAndHyphens()
    {
        var context = Md.Context(Lines("{{ TOC }}", "# Pre‐Processing Step"));
        context.ResetForSecondPass(new PdfMetadata { Outline = [Outline("PreProcessing  Step", 7)] });
        Assert.EndsWith(" | 7 |", Assert.Single(TocRows(context)));
    }

    [Fact]
    public void Apply_PageNumbersOfLevel6HeadingsAreCountedForDuplicates()
    {
        var context = Md.Context(Lines("{{ TOC }}", "###### X", "# X"));
        context.ResetForSecondPass(new PdfMetadata { Outline = [Outline("X", 1), Outline("X", 5)] });
        Assert.Equal(new[] { "| [X](#x-1) | 5 |" }, TocRows(context));
    }

    [Fact(Skip = "Bug: inline code in headings is dropped from the TOC (only literal text is used)")]
    public void Apply_KeepsInlineCodeInHeadings()
    {
        var rows = TocRows(Lines("{{ TOC }}", "# Install `qmdoc`"));
        Assert.Contains("qmdoc", Assert.Single(rows).Split("](")[0]);
    }

    [Fact]
    public void ContainsTocPlaceholder()
    {
        Assert.True(TableOfContentsPlaceholder.ContainsTocPlaceholder(Md.Context("{{toc}}")));
        Assert.False(TableOfContentsPlaceholder.ContainsTocPlaceholder(Md.Context("TOC")));
    }

    [Fact]
    public void Apply_WithoutPlaceholder_DoesNothing()
    {
        Assert.Equal("# A", new TableOfContentsPlaceholder().Apply("# A").Markdown);
    }
}
