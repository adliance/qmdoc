using System.Diagnostics;
using Adliance.QmDoc.Converter;
using Block = Adliance.QmDoc.Converter.LineDiff.Block;

namespace Adliance.QmDoc.Tests.Converter;

public class LineDiffTests
{
    [Fact]
    public void Diff_EqualLines_ReturnsNoBlocks()
    {
        Assert.Empty(LineDiff.Diff(["a", "", "b"], ["a", "", "b"]));
    }

    [Fact]
    public void Diff_ChangedLine_ReturnsReplaceBlock()
    {
        Assert.Equal([new Block(1, 1, 1, 1)], LineDiff.Diff(["a", "b", "c"], ["a", "x", "c"]));
    }

    [Fact]
    public void Diff_InsertedAndDeletedLines_ReturnsBlocksWithZeroCounts()
    {
        Assert.Equal([new Block(1, 0, 1, 2), new Block(2, 1, 4, 0)], LineDiff.Diff(["a", "b", "c", "d"], ["a", "x", "y", "b", "d"]));
    }

    [Fact]
    public void Diff_MovedLines_MatchesLongestSequenceInSameOrder()
    {
        // "c" moved before "a": "a" and "b" stay matched, "c" is deleted and inserted
        Assert.Equal([new Block(0, 0, 0, 1), new Block(2, 1, 3, 0)], LineDiff.Diff(["a", "b", "c"], ["c", "a", "b"]));
    }

    [Fact]
    public void Diff_PlaceholderWithEmptyLines_KeepsInsertedLinesTogether()
    {
        // only empty lines are in both, the empty line between the placeholders belongs after the inserted lines
        string[] source = ["{{ TOC }}", "", "## Git"];
        string[] processed = ["| A |", "", "| B |", "", "## 1. Git"];
        Assert.Equal([new Block(0, 1, 0, 3), new Block(2, 1, 4, 1)], LineDiff.Diff(source, processed));
    }

    [Fact]
    public void Diff_CarriageReturns_AreIgnored()
    {
        Assert.Empty(LineDiff.Diff(LineDiff.SplitLines("a\r\nb\r\n"), LineDiff.SplitLines("a\nb\n")));
    }

    [Fact]
    public void Diff_LargeDocuments_IsFast()
    {
        // repeated lines without unique anchors are too large for the longest common subsequence and are treated as changed
        var source = Enumerable.Range(0, 20_000).Select(i => i % 3 == 0 ? "" : $"Line {i}").ToArray();
        var processed = source.Select(l => l.Length > 0 && l.EndsWith('7') ? l + " changed" : l).Concat(Enumerable.Repeat("x", 5_000)).ToArray();

        var stopwatch = Stopwatch.StartNew();
        var blocks = LineDiff.Diff(source, processed);
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"Diff took {stopwatch.ElapsedMilliseconds} ms");
        Assert.Equal(processed.Length - source.Length, blocks.Sum(b => b.InsertCountB - b.DeleteCountA));
    }
}
