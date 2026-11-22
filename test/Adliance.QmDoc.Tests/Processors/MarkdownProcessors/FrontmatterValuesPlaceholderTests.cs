using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Tests.Helpers;
using static Adliance.QmDoc.Tests.Helpers.Md;

namespace Adliance.QmDoc.Tests.Processors.MarkdownProcessors;

public class FrontmatterValuesPlaceholderTests
{
    [Fact]
    public void Apply_ReplacesFrontmatterValues()
    {
        var markdown = Lines("---", "clientName: ACME", "PROJECTCODE: P-42", "author: Hannes", "Stundensatz: 135 €/h", "---", "{{ clientName }}, {{projectcode}}, {{ AUTHOR }}, {{ Stundensatz }}");
        Assert.Equal("ACME, P-42, Hannes, 135 €/h", new FrontmatterValuesPlaceholder().Apply(markdown).Markdown);
    }

    [Fact]
    public void Apply_EmptyValue_RemovesPlaceholder()
    {
        Assert.Equal("[]", new FrontmatterValuesPlaceholder().Apply(Lines("---", "Leer:", "---", "[{{ Leer }}]")).Markdown);
    }

    [Fact]
    public void Apply_UnknownPlaceholder_StaysInDocument()
    {
        Assert.Equal("{{ Unknown }}", new FrontmatterValuesPlaceholder().Apply(Lines("---", "Known: 1", "---", "{{ Unknown }}")).Markdown);
    }

    [Fact]
    public void Apply_DoesNotReplaceCalculations()
    {
        Assert.Equal("{{ = Faktor * 2 }} 3", new FrontmatterValuesPlaceholder().Apply(Lines("---", "Faktor: 3", "---", "{{ = Faktor * 2 }} {{ Faktor }}")).Markdown);
    }
}
