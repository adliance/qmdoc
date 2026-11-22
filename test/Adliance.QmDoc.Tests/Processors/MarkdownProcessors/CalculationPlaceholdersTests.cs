using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Tests.Helpers;
using static Adliance.QmDoc.Tests.Helpers.Md;

namespace Adliance.QmDoc.Tests.Processors.MarkdownProcessors;

public class CalculationPlaceholdersTests
{
    private const string FilePath = "/docs/estimate.md";

    private static MarkdownProcessorContext Calculate(string markdown) => new CalculationPlaceholders(FilePath).Apply(markdown);

    private static string Result(string markdown)
    {
        var context = Calculate(markdown);
        Assert.Empty(context.Errors.Select(e => e.ErrorMessage));
        return context.Markdown;
    }

    private static string SingleError(string markdown)
    {
        var context = Calculate(markdown);
        var error = Assert.Single(context.Errors);
        Assert.Equal(FilePath, error.FilePath);
        Assert.Equal("estimate.md", error.FileName);
        Assert.False(error.IsWarningOnly);
        return error.ErrorMessage;
    }

    #region Formulas in text

    [Fact]
    public void Apply_WithoutPlaceholders_ReturnsUnchangedMarkdown()
    {
        const string markdown = "# Title\n\nNo calculations here, not even {{ TITLE }}.";
        Assert.Equal(markdown, Result(markdown));
    }

    [Theory]
    [InlineData("{{ = 1 + 2 }}", "3")]
    [InlineData("{{=1+2}}", "3")]
    [InlineData("{{   =   1 + 2   }}", "3")]
    [InlineData("{{ = 145h * 2€ }}", "290€")]
    [InlineData("{{ = (12h + 4h) * 2 }}", "32h")]
    [InlineData("{{ = 1.200€ * 20% }}", "240€")]
    [InlineData("{{ = -5h + 8h }}", "3h")]
    public void Apply_ReplacesFormulasInText(string formula, string expected)
    {
        Assert.Equal($"Result: {expected}.", Result($"Result: {formula}."));
    }

    [Fact]
    public void Apply_ReplacesMultipleFormulasInOneLine()
    {
        Assert.Equal("1h, 2h and 3h", Result("{{ = 1h }}, {{ = 1h + 1h }} and {{ = 3h }}"));
    }

    [Fact]
    public void Apply_ReplacesFormulasInHeadingsListsAndQuotes()
    {
        var markdown = Lines("# Costs: {{ = 2 * 3 }}", "", "- {{ = 1 + 1 }}", "", "> {{ = 10 / 4 }}");
        Assert.Equal(Lines("# Costs: 6", "", "- 2", "", "> 2,5"), Result(markdown));
    }

    [Fact]
    public void Apply_KeepsFormattingAroundFormula()
    {
        Assert.Equal("**3h** and _4h_", Result("**{{ = 1h + 2h }}** and _{{ = 4h }}_"));
    }

    [Fact]
    public void Apply_DoesNotMatchAcrossLines()
    {
        const string markdown = "{{ = 1 +\n2 }}";
        Assert.Equal(markdown, Result(markdown));
    }

    [Fact]
    public void Apply_UsesFrontmatterValues()
    {
        var markdown = Lines("---", "Stundensatz: 135 €/h", "Pauschale: 1.500€", "---", "", "{{ = (40h + 90min) * Stundensatz }} plus {{ = Pauschale }}");
        Assert.Equal($"5.602,50{Nbsp}€ plus 1.500€", Result(markdown));
    }

    [Fact]
    public void Apply_FrontmatterNames_AreCaseInsensitive()
    {
        var markdown = Lines("---", "Stundensatz: 100€/h", "---", "{{ = 2h * stundensatz }} {{ = 2h * STUNDENSATZ }}");
        Assert.Equal("200€ 200€", Result(markdown));
    }

    [Fact]
    public void Apply_FrontmatterValueWithDashForNoCents()
    {
        var markdown = Lines("---", "Stundensatz: € 135,-", "---", "{{ = 150h * Stundensatz }}");
        Assert.Equal($"€{Nbsp}20.250,-", Result(markdown));
    }

    [Fact]
    public void Apply_FrontmatterValueCanBeUsedMultipleTimes()
    {
        var markdown = Lines("---", "Satz: 10€", "---", "{{ = Satz * 2 }} {{ = Satz * 3 }} {{ = Satz + Satz }}");
        Assert.Equal("20€ 30€ 20€", Result(markdown));
    }

