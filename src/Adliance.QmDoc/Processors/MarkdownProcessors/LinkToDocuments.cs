using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

/// <summary>
/// Converts "[other-document.md]" and document codes like "[ABC-123]" into links to these documents.
/// Regular Markdown links "[other.md](other.md)", images "![chart.png](chart.png)", reference links "[text][other.md]",
/// link definitions "[other.md]: url" and code are left alone.
/// </summary>
public partial class LinkToDocuments(string baseDirectory, string filePath) : IMarkdownProcessor
{
    // the extension starts with a letter, so that "[1.2]" or "[see fig. 3.1]" aren't mistaken for file names
    [GeneratedRegex(@"(?<![!\]])\[([^\[\]]*?\.[A-Za-z]\w{0,3})\](?![(\[])")]
    private static partial Regex FileNameRegex();

    [GeneratedRegex(@"(?<![!\]])\[(\w\w\w\-\d\d\d)\](?![(\[])")]
    private static partial Regex DocumentCodeRegex();

    /// <summary>
    /// A link reference definition "[other.md]: url" (at the beginning of a line, indented by up to three spaces).
    /// </summary>
    [GeneratedRegex(@"(?:^|\n)[ ]{0,3}$")]
    private static partial Regex LineStartRegex();

    private static bool IsLinkReferenceDefinition(string markdown, Match m) =>
        m.Index + m.Length < markdown.Length && markdown[m.Index + m.Length] == ':' && LineStartRegex().IsMatch(markdown[Math.Max(0, m.Index - 4)..m.Index]);

    private static bool Skip(string markdown, Match m, CodeSpans codeSpans) => codeSpans.Contains(m.Index) || IsLinkReferenceDefinition(markdown, m);

    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        ReplaceDocumentsByFileExtension(markdownContext);
        ReplaceDocumentsByCode(markdownContext);
        return markdownContext;
    }

    private string DocumentDirectory => Path.GetDirectoryName(filePath)!;

    /// <summary>
    /// Folders that can't be read (eg. system folders next to the source directory) are skipped instead of aborting the conversion.
    /// </summary>
    private static readonly EnumerationOptions Recursive = new() { RecurseSubdirectories = true, IgnoreInaccessible = true };

    private static List<string> FindFiles(string directory, string pattern) =>
        Directory.Exists(directory) ? Directory.GetFiles(directory, pattern, Recursive).ToList() : [];

    private void ReplaceDocumentsByCode(MarkdownProcessorContext markdownContext)
    {
        var codeSpans = CodeSpans.Find(markdownContext.Markdown, markdownContext.Pipeline);
        List<string>? markdownFiles = null;
        List<string>? allFiles = null;

        var markdown = markdownContext.Markdown;
        markdownContext.Markdown = DocumentCodeRegex().Replace(markdown, m =>
        {
            if (Skip(markdown, m, codeSpans)) return m.Value;

            var code = m.Groups[1].Value;
            bool StartsWithCode(string x) => Path.GetFileName(x).StartsWith(code, true, CultureInfo.InvariantCulture);

            // we prefer *.md files ... but if we can't find a matching one, we just take whatever
            markdownFiles ??= FindFiles(baseDirectory, "*.md");
            var linkedFilePath = markdownFiles.FirstOrDefault(StartsWithCode);
            if (linkedFilePath == null)
            {
                allFiles ??= FindFiles(baseDirectory, "*.*");
                linkedFilePath = allFiles.FirstOrDefault(StartsWithCode);
            }

            if (linkedFilePath == null)
            {
                markdownContext.Errors.Add(new ProcessorError(filePath, $"Unable to find a document \"{code}\", but there's a referenced document number to it."));
                return m.Value;
            }

            return Link(markdownContext, linkedFilePath);
        });
    }

    private void ReplaceDocumentsByFileExtension(MarkdownProcessorContext markdownContext)
    {
        var codeSpans = CodeSpans.Find(markdownContext.Markdown, markdownContext.Pipeline);

        var markdown = markdownContext.Markdown;
        List<string>? filesAround = null;
        markdownContext.Markdown = FileNameRegex().Replace(markdown, m =>
        {
            if (Skip(markdown, m, codeSpans)) return m.Value;

            var linkedFileName = Uri.UnescapeDataString(m.Groups[1].Value);
            if (linkedFileName.Contains('@', StringComparison.OrdinalIgnoreCase)) return m.Value; // e-mail addresses

            var matchingFile = new FileInfo(Path.Combine(DocumentDirectory, linkedFileName));
            if (!matchingFile.Exists)
            {
                // look for the file in the folders around the source directory
                // (only once per document, this can be a large folder)
                filesAround ??= FindFiles(Path.GetDirectoryName(baseDirectory) ?? baseDirectory, "*.md");
                var matchingFilePath = filesAround
                    .FirstOrDefault(x => Path.GetFileName(x).Equals(linkedFileName, StringComparison.OrdinalIgnoreCase));
                if (matchingFilePath != null) matchingFile = new FileInfo(matchingFilePath);
            }

            if (!matchingFile.Exists)
            {
                markdownContext.Errors.Add(new ProcessorError(filePath, $"Unable to find a document \"{linkedFileName}\", but there's a reference to it."));
                return m.Value;
            }

            return Link(markdownContext, matchingFile.FullName);
        });
    }

    /// <summary>
    /// Returns the link to the document (relative to the current document, Markdown files are linked as the converted HTML/PDF file)
    /// and adds it to the linked documents.
    /// </summary>
    private string Link(MarkdownProcessorContext markdownContext, string linkedFilePath)
    {
        var relativeFilePath = Path.GetRelativePath(DocumentDirectory, linkedFilePath).Replace('\\', '/');
        if (Path.GetExtension(relativeFilePath).Equals(".md", StringComparison.OrdinalIgnoreCase)) relativeFilePath = Path.ChangeExtension(relativeFilePath, ".html");

        var linkedDocument = new LinkedDocument(relativeFilePath.Replace(" ", "%20"), Path.GetFileNameWithoutExtension(linkedFilePath));
        markdownContext.LinkedDocuments.Add(linkedDocument);
        return $"<span class=\"link-to-document\"><i></i>[{linkedDocument.NiceName}]({linkedDocument.FileName})</span>";
    }
}
