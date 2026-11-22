using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Adliance.QmDoc.Extensions;
using Adliance.QmDoc.Processors.MarkdownProcessors.Calculation;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

/// <summary>
/// Replaces calculations with their results:
/// <list type="bullet">
/// <item>"{{ = 145h * 2€ }}" calculates a formula, names without brackets refer to frontmatter values ("{{ = 145h * Stundensatz }}").</item>
/// <item>Inside tables, "[Kosten]" refers to the "Kosten" column of the same row, "[Kosten:3]" to its third row,
/// "[Kosten:Summe netto]" to the row whose first cell is "Summe netto" and "[3:1]" to the third column of the first row.</item>
/// <item>"SUM" in a table cell ("{{ = SUM }}", "{{ = SUM * 20% }}") adds up all values above it in the same column, except other "{{ = SUM }}" cells.</item>
/// </list>
/// Numbers are parsed and formatted with the culture set in the frontmatter ("culture: de-AT"), default is "de-DE".
/// </summary>
public partial class CalculationPlaceholders(string filePath) : IMarkdownProcessor
{
    private static readonly CultureInfo DefaultCulture = CultureInfo.GetCultureInfo("de-DE");

    private const string SumKeyword = "SUM";

    /// <summary>
    /// Every calculation starts with "=", just like in Excel.
    /// </summary>
    [GeneratedRegex(@"\{\{\s*=(?<formula>[^\n]*?)\s*\}\}")]
    private static partial Regex PlaceholderRegex();

    /// <summary>
    /// Looks like a calculation, but the "=" is missing.
    /// </summary>
    [GeneratedRegex(@"\{\{\s*SUM\s*\}\}", RegexOptions.IgnoreCase)]
    private static partial Regex SumWithoutEqualsRegex();

    [GeneratedRegex(@"[*_~`]")]
    private static partial Regex FormattingRegex();

    /// <summary>
    /// Chapter links "[#Title]", links "[Text](url)" and images "![Text](url)".
    /// </summary>
    [GeneratedRegex(@"!?\[#?(?<text>[^\]]*)\](\([^)]*\))?")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        var markdown = markdownContext.Markdown;
        var matches = PlaceholderRegex().Matches(markdown);
        var sumsWithoutEquals = SumWithoutEqualsRegex().Matches(markdown);
        if (matches.Count == 0 && sumsWithoutEquals.Count == 0) return markdownContext;

        var document = Markdig.Markdown.Parse(markdown, markdownContext.Pipeline);
        var codeSpans = CodeSpans.Find(document);
        bool IsInCode(Match m) => codeSpans.Contains(m.Index); // leave code blocks alone

        foreach (var match in sumsWithoutEquals.Where(m => !IsInCode(m)))
        {
            markdownContext.Errors.Add(new ProcessorError(filePath, $"\"{match.Value}\" is not calculated, calculations must start with \"=\", like \"{{{{ = SUM }}}}\"."));
        }

        var placeholders = matches.Where(m => !IsInCode(m)).Select(m => new Placeholder(m)).ToList();
        if (placeholders.Count == 0) return markdownContext;

        foreach (var table in document.Descendants<Table>()) TableModel.Build(table, markdown, placeholders);

        var evaluator = new Evaluator(GetCulture(markdownContext), markdownContext.Frontmatter.Custom);
        foreach (var placeholder in placeholders)
        {
            try
            {
                evaluator.Evaluate(placeholder);
            }
            catch (CalculationException ex)
            {
                // leave the placeholder in the document, so that nobody overlooks a missing value
                markdownContext.Errors.Add(new ProcessorError(filePath, $"Calculation \"{placeholder.Match.Value}\": {ex.Message}"));
            }
        }

        // if one amount of money in a table column has cents, show all calculated amounts of that column with cents
        var currencyDecimalsPerColumn = placeholders
            .Where(p => p is { Cell: not null, Result.Unit.ContainsCurrency: true })
            .GroupBy(p => (p.Cell!.Table, p.Cell.Column))
            .ToDictionary(g => g.Key, g => g.Max(p => p.Result!.DisplayDecimals));

        var result = new StringBuilder(markdown);
        foreach (var placeholder in placeholders.Where(p => p.Result != null).OrderByDescending(x => x.Match.Index))
        {
            var minimumDecimals = placeholder.Cell != null && placeholder.Result!.Unit.ContainsCurrency ? currencyDecimalsPerColumn[(placeholder.Cell.Table, placeholder.Cell.Column)] : 0;
            result.Remove(placeholder.Match.Index, placeholder.Match.Length);
            result.Insert(placeholder.Match.Index, placeholder.Result!.Format(evaluator.Culture, minimumDecimals));
        }
        markdownContext.Markdown = result.ToString();
        return markdownContext;
    }

