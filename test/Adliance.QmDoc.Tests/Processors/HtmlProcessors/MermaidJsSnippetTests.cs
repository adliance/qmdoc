using Adliance.QmDoc.Converter;
using Adliance.QmDoc.Processors.HtmlProcessors;

namespace Adliance.QmDoc.Tests.Processors.HtmlProcessors;

public class MermaidJsSnippetTests
{
    private const string Html = "<body><script data-placeholder-for=\"mermaid\"></script></body>";

    [Theory]
    [InlineData(TargetExtension.Html)]
    [InlineData(TargetExtension.Docx)]
    public void Apply_ForHtml_StartsMermaidOnLoad(TargetExtension target)
    {
        var result = new MermaidJsSnippet(target).Apply(Html).ResultingHtml;
        Assert.DoesNotContain("data-placeholder-for", result);
        Assert.Contains("import mermaid from", result);
        Assert.Contains("startOnLoad: true", result);
        Assert.DoesNotContain("window.mermaid", result);
    }

    [Fact]
    public void Apply_ForPdf_LetsThePdfServiceRenderMermaid()
    {
        var result = new MermaidJsSnippet(TargetExtension.Pdf).Apply(Html).ResultingHtml;
        Assert.Contains("startOnLoad: false", result);
        Assert.Contains("window.mermaid = mermaid;", result);
    }

    [Fact]
    public void Apply_WithoutPlaceholder_DoesNothing()
    {
        Assert.Equal("<body></body>", new MermaidJsSnippet(TargetExtension.Pdf).Apply("<body></body>").ResultingHtml);
    }
}