    [Fact]
    public void Apply_UnknownName_ReportsError()
    {
        var markdown = "{{ = 2h * Stundensatz }}";
        var context = Calculate(markdown);
        Assert.Equal(markdown, context.Markdown); // the formula stays in the document
        Assert.Equal("Calculation \"{{ = 2h * Stundensatz }}\": \"Stundensatz\" is not defined in the frontmatter. Table columns are written in square brackets like \"[Stundensatz]\".",
            Assert.Single(context.Errors).ErrorMessage);
    }

    [Fact]
    public void Apply_InvalidFrontmatterValue_ReportsError()
    {
        var markdown = Lines("---", "Stundensatz: viel", "---", "{{ = 2h * Stundensatz }}");
        Assert.Equal("Calculation \"{{ = 2h * Stundensatz }}\": Frontmatter value \"Stundensatz: viel\": Names like \"viel\" cannot be used here.", SingleError(markdown));
    }

    [Fact]
    public void Apply_FrontmatterValueThatIsAFormula_IsCalculated()
    {
        var markdown = Lines("---", "Summe: 10€ + 5€", "---", "{{ = Summe * 2 }}");
        Assert.Equal("30€", Result(markdown));
    }

    [Fact]
    public void Apply_FrontmatterValueReferringToOtherFrontmatterValue_ReportsError()
    {
        var markdown = Lines("---", "A: 10€", "B: A * 2", "---", "{{ = B }}");
        Assert.Contains("Names like \"A\" cannot be used here.", SingleError(markdown));
    }

    [Fact]
    public void Apply_ErrorsOfOnePlaceholder_DoNotAffectOthers()
    {
        var context = Calculate("{{ = 1 + }} and {{ = 1 + 1 }} and {{ = 2h + 1€ }}");
        Assert.Equal("{{ = 1 + }} and 2 and {{ = 2h + 1€ }}", context.Markdown);
        Assert.Equal(2, context.Errors.Count);
        Assert.Equal("Calculation \"{{ = 1 + }}\": The formula ends unexpectedly.", context.Errors[0].ErrorMessage);
        Assert.Equal("Calculation \"{{ = 2h + 1€ }}\": Cannot add \"h\" and \"€\".", context.Errors[1].ErrorMessage);
    }

    [Fact]
    public void Apply_EmptyFormula_ReportsError()
    {
        Assert.Equal("Calculation \"{{ = }}\": The formula is empty.", SingleError("{{ = }}"));
    }

    [Fact]
    public void Apply_UnreplacedPlaceholderInFormula_ReportsError()
    {
        Assert.Equal("Calculation \"{{ = 2 * {{ Faktor }}\": The formula contains a placeholder that could not be replaced.", SingleError("{{ = 2 * {{ Faktor }} }}"));
    }

    [Fact]
    public void Apply_WrongDecimalSeparator_ReportsErrorWithCultureHint()
    {
        Assert.Equal("Calculation \"{{ = 12.5h * 2 }}\": Unexpected \".\" at position 3 (the culture is \"de-DE\", so the decimal separator is \",\" and the thousands separator is \".\").",
            SingleError("{{ = 12.5h * 2 }}"));
    }

    #endregion

    #region Code

    [Fact]
    public void Apply_IgnoresInlineCode()
    {
        const string markdown = "`{{ = 1 + 1 }}` but {{ = 1 + 1 }}";
        Assert.Equal("`{{ = 1 + 1 }}` but 2", Result(markdown));
    }

    [Fact]
    public void Apply_IgnoresFencedCodeBlocks()
    {
        var markdown = Lines("```", "{{ = 1 + 1 }}", "{{ SUM }}", "```", "", "{{ = 2 + 2 }}");
        Assert.Equal(Lines("```", "{{ = 1 + 1 }}", "{{ SUM }}", "```", "", "4"), Result(markdown));
    }

    [Fact]
    public void Apply_IgnoresIndentedCodeBlocks()
    {
        var markdown = Lines("Text", "", "    {{ = 1 + 1 }}", "", "{{ = 2 + 2 }}");
        Assert.Equal(Lines("Text", "", "    {{ = 1 + 1 }}", "", "4"), Result(markdown));
    }

    [Fact]
    public void Apply_OnlyCodeWithFormulas_ReturnsUnchangedMarkdown()
    {
        const string markdown = "`{{ = 1 + 1 }}`";
        Assert.Equal(markdown, Result(markdown));
    }

    #endregion

    #region Culture

    [Fact]
    public void Apply_UsesGermanCultureByDefault()
    {
        Assert.Equal("1.234,50€", Result("{{ = 1.234,50€ }}"));
    }