    private CultureInfo GetCulture(MarkdownProcessorContext markdownContext)
    {
        var name = markdownContext.Frontmatter.Culture;
        if (string.IsNullOrWhiteSpace(name)) return DefaultCulture;

        // ICU accepts any well-formed name (eg. "xx-YY") and silently falls back to invariant formatting, so check against the known cultures
        var culture = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(c => c.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
        if (culture != null && !string.IsNullOrEmpty(culture.Name)) return culture;

        markdownContext.Errors.Add(new ProcessorError(filePath, $"Culture \"{name}\" is unknown, using \"{DefaultCulture.Name}\" instead."));
        return DefaultCulture;
    }

    /// <summary>
    /// Removes Markdown formatting (like "**12h**"), reduces links to their text ("[#Projektmeldung]" becomes "Projektmeldung")
    /// and normalizes whitespace, so that headers, row labels and values can be compared.
    /// </summary>
    private static string Normalize(string text)
    {
        text = LinkRegex().Replace(text, "${text}");
        text = FormattingRegex().Replace(text, "");
        return WhitespaceRegex().Replace(text, " ").Trim();
    }

    private sealed class Placeholder(Match match)
    {
        public Match Match { get; } = match;
        public string Formula { get; } = match.Groups["formula"].Value.Trim();

        /// <summary>
        /// A cell that is just "{{ = SUM }}" (a total or subtotal), these cells are skipped by other sums.
        /// </summary>
        public bool IsSum => Formula.Equals(SumKeyword, StringComparison.OrdinalIgnoreCase);
        public CellModel? Cell { get; set; }

        public Quantity? Result { get; set; }
        public string? Error { get; set; }
        public bool IsEvaluating { get; set; }
    }

    private sealed class TableModel
    {
        private readonly List<string> _headers = [];
        public List<List<CellModel>> Rows { get; } = [];

        public static void Build(Table table, string markdown, List<Placeholder> placeholders)
        {
            var model = new TableModel();
            var headerDone = false;
            foreach (var row in table.OfType<TableRow>())
            {
                var cells = row.OfType<TableCell>().ToList();
                if (row.IsHeader)
                {
                    // only the first header row names the columns, placeholders in headers can't use references
                    if (!headerDone) model._headers.AddRange(cells.Select(c => Normalize(GetText(c, markdown))));
                    headerDone = true;
                    continue;
                }

                var rowIndex = model.Rows.Count;
                var rowModel = new List<CellModel>();
                for (var columnIndex = 0; columnIndex < cells.Count; columnIndex++)
                {
                    var cell = cells[columnIndex];
                    var cellModel = new CellModel(model, rowIndex, columnIndex, GetText(cell, markdown));
                    if (cell.Span.End >= cell.Span.Start)
                    {
                        foreach (var placeholder in placeholders.Where(p => p.Match.Index >= cell.Span.Start && p.Match.Index <= cell.Span.End))
                        {
                            placeholder.Cell = cellModel;
                            cellModel.Placeholders.Add(placeholder);
                        }
                    }

                    rowModel.Add(cellModel);
                }

                model.Rows.Add(rowModel);
            }
        }

        private static string GetText(TableCell cell, string markdown) => cell.Span.End >= cell.Span.Start ? markdown[cell.Span.Start..(cell.Span.End + 1)] : "";

        /// <summary>
        /// Finds the column by its header (see <see cref="Find"/>).
        /// </summary>
        public int FindColumn(string reference)
        {
            var columns = _headers.Count > 0 ? _headers : Enumerable.Repeat("", Rows.Count > 0 ? Rows.Max(r => r.Count) : 0).ToList();
            return Find(columns, reference, "column", "columns",
                $"the columns are {string.Join(", ", _headers.Select(h => $"\"{h}\""))}. Frontmatter values are written without square brackets.");
        }

        /// <summary>
        /// Finds the row by the text in its first cell (see <see cref="Find"/>), the header row is not counted.
        /// </summary>
        public int FindRow(string reference)
        {
            var labels = Rows.Select(r => r.Count > 0 ? Normalize(r[0].Text) : "").ToList();
            return Find(labels, reference, "row", "rows", "the first cell of the row must contain this text.");
        }

