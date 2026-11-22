using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Tests.Helpers;

namespace Adliance.QmDoc.Tests.Processors.MarkdownProcessors;

public class GitVersionsPlaceholderTests
{
    [Fact]
    public void Apply_RendersTableOfChanges()
    {
        var changes = new List<GitService.Change>
        {
            GitDatePlaceholderTests.Change,
            new() { Author = "Max", Date = new DateTimeOffset(2025, 12, 24, 8, 0, 0, TimeSpan.Zero), Sha = "fedcba9876543210", Message = "Initial", MessageShort = "Initial" }
        };

        var lines = new GitVersionsPlaceholder(changes).Apply("{{ GIT_VERSIONS }}").Markdown.Split('\n');

        Assert.Equal(4, lines.Length);
        Assert.Equal("| Datum | Person | Version | Änderung | ", lines[0]);
        Assert.Equal("|-|-|-|-|", lines[1]);
        Assert.Equal("| 05.&nbsp;03.&nbsp;2026 | Hannes&nbsp;Sachsenhofer | 0123456 | Fix typo <span class=\"git-version-details\"> In chapter 2.</span> |", lines[2]);
        Assert.Equal("| 24.&nbsp;12.&nbsp;2025 | Max | fedcba9 | Initial |", lines[3]);
    }

    [Fact]
    public void Apply_RendersValidMarkdownTable()
    {
        var context = new GitVersionsPlaceholder([GitDatePlaceholderTests.Change]).Apply("{{ GIT_VERSIONS }}");
        var html = Markdig.Markdown.ToHtml(context.Markdown, context.Pipeline);
        Assert.Contains("<table>", html);
        Assert.Contains("<td>0123456</td>", html);
    }

    [Fact]
    public void Apply_EmptyMessage_RendersBalancedHtml()
    {
        var change = new GitService.Change { Author = "Max", Date = DateTimeOffset.Now, Sha = "fedcba9876543210", Message = "", MessageShort = "" };
        var result = new GitVersionsPlaceholder([change]).Apply("{{ GIT_VERSIONS }}").Markdown;
        Assert.DoesNotContain("span", result);
        Assert.EndsWith("| fedcba9 |  |", result);
    }

    [Fact]
    public void Apply_MultiLineDetails_AreJoinedWithSpaces()
    {
        var change = new GitService.Change { Author = "Max", Date = DateTimeOffset.Now, Sha = "fedcba9876543210", Message = "Summary\n\nLine 1\r\nLine 2\n\n- Item", MessageShort = "Summary" };
        var result = new GitVersionsPlaceholder([change]).Apply("{{ GIT_VERSIONS }}").Markdown;
        Assert.EndsWith("| Summary <span class=\"git-version-details\"> Line 1 Line 2 - Item</span> |", result);
    }

    [Fact]
    public void Apply_MessageNotStartingWithShortMessage_HasNoDetails()
    {
        var change = new GitService.Change { Author = "Max", Date = DateTimeOffset.Now, Sha = "fedcba9876543210", Message = "Something else entirely", MessageShort = "Summary" };
        Assert.EndsWith("| Summary |", new GitVersionsPlaceholder([change]).Apply("{{ GIT_VERSIONS }}").Markdown);
    }

    [Fact]
    public void Apply_WithoutChanges_RendersWarning()
    {
        Assert.Equal("{{!}} Dieses Dokument befindet sich nicht in Versionskontrolle.", new GitVersionsPlaceholder([]).Apply("{{ GIT_VERSIONS }}").Markdown);
    }
}
