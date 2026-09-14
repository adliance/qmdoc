using Adliance.QmDoc.Converter;

namespace Adliance.QmDoc.Processors.HtmlProcessors;

public class MermaidJsSnippet(TargetExtension targetExtension) : IHtmlProcessor
{
    private const string Placeholder = """
                                       <script data-placeholder-for="mermaid"></script>
                                       """;

    private const string HtmlSnippet = """
                                       <script type="module">
                                       import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';

                                       mermaid.initialize({
                                           startOnLoad: true,
                                           theme: "base",
                                           themeVariables: {}
                                       });
                                       </script>
                                       """;

    private const string PdfServiceSnippet = """
                                             <script type="module">
                                             import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';

                                             mermaid.initialize({
                                                 startOnLoad: false,
                                                 theme: "base",
                                                 themeVariables: {}
                                             });
                                             window.mermaid = mermaid;
                                             </script>
                                             """;

    public HtmlProcessorResult Apply(string html)
    {
        var replacement = targetExtension == TargetExtension.Pdf ? PdfServiceSnippet : HtmlSnippet;

        var result = html.Replace(Placeholder.Trim(), replacement);

        return new HtmlProcessorResult(result);
    }
}
