using Adliance.QmDoc.Processors.HtmlProcessors;
using Adliance.QmDoc.Tests.Helpers;

namespace Adliance.QmDoc.Tests.Processors.HtmlProcessors;

public sealed class ConfigurablePlaceholdersTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void Apply_ReplacesPlaceholdersFromJsonFileNextToDocument()
    {
        _dir.Write("placeholders.json", """{ "CUSTOMER": "Medizinische Universität Wien", "Project": "QmDoc" }""");
        var processor = new ConfigurablePlaceholders(_dir.Combine("doc.md"), "placeholders.json");
        Assert.Equal("<p>Medizinische Universität Wien / QmDoc / QmDoc</p>", processor.Apply("<p>{{CUSTOMER}} / {{ project }} / {{PROJECT}}</p>").ResultingHtml);
    }

    [Fact]
    public void Apply_AbsolutePath()
    {
        var path = _dir.Write("config/values.json", """{ "A": "1" }""");
        var processor = new ConfigurablePlaceholders(_dir.Combine("doc.md"), path);
        Assert.Equal("1", processor.Apply("{{ A }}").ResultingHtml);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("missing.json")]
    public void Apply_WithoutFile_DoesNothing(string? path)
    {
        var processor = new ConfigurablePlaceholders(_dir.Combine("doc.md"), path);
        Assert.Equal("{{ A }}", processor.Apply("{{ A }}").ResultingHtml);
    }

    [Fact]
    public void Constructor_InvalidJson_Throws()
    {
        _dir.Write("placeholders.json", "{ invalid");
        var ex = Assert.Throws<Exception>(() => new ConfigurablePlaceholders(_dir.Combine("doc.md"), "placeholders.json"));
        Assert.StartsWith($"Unable to read {_dir.Combine("placeholders.json")}: ", ex.Message);
    }

    [Fact]
    public void Constructor_NullJson_Throws()
    {
        _dir.Write("placeholders.json", "null");
        var ex = Assert.Throws<Exception>(() => new ConfigurablePlaceholders(_dir.Combine("doc.md"), "placeholders.json"));
        Assert.EndsWith("Unable to deserialize placeholders file.", ex.Message);
    }

    [Fact]
    public void Apply_DoesNotReplaceLongerPlaceholders()
    {
        _dir.Write("placeholders.json", """{ "A": "1" }""");
        var processor = new ConfigurablePlaceholders(_dir.Combine("doc.md"), "placeholders.json");
        Assert.Equal("{{ AB }}", processor.Apply("{{ AB }}").ResultingHtml);
    }

    [Fact]
    public void Apply_ValueWithDollarSigns_IsInsertedLiterally()
    {
        _dir.Write("placeholders.json", """{ "PRICE": "$0 / $$ / $1" }""");
        var processor = new ConfigurablePlaceholders(_dir.Combine("doc.md"), "placeholders.json");
        Assert.Equal("$0 / $$ / $1", processor.Apply("{{ PRICE }}").ResultingHtml);
    }

    [Fact]
    public void Apply_PlaceholderWithRegexCharacters()
    {
        _dir.Write("placeholders.json", """{ "a.b": "1", "c+": "2" }""");
        var processor = new ConfigurablePlaceholders(_dir.Combine("doc.md"), "placeholders.json");
        Assert.Equal("1 {{ axb }} 2", processor.Apply("{{ a.b }} {{ axb }} {{ c+ }}").ResultingHtml);
    }
}
