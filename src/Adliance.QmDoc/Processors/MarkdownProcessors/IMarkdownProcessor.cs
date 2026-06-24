using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Adliance.AspNetCore.Buddy.Pdf;
using Markdig;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public interface IMarkdownProcessor
{
    MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext);
}

public class MarkdownProcessorContext
{
    public readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseYamlFrontMatter().UseAdvancedExtensions().Build();

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
    }

    public bool ContainsPlaceholderInSource(string placeholder)
    {
        return Regex.IsMatch(SourceMarkdown, @"\{\{\W*" + placeholder + @"\W*\}\}", RegexOptions.IgnoreCase);
    }

    public bool ContainsPlaceholderInSource(params string[] placeholder)
    {
        return placeholder.Any(ContainsPlaceholderInSource);
    }

    public void ReplacePlaceholder(string placeholder, string replaceWith)
    {
        Markdown = Regex.Replace(Markdown, @"\{\{\W*" + placeholder + @"\W*\}\}", replaceWith, RegexOptions.IgnoreCase);
    }
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
