using System.Collections.Generic;
using System.Linq;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

/// <summary>
/// The positions of code blocks and inline code in a Markdown document, so that processors can leave placeholders, links etc. inside of code alone.
/// </summary>
public sealed class CodeSpans
{
    private readonly List<SourceSpan> _spans;

    private CodeSpans(List<SourceSpan> spans)
    {
        _spans = spans;
    }

    public static CodeSpans Find(string markdown, MarkdownPipeline pipeline) => Find(Markdown.Parse(markdown, pipeline));

    public static CodeSpans Find(MarkdownDocument document)
    {
        // the frontmatter is already removed, so a YAML block at the beginning is something else (e.g. a page break) and not code
        var blocks = document.Descendants<CodeBlock>().Where(x => x is not YamlFrontMatterBlock).Select(x => x.Span);
        var inlines = document.Descendants<CodeInline>().Select(x => x.Span);
        return new CodeSpans(blocks.Concat(inlines).ToList());
    }

    /// <summary>
    /// True if the character at the given index is part of a code block or inline code.
    /// </summary>
    public bool Contains(int index) => _spans.Any(s => index >= s.Start && index <= s.End);
}