    [Fact]
    public void Apply_UsesCultureFromFrontmatter()
    {
        var markdown = Lines("---", "culture: en-US", "---", "{{ = 1,234.50€ * 2 }}");
        Assert.Equal("2,469.00€", Result(markdown));
    }

    [Fact]
    public void Apply_CultureIsCaseInsensitiveAndTrimmed()
    {
        var markdown = Lines("---", "culture: ' EN-us '", "---", "{{ = 1.5h }}");
        Assert.Equal("1.5h", Result(markdown));
    }

    [Fact]
    public void Apply_SwissCulture()
    {
        var markdown = Lines("---", "culture: de-CH", "---", "{{ = 1'234.50 CHF * 2 }}");
        var expected = 2469m.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("de-CH")) + Nbsp + "CHF";
        Assert.Equal(expected, Result(markdown));
    }

    [Theory]
    [InlineData("xx-YY")]
    [InlineData("klingon")]
    public void Apply_UnknownCulture_FallsBackToGerman(string culture)
    {
        var context = Calculate(Lines("---", $"culture: {culture}", "---", "{{ = 1,5h * 2 }}"));
        Assert.Equal("3,0h", context.Markdown);
        Assert.Equal($"Culture \"{culture}\" is unknown, using \"de-DE\" instead.", Assert.Single(context.Errors).ErrorMessage);
    }

    [Fact]
    public void Apply_InvariantCultureName_FallsBackToGerman()
    {
        // an empty culture name is the invariant culture, which is ignored
        var context = Calculate(Lines("---", "culture: ''", "---", "{{ = 1,5h }}"));
        Assert.Equal("1,5h", context.Markdown);
        Assert.Empty(context.Errors);
    }

    #endregion

    #region SUM without "="

    [Theory]
    [InlineData("{{ SUM }}")]
    [InlineData("{{sum}}")]
    [InlineData("{{  Sum  }}")]
    public void Apply_SumWithoutEquals_ReportsError(string placeholder)
    {
        var context = Calculate($"| A |\n|---|\n| 1 |\n| {placeholder} |");
        Assert.Contains(placeholder, context.Markdown);
        Assert.Equal($"\"{placeholder}\" is not calculated, calculations must start with \"=\", like \"{{{{ = SUM }}}}\".", Assert.Single(context.Errors).ErrorMessage);
    }

    [Fact]
    public void Apply_SumWithoutEqualsInCode_IsIgnored()
    {
        Assert.Empty(Calculate("`{{ SUM }}`").Errors);
    }

    #endregion

    #region Tables: references

    private static readonly string Estimate = Lines(
        "| Position         | Stunden         | Satz    | Kosten                                              |",
        "|------------------|----------------:|--------:|----------------------------------------------------:|",
        "| Konzept          | 12h             | 135 €/h | {{ = [Stunden] * [Satz] }}                          |",
        "| Umsetzung        | 40h             | 120 €/h | {{ = [Stunden] * [Satz] }}                          |",
        "| Abstimmungen     | 90min           | 135 €/h | {{ = [Stunden] * [Satz] }}                          |",
        "| **Summe netto**  | **{{ = SUM }}** |         | **{{ = SUM }}**                                     |",
        "| USt 20%          |                 |         | {{ = [Kosten:Summe netto] * 20% }}                  |",
        "| **Summe brutto** |                 |         | **{{ = [Kosten:Summe netto] + [Kosten:USt 20%] }}** |");

    [Fact]
    public void Apply_CompleteEstimate()
    {
        var result = Result(Estimate);

        Assert.Equal($"1.620,00{Nbsp}€", Row(result, "Konzept")[3]);
        Assert.Equal($"4.800,00{Nbsp}€", Row(result, "Umsetzung")[3]);
        Assert.Equal($"202,50{Nbsp}€", Row(result, "Abstimmungen")[3]);
        Assert.Equal("**53,5h**", Row(result, "**Summe netto**")[1]);
        Assert.Equal($"**6.622,50{Nbsp}€**", Row(result, "**Summe netto**")[3]);
        Assert.Equal($"1.324,50{Nbsp}€", Row(result, "USt 20%")[3]);
        Assert.Equal($"**7.947,00{Nbsp}€**", Row(result, "**Summe brutto**")[3]);
    }

    [Fact]
    public void Apply_CompleteEstimate_LeavesOtherCellsAlone()
    {
        var result = Result(Estimate);
        Assert.Equal(new[] { "Konzept", "12h", "135 €/h" }, Row(result, "Konzept")[..3]);
        Assert.Equal(Estimate.Split('\n').Length, result.Split('\n').Length);
    }

    private static string Table(params string[] rows) => Lines(["| A | B | C |", "|---|---|---|", .. rows]);

    [Fact]
    public void Apply_ReferenceToSameRow()
    {
        Assert.Equal("4", Row(Result(Table("| x | 2 | {{ = [B] * 2 }} |")), "x")[2]);
    }

    [Fact]
    public void Apply_ReferenceByRowNumber()
    {
        var result = Result(Table("| x | 2 | |", "| y | 3 | {{ = [B:1] + [B:2] }} |"));
        Assert.Equal("5", Row(result, "y")[2]);
    }

    [Fact]
    public void Apply_ReferenceByColumnAndRowNumber()
    {
        var result = Result(Table("| x | 2 | |", "| y | 3 | {{ = [2:1] * [2:2] }} |"));
        Assert.Equal("6", Row(result, "y")[2]);
    }

    [Fact]
    public void Apply_ReferenceByRowLabel()
    {
        var result = Result(Table("| Basis | 100€ | |", "| Aufschlag | | {{ = [B:Basis] * 10% }} |"));
        Assert.Equal("10€", Row(result, "Aufschlag")[2]);
    }

    [Fact]
    public void Apply_ReferencesAreCaseInsensitive()
    {
        var result = Result(Table("| Basis | 100€ | |", "| y | | {{ = [b:BASIS] }} |"));
        Assert.Equal("100€", Row(result, "y")[2]);
    }

    [Fact]
    public void Apply_ReferenceByPartialRowLabel()
    {
        var result = Result(Table(
            "| Konzeption, Projektmanagement, Besprechungen | 10h | |",
            "| Umsetzung | 20h | |",
            "| y | | {{ = [B:Projektmanagement] + [B:Umsetz] }} |"));
        Assert.Equal("30h", Row(result, "y")[2]);
    }

    [Fact]
    public void Apply_ReferenceByPartialColumnHeader()
    {
        var markdown = Lines("| Position | Stunden (geschätzt) | Kosten |", "|---|---|---|", "| x | 2h | {{ = [Stunden] * 10€ }} |");
        Assert.Equal("20€", Row(Result(markdown), "x")[2]);
    }

    [Fact]
    public void Apply_ExactMatchIsPreferredOverPartialMatch()
    {
        var result = Result(Table("| Summe netto | 100€ | |", "| Summe | 50€ | |", "| y | | {{ = [B:Summe] }} |"));
        Assert.Equal("50€", Row(result, "y")[2]);
    }

    [Fact]
    public void Apply_StartsWithIsPreferredOverContains()
    {
        var result = Result(Table("| Netto Summe | 100€ | |", "| Summe brutto | 50€ | |", "| y | | {{ = [B:Summe] }} |"));
        Assert.Equal("50€", Row(result, "y")[2]);
    }

    [Fact]
    public void Apply_RowLabelWithFormattingAndLinks_IsFound()
    {
        var result = Result(Table(
            "| **[#Projektmeldung]** | 10h | |",
            "| _[Doku](doku.md)_ | 5h | |",
            "| ~~`Alt`~~ | 1h | |",
            "| y | | {{ = [B:Projektmeldung] + [B:Doku] + [B:Alt] }} |"));
        Assert.Equal("16h", Row(result, "y")[2]);
    }

    [Fact]
    public void Apply_HeaderWithFormatting_IsFound()
    {
        var markdown = Lines("| Position | **Stunden** |", "|---|---|", "| x | 2h |", "| y | {{ = [Stunden:x] }} |");
        Assert.Equal("2h", Row(Result(markdown), "y")[1]);
    }

    [Fact]
    public void Apply_RowLabelWithMultipleSpaces_IsNormalized()
    {
        var result = Result(Table("| Summe   netto | 100€ | |", "| y | | {{ = [B:Summe netto] }} |"));
        Assert.Equal("100€", Row(result, "y")[2]);
    }

    [Fact]
    public void Apply_ReferencesToCalculatedCells_AreResolvedInAnyOrder()
    {
        var result = Result(Table(
            "| x | {{ = [B:y] * 2 }} | |",
            "| y | {{ = [C:z] + 1 }} | |",
            "| z | | 5 |"));
        Assert.Equal("12", Row(result, "x")[1]);
        Assert.Equal("6", Row(result, "y")[1]);
    }

    [Fact]
    public void Apply_ReferenceWithFrontmatterValue()
    {
        var markdown = Lines("---", "Stundensatz: 100€/h", "---", "| Position | Stunden | Kosten |", "|---|---|---|", "| x | 2h | {{=[Stunden]*Stundensatz}} |");
        Assert.Equal("200€", Row(Result(markdown), "x")[2]);
    }

    [Fact]
    public void Apply_MultipleTables_AreIndependent()
    {
        var markdown = Lines(
            "| A | B |", "|---|---|", "| x | 1 |", "| y | {{ = SUM }} |",
            "",
            "| A | B |", "|---|---|", "| x | 10 |", "| y | {{ = SUM + [B:x] }} |");
        var result = Result(markdown);
        var sums = result.Split('\n').Where(l => l.StartsWith("| y")).ToList();
        Assert.Equal(new[] { "| y | 1 |", "| y | 20 |" }, sums);
    }

    [Fact]
    public void Apply_FormulaInHeader_IsCalculatedButCannotUseReferences()
    {
        var context = Calculate(Lines("| A {{ = 1 + 1 }} | B {{ = [A] }} |", "|---|---|", "| 1 | 2 |"));
        Assert.StartsWith("| A 2 | B {{ = [A] }} |", context.Markdown);
        Assert.Equal("Calculation \"{{ = [A] }}\": References like \"[A]\" only work inside table cells (not in the header).", Assert.Single(context.Errors).ErrorMessage);
    }

    [Fact]
    public void Apply_ReferenceOutsideOfTable_ReportsError()
    {
        Assert.Equal("Calculation \"{{ = [Kosten] * 2 }}\": References like \"[Kosten]\" only work inside table cells (not in the header).", SingleError("{{ = [Kosten] * 2 }}"));
    }

    [Fact]
    public void Apply_UnknownColumn_ReportsError()
    {
        Assert.Equal("Calculation \"{{ = [D] }}\": [D]: There is no column \"D\", the columns are \"A\", \"B\", \"C\". Frontmatter values are written without square brackets.",
            SingleError(Table("| x | | {{ = [D] }} |")));
    }

    [Fact]
    public void Apply_UnknownRow_ReportsError()
    {
        Assert.Equal("Calculation \"{{ = [B:Gibtsnicht] }}\": [B:Gibtsnicht]: There is no row \"Gibtsnicht\", the first cell of the row must contain this text.",
            SingleError(Table("| x | | {{ = [B:Gibtsnicht] }} |")));
    }

    [Fact]
    public void Apply_ColumnNumberOutOfRange_ReportsError()
    {
        Assert.Equal("Calculation \"{{ = [4] }}\": [4]: There is no column 4, the table has 3 columns.", SingleError(Table("| x | | {{ = [4] }} |")));
        Assert.Equal("Calculation \"{{ = [0] }}\": [0]: There is no column 0, the table has 3 columns.", SingleError(Table("| x | | {{ = [0] }} |")));
    }

    [Fact]
    public void Apply_RowNumberOutOfRange_ReportsError()
    {
        Assert.Equal("Calculation \"{{ = [B:9] }}\": [B:9]: There is no row 9, the table has 2 rows (not counting the header).",
            SingleError(Table("| x | 1 | |", "| y | | {{ = [B:9] }} |")));
    }

    [Fact]
    public void Apply_AmbiguousPartialRow_ReportsError()
    {
        Assert.Equal("Calculation \"{{ = [B:Konzept] }}\": [B:Konzept]: \"Konzept\" matches multiple rows (\"Konzept A\", \"Konzept B\"), please use more of the text or the row number.",
            SingleError(Table("| Konzept A | 1 | |", "| Konzept B | 2 | |", "| y | | {{ = [B:Konzept] }} |")));
    }

    [Fact]
    public void Apply_DuplicateColumnNames_ReportsError()
    {
        var markdown = Lines("| A | Kosten | Kosten |", "|---|---|---|", "| x | 1 | {{ = [Kosten] }} |");
        Assert.Equal("Calculation \"{{ = [Kosten] }}\": [Kosten]: There are multiple columns named \"Kosten\", please use the column number instead.", SingleError(markdown));
    }

    [Fact]
    public void Apply_DuplicateColumnNames_CanBeReferencedByNumber()
    {
        var markdown = Lines("| A | Kosten | Kosten |", "|---|---|---|", "| x | 1 | {{ = [2] + 1 }} |");
        Assert.Equal("2", Row(Result(markdown), "x")[2]);
    }

    [Fact]
    public void Apply_DuplicateRowLabels_ReportsError()
    {
        Assert.Contains("There are multiple rows named \"x\", please use the row number instead.",
            SingleError(Table("| x | 1 | |", "| x | 2 | |", "| y | | {{ = [B:x] }} |")));
    }

    [Fact]
    public void Apply_ReferenceToOwnCell_ReportsError()
    {
        Assert.Equal("Calculation \"{{ = [C] + 1 }}\": [C] refers to its own cell.", SingleError(Table("| x | | {{ = [C] + 1 }} |")));
    }

    [Fact]
    public void Apply_CircularReference_ReportsErrors()
    {
        var context = Calculate(Table("| x | {{ = [C] }} | {{ = [B] }} |"));
        Assert.Equal(2, context.Errors.Count);
        Assert.All(context.Errors, e => Assert.Contains("The calculation refers to itself (circular reference).", e.ErrorMessage));
        Assert.Contains("{{ = [C] }}", context.Markdown);
        Assert.Contains("{{ = [B] }}", context.Markdown);
    }

    [Theory]
    [InlineData("")]
    [InlineData("-")]
    [InlineData("–")]
    [InlineData("—")]
    public void Apply_ReferenceToEmptyCell_ReportsError(string empty)
    {
        Assert.Equal("Calculation \"{{ = [B] }}\": [B]: The cell is empty.", SingleError(Table($"| x | {empty} | {{{{ = [B] }}}} |")));
    }

    [Fact]
    public void Apply_ReferenceToMissingCell_ReportsError()
    {
        // the row has fewer cells than the header
        var markdown = Lines("| A | B | C |", "|---|---|---|", "| x | {{ = [C] }} |");
        Assert.Equal("Calculation \"{{ = [C] }}\": [C]: The cell is empty.", SingleError(markdown));
    }

    [Fact]
    public void Apply_ReferenceToText_ReportsError()
    {
        Assert.Equal("Calculation \"{{ = [B] }}\": [B]: \"viel\" is not a number.", SingleError(Table("| x | viel | {{ = [B] }} |")));
    }

    [Fact]
    public void Apply_ReferenceToCellWithFormattedValue()
    {
        Assert.Equal("24h", Row(Result(Table("| x | **12h** | {{ = [B] * 2 }} |")), "x")[2]);
    }

    [Fact]
    public void Apply_ReferenceToCellWithTwoFormulas_ReportsError()
    {
        var context = Calculate(Table("| x | {{ = 1 }} {{ = 2 }} | {{ = [B] }} |"));
        Assert.Equal("Calculation \"{{ = [B] }}\": [B]: The cell contains more than one calculation.", Assert.Single(context.Errors).ErrorMessage);
        Assert.Contains("| x | 1 2 |", context.Markdown);
    }

    [Fact]
    public void Apply_ReferenceToCellWithFailingFormula_ReportsBothErrors()
    {
        var context = Calculate(Table("| x | {{ = 1h + 1€ }} | {{ = [B] }} |"));
        Assert.Equal(2, context.Errors.Count);
        Assert.Equal("Calculation \"{{ = 1h + 1€ }}\": Cannot add \"h\" and \"€\".", context.Errors[0].ErrorMessage);
        Assert.Equal("Calculation \"{{ = [B] }}\": [B]: Cannot add \"h\" and \"€\".", context.Errors[1].ErrorMessage);
    }

    [Fact]
    public void Apply_SquareBracketsInText_AreNotReferences()
    {
        // only formulas are parsed, so links and chapter links elsewhere in the table are fine
        var result = Result(Table("| [#Kapitel] | [Link](x.md) | {{ = 1 + 1 }} |"));
        Assert.Equal(new[] { "[#Kapitel]", "[Link](x.md)", "2" }, Row(result, "[#Kapitel]"));
    }

    [Fact]
    public void Apply_TableWithoutOuterPipes()
    {
        var result = Result(Lines("A | B", "--|--", "x | 2", "y | {{ = [B:x] + 1 }}"));
        Assert.EndsWith("y | 3", result);
    }

    [Fact]
    public void Apply_ReferenceToNegativeValueWithUnicodeMinus()
    {
        Assert.Equal("-10h", Row(Result(Table("| x | −5h | {{ = [B] * 2 }} |")), "x")[2]);
    }

    [Fact]
    public void Apply_ReferenceToSuffixedCurrencyWithDash()
    {
        Assert.Equal($"270,-{Nbsp}€", Row(Result(Table("| x | 135,- € | {{ = [B] * 2 }} |")), "x")[2]);
    }

    [Fact]
    public void Apply_SumOfAmountsWithDash()
    {
        var result = Result(Lines("| A | B |", "|---|---|", "| a | 135,- € |", "| b | 15,- € |", "| Summe | {{ = SUM }} |"));
        Assert.Equal($"150,-{Nbsp}€", Row(result, "Summe")[1]);
    }

    [Fact]
    public void Apply_ReferenceToPrefixedCurrencyWithDash()
    {
        Assert.Equal($"€{Nbsp}270,-", Row(Result(Table("| x | € 135,- | {{ = [B] * 2 }} |")), "x")[2]);
    }

    [Fact]
    public void Apply_ReferenceToCellWithEscapedPipe_ReportsError()
    {
        Assert.Contains("is not a number", SingleError(Table("| x | 1 \\| 2 | {{ = [B] }} |")));
    }

    #endregion

    #region Tables: SUM

    [Fact]
    public void Apply_Sum_AddsUpAllValuesAbove()
    {
        var result = Result(Lines("| A | B |", "|---|---|", "| x | 1h |", "| y | 2,5h |", "| Summe | {{ = SUM }} |"));
        Assert.Equal("3,5h", Row(result, "Summe")[1]);
    }

    [Fact]
    public void Apply_Sum_IsCaseInsensitive()
    {
        var result = Result(Lines("| A | B |", "|---|---|", "| x | 1 |", "| y | 2 |", "| Summe | {{ = sum }} |"));
        Assert.Equal("3", Row(result, "Summe")[1]);
    }

    [Fact]
    public void Apply_Sum_SkipsEmptyCellsAndDashes()
    {
        var result = Result(Lines("| A | B |", "|---|---|", "| a | 1 |", "| b | |", "| c | - |", "| d | – |", "| e | 2 |", "| Summe | {{ = SUM }} |"));
        Assert.Equal("3", Row(result, "Summe")[1]);
    }

    [Fact]
    public void Apply_Sum_SkipsSubtotals()
    {
        var result = Result(Lines(
            "| A | B |", "|---|---|",
            "| a | 1 |", "| b | 2 |", "| Zwischensumme 1 | {{ = SUM }} |",
            "| c | 4 |", "| Zwischensumme 2 | {{ = SUM }} |",
            "| Summe | {{ = SUM }} |"));
        Assert.Equal("3", Row(result, "Zwischensumme 1")[1]);
        Assert.Equal("7", Row(result, "Zwischensumme 2")[1]);
        Assert.Equal("7", Row(result, "Summe")[1]);
    }

    [Fact]
    public void Apply_Sum_IncludesCalculatedCellsThatAreNotSums()
    {
        var result = Result(Lines(
            "| A | B |", "|---|---|",
            "| a | 10 |", "| b | {{ = [B:a] * 2 }} |", "| c | {{ = SUM * 2 }} |",
            "| Summe | {{ = SUM }} |"));
        Assert.Equal("60", Row(result, "c")[1]);
        Assert.Equal("90", Row(result, "Summe")[1]);
    }

    [Fact]
    public void Apply_SumInFormula()
    {
        var result = Result(Lines("| A | B |", "|---|---|", "| a | 100€ |", "| b | 50€ |", "| USt | {{ = SUM * 20% }} |"));
        Assert.Equal("30€", Row(result, "USt")[1]);
    }

    [Fact]
    public void Apply_SumOnlyAddsTheSameColumn()
    {
        var result = Result(Lines("| A | B | C |", "|---|---|---|", "| a | 1 | 10 |", "| b | 2 | 20 |", "| Summe | {{ = SUM }} | {{ = SUM }} |"));
        Assert.Equal(new[] { "Summe", "3", "30" }, Row(result, "Summe"));
    }

    [Fact]
    public void Apply_SumIgnoresRowsBelow()
    {
        var result = Result(Lines("| A | B |", "|---|---|", "| a | 1 |", "| Summe | {{ = SUM }} |", "| b | 100 |"));
        Assert.Equal("1", Row(result, "Summe")[1]);
    }

    [Fact]
    public void Apply_SumReferencedFromOtherRow()
    {
        var result = Result(Lines("| A | B |", "|---|---|", "| a | 1 |", "| b | 2 |", "| Summe | {{ = SUM }} |", "| Doppelt | {{ = [B:Summe] * 2 }} |"));
        Assert.Equal("6", Row(result, "Doppelt")[1]);
    }

    [Fact]
    public void Apply_SumWithoutValuesAbove_ReportsError()
    {
        Assert.Equal("Calculation \"{{ = SUM }}\": There are no values above to add up.", SingleError(Lines("| A | B |", "|---|---|", "| a | |", "| Summe | {{ = SUM }} |")));
    }

    [Fact]
    public void Apply_SumInFirstRow_ReportsError()
    {
        Assert.Equal("Calculation \"{{ = SUM }}\": There are no values above to add up.", SingleError(Lines("| A | B |", "|---|---|", "| Summe | {{ = SUM }} |")));
    }

    [Fact]
    public void Apply_SumWithText_ReportsErrorWithRow()
    {
        Assert.Equal("Calculation \"{{ = SUM }}\": Row 2: \"viel\" is not a number.", SingleError(Lines("| A | B |", "|---|---|", "| a | 1 |", "| b | viel |", "| Summe | {{ = SUM }} |")));
    }

    [Fact]
    public void Apply_SumWithDifferentUnits_ReportsError()
    {
        Assert.Equal("Calculation \"{{ = SUM }}\": Cannot add \"h\" and \"€\".", SingleError(Lines("| A | B |", "|---|---|", "| a | 1h |", "| b | 1€ |", "| Summe | {{ = SUM }} |")));
    }

    [Fact]
    public void Apply_SumOutsideOfTable_ReportsError()
    {
        Assert.Equal("Calculation \"{{ = SUM }}\": SUM only works inside table cells (not in the header).", SingleError("Total: {{ = SUM }}"));
    }

    [Fact]
    public void Apply_SumInHeader_ReportsError()
    {
        var context = Calculate(Lines("| A | {{ = SUM }} |", "|---|---|", "| a | 1 |"));
        Assert.Equal("Calculation \"{{ = SUM }}\": SUM only works inside table cells (not in the header).", Assert.Single(context.Errors).ErrorMessage);
    }

    [Fact]
    public void Apply_SumOverRowsWithFewerCells_SkipsMissingCells()
    {
        var result = Result(Lines("| A | B | C |", "|---|---|---|", "| a | 1 | 5 |", "| b | 2 |", "| Summe | | {{ = SUM }} |"));
        Assert.Equal("5", Row(result, "Summe")[2]);
    }

    [Fact]
    public void Apply_FrontmatterValueNamedSum_IsShadowedBySum()
    {
        var markdown = Lines("---", "Sum: 1000", "---", "| A | B |", "|---|---|", "| a | 1 |", "| b | {{ = SUM }} |");
        Assert.Equal("1", Row(Result(markdown), "b")[1]);
    }

    #endregion

    #region Tables: money formatting

    [Fact]
    public void Apply_MoneyInColumn_UsesCentsIfOneAmountHasCents()
    {
        var result = Result(Lines("| A | B |", "|---|---|", "| a | {{ = 10€ }} |", "| b | {{ = 2,50€ }} |", "| c | {{ = 3€ }} |"));
        Assert.Equal("10,00€", Row(result, "a")[1]);
        Assert.Equal("2,50€", Row(result, "b")[1]);
        Assert.Equal("3,00€", Row(result, "c")[1]);
    }

    [Fact]
    public void Apply_MoneyInColumn_WithoutCents()
    {
        var result = Result(Lines("| A | B |", "|---|---|", "| a | {{ = 10€ }} |", "| b | {{ = 2€ }} |"));
        Assert.Equal("10€", Row(result, "a")[1]);
        Assert.Equal("2€", Row(result, "b")[1]);
    }

    [Fact]
    public void Apply_MoneyCents_AreOnlyAlignedPerColumn()
    {
        var result = Result(Lines("| A | B | C |", "|---|---|---|", "| a | {{ = 10€ }} | {{ = 1,50€ }} |", "| b | {{ = 2€ }} | {{ = 1h }} |"));
        Assert.Equal(new[] { "a", "10€", "1,50€" }, Row(result, "a"));
        Assert.Equal(new[] { "b", "2€", "1h" }, Row(result, "b"));
    }

    [Fact]
    public void Apply_MoneyCents_AreOnlyAlignedPerTable()
    {
        var markdown = Lines("| A | B |", "|---|---|", "| a | {{ = 10€ }} |", "", "| A | B |", "|---|---|", "| b | {{ = 2,50€ }} |");
        var result = Result(markdown);
        Assert.Equal("10€", Row(result, "a")[1]);
        Assert.Equal("2,50€", Row(result, "b")[1]);
    }

    [Fact]
    public void Apply_MoneyCents_DoNotAffectTextOutsideTables()
    {
        var result = Result(Lines("{{ = 10€ }}", "", "| A | B |", "|---|---|", "| b | {{ = 2,50€ }} |"));
        Assert.StartsWith("10€\n", result);
    }

    [Fact]
    public void Apply_MoneyCents_DoNotAffectNonMoneyValues()
    {
        var result = Result(Lines("| A | B |", "|---|---|", "| a | {{ = 10h }} |", "| b | {{ = 2,50€ }} |"));
        Assert.Equal("10h", Row(result, "a")[1]);
    }

    #endregion
}