        /// <summary>
        /// Finds a column or row, trying (in this order):
        /// 1. the exact text (ignoring upper/lower case, formatting and links),
        /// 2. the (1-based) number,
        /// 3. the beginning of the text, or else a part of the text, if it only matches a single column or row.
        /// </summary>
        private static int Find(List<string> names, string reference, string kind, string kindPlural, string notFoundHint)
        {
            var normalized = Normalize(reference);

            var exact = IndexesWhere(names, n => n.Equals(normalized, StringComparison.OrdinalIgnoreCase));
            if (exact.Count == 1) return exact[0];
            if (exact.Count > 1) throw new CalculationException($"There are multiple {kindPlural} named \"{normalized}\", please use the {kind} number instead.");

            if (int.TryParse(normalized, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            {
                if (number >= 1 && number <= names.Count) return number - 1;
                throw new CalculationException($"There is no {kind} {number}, the table has {names.Count} {kindPlural}{(kind == "row" ? " (not counting the header)" : "")}.");
            }

            var partial = LenientMatching.FindPartial(names, normalized);
            if (partial.Count == 1) return partial[0];
            if (partial.Count > 1)
            {
                var candidates = string.Join(", ", partial.Select(i => $"\"{names[i]}\""));
                throw new CalculationException($"\"{normalized}\" matches multiple {kindPlural} ({candidates}), please use more of the text or the {kind} number.");
            }

            throw new CalculationException($"There is no {kind} \"{normalized}\", {notFoundHint}");
        }

        private static List<int> IndexesWhere(List<string> names, Func<string, bool> predicate) => names.Select((n, i) => (Name: n, Index: i)).Where(x => predicate(x.Name)).Select(x => x.Index).ToList();
    }

    private sealed class CellModel(TableModel table, int row, int column, string text)
    {
        public TableModel Table { get; } = table;
        public int Row { get; } = row;
        public int Column { get; } = column;
        public string Text { get; } = text;
        public List<Placeholder> Placeholders { get; } = [];
        public bool IsSum => Placeholders.Any(p => p.IsSum);
    }

    private sealed class Evaluator(CultureInfo culture, IReadOnlyDictionary<string, string> frontmatter)
    {
        private static readonly HashSet<string> EmptyValues = ["", "-", "–", "—"];
        private readonly Dictionary<string, Quantity> _frontmatterCache = new(StringComparer.OrdinalIgnoreCase);

        public CultureInfo Culture { get; } = culture;

        public Quantity Evaluate(Placeholder placeholder)
        {
            if (placeholder.Result != null) return placeholder.Result;
            if (placeholder.Error != null) throw new CalculationException(placeholder.Error);
            if (placeholder.IsEvaluating) throw new CalculationException("The calculation refers to itself (circular reference).");

            placeholder.IsEvaluating = true;
            try
            {
                placeholder.Result = ExpressionParser.Evaluate(placeholder.Formula, Culture, (column, row) => ResolveReference(placeholder, column, row), name => ResolveName(placeholder, name));
                return placeholder.Result;
            }
            catch (CalculationException ex)
            {
                placeholder.Error = ex.Message;
                throw;
            }
            finally
            {
                placeholder.IsEvaluating = false;
            }
        }

        private Quantity Sum(Placeholder placeholder)
        {
            var cell = placeholder.Cell ?? throw new CalculationException("SUM only works inside table cells (not in the header).");

            Quantity? result = null;
            for (var row = 0; row < cell.Row; row++)
            {
                var other = cell.Table.Rows[row].ElementAtOrDefault(cell.Column);
                if (other == null || other.IsSum) continue;

                Quantity? value;
                try
                {
                    value = GetCellValue(other);
                }
                catch (CalculationException ex)
                {
                    throw new CalculationException($"Row {row + 1}: {ex.Message}");
                }

                if (value == null) continue;
                result = result == null ? value : result.Add(value);
            }

            return result ?? throw new CalculationException("There are no values above to add up.");
        }

        private Quantity ResolveReference(Placeholder placeholder, string columnReference, string? rowReference)
        {
            var cell = placeholder.Cell ?? throw new CalculationException($"References like \"[{columnReference}]\" only work inside table cells (not in the header).");
            var description = rowReference == null ? $"[{columnReference}]" : $"[{columnReference}:{rowReference}]";

            int column, row;
            try
            {
                column = cell.Table.FindColumn(columnReference);
                row = rowReference == null ? cell.Row : cell.Table.FindRow(rowReference);
            }
            catch (CalculationException ex)
            {
                throw new CalculationException($"{description}: {ex.Message}");
            }

            var target = cell.Table.Rows[row].ElementAtOrDefault(column);
            if (target == cell) throw new CalculationException($"{description} refers to its own cell.");

            try
            {
                return (target == null ? null : GetCellValue(target)) ?? throw new CalculationException("The cell is empty.");
            }
            catch (CalculationException ex)
            {
                throw new CalculationException($"{description}: {ex.Message}");
            }
        }

        private Quantity ResolveName(Placeholder placeholder, string name)
        {
            if (name.Equals(SumKeyword, StringComparison.OrdinalIgnoreCase)) return Sum(placeholder);
            if (_frontmatterCache.TryGetValue(name, out var cached)) return cached;
            if (!frontmatter.TryGetValue(name, out var value))
            {
                throw new CalculationException($"\"{name}\" is not defined in the frontmatter. Table columns are written in square brackets like \"[{name}]\".");
            }

            try
            {
                var result = ExpressionParser.Evaluate(value, Culture, null, null);
                _frontmatterCache[name] = result;
                return result;
            }
            catch (CalculationException ex)
            {
                throw new CalculationException($"Frontmatter value \"{name}: {value}\": {ex.Message}");
            }
        }

        /// <summary>
        /// Returns the value of a table cell (null for empty cells), calculating it if it contains a calculation.
        /// </summary>
        private Quantity? GetCellValue(CellModel cell)
        {
            if (cell.Placeholders.Count > 1) throw new CalculationException("The cell contains more than one calculation.");
            if (cell.Placeholders.Count == 1) return Evaluate(cell.Placeholders[0]);

            var text = Normalize(cell.Text);
            return EmptyValues.Contains(text) ? null : ExpressionParser.ParseValue(text, Culture);
        }
    }
}
