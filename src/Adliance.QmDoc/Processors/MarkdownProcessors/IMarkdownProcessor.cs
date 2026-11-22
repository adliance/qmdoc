using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Adliance.AspNetCore.Buddy.Pdf;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public interface IMarkdownProcessor
{
    MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext);
}

public class MarkdownProcessorContext
{
    // keep non-ASCII letters in heading ids (e.g. "изхвърляне", "élimination"), like VS Code and GitHub do;
    // must be registered before UseAdvancedExtensions(), which would otherwise add the ASCII-only default
    public readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseYamlFrontMatter().UseAutoIdentifiers(AutoIdentifierOptions.AutoLink).UseAdvancedExtensions().Build();

    public MarkdownProcessorContext(string sourceMarkdown)
    {
        SourceMarkdown = sourceMarkdown;
        Markdown = sourceMarkdown;
        Frontmatter = FrontmatterParser.Parse(this);
        Markdown = Frontmatter.MarkdownWithoutFrontmatter;
    }

    /// <summary>
    /// Contains the full, unaltered Markdown string from the source file.
    /// </summary>
    public string SourceMarkdown { get; }

    public Frontmatter Frontmatter { get; }

    /// <summary>
    /// Contains the "working copy" of the Markdown as it walks through the different processors
    /// </summary>
    public string Markdown { get; set; }

    /// <summary>
    /// Contains the list of errors as we walk through the different processors.
    /// </summary>
    public List<ProcessorError> Errors { get; } = [];

    public List<LinkedDocument> LinkedDocuments { get; } = [];
    public PdfMetadata? PdfMetadata { get; private set; }
    public string? Theme { get; set; }

    public void ResetForSecondPass(PdfMetadata? pdfMetadata)
    {
        PdfMetadata = pdfMetadata;
        Markdown = Frontmatter.MarkdownWithoutFrontmatter;
        Errors.Clear();
        LinkedDocuments.Clear();
    }

    public bool ContainsPlaceholderInSource(string placeholder)
    {
        return Regex.IsMatch(SourceMarkdown, PlaceholderPattern(placeholder), RegexOptions.IgnoreCase);
    }

    public bool ContainsPlaceholderInSource(params string[] placeholder)
    {
        return placeholder.Any(ContainsPlaceholderInSource);
    }

    /// <summary>
    /// Whether the current Markdown (including the content of includes, or a template of the theme) contains one of the placeholders.
    /// </summary>
    public bool ContainsPlaceholder(params string[] placeholder)
    {
        return placeholder.Any(p => Regex.IsMatch(Markdown, PlaceholderPattern(p), RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// Set when a "{{ TOC }}" was replaced (also in included files), the PDF then needs a second pass for the page numbers.
    /// </summary>
    public bool ContainsToc { get; set; }

    /// <summary>
    /// Set while placeholders are replaced in a template of the theme (eg. the footer), which is HTML and not Markdown.
    /// </summary>
    public bool IsTemplate { get; set; }

    /// <summary>
    /// Replaces the placeholder everywhere except in code, so that the placeholder syntax can be shown in code examples (eg. `{{ TOC }}`).
    /// </summary>
    public void ReplacePlaceholder(string placeholder, string replaceWith)
    {
        var regex = new Regex(PlaceholderPattern(Regex.Escape(placeholder)), RegexOptions.IgnoreCase);
        if (!regex.IsMatch(Markdown)) return;

        var codeSpans = IsTemplate ? null : CodeSpans.Find(Markdown, Pipeline);
        // use a MatchEvaluator, so that "$0" or "$$" in the value are inserted as they are and not interpreted as substitutions
        Markdown = regex.Replace(Markdown, m => codeSpans?.Contains(m.Index) == true ? m.Value : replaceWith);
    }

    /// <summary>
    /// Matches "{{KEY}}", "{{ KEY }}" etc., but must neither reach into a surrounding placeholder ("{{ = 2 * {{ KEY }} }}")
    /// nor match a calculation of the same name ("{{ = KEY }}").
    /// </summary>
    private static string PlaceholderPattern(string placeholder) => @"\{\{[^\w{}=]*" + placeholder + @"[^\w{}]*?\}\}";
}

public class LinkedDocument(string fileName, string niceName)
{
    public string FileName { get; set; } = fileName;
    public string NiceName { get; set; } = niceName;

    public override bool Equals(object? obj)
    {
        if (obj is LinkedDocument o)
        {
            return (FileName + NiceName).Equals(o.FileName + o.NiceName, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    public override int GetHashCode()
    {
        // ReSharper disable NonReadonlyMemberInGetHashCode
        return (FileName + NiceName).ToLower().GetHashCode();
        // ReSharper restore NonReadonlyMemberInGetHashCode
    }
}
