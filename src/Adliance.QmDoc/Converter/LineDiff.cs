using System;
using System.Collections.Generic;
using System.Linq;

namespace Adliance.QmDoc.Converter;

/// <summary>
/// A line diff (patience diff) to map the lines of the processed Markdown back to the source (see <see cref="SourceLines"/>).
/// Equal lines at the start and end are matched first, then the lines that occur exactly once in both texts (in the same order),
/// and the lines between them are diffed the same way. Small ranges without such lines (eg. only empty lines) use a longest common subsequence.
/// </summary>
internal static class LineDiff
{
    // the longest common subsequence needs a table of this many cells, larger ranges are treated as changed
    private const long MaxLcsCells = 1_000_000;

    /// <summary>
    /// Lines A[DeleteStartA..] were replaced by B[InsertStartB..], one of the counts may be zero (only inserted or only deleted).
    /// </summary>
    internal readonly record struct Block(int DeleteStartA, int DeleteCountA, int InsertStartB, int InsertCountB);

    public static string[] SplitLines(string text) => text.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();

    public static List<Block> Diff(IReadOnlyList<string> a, IReadOnlyList<string> b)
    {
        var ids = new Dictionary<string, int>(StringComparer.Ordinal);
        var x = a.Select(l => Id(ids, l)).ToArray();
        var y = b.Select(l => Id(ids, l)).ToArray();

        var matches = new List<(int A, int B)>();
        Match(x, 0, x.Length, y, 0, y.Length, matches);
        matches.Add((x.Length, y.Length));

        var blocks = new List<Block>();
        int aLine = 0, bLine = 0;
        foreach (var (matchA, matchB) in matches)
        {
            if (matchA > aLine || matchB > bLine) blocks.Add(new Block(aLine, matchA - aLine, bLine, matchB - bLine));
            aLine = matchA + 1;
            bLine = matchB + 1;
        }

        return blocks;
    }

    private static int Id(Dictionary<string, int> ids, string line)
    {
        if (!ids.TryGetValue(line, out var id)) ids[line] = id = ids.Count;
        return id;
    }

    /// <summary>
    /// Adds the matching lines of a[aStart..aEnd] and b[bStart..bEnd] in order.
    /// </summary>
    private static void Match(int[] a, int aStart, int aEnd, int[] b, int bStart, int bEnd, List<(int A, int B)> matches)
    {
        while (aStart < aEnd && bStart < bEnd && a[aStart] == b[bStart]) matches.Add((aStart++, bStart++));

        var suffix = 0;
        while (aStart < aEnd - suffix && bStart < bEnd - suffix && a[aEnd - suffix - 1] == b[bEnd - suffix - 1]) suffix++;
        aEnd -= suffix;
        bEnd -= suffix;

        if (aStart < aEnd && bStart < bEnd)
        {
            var anchors = UniqueAnchors(a, aStart, aEnd, b, bStart, bEnd);
            if (anchors.Count > 0)
            {
                foreach (var (anchorA, anchorB) in anchors)
                {
                    Match(a, aStart, anchorA, b, bStart, anchorB, matches);
                    matches.Add((anchorA, anchorB));
                    aStart = anchorA + 1;
                    bStart = anchorB + 1;
                }

                Match(a, aStart, aEnd, b, bStart, bEnd, matches);
            }
            else if ((long)(aEnd - aStart) * (bEnd - bStart) <= MaxLcsCells)
            {
                LongestCommonSubsequence(a, aStart, aEnd, b, bStart, bEnd, matches);
            }
        }

        for (var i = 0; i < suffix; i++) matches.Add((aEnd + i, bEnd + i));
    }

    /// <summary>
    /// The lines that occur exactly once in both ranges, reduced to the longest sequence that has the same order in both.
    /// </summary>
    private static List<(int A, int B)> UniqueAnchors(int[] a, int aStart, int aEnd, int[] b, int bStart, int bEnd)
    {
        var occurrences = new Dictionary<int, (int CountA, int IndexA, int CountB, int IndexB)>();
        for (var i = aStart; i < aEnd; i++)
        {
            occurrences.TryGetValue(a[i], out var o);
            occurrences[a[i]] = o with { CountA = o.CountA + 1, IndexA = i };
        }

        for (var i = bStart; i < bEnd; i++)
        {
            if (!occurrences.TryGetValue(b[i], out var o)) continue;
            occurrences[b[i]] = o with { CountB = o.CountB + 1, IndexB = i };
        }

        var candidates = occurrences.Values
            .Where(o => o.CountA == 1 && o.CountB == 1)
            .Select(o => (A: o.IndexA, B: o.IndexB))
            .OrderBy(c => c.B)
            .ToList();

        // longest increasing subsequence of the A indexes (patience sorting)
        var tails = new List<int>();
        var previous = new int[candidates.Count];
        for (var i = 0; i < candidates.Count; i++)
        {
            int low = 0, high = tails.Count;
            while (low < high)
            {
                var middle = (low + high) / 2;
                if (candidates[tails[middle]].A < candidates[i].A) low = middle + 1;
                else high = middle;
            }

            previous[i] = low > 0 ? tails[low - 1] : -1;
            if (low == tails.Count) tails.Add(i);
            else tails[low] = i;
        }

        var result = new List<(int A, int B)>();
        for (var i = tails.Count > 0 ? tails[^1] : -1; i >= 0; i = previous[i]) result.Add(candidates[i]);
        result.Reverse();
        return result;
    }

    private static void LongestCommonSubsequence(int[] a, int aStart, int aEnd, int[] b, int bStart, int bEnd, List<(int A, int B)> matches)
    {
        int n = aEnd - aStart, m = bEnd - bStart;
        var lengths = new int[n + 1, m + 1];
        for (var i = n - 1; i >= 0; i--)
        {
            for (var j = m - 1; j >= 0; j--)
            {
                lengths[i, j] = a[aStart + i] == b[bStart + j] ? lengths[i + 1, j + 1] + 1 : Math.Max(lengths[i + 1, j], lengths[i, j + 1]);
            }
        }

        // if there are multiple options (eg. an empty line in the source and several in the processed lines), skip processed lines first,
        // so that inserted lines stay together (eg. "{{ TOC }}\n\n## Git" maps the empty line to the one after the table of contents)
        int x = 0, y = 0;
        while (x < n && y < m)
        {
            if (a[aStart + x] == b[bStart + y]) matches.Add((aStart + x++, bStart + y++));
            else if (lengths[x, y + 1] >= lengths[x + 1, y]) y++;
            else x++;
        }
    }
}
